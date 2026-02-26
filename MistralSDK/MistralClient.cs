using MistralSDK.Abstractions;
using MistralSDK.Agents;
using MistralSDK.Audio;
using MistralSDK.Batch;
using MistralSDK.ChatCompletion;
using MistralSDK.Classifiers;
using MistralSDK.Configuration;
using MistralSDK.Embeddings;
using MistralSDK.Exceptions;
using MistralSDK.Files;
using MistralSDK.Fim;
using MistralSDK.FineTuning;
using MistralSDK.Models;
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

        /// <inheritdoc/>
        public Task<MistralResponse> ChatCompletionAsync(string userMessage, string? model = null, int? maxTokens = null, CancellationToken cancellationToken = default)
        {
            var request = new ChatCompletionRequest
            {
                Model = model ?? MistralModels.Small,
                Messages = new List<MessageRequest> { MessageRequest.User(userMessage) },
                MaxTokens = maxTokens
            };
            return ChatCompletionAsync(request, cancellationToken);
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
                    if (successResponse != null)
                    {
                        response.Data = successResponse;
                        response.Message = successResponse.GetFirstChoiceContent();
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

        #region Embeddings API

        /// <inheritdoc/>
        public async Task<MistralResponse> EmbeddingsCreateAsync(EmbeddingRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Model))
                throw new ArgumentException("Model is required.", nameof(request));
            if (request.Input == null)
                throw new ArgumentException("Input is required.", nameof(request));

            var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_options.BaseUrl}/embeddings",
                content,
                cancellationToken).ConfigureAwait(false);

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, jsonResponse, cancellationToken).ConfigureAwait(false);

            var data = JsonSerializer.Deserialize<EmbeddingResponse>(jsonResponse, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize embedding response.");
            return new MistralResponse((int)response.StatusCode)
            {
                IsSuccess = true,
                Data = data,
                Message = "OK",
                Model = data.Model
            };
        }

        /// <inheritdoc/>
        public Task<MistralResponse> EmbeddingsCreateAsync(string text, string? model = null, CancellationToken cancellationToken = default)
        {
            var request = new EmbeddingRequest
            {
                Model = model ?? EmbeddingModels.MistralEmbed,
                Input = text
            };
            return EmbeddingsCreateAsync(request, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<MistralResponse> EmbeddingsCreateAsync(string[] texts, string? model = null, CancellationToken cancellationToken = default)
        {
            var request = new EmbeddingRequest
            {
                Model = model ?? EmbeddingModels.MistralEmbed,
                Input = texts
            };
            return EmbeddingsCreateAsync(request, cancellationToken);
        }

        #endregion

        #region Classifiers API

        /// <inheritdoc/>
        public async Task<MistralResponse> ModerationsCreateAsync(ModerationRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_options.BaseUrl}/moderations", content, cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<ModerationResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize moderation response.");
            return new MistralResponse((int)response.StatusCode) { IsSuccess = true, Data = data, Message = "OK" };
        }

        /// <inheritdoc/>
        public Task<MistralResponse> ModerateAsync(string text, string? model = null, CancellationToken cancellationToken = default)
        {
            var request = new ModerationRequest
            {
                Model = model ?? ModerationModels.MistralModerationLatest,
                Input = text
            };
            return ModerationsCreateAsync(request, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<MistralResponse> ChatModerationsCreateAsync(ChatModerationRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_options.BaseUrl}/chat/moderations", content, cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<ModerationResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize moderation response.");
            return new MistralResponse((int)response.StatusCode) { IsSuccess = true, Data = data, Message = "OK" };
        }

        /// <inheritdoc/>
        public async Task<MistralResponse> ClassificationsCreateAsync(ModerationRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_options.BaseUrl}/classifications", content, cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<ClassificationResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize classification response.");
            return new MistralResponse((int)response.StatusCode) { IsSuccess = true, Data = data, Message = "OK" };
        }

        /// <inheritdoc/>
        public async Task<MistralResponse> ChatClassificationsCreateAsync(ChatClassificationRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_options.BaseUrl}/chat/classifications", content, cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<ClassificationResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize classification response.");
            return new MistralResponse((int)response.StatusCode) { IsSuccess = true, Data = data, Message = "OK" };
        }

        #endregion

        #region Agents API

        /// <inheritdoc/>
        public async Task<MistralResponse> AgentCompletionAsync(AgentCompletionRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.AgentId))
                throw new ArgumentException("Agent ID is required.", nameof(request));
            if (request.Messages == null || request.Messages.Count == 0)
                throw new ArgumentException("At least one message is required.", nameof(request));

            var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_options.BaseUrl}/agents/completions",
                content,
                cancellationToken).ConfigureAwait(false);

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, jsonResponse, cancellationToken).ConfigureAwait(false);

            var data = JsonSerializer.Deserialize<ChatCompletionResponse>(jsonResponse, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize agent completion response.");
            return new MistralResponse((int)response.StatusCode)
            {
                IsSuccess = true,
                Data = data,
                Message = data.GetFirstChoiceContent(),
                Model = data.Model,
                Usage = data.Usage
            };
        }

        #endregion

        #region FIM API

        /// <inheritdoc/>
        public async Task<MistralResponse> FimCompletionAsync(FimCompletionRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Model))
                throw new ArgumentException("Model is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.Prompt))
                throw new ArgumentException("Prompt is required.", nameof(request));

            var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_options.BaseUrl}/fim/completions",
                content,
                cancellationToken).ConfigureAwait(false);

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, jsonResponse, cancellationToken).ConfigureAwait(false);

            var data = JsonSerializer.Deserialize<ChatCompletionResponse>(jsonResponse, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize FIM completion response.");
            return new MistralResponse((int)response.StatusCode)
            {
                IsSuccess = true,
                Data = data,
                Message = data.GetFirstChoiceContent(),
                Model = data.Model,
                Usage = data.Usage
            };
        }

        /// <inheritdoc/>
        public Task<MistralResponse> FimCompletionAsync(string prompt, string? suffix = null, int? maxTokens = null, string? model = null, CancellationToken cancellationToken = default)
        {
            var request = new FimCompletionRequest
            {
                Model = model ?? FimModels.CodestralLatest,
                Prompt = prompt,
                Suffix = suffix,
                MaxTokens = maxTokens
            };
            return FimCompletionAsync(request, cancellationToken);
        }

        #endregion

        #region Batch API

        /// <inheritdoc/>
        public async Task<BatchJobsListResponse> BatchJobsListAsync(int limit = 20, string? after = null, CancellationToken cancellationToken = default)
        {
            var query = new List<string> { $"limit={limit}" };
            if (!string.IsNullOrWhiteSpace(after))
                query.Add($"after={Uri.EscapeDataString(after)}");
            var url = $"{_options.BaseUrl}/batch/jobs?{string.Join("&", query)}";

            var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<BatchJobsListResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize batch jobs list response.");
        }

        /// <inheritdoc/>
        public async Task<BatchJobResponse> BatchJobCreateAsync(BatchJobCreateRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Endpoint))
                throw new ArgumentException("Endpoint is required.", nameof(request));
            var hasInput = (request.InputFiles != null && request.InputFiles.Count > 0) || (request.Requests != null && request.Requests.Count > 0);
            if (!hasInput)
                throw new ArgumentException("Either InputFiles or Requests is required.", nameof(request));

            var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_options.BaseUrl}/batch/jobs",
                content,
                cancellationToken).ConfigureAwait(false);

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<BatchJobResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize batch job response.");
        }

        /// <inheritdoc/>
        public async Task<BatchJobResponse> BatchJobGetAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                throw new ArgumentException("Job ID is required.", nameof(jobId));

            var response = await _httpClient.GetAsync(
                $"{_options.BaseUrl}/batch/jobs/{Uri.EscapeDataString(jobId)}",
                cancellationToken).ConfigureAwait(false);

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<BatchJobResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize batch job response.");
        }

        /// <inheritdoc/>
        public async Task<BatchJobResponse> BatchJobCancelAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                throw new ArgumentException("Job ID is required.", nameof(jobId));

            var response = await _httpClient.PostAsync(
                $"{_options.BaseUrl}/batch/jobs/{Uri.EscapeDataString(jobId)}/cancel",
                null,
                cancellationToken).ConfigureAwait(false);

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<BatchJobResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize batch job response.");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<BatchJobResponse>> BatchJobsListAllAsync(CancellationToken cancellationToken = default)
        {
            var all = new List<BatchJobResponse>();
            string? after = null;
            while (true)
            {
                var page = await BatchJobsListAsync(limit: 100, after, cancellationToken).ConfigureAwait(false);
                all.AddRange(page.Data);
                if (page.Data.Count < 100 || all.Count >= page.Total)
                    break;
                after = page.Data.Count > 0 ? page.Data[^1].Id : null;
            }
            return all;
        }

        /// <inheritdoc/>
        public async Task<BatchJobResponse> BatchJobWaitUntilCompleteAsync(string jobId, int pollIntervalMs = 5000, int timeoutMs = 86400000, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                throw new ArgumentException("Job ID is required.", nameof(jobId));

            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                var job = await BatchJobGetAsync(jobId, cancellationToken).ConfigureAwait(false);
                if (job.IsComplete)
                    return job;
                if (sw.ElapsedMilliseconds >= timeoutMs)
                    return job;
                await Task.Delay(pollIntervalMs, cancellationToken).ConfigureAwait(false);
            }
        }

        #endregion

        #region Fine-Tuning API

        /// <inheritdoc/>
        public async Task<FineTuningJobsListResponse> FineTuningJobsListAsync(int limit = 20, string? after = null, CancellationToken cancellationToken = default)
        {
            var query = new List<string> { $"limit={limit}" };
            if (!string.IsNullOrWhiteSpace(after))
                query.Add($"after={Uri.EscapeDataString(after)}");
            var url = $"{_options.BaseUrl}/fine_tuning/jobs?{string.Join("&", query)}";

            var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<FineTuningJobsListResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize fine-tuning jobs list response.");
        }

        /// <inheritdoc/>
        public async Task<FineTuningJobResponse> FineTuningJobCreateAsync(FineTuningJobCreateRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Model))
                throw new ArgumentException("Model is required.", nameof(request));
            if (request.TrainingFiles == null || request.TrainingFiles.Count == 0)
                throw new ArgumentException("At least one training file is required.", nameof(request));
            if (request.Hyperparameters == null)
                throw new ArgumentException("Hyperparameters are required.", nameof(request));

            var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_options.BaseUrl}/fine_tuning/jobs",
                content,
                cancellationToken).ConfigureAwait(false);

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<FineTuningJobResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize fine-tuning job response.");
        }

        /// <inheritdoc/>
        public async Task<FineTuningJobResponse> FineTuningJobGetAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                throw new ArgumentException("Job ID is required.", nameof(jobId));

            var response = await _httpClient.GetAsync(
                $"{_options.BaseUrl}/fine_tuning/jobs/{Uri.EscapeDataString(jobId)}",
                cancellationToken).ConfigureAwait(false);

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<FineTuningJobResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize fine-tuning job response.");
        }

        /// <inheritdoc/>
        public async Task<FineTuningJobResponse> FineTuningJobCancelAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                throw new ArgumentException("Job ID is required.", nameof(jobId));

            var response = await _httpClient.PostAsync(
                $"{_options.BaseUrl}/fine_tuning/jobs/{Uri.EscapeDataString(jobId)}/cancel",
                null,
                cancellationToken).ConfigureAwait(false);

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<FineTuningJobResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize fine-tuning job response.");
        }

        /// <inheritdoc/>
        public async Task<FineTuningJobResponse> FineTuningJobStartAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                throw new ArgumentException("Job ID is required.", nameof(jobId));

            var response = await _httpClient.PostAsync(
                $"{_options.BaseUrl}/fine_tuning/jobs/{Uri.EscapeDataString(jobId)}/start",
                null,
                cancellationToken).ConfigureAwait(false);

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<FineTuningJobResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize fine-tuning job response.");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<FineTuningJobResponse>> FineTuningJobsListAllAsync(CancellationToken cancellationToken = default)
        {
            var all = new List<FineTuningJobResponse>();
            string? after = null;
            while (true)
            {
                var page = await FineTuningJobsListAsync(limit: 100, after, cancellationToken).ConfigureAwait(false);
                all.AddRange(page.Data);
                if (page.Data.Count < 100 || all.Count >= page.Total)
                    break;
                after = page.Data.Count > 0 ? page.Data[^1].Id : null;
            }
            return all;
        }

        /// <inheritdoc/>
        public async Task<FineTuningJobResponse> FineTuningJobWaitUntilCompleteAsync(string jobId, int pollIntervalMs = 5000, int timeoutMs = 86400000, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                throw new ArgumentException("Job ID is required.", nameof(jobId));

            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                var job = await FineTuningJobGetAsync(jobId, cancellationToken).ConfigureAwait(false);
                if (job.IsComplete)
                    return job;
                if (sw.ElapsedMilliseconds >= timeoutMs)
                    return job;
                await Task.Delay(pollIntervalMs, cancellationToken).ConfigureAwait(false);
            }
        }

        #endregion

        #region Models API

        /// <inheritdoc/>
        public async Task<ModelListResponse> ModelsListAsync(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_options.BaseUrl}/models", cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            try
            {
                var result = JsonSerializer.Deserialize<ModelListResponse>(json, _jsonOptions);
                if (result != null)
                    return result;
            }
            catch (JsonException)
            {
                // API may return raw array
            }

            var array = JsonSerializer.Deserialize<List<ModelCard>>(json, _jsonOptions);
            return new ModelListResponse { Data = array ?? new List<ModelCard>() };
        }

        /// <inheritdoc/>
        public async Task<ModelCard> ModelsRetrieveAsync(string modelId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                throw new ArgumentException("Model ID is required.", nameof(modelId));

            var response = await _httpClient.GetAsync($"{_options.BaseUrl}/models/{Uri.EscapeDataString(modelId)}", cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<ModelCard>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize model response.");
        }

        /// <inheritdoc/>
        public async Task<ModelDeleteResponse> ModelsDeleteAsync(string modelId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                throw new ArgumentException("Model ID is required.", nameof(modelId));

            var response = await _httpClient.DeleteAsync($"{_options.BaseUrl}/models/{Uri.EscapeDataString(modelId)}", cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<ModelDeleteResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize delete response.");
        }

        /// <inheritdoc/>
        public async Task<ModelCard> ModelsUpdateAsync(string modelId, UpdateFTModelRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                throw new ArgumentException("Model ID is required.", nameof(modelId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PatchAsync(
                $"{_options.BaseUrl}/fine_tuning/models/{Uri.EscapeDataString(modelId)}",
                content,
                cancellationToken).ConfigureAwait(false);

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<ModelCard>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize model response.");
        }

        /// <inheritdoc/>
        public async Task<ArchiveFTModelResponse> ModelsArchiveAsync(string modelId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                throw new ArgumentException("Model ID is required.", nameof(modelId));

            var response = await _httpClient.PostAsync(
                $"{_options.BaseUrl}/fine_tuning/models/{Uri.EscapeDataString(modelId)}/archive",
                null,
                cancellationToken).ConfigureAwait(false);

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<ArchiveFTModelResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize archive response.");
        }

        /// <inheritdoc/>
        public async Task<UnarchiveFTModelResponse> ModelsUnarchiveAsync(string modelId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                throw new ArgumentException("Model ID is required.", nameof(modelId));

            var response = await _httpClient.DeleteAsync(
                $"{_options.BaseUrl}/fine_tuning/models/{Uri.EscapeDataString(modelId)}/archive",
                cancellationToken).ConfigureAwait(false);

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<UnarchiveFTModelResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize unarchive response.");
        }

        #endregion

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
                throw new ArgumentException("Purpose is required (ocr, fine-tune, batch, or audio).", nameof(purpose));

            var validPurposes = new[] { FilePurpose.Ocr, FilePurpose.FineTune, FilePurpose.Batch, FilePurpose.Audio };
            if (Array.IndexOf(validPurposes, purpose) < 0)
                throw new ArgumentException($"Purpose must be one of: ocr, fine-tune, batch, or audio.", nameof(purpose));

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
        public Task<MistralFileInfo> FilesUploadAsync(Stream fileStream, string fileName, FilePurposeType purpose, CancellationToken cancellationToken = default)
            => FilesUploadAsync(fileStream, fileName, purpose.ToApiString(), cancellationToken);

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

        /// <inheritdoc/>
        public async Task<string> OcrExtractTextAsync(Stream fileStream, string fileName, bool deleteAfter = true, CancellationToken cancellationToken = default)
        {
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name is required.", nameof(fileName));

            string? fileId = null;
            try
            {
                var file = await FilesUploadAsync(fileStream, fileName, FilePurposeType.Ocr, cancellationToken).ConfigureAwait(false);
                fileId = file.Id;

                var request = new OcrRequest { Document = OcrDocument.FromFileId(fileId) };
                var response = await OcrProcessAsync(request, cancellationToken).ConfigureAwait(false);
                return response.GetAllMarkdown();
            }
            finally
            {
                if (deleteAfter && !string.IsNullOrEmpty(fileId))
                {
                    try { await FilesDeleteAsync(fileId, cancellationToken).ConfigureAwait(false); }
                    catch { /* Best effort cleanup */ }
                }
            }
        }

        #endregion

        #region Audio API

        /// <inheritdoc/>
        public async Task<TranscriptionResponse> AudioTranscribeAsync(AudioTranscriptionRequest request, CancellationToken cancellationToken = default)
        {
            ValidateAudioRequest(request, forStreaming: false);
            var content = BuildAudioMultipartContent(request, stream: false);
            var response = await _httpClient.PostAsync($"{_options.BaseUrl}/audio/transcriptions", content, cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            await EnsureSuccessOrThrowAsync(response, json, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<TranscriptionResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize transcription response.");
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<TranscriptionStreamEvent> AudioTranscribeStreamAsync(
            AudioTranscriptionRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ValidateAudioRequest(request, forStreaming: true);
            var content = BuildAudioMultipartContent(request, stream: true);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/audio/transcriptions") { Content = content };
            using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var errorJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                await EnsureSuccessOrThrowAsync(response, errorJson, cancellationToken).ConfigureAwait(false);
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ", StringComparison.Ordinal))
                    continue;
                var data = line.Substring(6);
                if (data == "[DONE]")
                    yield break;
                TranscriptionStreamEvent? evt = null;
                try
                {
                    var wrapper = JsonSerializer.Deserialize<TranscriptionStreamEvents>(data, _jsonOptions);
                    evt = wrapper?.Data;
                }
                catch (JsonException) { continue; }
                if (evt != null)
                    yield return evt;
            }
        }

        private static void ValidateAudioRequest(AudioTranscriptionRequest request, bool forStreaming)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Model))
                throw new ArgumentException("Model is required.", nameof(request));
            var hasInput = request.AudioStream != null || !string.IsNullOrWhiteSpace(request.FileId) || !string.IsNullOrWhiteSpace(request.FileUrl);
            if (!hasInput)
                throw new ArgumentException("Audio input is required. Use FromStream, FromFileId, or FromFileUrl.");
            if (request.AudioStream != null && string.IsNullOrWhiteSpace(request.FileName))
                throw new ArgumentException("File name is required when using stream input.", nameof(request));
            if (!string.IsNullOrWhiteSpace(request.Language) && request.Language.Length != 2)
                throw new ArgumentException("Language must be a 2-character code (e.g. 'en').", nameof(request));
        }

        private static MultipartFormDataContent BuildAudioMultipartContent(AudioTranscriptionRequest request, bool stream)
        {
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(request.Model), "model");
            content.Add(new StringContent(stream ? "true" : "false"), "stream");

            if (request.AudioStream != null && request.FileName != null)
                content.Add(new StreamContent(request.AudioStream), "file", request.FileName);
            if (!string.IsNullOrWhiteSpace(request.FileId))
                content.Add(new StringContent(request.FileId), "file_id");
            if (!string.IsNullOrWhiteSpace(request.FileUrl))
                content.Add(new StringContent(request.FileUrl), "file_url");

            content.Add(new StringContent(request.Diarize ? "true" : "false"), "diarize");
            if (request.Language != null)
                content.Add(new StringContent(request.Language), "language");
            if (request.Temperature.HasValue)
                content.Add(new StringContent(request.Temperature.Value.ToString("G", System.Globalization.CultureInfo.InvariantCulture)), "temperature");
            if (request.ContextBias != null && request.ContextBias.Count > 0)
            {
                var biasJson = JsonSerializer.Serialize(request.ContextBias);
                content.Add(new StringContent(biasJson), "context_bias");
            }
            if (request.TimestampGranularities != null && request.TimestampGranularities.Count > 0)
            {
                var granJson = JsonSerializer.Serialize(request.TimestampGranularities);
                content.Add(new StringContent(granJson), "timestamp_granularities");
            }
            return content;
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
    /// All API methods return this type. Use <see cref="Data"/> for the strongly-typed response when <see cref="IsSuccess"/> is true.
    /// </summary>
    public class MistralResponse
    {
        /// <summary>
        /// Gets or sets the HTTP status code of the response.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the message content from the API response.
        /// For successful chat/FIM requests, this contains the generated text.
        /// For errors, this contains the error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the request was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets the model used (only available for successful completion responses).
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// Gets or sets the usage information (only available for successful completion responses).
        /// </summary>
        public UsageInfo? Usage { get; set; }

        /// <summary>
        /// Gets or sets the strongly-typed response data when <see cref="IsSuccess"/> is true.
        /// Cast to the appropriate type: <see cref="ChatCompletionResponse"/>, <see cref="EmbeddingResponse"/>, <see cref="ModerationResponse"/>, etc.
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// Gets the response data as the specified type. Returns null if Data is null or not of type T.
        /// </summary>
        public T? GetData<T>() => Data is T t ? t : default;

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
