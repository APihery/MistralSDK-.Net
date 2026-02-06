using System;
using System.Net;

namespace MistralSDK.Exceptions
{
    /// <summary>
    /// Represents an exception that occurs when interacting with the Mistral AI API.
    /// </summary>
    public class MistralApiException : Exception
    {
        /// <summary>
        /// Gets the HTTP status code associated with this error.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Gets the error type returned by the API.
        /// </summary>
        public string? ErrorType { get; }

        /// <summary>
        /// Gets the error code returned by the API.
        /// </summary>
        public string? ErrorCode { get; }

        /// <summary>
        /// Gets a value indicating whether this error is retryable.
        /// </summary>
        public bool IsRetryable { get; }

        /// <summary>
        /// Gets the recommended retry delay in seconds, if applicable.
        /// </summary>
        public int? RetryDelaySeconds { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MistralApiException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public MistralApiException(string message) 
            : base(message)
        {
            StatusCode = HttpStatusCode.InternalServerError;
            IsRetryable = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MistralApiException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        public MistralApiException(string message, HttpStatusCode statusCode) 
            : base(message)
        {
            StatusCode = statusCode;
            IsRetryable = IsStatusCodeRetryable(statusCode);
            RetryDelaySeconds = GetDefaultRetryDelay(statusCode);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MistralApiException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="errorType">The error type from the API.</param>
        /// <param name="errorCode">The error code from the API.</param>
        public MistralApiException(string message, HttpStatusCode statusCode, string? errorType, string? errorCode) 
            : base(message)
        {
            StatusCode = statusCode;
            ErrorType = errorType;
            ErrorCode = errorCode;
            IsRetryable = IsStatusCodeRetryable(statusCode) || IsErrorTypeRetryable(errorType);
            RetryDelaySeconds = GetDefaultRetryDelay(statusCode, errorType);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MistralApiException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public MistralApiException(string message, Exception innerException) 
            : base(message, innerException)
        {
            StatusCode = HttpStatusCode.InternalServerError;
            IsRetryable = innerException is TimeoutException;
        }

        /// <summary>
        /// Determines if a status code indicates a retryable error.
        /// </summary>
        private static bool IsStatusCodeRetryable(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.TooManyRequests => true,      // 429
                HttpStatusCode.ServiceUnavailable => true,    // 503
                HttpStatusCode.GatewayTimeout => true,        // 504
                HttpStatusCode.InternalServerError => true,   // 500
                HttpStatusCode.BadGateway => true,            // 502
                _ => false
            };
        }

        /// <summary>
        /// Determines if an error type indicates a retryable error.
        /// </summary>
        private static bool IsErrorTypeRetryable(string? errorType)
        {
            if (string.IsNullOrEmpty(errorType)) return false;
            
            return errorType switch
            {
                "rate_limit_error" => true,
                "server_error" => true,
                "timeout" => true,
                _ => false
            };
        }

        /// <summary>
        /// Gets the default retry delay based on status code and error type.
        /// </summary>
        private static int? GetDefaultRetryDelay(HttpStatusCode statusCode, string? errorType = null)
        {
            // First check error type
            if (!string.IsNullOrEmpty(errorType))
            {
                var delay = errorType switch
                {
                    "rate_limit_error" => 60,
                    "server_error" => 5,
                    "timeout" => 10,
                    _ => (int?)null
                };
                if (delay.HasValue) return delay;
            }

            // Then check status code
            return statusCode switch
            {
                HttpStatusCode.TooManyRequests => 60,        // 429 - rate limited
                HttpStatusCode.ServiceUnavailable => 30,     // 503 - service unavailable
                HttpStatusCode.GatewayTimeout => 10,         // 504 - timeout
                HttpStatusCode.InternalServerError => 5,     // 500 - server error
                HttpStatusCode.BadGateway => 10,             // 502 - bad gateway
                _ => null
            };
        }
    }

    /// <summary>
    /// Exception thrown when request validation fails.
    /// </summary>
    public class MistralValidationException : MistralApiException
    {
        /// <summary>
        /// Gets the validation errors.
        /// </summary>
        public IReadOnlyList<string> ValidationErrors { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MistralValidationException"/> class.
        /// </summary>
        /// <param name="errors">The validation errors.</param>
        public MistralValidationException(IReadOnlyList<string> errors)
            : base($"Request validation failed: {string.Join("; ", errors)}")
        {
            ValidationErrors = errors;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MistralValidationException"/> class.
        /// </summary>
        /// <param name="error">The validation error.</param>
        public MistralValidationException(string error)
            : base($"Request validation failed: {error}")
        {
            ValidationErrors = new[] { error };
        }
    }

    /// <summary>
    /// Exception thrown when authentication fails.
    /// </summary>
    public class MistralAuthenticationException : MistralApiException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MistralAuthenticationException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public MistralAuthenticationException(string message)
            : base(message, HttpStatusCode.Unauthorized)
        {
        }
    }

    /// <summary>
    /// Exception thrown when the rate limit is exceeded.
    /// </summary>
    public class MistralRateLimitException : MistralApiException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MistralRateLimitException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="retryAfterSeconds">The number of seconds to wait before retrying.</param>
        public MistralRateLimitException(string message, int? retryAfterSeconds = null)
            : base(message, HttpStatusCode.TooManyRequests, "rate_limit_error", "rate_limit_exceeded")
        {
        }
    }

    /// <summary>
    /// Exception thrown when the requested model is not found.
    /// </summary>
    public class MistralModelNotFoundException : MistralApiException
    {
        /// <summary>
        /// Gets the model identifier that was not found.
        /// </summary>
        public string ModelId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MistralModelNotFoundException"/> class.
        /// </summary>
        /// <param name="modelId">The model identifier that was not found.</param>
        public MistralModelNotFoundException(string modelId)
            : base($"Model '{modelId}' not found. Please check the model name and try again.", 
                   HttpStatusCode.NotFound, "model_not_found", "model_not_found")
        {
            ModelId = modelId;
        }
    }
}
