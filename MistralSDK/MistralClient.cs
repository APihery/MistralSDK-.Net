using MistralSDK.ChatCompletion;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MistralSDK
{
    /// <summary>
    /// Client for interacting with the Mistral AI API.
    /// Provides methods to send chat completion requests and handle responses.
    /// </summary>
    public class MistralClient : IDisposable
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed = false;

        /// <summary>
        /// The base URL for the Mistral AI API.
        /// </summary>
        private const string BaseUrl = "https://api.mistral.ai/v1";

        /// <summary>
        /// Initializes a new instance of the <see cref="MistralClient"/> class.
        /// </summary>
        /// <param name="apiKey">The API key for authentication with Mistral AI.</param>
        /// <exception cref="ArgumentException">Thrown when the API key is null, empty, or whitespace.</exception>
        public MistralClient(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("API key cannot be null, empty, or whitespace.", nameof(apiKey));
            }

            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            
            // Configure JSON serialization options
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Sends a chat completion request to the Mistral AI API.
        /// </summary>
        /// <param name="request">The chat completion request containing the model, messages, and parameters.</param>
        /// <returns>A <see cref="MistralResponse"/> object containing the API response or error information.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
        /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
        public async Task<MistralResponse> ChatCompletionAsync(ChatCompletionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            try
            {
                var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}/chat/completions", content);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                return ParseResponse((int)response.StatusCode, jsonResponse);
            }
            catch (HttpRequestException ex)
            {
                return new MistralResponse(500)
                {
                    Message = $"HTTP request failed: {ex.Message}",
                    IsSuccess = false
                };
            }
            catch (TaskCanceledException ex)
            {
                return new MistralResponse(408)
                {
                    Message = $"Request timeout: {ex.Message}",
                    IsSuccess = false
                };
            }
        }

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
                    _httpClient?.Dispose();
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
