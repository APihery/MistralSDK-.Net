using System;

namespace MistralSDK.Configuration
{
    /// <summary>
    /// Configuration options for the Mistral AI client.
    /// </summary>
    public class MistralClientOptions
    {
        /// <summary>
        /// The default configuration section name in appsettings.json.
        /// </summary>
        public const string SectionName = "MistralApi";

        /// <summary>
        /// Gets or sets the API key for authentication with Mistral AI.
        /// This should be set via environment variables or secure configuration.
        /// Never commit API keys to source control.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the base URL for the Mistral AI API.
        /// Default is "https://api.mistral.ai/v1".
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.mistral.ai/v1";

        /// <summary>
        /// Gets or sets the timeout for HTTP requests in seconds.
        /// Default is 30 seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for transient failures.
        /// Default is 3.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial delay in milliseconds between retry attempts.
        /// This value is used as the base for exponential backoff.
        /// Default is 1000 milliseconds (1 second).
        /// </summary>
        public int RetryDelayMilliseconds { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to enable request/response caching.
        /// Default is false.
        /// </summary>
        public bool EnableCaching { get; set; } = false;

        /// <summary>
        /// Gets or sets the cache expiration time in minutes.
        /// Only used when <see cref="EnableCaching"/> is true.
        /// Default is 5 minutes.
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 5;

        /// <summary>
        /// Gets or sets whether to throw exceptions on API errors.
        /// When false, errors are returned in the response object.
        /// Default is false for backward compatibility.
        /// </summary>
        public bool ThrowOnError { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to validate requests before sending.
        /// Default is true.
        /// </summary>
        public bool ValidateRequests { get; set; } = true;

        /// <summary>
        /// Validates the options and throws if invalid.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the options are invalid.</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                throw new ArgumentException(
                    "API key is required. Set it via configuration or environment variables. " +
                    "Never commit API keys to source control.", 
                    nameof(ApiKey));
            }

            if (string.IsNullOrWhiteSpace(BaseUrl))
            {
                throw new ArgumentException("Base URL is required.", nameof(BaseUrl));
            }

            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) || 
                (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                throw new ArgumentException("Base URL must be a valid HTTP or HTTPS URL.", nameof(BaseUrl));
            }

            if (TimeoutSeconds <= 0)
            {
                throw new ArgumentException("Timeout must be greater than 0.", nameof(TimeoutSeconds));
            }

            if (MaxRetries < 0)
            {
                throw new ArgumentException("Max retries cannot be negative.", nameof(MaxRetries));
            }

            if (RetryDelayMilliseconds < 0)
            {
                throw new ArgumentException("Retry delay cannot be negative.", nameof(RetryDelayMilliseconds));
            }

            if (CacheExpirationMinutes <= 0)
            {
                throw new ArgumentException("Cache expiration must be greater than 0.", nameof(CacheExpirationMinutes));
            }
        }
    }
}
