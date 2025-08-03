using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MistralSDK.ChatCompletion
{
    /// <summary>
    /// Represents a model-specific error response from the Mistral AI API.
    /// This is typically returned when there are issues with the model itself or model-specific parameters.
    /// </summary>
    public class ChatCompletionErrorModelResponse
    {
        /// <summary>
        /// Gets or sets the object type. Always "error" for error responses.
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message describing what went wrong.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of error that occurred.
        /// Common values include: "invalid_request_error", "authentication_error", "rate_limit_error", "server_error".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameter that caused the error (if applicable).
        /// This can be any object type depending on the error context.
        /// </summary>
        [JsonPropertyName("param")]
        public object? Param { get; set; }

        /// <summary>
        /// Gets or sets the error code for programmatic error handling.
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional error details (if available).
        /// </summary>
        [JsonPropertyName("details")]
        public string? Details { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code associated with this error.
        /// </summary>
        [JsonPropertyName("status")]
        public int? Status { get; set; }

        /// <summary>
        /// Gets a user-friendly error message.
        /// </summary>
        /// <returns>A formatted error message suitable for display to users.</returns>
        public string GetUserFriendlyMessage()
        {
            if (!string.IsNullOrWhiteSpace(Message))
            {
                return Message;
            }

            return Type switch
            {
                "invalid_request_error" => "The request was invalid or malformed.",
                "authentication_error" => "Authentication failed. Please check your API key.",
                "rate_limit_error" => "Rate limit exceeded. Please try again later.",
                "server_error" => "An internal server error occurred. Please try again.",
                "model_not_found" => "The specified model was not found.",
                "insufficient_quota" => "Insufficient quota for the requested operation.",
                _ => "An unexpected error occurred."
            };
        }

        /// <summary>
        /// Determines if this is a retryable error.
        /// </summary>
        /// <returns>True if the error is retryable; otherwise, false.</returns>
        public bool IsRetryable()
        {
            return Type switch
            {
                "rate_limit_error" => true,
                "server_error" => true,
                "timeout" => true,
                _ => false
            };
        }

        /// <summary>
        /// Gets the recommended retry delay in seconds.
        /// </summary>
        /// <returns>The recommended delay before retrying, or null if not retryable.</returns>
        public int? GetRetryDelaySeconds()
        {
            return Type switch
            {
                "rate_limit_error" => 60, // 1 minute
                "server_error" => 5,      // 5 seconds
                "timeout" => 10,          // 10 seconds
                _ => null
            };
        }
    }

    /// <summary>
    /// Provides constants for common error types returned by the Mistral AI API.
    /// </summary>
    public static class ModelErrorTypes
    {
        /// <summary>
        /// Error type for invalid or malformed requests.
        /// </summary>
        public const string InvalidRequestError = "invalid_request_error";

        /// <summary>
        /// Error type for authentication failures.
        /// </summary>
        public const string AuthenticationError = "authentication_error";

        /// <summary>
        /// Error type for rate limiting.
        /// </summary>
        public const string RateLimitError = "rate_limit_error";

        /// <summary>
        /// Error type for server errors.
        /// </summary>
        public const string ServerError = "server_error";

        /// <summary>
        /// Error type for model not found.
        /// </summary>
        public const string ModelNotFound = "model_not_found";

        /// <summary>
        /// Error type for insufficient quota.
        /// </summary>
        public const string InsufficientQuota = "insufficient_quota";

        /// <summary>
        /// Error type for request timeouts.
        /// </summary>
        public const string Timeout = "timeout";

        /// <summary>
        /// Error type for content policy violations.
        /// </summary>
        public const string ContentPolicyViolation = "content_policy_violation";
    }

    /// <summary>
    /// Provides constants for common error codes returned by the Mistral AI API.
    /// </summary>
    public static class ModelErrorCodes
    {
        /// <summary>
        /// Error code for invalid API key.
        /// </summary>
        public const string InvalidApiKey = "invalid_api_key";

        /// <summary>
        /// Error code for expired API key.
        /// </summary>
        public const string ExpiredApiKey = "expired_api_key";

        /// <summary>
        /// Error code for rate limit exceeded.
        /// </summary>
        public const string RateLimitExceeded = "rate_limit_exceeded";

        /// <summary>
        /// Error code for quota exceeded.
        /// </summary>
        public const string QuotaExceeded = "quota_exceeded";

        /// <summary>
        /// Error code for model not found.
        /// </summary>
        public const string ModelNotFound = "model_not_found";

        /// <summary>
        /// Error code for invalid model.
        /// </summary>
        public const string InvalidModel = "invalid_model";

        /// <summary>
        /// Error code for content filter violation.
        /// </summary>
        public const string ContentFilterViolation = "content_filter_violation";
    }
}
