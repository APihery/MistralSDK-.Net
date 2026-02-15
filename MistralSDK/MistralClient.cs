using MistralSDK.Abstractions;
using MistralSDK.ChatCompletion;
using MistralSDK.Configuration;
using MistralSDK.Exceptions;
using MistralSDK.Files;
using MistralSDK.Ocr;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MistralSDK
{
    /// <summary>
    /// Client for interacting with the Mistral AI API.
    /// Provides methods to send chat completion requests and handle responses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For production use, it is recommended to use <see cref="IHttpClientFactory"/> to manage
    /// the lifecycle of HttpClient instances. This prevents socket exhaustion issues.
    /// </para>
    /// <para>
    /// Use the <see cref="MistralClientOptions"/> class to configure the client behavior,
    /// including timeouts, retries, and caching.
    /// </para>
    /// </remarks>
    public class MistralClient : IMistralClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly MistralClientOptions _options;
        private readonly bool _ownsHttpClient;
        private bool _disposed = false;

        /// <summary>
        /// The default base URL for the Mistral AI API.
        /// </summary>
        private const string DefaultBaseUrl = "https://api.mistral.ai/v1";

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MistralClient"/> class with an API key.
        /// </summary>
        /// <param name="apiKey">The API key for authentication with Mistral AI.</param>
        /// <exception cref="ArgumentException">Thrown when the API key is null, empty, or whitespace.</exception>
        /// <remarks>
        /// This constructor creates its own HttpClient instance. For better resource management
        /// in production applications, consider using the constructor that accepts an HttpClient
        /// from IHttpClientFactory.
        /// </remarks>
        public MistralClient(string apiKey) : this(new MistralClientOptions { ApiKey = apiKey })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MistralClient"/> class with options.
        /// </summary>
        /// <param name="options">The configuration options for the client.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the options are invalid.</exception>
        public MistralClient(MistralClientOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.Validate();
            _options = options;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_options.BaseUrl),
                Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds)
            };
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);
            
            _ownsHttpClient = true;
            _jsonOptions = CreateJsonOptions();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MistralClient"/> class with options (IOptions pattern).
        /// </summary>
        /// <param name="options">The configuration options wrapper.</param>
        public MistralClient(IOptions<MistralClientOptions> options) : this(options?.Value ?? throw new ArgumentNullException(nameof(options)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MistralClient"/> class with a pre-configured HttpClient.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance to use (typically from IHttpClientFactory).</param>
        /// <param name="options">The configuration options for the client.</param>
        /// <exception cref="ArgumentNullException">Thrown when httpClient or options is null.</exception>
        /// <remarks>
        /// Use this constructor with IHttpClientFactory for production applications.
        /// The HttpClient should be configured with the appropriate base address and authorization header.
        /// </remarks>
        public MistralClient(HttpClient httpClient, MistralClientOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            
            options.Validate();
            _options = options;
            _ownsHttpClient = false; // HttpClient is managed externally (by IHttpClientFactory)
            _jsonOptions = CreateJsonOptions();

            // Ensure the HttpClient has the correct base address if not already set
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            }

            // Ensure authorization header is set
            if (_httpClient.DefaultRequestHeaders.Authorization == null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MistralClient"/> class with a pre-configured HttpClient (IOptions pattern).
        /// </summary>
        /// <param name="httpClient">The HttpClient instance to use.</param>
        /// <param name="options">The configuration options wrapper.</param>
        public MistralClient(HttpClient httpClient, IOptions<MistralClientOptions> options) 
            : this(httpClient, options?.Value ?? throw new ArgumentNullException(nameof(options)))
        {
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Creates the JSON serializer options used for API communication.
        /// </summary>
        private static JsonSerializerOptions CreateJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Validates a chat completion request without sending it to the API.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <returns>A validation result indicating whether the request is valid.</returns>
        public ValidationResult ValidateRequest(ChatCompletionRequest request)
        {
            if (request == null)
            {
                return ValidationResult.Failure("Request cannot be null.");
            }

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Model))
            {
                errors.Add("Model is required.");
            }

            if (request.Messages == null || request.Messages.Count == 0)
            {
                errors.Add("At least one message is required.");
            }
            else
            {
                for (int i = 0; i < request.Messages.Count; i++)
                {
                    var msg = request.Messages[i];
                    if (string.IsNullOrWhiteSpace(msg.Role))
                    {
                        errors.Add($"Message at index {i}: Role is required.");
                    }
                    else if (!msg.IsValid())
                    {
                        errors.Add($"Message at index {i}: Invalid message. Check role and content.");
                    }
                }
            }

            if (request.Temperature.HasValue && (request.Temperature.Value < 0.0 || request.Temperature.Value > 2.0))
            {
                errors.Add($"Temperature must be between 0.0 and 2.0. Got: {request.Temperature}");
            }

            if (request.TopP.HasValue && (request.TopP.Value < 0.0 || request.TopP.Value > 1.0))
            {
                errors.Add($"TopP must be between 0.0 and 1.0. Got: {request.TopP}");
            }

            if (request.MaxTokens.HasValue && request.MaxTokens.Value <= 0)
            {
                errors.Add($"MaxTokens must be greater than 0. Got: {request.MaxTokens}");
            }

            if (request.N.HasValue && request.N.Value < 1)
            {
                errors.Add($"N must be at least 1. Got: {request.N}");
            }

            if (request.FrequencyPenalty.HasValue && (request.FrequencyPenalty.Value < 0.0 || request.FrequencyPenalty.Value > 2.0))
            {
                errors.Add($"FrequencyPenalty must be between 0.0 and 2.0. Got: {request.FrequencyPenalty}");
            }

            if (request.PresencePenalty.HasValue && (request.PresencePenalty.Value < 0.0 || request.PresencePenalty.Value > 2.0))
            {
                errors.Add($"PresencePenalty must be between 0.0 and 2.0. Got: {request.PresencePenalty}");
            }

            return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
        }

        /// <summary>
        /// Sends a chat completion request to the Mistral AI API.
        /// </summary>
        /// <param name="request">The chat completion request containing the model, messages, and parameters.</param>
        /// <returns>A <see cref="MistralResponse"/> object containing the API response or error information.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
        /// <exception cref="MistralApiException">Thrown when the API returns an error (if ThrowOnError is enabled).</exception>
        public Task<MistralResponse> ChatCompletionAsync(ChatCompletionRequest request)
        {
            return ChatCompletionAsync(request, CancellationToken.None);
        }

        /// <summary>
        /// Sends a chat completion request to the Mistral AI API.
        /// </summary>
        /// <param name="request">The chat completion request containing the model, messages, and parameters.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A <see cref="MistralResponse"/> object containing the API response or error information.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
        /// <exception cref="MistralValidationException">Thrown when request validation fails (if ValidateRequests is enabled).</exception>
        /// <exception cref="MistralApiException">Thrown when the API returns an error (if ThrowOnError is enabled).</exception>
        public async Task<MistralResponse> ChatCompletionAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Validate request if enabled
            if (_options.ValidateRequests)
            {
                var validationResult = ValidateRequest(request);
                if (!validationResult.IsValid)
                {
                    if (_options.ThrowOnError)
                    {
                        throw new MistralValidationException(validationResult.Errors.ToArray());
                    }
                    return new MistralResponse(400)
                    {
                        Message = $"Validation failed: {string.Join("; ", validationResult.Errors)}",
                        IsSuccess = false
                    };
                }
            }

            try
            {
                var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_options.BaseUrl}/chat/completions", 
                    content, 
                    cancellationToken).ConfigureAwait(false);
                    
                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                var result = ParseResponse((int)response.StatusCode, jsonResponse);

                // Throw exception if enabled and request failed
                if (_options.ThrowOnError && !result.IsSuccess)
                {
                    throw new MistralApiException(result.Message, (HttpStatusCode)result.StatusCode);
                }

                return result;
            }
            catch (MistralApiException)
            {
                throw; // Re-throw our own exceptions
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // Re-throw cancellation
            }
            catch (HttpRequestException ex)
            {
                var response = new MistralResponse(500)
                {
                    Message = $"HTTP request failed: {ex.Message}",
                    IsSuccess = false
                };

                if (_options.ThrowOnError)
                {
                    throw new MistralApiException(response.Message, ex);
                }

                return response;
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // This is a timeout, not a cancellation
                var response = new MistralResponse(408)
                {
                    Message = $"Request timeout: {ex.Message}",
                    IsSuccess = false
                };

                if (_options.ThrowOnError)
                {
                    throw new MistralApiException(response.Message, HttpStatusCode.RequestTimeout);
                }

                return response;
            }
        }

        /// <summary>
        /// Sends a streaming chat completion request to the Mistral AI API.
        /// Tokens are returned as they are generated via an async enumerable.
        /// </summary>
        /// <param name="request">The chat completion request. The Stream property will be automatically set to true.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>An async enumerable of streaming chunks.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
        /// <exception cref="MistralValidationException">Thrown when request validation fails (if ValidateRequests is enabled).</exception>
        /// <exception cref="MistralApiException">Thrown when the API returns an error.</exception>
        public async IAsyncEnumerable<StreamingChatCompletionChunk> ChatCompletionStreamAsync(
            ChatCompletionRequest request, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Force streaming mode
            request.Stream = true;

            // Validate request if enabled
            if (_options.ValidateRequests)
            {
                var validationResult = ValidateRequest(request);
                if (!validationResult.IsValid)
                {
                    throw new MistralValidationException(validationResult.Errors.ToArray());
                }
            }

            var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/chat/completions")
            {
                Content = content
            };

            using var response = await _httpClient.SendAsync(
                httpRequest, 
                HttpCompletionOption.ResponseHeadersRead, 
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new MistralApiException($"API error: {errorContent}", response.StatusCode);
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // SSE format: "data: {...}" or "data: [DONE]"
                if (!line.StartsWith("data: ", StringComparison.Ordinal))
                    continue;

                var data = line.Substring(6); // Remove "data: " prefix

                if (data == "[DONE]")
                    yield break;

                StreamingChatCompletionChunk? chunk = null;
                try
                {
                    chunk = JsonSerializer.Deserialize<StreamingChatCompletionChunk>(data, _jsonOptions);
                }
                catch (JsonException)
                {
                    // Skip malformed chunks
                    continue;
                }

                if (chunk != null)
                {
                    yield return chunk;
                }
            }
        }

        /// <summary>
        /// Sends a streaming chat completion request and collects all chunks into a single result.
        /// Useful when you want streaming behavior but need the complete response.
        /// </summary>
        /// <param name="request">The chat completion request.</param>
        /// <param name="onChunk">Optional callback invoked for each chunk received.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The complete streaming result with all accumulated content.</returns>
        public async Task<StreamingChatCompletionResult> ChatCompletionStreamCollectAsync(
            ChatCompletionRequest request, 
            Action<StreamingChatCompletionChunk>? onChunk = null,
            CancellationToken cancellationToken = default)
        {
            var result = new StreamingChatCompletionResult();
            var contentBuilder = new StringBuilder();

            await foreach (var chunk in ChatCompletionStreamAsync(request, cancellationToken).ConfigureAwait(false))
            {
                result.Chunks.Add(chunk);
                
                // Set metadata from first chunk
                if (string.IsNullOrEmpty(result.Id))
                {
                    result.Id = chunk.Id;
                    result.Model = chunk.Model;
                }

                // Accumulate content
                var content = chunk.GetContent();
                if (!string.IsNullOrEmpty(content))
                {
                    contentBuilder.Append(content);
                }

                // Check for completion
                if (chunk.Choices?.Count > 0 && chunk.Choices[0].FinishReason != null)
                {
                    result.FinishReason = chunk.Choices[0].FinishReason;
                }

                // Get usage from final chunk
                if (chunk.Usage != null)
                {
                    result.Usage = chunk.Usage;
                }

                // Invoke callback
                onChunk?.Invoke(chunk);
            }

            result.Content = contentBuilder.ToString();
            return result;
        }

        #endregion

        /// <summary>
        /// Parses the API response and creates a standardized <see cref="MistralResponse"/> object.
        /// </summary>
        /// <param name="statusCode">The HTTP status code from the API response.</param>
        /// <param name="jsonResponse">The JSON response body from the API.</param>
        /// <returns>A <see cref="MistralResponse"/> object with parsed content or error information.</returns>
        private MistralResponse ParseResponse(int statusCode, string jsonResponse)
        {
            var response = new MistralResponse(statusCode);

            // Check if the request was successful
            if (statusCode >= 200 && statusCode < 300)
            {
                try
                {
                    var successResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(jsonResponse, _jsonOptions);
                    if (successResponse?.Choices?.Count > 0)
                    {
                        response.Message = successResponse.Choices[0].Message?.Content ?? string.Empty;
                        response.IsSuccess = true;
                        response.Model = successResponse.Model;
                        response.Usage = successResponse.Usage;
                        return response;
                    }
                }
                catch (JsonException)
                {
                    // Continue to error parsing if success parsing fails
                }
            }

            // Try to parse error responses
            response.IsSuccess = false;

            // Try ChatCompletionErrorResponse format
            try
            {
                var errorResponse = JsonSerializer.Deserialize<ChatCompletionErrorResponse>(jsonResponse, _jsonOptions);
                if (errorResponse?.Detail?.Count > 0)
                {
                    response.Message = errorResponse.Detail[0].Msg ?? "Unknown error";
                    return response;
                }
            }
            catch (JsonException)
            {
                // Continue to next error format
            }

            // Try ChatCompletionErrorModelResponse format
            try
            {
                var modelErrorResponse = JsonSerializer.Deserialize<ChatCompletionErrorModelResponse>(jsonResponse, _jsonOptions);
                if (modelErrorResponse != null)
                {
                    response.Message = modelErrorResponse.Message ?? "Unknown model error";
                    return response;
                }
            }
            catch (JsonException)
            {
                // Continue to fallback
            }

            // Fallback for unknown error formats
            response.Message = $"Unknown error format (Status: {statusCode}): {jsonResponse}";
            return response;
        }

        #region Files API

        /// <inheritdoc/>
        public async Task<FileListResponse> FilesListAsync(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_options.BaseUrl}/files", cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<FileListResponse>(json, _jsonOptions)
                ?? new FileListResponse();
        }

        /// <inheritdoc/>
        public async Task<MistralFileInfo> FilesUploadAsync(Stream fileStream, string fileName, string purpose, CancellationToken cancellationToken = default)
        {
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name is required.", nameof(fileName));
            if (string.IsNullOrWhiteSpace(purpose))
                throw new ArgumentException("Purpose is required (ocr, fine-tune, or batch).", nameof(purpose));

            var validPurposes = new[] { FilePurpose.Ocr, FilePurpose.FineTune, FilePurpose.Batch };
            if (Array.IndexOf(validPurposes, purpose) < 0)
                throw new ArgumentException($"Purpose must be one of: {string.Join(", ", validPurposes)}", nameof(purpose));

            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fileStream), "file", fileName);
            content.Add(new StringContent(purpose), "purpose");

            var response = await _httpClient.PostAsync($"{_options.BaseUrl}/files", content, cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<MistralFileInfo>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize file response.");
        }

        /// <inheritdoc/>
        public async Task<MistralFileInfo> FilesRetrieveAsync(string fileId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fileId))
                throw new ArgumentException("File ID is required.", nameof(fileId));

            var response = await _httpClient.GetAsync($"{_options.BaseUrl}/files/{Uri.EscapeDataString(fileId)}", cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<MistralFileInfo>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize file response.");
        }

        /// <inheritdoc/>
        public async Task<FileDeleteResponse> FilesDeleteAsync(string fileId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fileId))
                throw new ArgumentException("File ID is required.", nameof(fileId));

            var response = await _httpClient.DeleteAsync($"{_options.BaseUrl}/files/{Uri.EscapeDataString(fileId)}", cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<FileDeleteResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize delete response.");
        }

        /// <inheritdoc/>
        public async Task<Stream> FilesDownloadAsync(string fileId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fileId))
                throw new ArgumentException("File ID is required.", nameof(fileId));

            var response = await _httpClient.GetAsync($"{_options.BaseUrl}/files/{Uri.EscapeDataString(fileId)}/content", cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);
            }
            return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<FileSignedUrlResponse> FilesGetSignedUrlAsync(string fileId, int expiryHours = 24, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fileId))
                throw new ArgumentException("File ID is required.", nameof(fileId));

            var url = $"{_options.BaseUrl}/files/{Uri.EscapeDataString(fileId)}/url?expiry={expiryHours}";
            var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<FileSignedUrlResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize signed URL response.");
        }

        #endregion

        #region OCR API

        /// <inheritdoc/>
        public async Task<OcrResponse> OcrProcessAsync(OcrRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.Document == null)
                throw new ArgumentException("Document is required.", nameof(request));

            var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_options.BaseUrl}/ocr", content, cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<OcrResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize OCR response.");
        }

        #endregion

        /// <summary>
        /// Ensures the response is successful or throws MistralApiException.
        /// </summary>
        private static Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, string json, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode) return Task.CompletedTask;
            throw new MistralApiException($"API error: {json}", response.StatusCode);
        }

        #region IDisposable

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="MistralClient"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Only dispose the HttpClient if we own it
                    // When using IHttpClientFactory, the factory manages the HttpClient lifecycle
                    if (_ownsHttpClient)
                    {
                        _httpClient?.Dispose();
                    }
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="MistralClient"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    /// <summary>
    /// Represents a standardized response from the Mistral AI API.
    /// </summary>
    public class MistralResponse
    {
        /// <summary>
        /// Gets or sets the HTTP status code of the response.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the message content from the API response.
        /// For successful requests, this contains the generated text.
        /// For errors, this contains the error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the request was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets the model used for the completion (only available for successful responses).
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// Gets or sets the usage information from the API response (only available for successful responses).
        /// </summary>
        public UsageInfo? Usage { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MistralResponse"/> class.
        /// </summary>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        public MistralResponse(int statusCode)
        {
            StatusCode = statusCode;
        }
    }
}
