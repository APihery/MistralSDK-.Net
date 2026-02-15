using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using MistralSDK;
using MistralSDK.Abstractions;
using MistralSDK.ChatCompletion;
using MistralSDK.Configuration;
using MistralSDK.Exceptions;
using MistralSDK.Files;
using MistralSDK.Ocr;
using System;
using System.Collections.Generic;
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
                            Content = "Hello! How can I help you today?"
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

        #region Helper Methods

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

        #endregion
    }
}
