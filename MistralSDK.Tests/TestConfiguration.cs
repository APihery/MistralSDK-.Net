using System;

namespace MistralSDK.Tests
{
    /// <summary>
    /// Provides configuration settings for tests.
    /// API keys should be stored in environment variables, not in code.
    /// </summary>
    public static class TestConfiguration
    {
        /// <summary>
        /// Environment variable name for the Mistral API key.
        /// </summary>
        private const string ApiKeyEnvVar = "MISTRAL_API_KEY";

        /// <summary>
        /// Environment variable name to enable integration tests.
        /// </summary>
        private const string EnableIntegrationTestsEnvVar = "MISTRAL_ENABLE_INTEGRATION_TESTS";

        /// <summary>
        /// Gets the API key from environment variables.
        /// </summary>
        /// <returns>The API key, or null if not configured.</returns>
        public static string? GetApiKey()
        {
            return Environment.GetEnvironmentVariable(ApiKeyEnvVar);
        }

        /// <summary>
        /// Gets the API key from environment variables, throwing if not found.
        /// </summary>
        /// <returns>The API key.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the API key is not configured.</exception>
        public static string GetApiKeyOrThrow()
        {
            var apiKey = GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    $"API key not found. Please set the '{ApiKeyEnvVar}' environment variable. " +
                    "For security, never commit API keys to source control.");
            }
            return apiKey;
        }

        /// <summary>
        /// Checks if a valid API key is available.
        /// </summary>
        /// <returns>True if an API key is configured; otherwise, false.</returns>
        public static bool HasApiKey()
        {
            var apiKey = GetApiKey();
            return !string.IsNullOrWhiteSpace(apiKey);
        }

        /// <summary>
        /// Checks if integration tests are enabled.
        /// Integration tests require a valid API key and explicit opt-in.
        /// </summary>
        /// <returns>True if integration tests should run; otherwise, false.</returns>
        public static bool IsIntegrationTestEnabled()
        {
            var enabled = Environment.GetEnvironmentVariable(EnableIntegrationTestsEnvVar);
            return HasApiKey() && 
                   (string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(enabled, "1", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets a test API key for unit tests that don't make real API calls.
        /// </summary>
        /// <returns>A fake API key for testing.</returns>
        public static string GetTestApiKey()
        {
            return "test-api-key-for-unit-tests";
        }
    }
}
