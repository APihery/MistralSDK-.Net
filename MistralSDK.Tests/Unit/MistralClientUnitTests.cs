using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using MistralSDK;
using MistralSDK.Abstractions;
using MistralSDK.Batch;
using MistralSDK.ChatCompletion;
using MistralSDK.Configuration;
using MistralSDK.Exceptions;
using MistralSDK.Agents;
using MistralSDK.Audio;
using MistralSDK.Classifiers;
using MistralSDK.Embeddings;
using MistralSDK.Files;
using MistralSDK.Fim;
using MistralSDK.FineTuning;
using MistralSDK.Models;
using MistralSDK.Ocr;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MistralSDK.Tests.Unit
{
    /// <summary>
    /// Unit tests for the MistralClient class using mocked HTTP responses.
    /// These tests do not make real API calls.
    /// </summary>
    [TestClass]
    public class MistralClientUnitTests
    {
        private Mock<HttpMessageHandler> _mockHttpHandler = null!;
        private HttpClient _httpClient = null!;
        private MistralClientOptions _options = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockHttpHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpHandler.Object)
            {
                BaseAddress = new Uri("https://api.mistral.ai/v1")
            };

            _options = new MistralClientOptions
            {
                ApiKey = TestConfiguration.GetTestApiKey(),
                BaseUrl = "https://api.mistral.ai/v1",
                ValidateRequests = true,
                ThrowOnError = false
            };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _httpClient?.Dispose();
        }

        #region Constructor Tests

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_NullApiKey_ThrowsArgumentException()
        {
            _ = new MistralClient((string)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_EmptyApiKey_ThrowsArgumentException()
        {
            _ = new MistralClient("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_WhitespaceApiKey_ThrowsArgumentException()
        {
            _ = new MistralClient("   ");
        }

        [TestMethod]
        public void Constructor_ValidApiKey_CreatesClient()
        {
            using var client = new MistralClient("valid-api-key");
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Constructor_WithOptions_CreatesClient()
        {
            var options = new MistralClientOptions
            {
                ApiKey = "test-api-key",
                TimeoutSeconds = 60
            };

            using var client = new MistralClient(options);
            Assert.IsNotNull(client);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            _ = new MistralClient((MistralClientOptions)null!);
        }

        #endregion

        #region Validation Tests

        [TestMethod]
        public void ValidateRequest_NullRequest_ReturnsFailure()
        {
            using var client = new MistralClient(_httpClient, _options);
            var result = client.ValidateRequest(null!);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Count > 0);
        }

        [TestMethod]
        public void ValidateRequest_EmptyModel_ReturnsFailure()
        {
            using var client = new MistralClient(_httpClient, _options);
            var request = new ChatCompletionRequest
            {
                Model = "",
                Messages = new List<MessageRequest>
                {
                    new MessageRequest { Role = MessageRoles.User, Content = "Test" }
                }
            };

            var result = client.ValidateRequest(request);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("Model")));
        }

        [TestMethod]
        public void ValidateRequest_EmptyMessages_ReturnsFailure()
        {
            using var client = new MistralClient(_httpClient, _options);
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest>()
            };

            var result = client.ValidateRequest(request);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("message")));
        }

        [TestMethod]
        public void ValidateRequest_InvalidTemperature_ReturnsFailure()
        {
            using var client = new MistralClient(_httpClient, _options);
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest>
                {
                    new MessageRequest { Role = MessageRoles.User, Content = "Test" }
                },
                Temperature = 3.0 // Invalid: > 2.0
            };

            var result = client.ValidateRequest(request);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("Temperature")));
        }

        [TestMethod]
        public void ValidateRequest_InvalidTopP_ReturnsFailure()
        {
            using var client = new MistralClient(_httpClient, _options);
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest>
                {
                    new MessageRequest { Role = MessageRoles.User, Content = "Test" }
                },
                TopP = 1.5 // Invalid: > 1.0
            };

            var result = client.ValidateRequest(request);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("TopP")));
        }

        [TestMethod]
        public void ValidateRequest_ValidRequest_ReturnsSuccess()
        {
            using var client = new MistralClient(_httpClient, _options);
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest>
                {
                    new MessageRequest { Role = MessageRoles.User, Content = "Test" }
                },
                Temperature = 0.7,
                TopP = 1.0
            };

            var result = client.ValidateRequest(request);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }

        #endregion

        #region ChatCompletion Tests

        [TestMethod]
        public async Task ChatCompletionAsync_SuccessfulResponse_ReturnsSuccess()
        {
            // Arrange
            var expectedResponse = new ChatCompletionResponse
            {
                Id = "test-id",
                Model = MistralModels.Small,
                Choices = new List<ChatCompletionChoice>
                {
                    new ChatCompletionChoice
                    {
                        Index = 0,
                        Message = new MessageResponse
                        {
                            Role = "assistant",
                            ContentRaw = "Hello! How can I help you today?"
                        },
                        FinishReason = "stop"
                    }
                },
                Usage = new UsageInfo
                {
                    PromptTokens = 10,
                    CompletionTokens = 8,
                    TotalTokens = 18
                }
            };

            SetupMockResponse(HttpStatusCode.OK, expectedResponse);

            using var client = new MistralClient(_httpClient, _options);
            var request = CreateValidRequest();

            // Act
            var response = await client.ChatCompletionAsync(request);

            // Assert
            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual(200, response.StatusCode);
            Assert.AreEqual("Hello! How can I help you today?", response.Message);
            Assert.AreEqual(MistralModels.Small, response.Model);
            Assert.IsNotNull(response.Usage);
            Assert.AreEqual(18, response.Usage.TotalTokens);
        }

        [TestMethod]
        public async Task ChatCompletionAsync_ValidationError_ReturnsError()
        {
            using var client = new MistralClient(_httpClient, _options);
            var request = new ChatCompletionRequest
            {
                Model = "", // Invalid
                Messages = new List<MessageRequest>()
            };

            // Act
            var response = await client.ChatCompletionAsync(request);

            // Assert
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(400, response.StatusCode);
            Assert.IsTrue(response.Message.Contains("Validation failed"));
        }

        [TestMethod]
        public async Task ChatCompletionAsync_ApiError401_ReturnsUnauthorized()
        {
            // Arrange
            var errorResponse = new ChatCompletionErrorModelResponse
            {
                Object = "error",
                Message = "Invalid API key",
                Type = "authentication_error"
            };

            SetupMockResponse(HttpStatusCode.Unauthorized, errorResponse);

            using var client = new MistralClient(_httpClient, _options);
            var request = CreateValidRequest();

            // Act
            var response = await client.ChatCompletionAsync(request);

            // Assert
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(401, response.StatusCode);
            Assert.IsTrue(response.Message.Contains("Invalid API key"));
        }

        [TestMethod]
        public async Task ChatCompletionAsync_ApiError400_ReturnsBadRequest()
        {
            // Arrange
            var errorResponse = new ChatCompletionErrorModelResponse
            {
                Object = "error",
                Message = "Invalid model specified",
                Type = "invalid_request_error"
            };

            SetupMockResponse(HttpStatusCode.BadRequest, errorResponse);

            using var client = new MistralClient(_httpClient, _options);
            var request = CreateValidRequest();

            // Act
            var response = await client.ChatCompletionAsync(request);

            // Assert
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(400, response.StatusCode);
        }

        [TestMethod]
        public async Task ChatCompletionAsync_ApiError429_ReturnsRateLimited()
        {
            // Arrange
            var errorResponse = new ChatCompletionErrorModelResponse
            {
                Object = "error",
                Message = "Rate limit exceeded",
                Type = "rate_limit_error"
            };

            SetupMockResponse(HttpStatusCode.TooManyRequests, errorResponse);

            using var client = new MistralClient(_httpClient, _options);
            var request = CreateValidRequest();

            // Act
            var response = await client.ChatCompletionAsync(request);

            // Assert
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(429, response.StatusCode);
        }

        [TestMethod]
        public async Task ChatCompletionAsync_ThrowOnError_ThrowsException()
        {
            // Arrange
            var errorResponse = new ChatCompletionErrorModelResponse
            {
                Object = "error",
                Message = "Invalid API key",
                Type = "authentication_error"
            };

            SetupMockResponse(HttpStatusCode.Unauthorized, errorResponse);

            _options.ThrowOnError = true;
            using var client = new MistralClient(_httpClient, _options);
            var request = CreateValidRequest();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<MistralApiException>(() => 
                client.ChatCompletionAsync(request));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ChatCompletionAsync_NullRequest_ThrowsArgumentNullException()
        {
            using var client = new MistralClient(_httpClient, _options);
            await client.ChatCompletionAsync(null!);
        }

        [TestMethod]
        public async Task ChatCompletionAsync_CancellationRequested_ThrowsOperationCanceledException()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Configure the mock to throw when cancellation is requested
            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new OperationCanceledException(cts.Token));

            using var client = new MistralClient(_httpClient, _options);
            var request = CreateValidRequest();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(() =>
                client.ChatCompletionAsync(request, cts.Token));
        }

        #endregion

        #region Files API Tests

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task FilesUploadAsync_NullStream_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            await client.FilesUploadAsync(null!, "test.pdf", "ocr");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task FilesUploadAsync_EmptyFileName_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            using var stream = new MemoryStream();
            await client.FilesUploadAsync(stream, "", "ocr");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task FilesUploadAsync_WhitespaceFileName_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            using var stream = new MemoryStream();
            await client.FilesUploadAsync(stream, "   ", "ocr");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task FilesUploadAsync_InvalidPurpose_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            using var stream = new MemoryStream();
            await client.FilesUploadAsync(stream, "test.pdf", "invalid");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task FilesRetrieveAsync_EmptyFileId_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            await client.FilesRetrieveAsync("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task FilesRetrieveAsync_WhitespaceFileId_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            await client.FilesRetrieveAsync("   ");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task FilesDeleteAsync_EmptyFileId_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            await client.FilesDeleteAsync("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task FilesDownloadAsync_EmptyFileId_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            await client.FilesDownloadAsync("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task FilesGetSignedUrlAsync_EmptyFileId_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            await client.FilesGetSignedUrlAsync("");
        }

        [TestMethod]
        public async Task FilesListAsync_Success_ReturnsList()
        {
            SetupMockResponse(HttpStatusCode.OK, new { data = Array.Empty<object>(), @object = "list", total = 0 });
            using var client = new MistralClient(_httpClient, _options);
            var result = await client.FilesListAsync();
            Assert.IsNotNull(result);
            Assert.AreEqual("list", result.Object);
        }

        [TestMethod]
        public async Task FilesRetrieveAsync_NotFound404_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"File not found","type":"invalid_request_error"}""";
            SetupRawMockResponse(HttpStatusCode.NotFound, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.FilesRetrieveAsync("file-nonexistent-123"));
        }

        [TestMethod]
        public async Task FilesDeleteAsync_NotFound404_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"File not found","type":"invalid_request_error"}""";
            SetupRawMockResponse(HttpStatusCode.NotFound, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.FilesDeleteAsync("file-nonexistent-123"));
        }

        [TestMethod]
        public async Task FilesDownloadAsync_NotFound404_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"File not found","type":"invalid_request_error"}""";
            SetupRawMockResponse(HttpStatusCode.NotFound, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.FilesDownloadAsync("file-nonexistent-123"));
        }

        [TestMethod]
        public async Task FilesGetSignedUrlAsync_NotFound404_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"File not found","type":"invalid_request_error"}""";
            SetupRawMockResponse(HttpStatusCode.NotFound, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.FilesGetSignedUrlAsync("file-nonexistent-123"));
        }

        [TestMethod]
        public async Task FilesListAsync_Unauthorized401_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"Invalid API key","type":"authentication_error"}""";
            SetupRawMockResponse(HttpStatusCode.Unauthorized, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.FilesListAsync());
        }

        [TestMethod]
        public async Task FilesListAsync_InternalServerError500_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"Internal server error","type":"server_error"}""";
            SetupRawMockResponse(HttpStatusCode.InternalServerError, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.FilesListAsync());
        }

        [TestMethod]
        public async Task FilesRetrieveAsync_Unauthorized401_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"Invalid API key","type":"authentication_error"}""";
            SetupRawMockResponse(HttpStatusCode.Unauthorized, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.FilesRetrieveAsync("file-123"));
        }

        [TestMethod]
        public async Task FilesRetrieveAsync_InternalServerError500_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"Internal server error","type":"server_error"}""";
            SetupRawMockResponse(HttpStatusCode.InternalServerError, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.FilesRetrieveAsync("file-123"));
        }

        [TestMethod]
        public async Task FilesUploadAsync_BadRequest400_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"Unsupported file type","type":"invalid_request_error"}""";
            SetupRawMockResponse(HttpStatusCode.BadRequest, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            using var stream = new MemoryStream(new byte[100]);
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.FilesUploadAsync(stream, "test.xyz", "ocr"));
        }

        [TestMethod]
        public async Task FilesUploadAsync_Unauthorized401_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"Invalid API key","type":"authentication_error"}""";
            SetupRawMockResponse(HttpStatusCode.Unauthorized, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            using var stream = new MemoryStream(new byte[100]);
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.FilesUploadAsync(stream, "test.pdf", "ocr"));
        }

        #endregion

        #region OCR API Tests

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task OcrProcessAsync_NullRequest_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            await client.OcrProcessAsync(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task OcrProcessAsync_NullDocument_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            await client.OcrProcessAsync(new OcrRequest { Document = null! });
        }

        [TestMethod]
        public async Task OcrProcessAsync_Success_ReturnsResponse()
        {
            var ocrResponse = new { pages = new[] { new { index = 1, markdown = "Hello" } }, model = "mistral-ocr-latest" };
            SetupMockResponse(HttpStatusCode.OK, ocrResponse);
            using var client = new MistralClient(_httpClient, _options);
            var request = new OcrRequest
            {
                Document = OcrDocument.FromImageUrl("https://example.com/image.jpg"),
                Model = OcrModels.MistralOcrLatest
            };
            var result = await client.OcrProcessAsync(request);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Pages.Count);
            Assert.AreEqual("Hello", result.Pages[0].Markdown);
        }

        [TestMethod]
        public async Task OcrProcessAsync_InvalidUrl400_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":{"detail":[{"msg":"Invalid document URL"}]},"type":"invalid_request_error"}""";
            SetupRawMockResponse(HttpStatusCode.BadRequest, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            var request = new OcrRequest
            {
                Document = OcrDocument.FromDocumentUrl("https://invalid-url-that-fails.com/doc.pdf"),
                Model = OcrModels.MistralOcrLatest
            };
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.OcrProcessAsync(request));
        }

        [TestMethod]
        public async Task OcrProcessAsync_InvalidFileId404_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"File not found","type":"invalid_request_error"}""";
            SetupRawMockResponse(HttpStatusCode.NotFound, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            var request = new OcrRequest
            {
                Document = OcrDocument.FromFileId("file-id-does-not-exist"),
                Model = OcrModels.MistralOcrLatest
            };
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.OcrProcessAsync(request));
        }

        [TestMethod]
        public async Task OcrProcessAsync_Unauthorized401_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"Invalid API key","type":"authentication_error"}""";
            SetupRawMockResponse(HttpStatusCode.Unauthorized, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            var request = new OcrRequest
            {
                Document = OcrDocument.FromImageUrl("https://example.com/image.jpg"),
                Model = OcrModels.MistralOcrLatest
            };
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.OcrProcessAsync(request));
        }

        [TestMethod]
        public async Task OcrProcessAsync_RateLimit429_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"Rate limit exceeded","type":"rate_limit_error"}""";
            SetupRawMockResponse(HttpStatusCode.TooManyRequests, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            var request = new OcrRequest
            {
                Document = OcrDocument.FromImageUrl("https://example.com/image.jpg"),
                Model = OcrModels.MistralOcrLatest
            };
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.OcrProcessAsync(request));
        }

        [TestMethod]
        public async Task OcrProcessAsync_InternalServerError500_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"Internal server error","type":"server_error"}""";
            SetupRawMockResponse(HttpStatusCode.InternalServerError, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            var request = new OcrRequest
            {
                Document = OcrDocument.FromImageUrl("https://example.com/image.jpg"),
                Model = OcrModels.MistralOcrLatest
            };
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.OcrProcessAsync(request));
        }

        [TestMethod]
        public async Task MistralApiException_ContainsStatusCodeAndMessage()
        {
            var errorJson = """{"object":"error","message":"File not found","type":"invalid_request_error"}""";
            SetupRawMockResponse(HttpStatusCode.NotFound, errorJson);
            using var client = new MistralClient(_httpClient, _options);

            var ex = await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.FilesRetrieveAsync("file-nonexistent"));

            Assert.AreEqual(HttpStatusCode.NotFound, ex.StatusCode);
            Assert.IsTrue(ex.Message.Contains("API error"));
            Assert.IsTrue(ex.Message.Contains(errorJson));
        }

        #endregion

        #region Audio API Tests

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AudioTranscribeAsync_NullRequest_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            await client.AudioTranscribeAsync(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AudioTranscribeAsync_NoInput_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            var request = new AudioTranscriptionRequest { Model = AudioModels.VoxtralMiniLatest };
            await client.AudioTranscribeAsync(request);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AudioTranscribeAsync_EmptyModel_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            var request = AudioTranscriptionRequestBuilder.FromFileUrl("https://example.com/audio.mp3", "");
            await client.AudioTranscribeAsync(request);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AudioTranscribeAsync_InvalidLanguage_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            var request = AudioTranscriptionRequestBuilder.FromFileUrl("https://example.com/audio.mp3");
            request.Language = "english"; // Must be 2 chars
            await client.AudioTranscribeAsync(request);
        }

        [TestMethod]
        public async Task AudioTranscribeAsync_FromFileUrl_Success_ReturnsResponse()
        {
            var response = new { model = "voxtral-mini-2507", text = "Transcribed text", language = "en", segments = Array.Empty<object>(), usage = new { prompt_tokens = 4, completion_tokens = 10, total_tokens = 14, prompt_audio_seconds = 5 } };
            SetupMockResponse(HttpStatusCode.OK, response);
            using var client = new MistralClient(_httpClient, _options);
            var request = AudioTranscriptionRequestBuilder.FromFileUrl("https://example.com/audio.mp3");
            var result = await client.AudioTranscribeAsync(request);
            Assert.IsNotNull(result);
            Assert.AreEqual("voxtral-mini-2507", result.Model);
            Assert.AreEqual("Transcribed text", result.Text);
            Assert.AreEqual("en", result.Language);
        }

        [TestMethod]
        public async Task AudioTranscribeAsync_FromFileId_Success_ReturnsResponse()
        {
            var response = new { model = "voxtral-mini-2507", text = "Transcribed", language = "en", segments = Array.Empty<object>(), usage = new { prompt_tokens = 1, completion_tokens = 5, total_tokens = 6 } };
            SetupMockResponse(HttpStatusCode.OK, response);
            using var client = new MistralClient(_httpClient, _options);
            var request = AudioTranscriptionRequestBuilder.FromFileId("file-123");
            var result = await client.AudioTranscribeAsync(request);
            Assert.IsNotNull(result);
            Assert.AreEqual("Transcribed", result.Text);
        }

        [TestMethod]
        public async Task AudioTranscribeAsync_FromStream_Success_ReturnsResponse()
        {
            var response = new { model = "voxtral-mini-2507", text = "Stream transcribed", language = "en", segments = Array.Empty<object>(), usage = new { prompt_tokens = 2, completion_tokens = 8, total_tokens = 10, prompt_audio_seconds = 3 } };
            SetupMockResponse(HttpStatusCode.OK, response);
            using var client = new MistralClient(_httpClient, _options);
            using var stream = new MemoryStream(new byte[500]);
            var request = AudioTranscriptionRequestBuilder.FromStream(stream, "audio.mp3");
            var result = await client.AudioTranscribeAsync(request);
            Assert.IsNotNull(result);
            Assert.AreEqual("Stream transcribed", result.Text);
        }

        [TestMethod]
        public async Task AudioTranscribeAsync_Unauthorized401_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"Invalid API key","type":"authentication_error"}""";
            SetupRawMockResponse(HttpStatusCode.Unauthorized, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            var request = AudioTranscriptionRequestBuilder.FromFileUrl("https://example.com/audio.mp3");
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.AudioTranscribeAsync(request));
        }

        [TestMethod]
        public async Task AudioTranscribeAsync_BadRequest400_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"Unsupported audio format","type":"invalid_request_error"}""";
            SetupRawMockResponse(HttpStatusCode.BadRequest, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            var request = AudioTranscriptionRequestBuilder.FromFileUrl("https://example.com/audio.xyz");
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.AudioTranscribeAsync(request));
        }

        [TestMethod]
        public async Task AudioTranscribeAsync_NotFound404_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"File not found","type":"invalid_request_error"}""";
            SetupRawMockResponse(HttpStatusCode.NotFound, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            var request = AudioTranscriptionRequestBuilder.FromFileId("file-nonexistent");
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.AudioTranscribeAsync(request));
        }

        [TestMethod]
        public async Task AudioTranscribeAsync_InternalServerError500_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"Internal server error","type":"server_error"}""";
            SetupRawMockResponse(HttpStatusCode.InternalServerError, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            var request = AudioTranscriptionRequestBuilder.FromFileUrl("https://example.com/audio.mp3");
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.AudioTranscribeAsync(request));
        }

        [TestMethod]
        public async Task AudioTranscribeAsync_RateLimit429_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"Rate limit exceeded","type":"rate_limit_error"}""";
            SetupRawMockResponse(HttpStatusCode.TooManyRequests, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            var request = AudioTranscriptionRequestBuilder.FromFileUrl("https://example.com/audio.mp3");
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.AudioTranscribeAsync(request));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AudioTranscribeStreamAsync_NullRequest_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            await foreach (var _ in client.AudioTranscribeStreamAsync(null!))
                break;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AudioTranscribeStreamAsync_NoInput_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            var request = new AudioTranscriptionRequest { Model = AudioModels.VoxtralMiniLatest };
            await foreach (var _ in client.AudioTranscribeStreamAsync(request))
                break;
        }

        [TestMethod]
        public async Task AudioTranscribeAsync_EmptyStream_SendsToApi_ReturnsError()
        {
            var errorJson = """{"object":"error","message":"Invalid or empty audio file","type":"invalid_request_error"}""";
            SetupRawMockResponse(HttpStatusCode.BadRequest, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            using var stream = new MemoryStream();
            var request = AudioTranscriptionRequestBuilder.FromStream(stream, "empty.mp3");
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.AudioTranscribeAsync(request));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AudioTranscribeAsync_LanguageThreeChars_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            var request = AudioTranscriptionRequestBuilder.FromFileUrl("https://example.com/a.mp3");
            request.Language = "eng";
            await client.AudioTranscribeAsync(request);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AudioTranscribeAsync_LanguageOneChar_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            var request = AudioTranscriptionRequestBuilder.FromFileUrl("https://example.com/a.mp3");
            request.Language = "e";
            await client.AudioTranscribeAsync(request);
        }

        [TestMethod]
        public async Task AudioTranscribeAsync_ServiceUnavailable503_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"Service unavailable","type":"server_error"}""";
            SetupRawMockResponse(HttpStatusCode.ServiceUnavailable, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            var request = AudioTranscriptionRequestBuilder.FromFileUrl("https://example.com/audio.mp3");
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.AudioTranscribeAsync(request));
        }

        [TestMethod]
        public async Task AudioTranscribeAsync_GatewayTimeout504_ThrowsMistralApiException()
        {
            var errorJson = """{"object":"error","message":"Gateway timeout","type":"timeout"}""";
            SetupRawMockResponse(HttpStatusCode.GatewayTimeout, errorJson);
            using var client = new MistralClient(_httpClient, _options);
            var request = AudioTranscriptionRequestBuilder.FromFileUrl("https://example.com/audio.mp3");
            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.AudioTranscribeAsync(request));
        }

        #region Agents API Tests

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AgentCompletionAsync_NullRequest_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            await client.AgentCompletionAsync(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AgentCompletionAsync_EmptyAgentId_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            var request = new AgentCompletionRequest
            {
                AgentId = "",
                Messages = new List<MessageRequest> { MessageRequest.User("Hello") }
            };
            await client.AgentCompletionAsync(request);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AgentCompletionAsync_NoMessages_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            var request = new AgentCompletionRequest
            {
                AgentId = "agent-123",
                Messages = new List<MessageRequest>()
            };
            await client.AgentCompletionAsync(request);
        }

        [TestMethod]
        public async Task AgentCompletionAsync_Success_ReturnsResponse()
        {
            var json = """{"id":"gen-1","object":"chat.completion","model":"mistral-medium-latest","created":1759500534,"choices":[{"index":0,"message":{"content":"Claude Monet.","role":"assistant","tool_calls":null,"prefix":false},"finish_reason":"stop"}],"usage":{"prompt_tokens":24,"completion_tokens":3,"total_tokens":27}}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);
            var request = new AgentCompletionRequest
            {
                AgentId = "agent-123",
                Messages = new List<MessageRequest> { MessageRequest.User("Who is the best French painter?") }
            };

            var result = await client.AgentCompletionAsync(request);

            Assert.IsTrue(result.IsSuccess);
            var data = result.GetData<ChatCompletionResponse>();
            Assert.IsNotNull(data);
            Assert.AreEqual("gen-1", data.Id);
            Assert.AreEqual("mistral-medium-latest", data.Model);
            Assert.IsTrue(data.GetFirstChoiceContent().Contains("Monet"));
        }

        #endregion

        #region Models API Tests

        [TestMethod]
        public async Task ModelsListAsync_Success_ReturnsModels()
        {
            var json = """{"object":"list","data":[{"id":"mistral-small","object":"model","type":"base","capabilities":{"completion_chat":true,"vision":false},"max_context_length":32768}]}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);

            var result = await client.ModelsListAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Data.Count);
            Assert.AreEqual("mistral-small", result.Data[0].Id);
            Assert.AreEqual(32768, result.Data[0].MaxContextLength);
        }

        [TestMethod]
        public async Task ModelsRetrieveAsync_Success_ReturnsModel()
        {
            var json = """{"id":"mistral-small-latest","object":"model","type":"base","capabilities":{"completion_chat":true},"max_context_length":32768}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);

            var result = await client.ModelsRetrieveAsync("mistral-small-latest");

            Assert.IsNotNull(result);
            Assert.AreEqual("mistral-small-latest", result.Id);
            Assert.AreEqual(32768, result.MaxContextLength);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task ModelsRetrieveAsync_EmptyModelId_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            await client.ModelsRetrieveAsync("");
        }

        [TestMethod]
        public async Task ModelsDeleteAsync_Success_ReturnsDeleted()
        {
            var json = """{"id":"ft:model-123","object":"model","deleted":true}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);

            var result = await client.ModelsDeleteAsync("ft:model-123");

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Deleted);
            Assert.AreEqual("ft:model-123", result.Id);
        }

        [TestMethod]
        public async Task ModelsArchiveAsync_Success_ReturnsArchived()
        {
            var json = """{"id":"ft:model-123","object":"model","archived":true}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);

            var result = await client.ModelsArchiveAsync("ft:model-123");

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Archived);
        }

        [TestMethod]
        public async Task ModelsUnarchiveAsync_Success_ReturnsUnarchived()
        {
            var json = """{"id":"ft:model-123","object":"model","archived":false}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);

            var result = await client.ModelsUnarchiveAsync("ft:model-123");

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Archived);
        }

        [TestMethod]
        public async Task ModelsUpdateAsync_Success_ReturnsModel()
        {
            var json = """{"id":"ft:model-123","object":"model","name":"My Model","description":"Custom"}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);
            var updateRequest = new UpdateFTModelRequest { Name = "My Model", Description = "Custom" };

            var result = await client.ModelsUpdateAsync("ft:model-123", updateRequest);

            Assert.IsNotNull(result);
            Assert.AreEqual("My Model", result.Name);
        }

        #endregion

        #region Embeddings API Tests

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task EmbeddingsCreateAsync_NullRequest_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            await client.EmbeddingsCreateAsync((EmbeddingRequest)null!);
        }

        [TestMethod]
        public async Task EmbeddingsCreateAsync_Success_ReturnsEmbeddings()
        {
            var json = """{"id":"emb-1","object":"list","model":"mistral-embed","data":[{"object":"embedding","embedding":[0.1,0.2,0.3],"index":0},{"object":"embedding","embedding":[0.4,0.5,0.6],"index":1}],"usage":{"prompt_tokens":5,"completion_tokens":0,"total_tokens":5}}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);
            var request = new EmbeddingRequest
            {
                Model = EmbeddingModels.MistralEmbed,
                Input = new[] { "Hello", "World" }
            };

            var result = await client.EmbeddingsCreateAsync(request);

            Assert.IsTrue(result.IsSuccess);
            var data = result.GetData<EmbeddingResponse>();
            Assert.IsNotNull(data);
            Assert.AreEqual(2, data.Data.Count);
            Assert.AreEqual(3, data.Data[0].Embedding.Count);
            Assert.AreEqual(0.1, data.Data[0].Embedding[0]);
        }

        #endregion

        #region Classifiers API Tests

        [TestMethod]
        public async Task ModerationsCreateAsync_Success_ReturnsResults()
        {
            var json = """{"id":"mod-1","model":"mistral-moderation-latest","results":[{"categories":{"sexual":false,"hate_and_discrimination":false},"category_scores":{"sexual":0.001,"hate_and_discrimination":0.002}}]}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);
            var request = new ModerationRequest
            {
                Model = ModerationModels.MistralModerationLatest,
                Input = "Text to moderate"
            };

            var result = await client.ModerationsCreateAsync(request);

            Assert.IsTrue(result.IsSuccess);
            var data = result.GetData<ModerationResponse>();
            Assert.IsNotNull(data);
            Assert.AreEqual(1, data.Results.Count);
            Assert.IsFalse(data.Results[0].Categories["sexual"]);
        }

        [TestMethod]
        public async Task ClassificationsCreateAsync_Success_ReturnsResults()
        {
            var json = """{"id":"cls-1","model":"classifier-1","results":[{"target1":{"scores":{"label_a":0.9,"label_b":0.1}}}]}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);
            var request = new ModerationRequest
            {
                Model = "classifier-1",
                Input = "Text to classify"
            };

            var result = await client.ClassificationsCreateAsync(request);

            Assert.IsTrue(result.IsSuccess);
            var data = result.GetData<ClassificationResponse>();
            Assert.IsNotNull(data);
            Assert.AreEqual(1, data.Results.Count);
        }

        #endregion

        #region FIM API Tests

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task FimCompletionAsync_NullRequest_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            await client.FimCompletionAsync((FimCompletionRequest)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task FimCompletionAsync_EmptyModel_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            var request = new FimCompletionRequest { Model = "", Prompt = "def foo(): " };
            await client.FimCompletionAsync(request);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task FimCompletionAsync_EmptyPrompt_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            var request = new FimCompletionRequest { Model = FimModels.CodestralLatest, Prompt = "" };
            await client.FimCompletionAsync(request);
        }

        [TestMethod]
        public async Task FimCompletionAsync_Success_ReturnsResponse()
        {
            var json = """{"id":"fim-1","object":"chat.completion","model":"codestral-latest","created":1759500534,"choices":[{"index":0,"message":{"content":"fibonacci(n-1) + fibonacci(n-2)","role":"assistant","tool_calls":null,"prefix":false},"finish_reason":"stop"}],"usage":{"prompt_tokens":15,"completion_tokens":8,"total_tokens":23}}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);
            var request = new FimCompletionRequest
            {
                Model = FimModels.CodestralLatest,
                Prompt = "def fibonacci(n):\n    if n <= 1:\n        return n\n    return "
            };

            var result = await client.FimCompletionAsync(request);

            Assert.IsTrue(result.IsSuccess);
            var data = result.GetData<ChatCompletionResponse>();
            Assert.IsNotNull(data);
            Assert.AreEqual("fim-1", data.Id);
            Assert.AreEqual("codestral-latest", data.Model);
            Assert.IsTrue(data.GetFirstChoiceContent().Contains("fibonacci"));
        }

        #endregion

        #region Batch API Tests

        [TestMethod]
        public async Task BatchJobsListAsync_Success_ReturnsJobs()
        {
            var json = """{"object":"list","data":[{"id":"batch-1","object":"batch","status":"SUCCESS","input_files":["file-1"],"endpoint":"/v1/chat/completions","model":"mistral-small-latest","total_requests":10,"completed_requests":10,"succeeded_requests":10,"failed_requests":0,"created_at":1759500000}],"total":1}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);

            var result = await client.BatchJobsListAsync(limit: 20);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Data.Count);
            Assert.AreEqual("batch-1", result.Data[0].Id);
            Assert.AreEqual("SUCCESS", result.Data[0].Status);
            Assert.AreEqual(10, result.Data[0].TotalRequests);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task BatchJobCreateAsync_NullRequest_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            await client.BatchJobCreateAsync(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task BatchJobCreateAsync_NoInputFilesOrRequests_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            var request = new BatchJobCreateRequest
            {
                Endpoint = ApiEndpoints.ChatCompletions,
                InputFiles = null,
                Requests = null
            };
            await client.BatchJobCreateAsync(request);
        }

        [TestMethod]
        public async Task BatchJobCreateAsync_WithInputFiles_Success()
        {
            var json = """{"id":"batch-new","object":"batch","status":"QUEUED","input_files":["file-123"],"endpoint":"/v1/chat/completions","model":"mistral-small-latest","total_requests":5,"completed_requests":0,"created_at":1759500000}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);
            var request = new BatchJobCreateRequest
            {
                Endpoint = ApiEndpoints.ChatCompletions,
                InputFiles = new List<string> { "file-123" },
                Model = "mistral-small-latest"
            };

            var result = await client.BatchJobCreateAsync(request);

            Assert.IsNotNull(result);
            Assert.AreEqual("batch-new", result.Id);
            Assert.AreEqual("QUEUED", result.Status);
        }

        [TestMethod]
        public async Task BatchJobGetAsync_Success_ReturnsJob()
        {
            var json = """{"id":"batch-1","object":"batch","status":"RUNNING","input_files":["file-1"],"endpoint":"/v1/chat/completions","total_requests":10,"completed_requests":5,"created_at":1759500000}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);

            var result = await client.BatchJobGetAsync("batch-1");

            Assert.IsNotNull(result);
            Assert.AreEqual("batch-1", result.Id);
            Assert.AreEqual("RUNNING", result.Status);
        }

        [TestMethod]
        public async Task BatchJobCancelAsync_Success_ReturnsJob()
        {
            var json = """{"id":"batch-1","object":"batch","status":"CANCELLATION_REQUESTED","input_files":["file-1"],"endpoint":"/v1/chat/completions","total_requests":10,"completed_requests":3,"created_at":1759500000}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);

            var result = await client.BatchJobCancelAsync("batch-1");

            Assert.IsNotNull(result);
            Assert.AreEqual("CANCELLATION_REQUESTED", result.Status);
        }

        #endregion

        #region Fine-Tuning API Tests

        [TestMethod]
        public async Task FineTuningJobsListAsync_Success_ReturnsJobs()
        {
            var json = """{"object":"list","data":[{"id":"ft-job-1","object":"job","status":"SUCCESS","model":"open-mistral-7b","created_at":1759500000,"modified_at":1759503600,"training_files":["file-1"],"fine_tuned_model":"ft:open-mistral-7b:my-suffix"}],"total":1}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);

            var result = await client.FineTuningJobsListAsync(limit: 20);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Data.Count);
            Assert.AreEqual("ft-job-1", result.Data[0].Id);
            Assert.AreEqual("SUCCESS", result.Data[0].Status);
            Assert.AreEqual("ft:open-mistral-7b:my-suffix", result.Data[0].FineTunedModel);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task FineTuningJobCreateAsync_NullRequest_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            await client.FineTuningJobCreateAsync(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task FineTuningJobCreateAsync_EmptyModel_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            var request = new FineTuningJobCreateRequest
            {
                Model = "",
                TrainingFiles = new List<TrainingFile> { new TrainingFile { FileId = "file-1" } },
                Hyperparameters = new CompletionTrainingParameters()
            };
            await client.FineTuningJobCreateAsync(request);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task FineTuningJobCreateAsync_NoTrainingFiles_Throws()
        {
            using var client = new MistralClient(_httpClient, _options);
            var request = new FineTuningJobCreateRequest
            {
                Model = FineTuneableModels.OpenMistral7B,
                TrainingFiles = new List<TrainingFile>(),
                Hyperparameters = new CompletionTrainingParameters()
            };
            await client.FineTuningJobCreateAsync(request);
        }

        [TestMethod]
        public async Task FineTuningJobCreateAsync_Success_ReturnsJob()
        {
            var json = """{"id":"ft-job-new","object":"job","status":"QUEUED","model":"open-mistral-7b","created_at":1759500000,"modified_at":1759500000,"training_files":["file-123"],"auto_start":true,"job_type":"completion"}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);
            var request = new FineTuningJobCreateRequest
            {
                Model = FineTuneableModels.OpenMistral7B,
                TrainingFiles = new List<TrainingFile> { new TrainingFile { FileId = "file-123" } },
                Hyperparameters = new CompletionTrainingParameters { TrainingSteps = 100 },
                AutoStart = true
            };

            var result = await client.FineTuningJobCreateAsync(request);

            Assert.IsNotNull(result);
            Assert.AreEqual("ft-job-new", result.Id);
            Assert.AreEqual("QUEUED", result.Status);
        }

        [TestMethod]
        public async Task FineTuningJobGetAsync_Success_ReturnsJob()
        {
            var json = """{"id":"ft-job-1","object":"job","status":"RUNNING","model":"open-mistral-7b","created_at":1759500000,"modified_at":1759501000,"training_files":["file-1"],"job_type":"completion"}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);

            var result = await client.FineTuningJobGetAsync("ft-job-1");

            Assert.IsNotNull(result);
            Assert.AreEqual("ft-job-1", result.Id);
            Assert.AreEqual("RUNNING", result.Status);
        }

        [TestMethod]
        public async Task FineTuningJobCancelAsync_Success_ReturnsJob()
        {
            var json = """{"id":"ft-job-1","object":"job","status":"CANCELLATION_REQUESTED","model":"open-mistral-7b","created_at":1759500000,"modified_at":1759502000,"training_files":["file-1"],"job_type":"completion"}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);

            var result = await client.FineTuningJobCancelAsync("ft-job-1");

            Assert.IsNotNull(result);
            Assert.AreEqual("CANCELLATION_REQUESTED", result.Status);
        }

        [TestMethod]
        public async Task FineTuningJobStartAsync_Success_ReturnsJob()
        {
            var json = """{"id":"ft-job-1","object":"job","status":"STARTED","model":"open-mistral-7b","created_at":1759500000,"modified_at":1759503000,"training_files":["file-1"],"job_type":"completion"}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);

            var result = await client.FineTuningJobStartAsync("ft-job-1");

            Assert.IsNotNull(result);
            Assert.AreEqual("STARTED", result.Status);
        }

        #endregion

        #region Convenience overloads and helpers

        [TestMethod]
        public async Task ChatCompletionAsync_SimpleOverload_Success()
        {
            var json = """{"id":"gen-1","object":"chat.completion","model":"mistral-small-latest","created":1759500534,"choices":[{"index":0,"message":{"content":"Hello!","role":"assistant","tool_calls":null,"prefix":false},"finish_reason":"stop"}],"usage":{"prompt_tokens":5,"completion_tokens":2,"total_tokens":7}}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);

            var result = await client.ChatCompletionAsync("Hello");

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("Hello"));
        }

        [TestMethod]
        public async Task EmbeddingsCreateAsync_StringOverload_Success()
        {
            var json = """{"id":"emb-1","object":"list","model":"mistral-embed","data":[{"object":"embedding","embedding":[0.1,0.2],"index":0}],"usage":{"prompt_tokens":3,"completion_tokens":0,"total_tokens":3}}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);

            var result = await client.EmbeddingsCreateAsync("Hello");

            Assert.IsTrue(result.IsSuccess);
            var data = result.GetData<EmbeddingResponse>();
            Assert.IsNotNull(data);
            Assert.AreEqual(2, data.GetFirstVector().Count);
        }

        [TestMethod]
        public async Task ModerateAsync_SimpleOverload_Success()
        {
            var json = """{"id":"mod-1","model":"mistral-moderation-latest","results":[{"categories":{"sexual":false,"hate_and_discrimination":false},"category_scores":{"sexual":0.001,"hate_and_discrimination":0.002}}]}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);

            var result = await client.ModerateAsync("Safe text");

            Assert.IsTrue(result.IsSuccess);
            var data = result.GetData<ModerationResponse>();
            Assert.IsNotNull(data);
            Assert.AreEqual(1, data.Results.Count);
        }

        [TestMethod]
        public async Task FimCompletionAsync_SimpleOverload_Success()
        {
            var json = """{"id":"fim-1","object":"chat.completion","model":"codestral-latest","created":1759500534,"choices":[{"index":0,"message":{"content":"return n","role":"assistant","tool_calls":null,"prefix":false},"finish_reason":"stop"}],"usage":{"prompt_tokens":10,"completion_tokens":2,"total_tokens":12}}""";
            SetupRawMockResponse(HttpStatusCode.OK, json);
            using var client = new MistralClient(_httpClient, _options);

            var result = await client.FimCompletionAsync("def foo(): ");

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("return"));
        }

        [TestMethod]
        public async Task FilesUploadAsync_FilePurposeTypeOverload_Success()
        {
            SetupRawMockResponse(HttpStatusCode.OK, """{"id":"file-123","object":"file","bytes":100,"created_at":0,"filename":"test.pdf","purpose":"ocr"}""");
            using var client = new MistralClient(_httpClient, _options);
            using var stream = new MemoryStream(new byte[100]);

            var result = await client.FilesUploadAsync(stream, "test.pdf", FilePurposeType.Ocr);

            Assert.IsNotNull(result);
            Assert.AreEqual("file-123", result.Id);
            Assert.AreEqual("ocr", result.Purpose);
        }

        [TestMethod]
        public void BatchRequest_ForChat_CreatesValidRequest()
        {
            var req = BatchRequest.ForChat("Hello world", maxTokens: 50, customId: "custom-1");

            Assert.AreEqual("custom-1", req.CustomId);
            Assert.IsTrue(req.Body.ContainsKey("max_tokens"));
            Assert.AreEqual(50, Convert.ToInt32(req.Body["max_tokens"]));
            Assert.IsTrue(req.Body.ContainsKey("messages"));
        }

        [TestMethod]
        public void TrainingFile_From_CreatesValidFile()
        {
            var tf = TrainingFile.From("file-123", 0.5);

            Assert.AreEqual("file-123", tf.FileId);
            Assert.AreEqual(0.5, tf.Weight);
        }

        [TestMethod]
        public void BatchJobResponse_IsComplete_TrueWhenTerminal()
        {
            var job = new BatchJobResponse { Status = BatchJobStatus.Success };
            Assert.IsTrue(job.IsComplete);

            job.Status = BatchJobStatus.Running;
            Assert.IsFalse(job.IsComplete);
        }

        [TestMethod]
        public void FineTuningJobResponse_IsComplete_TrueWhenTerminal()
        {
            var job = new FineTuningJobResponse { Status = FineTuningJobStatus.Success };
            Assert.IsTrue(job.IsComplete);

            job.Status = FineTuningJobStatus.Running;
            Assert.IsFalse(job.IsComplete);
        }

        [TestMethod]
        public void EmbeddingResponse_GetFirstVector_ReturnsVectorOrEmpty()
        {
            var resp = new EmbeddingResponse
            {
                Data = new List<EmbeddingData> { new EmbeddingData { Embedding = new List<double> { 0.1, 0.2, 0.3 } } }
            };
            var v = resp.GetFirstVector();
            Assert.AreEqual(3, v.Count);
            Assert.AreEqual(0.1, v[0]);

            var empty = new EmbeddingResponse { Data = new List<EmbeddingData>() };
            Assert.AreEqual(0, empty.GetFirstVector().Count);
        }

        #endregion

        #region Pen-test / Adversarial tests

        [TestMethod]
        public async Task ChatCompletionAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            var options = new MistralClientOptions
            {
                ApiKey = "test-key",
                BaseUrl = "https://api.mistral.ai/v1",
                ValidateRequests = false
            };
            var client = new MistralClient(options);
            client.Dispose();

            await Assert.ThrowsExceptionAsync<ObjectDisposedException>(() =>
                client.ChatCompletionAsync(CreateValidRequest()));
        }

        [TestMethod]
        public async Task FilesUploadAsync_MalformedJson200_ThrowsJsonException()
        {
            SetupRawMockResponse(HttpStatusCode.OK, "{invalid json");
            using var client = new MistralClient(_httpClient, _options);
            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

            await Assert.ThrowsExceptionAsync<JsonException>(() =>
                client.FilesUploadAsync(stream, "test.pdf", "ocr"));
        }

        [TestMethod]
        public async Task OcrProcessAsync_MalformedJson200_ThrowsJsonException()
        {
            SetupRawMockResponse(HttpStatusCode.OK, "not valid json at all");
            using var client = new MistralClient(_httpClient, _options);
            var request = new OcrRequest
            {
                Document = OcrDocument.FromImageUrl("https://example.com/image.jpg"),
                Model = OcrModels.MistralOcrLatest
            };

            await Assert.ThrowsExceptionAsync<JsonException>(() =>
                client.OcrProcessAsync(request));
        }

        [TestMethod]
        public async Task FilesUploadAsync_StreamThrowsOnRead_PropagatesException()
        {
            var throwingStream = new ThrowingOnReadStream(new byte[] { 1, 2, 3 });
            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (req, ct) =>
                {
                    if (req.Content != null)
                        await req.Content.ReadAsByteArrayAsync(ct);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("""{"id":"file-123","object":"file","bytes":3,"created_at":0,"filename":"test.pdf","purpose":"ocr"}""", System.Text.Encoding.UTF8, "application/json")
                    };
                });
            using var client = new MistralClient(_httpClient, _options);

            var ex = await Assert.ThrowsExceptionAsync<HttpRequestException>(() =>
                client.FilesUploadAsync(throwingStream, "test.pdf", "ocr"));
            Assert.IsNotNull(ex.InnerException);
            Assert.IsInstanceOfType(ex.InnerException, typeof(IOException));
        }

        [TestMethod]
        public async Task FilesRetrieveAsync_FileIdWithPathTraversal_EncodedAndSent()
        {
            var pathTraversalId = "file-../../../etc/passwd";
            SetupRawMockResponse(HttpStatusCode.NotFound, """{"object":"error","message":"File not found","type":"invalid_request_error"}""");
            using var client = new MistralClient(_httpClient, _options);

            await Assert.ThrowsExceptionAsync<MistralApiException>(() =>
                client.FilesRetrieveAsync(pathTraversalId));
        }

        [TestMethod]
        public async Task AudioTranscribeAsync_MalformedJson200_ThrowsJsonException()
        {
            SetupRawMockResponse(HttpStatusCode.OK, """{"model":"voxtral","text":""");
            using var client = new MistralClient(_httpClient, _options);
            var request = AudioTranscriptionRequestBuilder.FromFileUrl("https://example.com/audio.mp3");

            await Assert.ThrowsExceptionAsync<JsonException>(() =>
                client.AudioTranscribeAsync(request));
        }

        private ChatCompletionRequest CreateValidRequest()
        {
            return new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest>
                {
                    new MessageRequest
                    {
                        Role = MessageRoles.User,
                        Content = "Hello!"
                    }
                },
                Temperature = 0.7,
                MaxTokens = 100
            };
        }

        private void SetupMockResponse<T>(HttpStatusCode statusCode, T responseBody)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
            var json = JsonSerializer.Serialize(responseBody, jsonOptions);

            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };

            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        private void SetupRawMockResponse(HttpStatusCode statusCode, string jsonBody)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json")
            };

            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        private sealed class ThrowingOnReadStream : MemoryStream
        {
            public ThrowingOnReadStream(byte[] buffer) : base(buffer) { }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new IOException("Simulated read failure");
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                throw new IOException("Simulated async read failure");
            }
        }

        #endregion

        #endregion
    }
}
