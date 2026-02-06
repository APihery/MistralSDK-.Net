using System;
using System.IO;

namespace MistralSDK.Tests
{
    /// <summary>
    /// Provides configuration settings for tests.
    /// API keys can be stored in environment variables or in an api-key.txt file.
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
        /// File name for the API key file (alternative to environment variable).
        /// </summary>
        private const string ApiKeyFileName = "api-key.txt";

        /// <summary>
        /// Gets the API key from environment variables or api-key.txt file.
        /// Priority: 1. Environment variable, 2. api-key.txt file
        /// </summary>
        /// <returns>The API key, or null if not configured.</returns>
        public static string? GetApiKey()
        {
            // First, try environment variable
            var apiKey = Environment.GetEnvironmentVariable(ApiKeyEnvVar);
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                return apiKey;
            }

            // Second, try api-key.txt file in project root
            return GetApiKeyFromFile();
        }

        /// <summary>
        /// Attempts to read the API key from the api-key.txt file.
        /// Searches in current directory and parent directories up to the solution root.
        /// </summary>
        /// <returns>The API key from file, or null if not found.</returns>
        private static string? GetApiKeyFromFile()
        {
            try
            {
                // Try current directory first
                var currentDir = Directory.GetCurrentDirectory();
                
                // Search up to 5 levels up for the api-key.txt file
                for (int i = 0; i < 5; i++)
                {
                    var filePath = Path.Combine(currentDir, ApiKeyFileName);
                    if (File.Exists(filePath))
                    {
                        var key = File.ReadAllText(filePath).Trim();
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            return key;
                        }
                    }

                    var parent = Directory.GetParent(currentDir);
                    if (parent == null) break;
                    currentDir = parent.FullName;
                }
            }
            catch
            {
                // Ignore file read errors
            }

            return null;
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
        /// Integration tests are enabled if:
        /// - An api-key.txt file exists (auto-enables integration tests), OR
        /// - MISTRAL_API_KEY env var is set AND MISTRAL_ENABLE_INTEGRATION_TESTS=true
        /// </summary>
        /// <returns>True if integration tests should run; otherwise, false.</returns>
        public static bool IsIntegrationTestEnabled()
        {
            // If api-key.txt file exists, auto-enable integration tests
            if (GetApiKeyFromFile() != null)
            {
                return true;
            }

            // Otherwise, require explicit opt-in via environment variable
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
