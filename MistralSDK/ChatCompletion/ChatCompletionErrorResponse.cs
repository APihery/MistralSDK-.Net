using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MistralSDK.ChatCompletion
{
    /// <summary>
    /// Represents an error response from the Mistral AI API with detailed validation errors.
    /// </summary>
    public class ChatCompletionErrorResponse
    {
        /// <summary>
        /// Gets or sets the list of detailed error information.
        /// </summary>
        [JsonPropertyName("detail")]
        public List<ErrorDetail> Detail { get; set; } = new List<ErrorDetail>();

        /// <summary>
        /// Gets the first error message from the detail list.
        /// </summary>
        /// <returns>The first error message, or "Unknown error" if no details are available.</returns>
        public string GetFirstErrorMessage()
        {
            return Detail?.Count > 0 ? Detail[0].Msg ?? "Unknown error" : "Unknown error";
        }

        /// <summary>
        /// Gets all error messages as a single string.
        /// </summary>
        /// <returns>A string containing all error messages separated by semicolons.</returns>
        public string GetAllErrorMessages()
        {
            if (Detail?.Count == 0) return "Unknown error";
            
            return string.Join("; ", Detail.Select(d => d.Msg ?? "Unknown error"));
        }
    }

    /// <summary>
    /// Represents detailed information about a specific error.
    /// </summary>
    public class ErrorDetail
    {
        /// <summary>
        /// Gets or sets the type of error that occurred.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message describing what went wrong.
        /// </summary>
        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the input that caused the error.
        /// </summary>
        [JsonPropertyName("input")]
        public ErrorInput? Input { get; set; }

        /// <summary>
        /// Gets or sets the context information about the error.
        /// </summary>
        [JsonPropertyName("ctx")]
        public Ctx? Ctx { get; set; }

        /// <summary>
        /// Gets or sets the location where the error occurred.
        /// </summary>
        [JsonPropertyName("loc")]
        public List<object>? Loc { get; set; }
    }

    /// <summary>
    /// Represents the input that caused an error.
    /// </summary>
    public class ErrorInput
    {
        /// <summary>
        /// Gets or sets the role of the message that caused the error.
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content of the message that caused the error.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the message sender (if applicable).
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    /// <summary>
    /// Represents context information about an error.
    /// </summary>
    public class Ctx
    {
        /// <summary>
        /// Gets or sets the discriminator used to identify the error type.
        /// </summary>
        [JsonPropertyName("discriminator")]
        public string Discriminator { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tag associated with the error.
        /// </summary>
        [JsonPropertyName("tag")]
        public string Tag { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expected tags for the context.
        /// </summary>
        [JsonPropertyName("expected_tags")]
        public string ExpectedTags { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional context information.
        /// </summary>
        [JsonPropertyName("additional_info")]
        public string? AdditionalInfo { get; set; }
    }

    /// <summary>
    /// Provides constants for common error types.
    /// </summary>
    public static class ErrorTypes
    {
        /// <summary>
        /// Error type for validation failures.
        /// </summary>
        public const string ValidationError = "validation_error";

        /// <summary>
        /// Error type for missing required fields.
        /// </summary>
        public const string MissingField = "missing";

        /// <summary>
        /// Error type for type mismatches.
        /// </summary>
        public const string TypeError = "type_error";

        /// <summary>
        /// Error type for value errors.
        /// </summary>
        public const string ValueError = "value_error";

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
    }
}
