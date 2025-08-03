using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MistralSDK.ChatCompletion
{
    /// <summary>
    /// Represents a request for chat completion from the Mistral AI API.
    /// </summary>
    public class ChatCompletionRequest
    {
        /// <summary>
        /// Gets or sets the model identifier to use for the completion.
        /// Required field that specifies which Mistral model to use.
        /// </summary>
        /// <example>mistral-tiny, mistral-small, mistral-medium, mistral-large</example>
        [JsonPropertyName("model")]
        [Required]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the temperature parameter for controlling randomness in the response.
        /// Values range from 0.0 (deterministic) to 2.0 (very random).
        /// Default value is 0.7.
        /// </summary>
        [JsonPropertyName("temperature")]
        [Range(0.0, 2.0)]
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// Gets or sets the top-p parameter for nucleus sampling.
        /// Controls diversity by considering only the most likely tokens.
        /// Values range from 0.0 to 1.0. Default value is 1.0.
        /// </summary>
        [JsonPropertyName("top_p")]
        [Range(0.0, 1.0)]
        public double TopP { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the maximum number of tokens to generate in the response.
        /// If not specified, the model will generate until it reaches a natural stopping point.
        /// </summary>
        [JsonPropertyName("max_tokens")]
        [Range(1, int.MaxValue)]
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Gets or sets the list of messages in the conversation.
        /// Required field that contains the conversation history and current prompt.
        /// </summary>
        [JsonPropertyName("messages")]
        [Required]
        public List<MessageRequest> Messages { get; set; } = new List<MessageRequest>();

        /// <summary>
        /// Gets or sets the prompt mode for the request.
        /// Optional parameter that can be used to specify special prompt handling.
        /// </summary>
        [JsonPropertyName("prompt_mode")]
        public string? PromptMode { get; set; }

        /// <summary>
        /// Gets or sets whether to enable safe prompt mode.
        /// When enabled, the API will apply additional safety filters.
        /// Default value is false.
        /// </summary>
        [JsonPropertyName("safe_prompt")]
        public bool SafePrompt { get; set; } = false;

        /// <summary>
        /// Gets or sets the random seed for reproducible results.
        /// When set, the model will generate consistent results for the same input.
        /// </summary>
        [JsonPropertyName("random_seed")]
        public int? RandomSeed { get; set; }

        /// <summary>
        /// Gets or sets whether to stream the response.
        /// When true, the response will be streamed token by token.
        /// Default value is false.
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        /// <summary>
        /// Validates the request parameters.
        /// </summary>
        /// <returns>True if the request is valid; otherwise, false.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Model) && 
                   Messages != null && 
                   Messages.Count > 0 &&
                   Temperature >= 0.0 && Temperature <= 2.0 &&
                   TopP >= 0.0 && TopP <= 1.0;
        }
    }

    /// <summary>
    /// Represents a message in a chat conversation.
    /// </summary>
    public class MessageRequest
    {
        /// <summary>
        /// Gets or sets the role of the message sender.
        /// Valid values are: "system", "user", "assistant".
        /// </summary>
        /// <example>user, assistant, system</example>
        [JsonPropertyName("role")]
        [Required]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content of the message.
        /// Required field that contains the actual text of the message.
        /// </summary>
        [JsonPropertyName("content")]
        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Validates the message parameters.
        /// </summary>
        /// <returns>True if the message is valid; otherwise, false.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Role) && 
                   !string.IsNullOrWhiteSpace(Content) &&
                   IsValidRole(Role);
        }

        /// <summary>
        /// Checks if the role is valid.
        /// </summary>
        /// <param name="role">The role to validate.</param>
        /// <returns>True if the role is valid; otherwise, false.</returns>
        private static bool IsValidRole(string role)
        {
            return role.Equals("system", StringComparison.OrdinalIgnoreCase) ||
                   role.Equals("user", StringComparison.OrdinalIgnoreCase) ||
                   role.Equals("assistant", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Provides constants for commonly used model names.
    /// </summary>
    public static class MistralModels
    {
        /// <summary>
        /// Mistral Tiny model - fastest and most cost-effective.
        /// </summary>
        public const string Tiny = "mistral-tiny";

        /// <summary>
        /// Mistral Small model - good balance of speed and quality.
        /// </summary>
        public const string Small = "mistral-small-latest";

        /// <summary>
        /// Mistral Medium model - high quality with good performance.
        /// </summary>
        public const string Medium = "mistral-medium-latest";

        /// <summary>
        /// Mistral Large model - highest quality, best for complex tasks.
        /// </summary>
        public const string Large = "mistral-large-latest";
    }

    /// <summary>
    /// Provides constants for message roles.
    /// </summary>
    public static class MessageRoles
    {
        /// <summary>
        /// System role - used for system instructions and context.
        /// </summary>
        public const string System = "system";

        /// <summary>
        /// User role - represents messages from the user.
        /// </summary>
        public const string User = "user";

        /// <summary>
        /// Assistant role - represents responses from the AI assistant.
        /// </summary>
        public const string Assistant = "assistant";
    }
}
