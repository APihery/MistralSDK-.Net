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
        /// Gets or sets the list of messages in the conversation.
        /// Required field that contains the conversation history and current prompt.
        /// </summary>
        [JsonPropertyName("messages")]
        [Required]
        public List<MessageRequest> Messages { get; set; } = new List<MessageRequest>();

        /// <summary>
        /// Gets or sets the temperature parameter for controlling randomness in the response.
        /// Values range from 0.0 (deterministic) to 2.0 (very random).
        /// We recommend between 0.0 and 0.7. Default value is 0.7.
        /// </summary>
        [JsonPropertyName("temperature")]
        [Range(0.0, 2.0)]
        public double? Temperature { get; set; }

        /// <summary>
        /// Gets or sets the top-p parameter for nucleus sampling.
        /// Controls diversity by considering only the most likely tokens.
        /// Values range from 0.0 to 1.0. Default value is 1.0.
        /// We recommend altering this or Temperature but not both.
        /// </summary>
        [JsonPropertyName("top_p")]
        [Range(0.0, 1.0)]
        public double? TopP { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of tokens to generate in the response.
        /// If not specified, the model will generate until it reaches a natural stopping point.
        /// </summary>
        [JsonPropertyName("max_tokens")]
        [Range(1, int.MaxValue)]
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Gets or sets the number of completions to return for each request.
        /// Input tokens are only billed once. Default is 1.
        /// Note: mistral-large-2512 does not support N completions.
        /// </summary>
        [JsonPropertyName("n")]
        [Range(1, int.MaxValue)]
        public int? N { get; set; }

        /// <summary>
        /// Gets or sets the stop sequences.
        /// Stop generation if one of these tokens/strings is detected.
        /// Can be a single string or multiple strings.
        /// </summary>
        [JsonPropertyName("stop")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Stop { get; set; }

        /// <summary>
        /// Gets or sets the frequency penalty.
        /// Penalizes the repetition of words based on their frequency in the generated text.
        /// A higher value discourages repeating words that have already appeared frequently.
        /// Values range from 0.0 to 2.0. Default is 0.
        /// </summary>
        [JsonPropertyName("frequency_penalty")]
        [Range(0.0, 2.0)]
        public double? FrequencyPenalty { get; set; }

        /// <summary>
        /// Gets or sets the presence penalty.
        /// Determines how much the model penalizes the repetition of words or phrases.
        /// A higher value encourages the model to use a wider variety of words.
        /// Values range from 0.0 to 2.0. Default is 0.
        /// </summary>
        [JsonPropertyName("presence_penalty")]
        [Range(0.0, 2.0)]
        public double? PresencePenalty { get; set; }

        /// <summary>
        /// Gets or sets the response format.
        /// Use to enforce JSON output or a specific JSON schema.
        /// </summary>
        [JsonPropertyName("response_format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ResponseFormat? ResponseFormat { get; set; }

        /// <summary>
        /// Gets or sets whether to enable safe prompt mode.
        /// When enabled, a safety prompt is injected before all conversations.
        /// Default value is false.
        /// </summary>
        [JsonPropertyName("safe_prompt")]
        public bool SafePrompt { get; set; } = false;

        /// <summary>
        /// Gets or sets the random seed for reproducible results.
        /// When set, the model will generate deterministic results for the same input.
        /// </summary>
        [JsonPropertyName("random_seed")]
        public int? RandomSeed { get; set; }

        /// <summary>
        /// Gets or sets whether to stream the response.
        /// When true, tokens will be sent as server-sent events as they become available.
        /// Default value is false.
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        /// <summary>
        /// Gets or sets the prompt mode for the request.
        /// Available option: "reasoning" for enhanced reasoning capabilities.
        /// </summary>
        [JsonPropertyName("prompt_mode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PromptMode { get; set; }

        /// <summary>
        /// Validates the request parameters.
        /// </summary>
        /// <returns>True if the request is valid; otherwise, false.</returns>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Model) || Messages == null || Messages.Count == 0)
                return false;

            if (Temperature.HasValue && (Temperature.Value < 0.0 || Temperature.Value > 2.0))
                return false;

            if (TopP.HasValue && (TopP.Value < 0.0 || TopP.Value > 1.0))
                return false;

            if (FrequencyPenalty.HasValue && (FrequencyPenalty.Value < 0.0 || FrequencyPenalty.Value > 2.0))
                return false;

            if (PresencePenalty.HasValue && (PresencePenalty.Value < 0.0 || PresencePenalty.Value > 2.0))
                return false;

            if (N.HasValue && N.Value < 1)
                return false;

            return true;
        }

        #region Fluent Builder Methods

        /// <summary>
        /// Sets the stop sequence(s) for the request.
        /// </summary>
        /// <param name="stop">A single stop string.</param>
        /// <returns>This request instance for method chaining.</returns>
        public ChatCompletionRequest WithStop(string stop)
        {
            Stop = stop;
            return this;
        }

        /// <summary>
        /// Sets multiple stop sequences for the request.
        /// </summary>
        /// <param name="stops">An array of stop strings.</param>
        /// <returns>This request instance for method chaining.</returns>
        public ChatCompletionRequest WithStops(params string[] stops)
        {
            Stop = stops;
            return this;
        }

        /// <summary>
        /// Configures the request to return JSON output.
        /// </summary>
        /// <returns>This request instance for method chaining.</returns>
        public ChatCompletionRequest AsJson()
        {
            ResponseFormat = new ResponseFormat { Type = ResponseFormatType.JsonObject };
            return this;
        }

        /// <summary>
        /// Configures the request to return JSON output matching a specific schema.
        /// </summary>
        /// <param name="schema">The JSON schema to enforce.</param>
        /// <returns>This request instance for method chaining.</returns>
        public ChatCompletionRequest AsJsonSchema(JsonSchema schema)
        {
            ResponseFormat = new ResponseFormat 
            { 
                Type = ResponseFormatType.JsonSchema,
                JsonSchema = schema
            };
            return this;
        }

        #endregion
    }

    /// <summary>
    /// Specifies the format that the model must output.
    /// </summary>
    public class ResponseFormat
    {
        /// <summary>
        /// Gets or sets the type of response format.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = ResponseFormatType.Text;

        /// <summary>
        /// Gets or sets the JSON schema (required when type is "json_schema").
        /// </summary>
        [JsonPropertyName("json_schema")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonSchema? JsonSchema { get; set; }
    }

    /// <summary>
    /// Constants for response format types.
    /// </summary>
    public static class ResponseFormatType
    {
        /// <summary>
        /// Standard text output (default).
        /// </summary>
        public const string Text = "text";

        /// <summary>
        /// JSON mode - guarantees the output is valid JSON.
        /// You MUST also instruct the model to produce JSON in your prompt.
        /// </summary>
        public const string JsonObject = "json_object";

        /// <summary>
        /// JSON Schema mode - guarantees output follows a specific schema.
        /// </summary>
        public const string JsonSchema = "json_schema";
    }

    /// <summary>
    /// Represents a JSON schema for structured output.
    /// </summary>
    public class JsonSchema
    {
        /// <summary>
        /// Gets or sets the name of the schema.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the schema.
        /// </summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets whether strict mode is enabled.
        /// </summary>
        [JsonPropertyName("strict")]
        public bool Strict { get; set; } = true;

        /// <summary>
        /// Gets or sets the schema definition as a dictionary.
        /// </summary>
        [JsonPropertyName("schema")]
        public Dictionary<string, object> Schema { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a message in a chat conversation.
    /// </summary>
    public class MessageRequest
    {
        /// <summary>
        /// Gets or sets the role of the message sender.
        /// Valid values are: "system", "user", "assistant", "tool".
        /// </summary>
        /// <example>user, assistant, system, tool</example>
        [JsonPropertyName("role")]
        [Required]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content of the message as a string.
        /// For structured content (reasoning), use <see cref="ContentChunks"/>.
        /// </summary>
        [JsonIgnore]
        public string Content
        {
            get => MessageContentExtensions.GetContentText(_contentRaw) ?? string.Empty;
            set => _contentRaw = value;
        }

        /// <summary>
        /// Gets or sets structured content chunks (for reasoning models).
        /// When set, this is serialized as "content" instead of <see cref="Content"/>.
        /// </summary>
        [JsonIgnore]
        public List<ContentChunk>? ContentChunks
        {
            get => _contentRaw as List<ContentChunk>;
            set => _contentRaw = value;
        }

        /// <summary>Raw content for JSON serialization. Use <see cref="Content"/> or <see cref="ContentChunks"/> in code.</summary>
        [JsonPropertyName("content")]
        [JsonConverter(typeof(MessageContentConverter))]
        public object? ContentRaw { get => _contentRaw; set => _contentRaw = value; }

        private object? _contentRaw;

        /// <summary>
        /// Gets or sets whether this message is a prefix.
        /// When true, enables prepending content to the assistant's response.
        /// Only valid for assistant role messages.
        /// </summary>
        [JsonPropertyName("prefix")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Prefix { get; set; } = false;

        /// <summary>
        /// Gets or sets the tool call ID (for tool role messages).
        /// Used when providing function call results back to the model.
        /// </summary>
        [JsonPropertyName("tool_call_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ToolCallId { get; set; }

        /// <summary>
        /// Gets or sets the name of the tool (for tool role messages).
        /// </summary>
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }

        /// <summary>
        /// Validates the message parameters.
        /// </summary>
        /// <returns>True if the message is valid; otherwise, false.</returns>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Role))
                return false;

            if (!IsValidRole(Role))
                return false;

            // Tool messages need tool_call_id
            if (Role.Equals(MessageRoles.Tool, StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrWhiteSpace(ToolCallId);

            // Other messages need content (string or chunks)
            return HasContent();
        }

        /// <summary>
        /// Checks if the role is valid.
        /// </summary>
        /// <param name="role">The role to validate.</param>
        /// <returns>True if the role is valid; otherwise, false.</returns>
        private static bool IsValidRole(string role)
        {
            return role.Equals(MessageRoles.System, StringComparison.OrdinalIgnoreCase) ||
                   role.Equals(MessageRoles.User, StringComparison.OrdinalIgnoreCase) ||
                   role.Equals(MessageRoles.Assistant, StringComparison.OrdinalIgnoreCase) ||
                   role.Equals(MessageRoles.Tool, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Returns true if the message has content (string or chunks).</summary>
        private bool HasContent()
        {
            if (_contentRaw is string s) return !string.IsNullOrWhiteSpace(s);
            if (_contentRaw is List<ContentChunk> chunks) return chunks != null && chunks.Count > 0;
            return false;
        }

        #region Static Factory Methods

        /// <summary>
        /// Creates a system message.
        /// </summary>
        /// <param name="content">The system instruction content.</param>
        /// <returns>A new MessageRequest with system role.</returns>
        public static MessageRequest System(string content) => new() { Role = MessageRoles.System, Content = content };

        /// <summary>
        /// Creates a system message with structured content (e.g. for reasoning).
        /// </summary>
        public static MessageRequest SystemWithChunks(List<ContentChunk> chunks) => new() { Role = MessageRoles.System, ContentChunks = chunks };

        /// <summary>
        /// Creates a user message.
        /// </summary>
        /// <param name="content">The user message content.</param>
        /// <returns>A new MessageRequest with user role.</returns>
        public static MessageRequest User(string content) => new() { Role = MessageRoles.User, Content = content };

        /// <summary>
        /// Creates an assistant message.
        /// </summary>
        /// <param name="content">The assistant message content.</param>
        /// <param name="prefix">Whether this is a prefix message.</param>
        /// <returns>A new MessageRequest with assistant role.</returns>
        public static MessageRequest Assistant(string content, bool prefix = false) => new() { Role = MessageRoles.Assistant, Content = content, Prefix = prefix };

        /// <summary>
        /// Creates a tool result message.
        /// </summary>
        /// <param name="toolCallId">The ID of the tool call this is responding to.</param>
        /// <param name="content">The tool result content.</param>
        /// <param name="name">Optional name of the tool.</param>
        /// <returns>A new MessageRequest with tool role.</returns>
        public static MessageRequest Tool(string toolCallId, string content, string? name = null) => new() 
        { 
            Role = MessageRoles.Tool, 
            ToolCallId = toolCallId,
            Content = content,
            Name = name
        };

        #endregion
    }

    /// <summary>
    /// Provides constants for commonly used model names.
    /// </summary>
    public static class MistralModels
    {
        #region Premier Models

        /// <summary>
        /// Mistral Large - flagship model, top-tier reasoning for complex tasks.
        /// </summary>
        public const string Large = "mistral-large-latest";

        /// <summary>
        /// Pixtral Large - multimodal model with image understanding.
        /// </summary>
        public const string PixtralLarge = "pixtral-large-latest";

        /// <summary>
        /// Mistral Saba - optimized for Middle Eastern and South Asian languages.
        /// </summary>
        public const string Saba = "mistral-saba-latest";

        #endregion

        #region Free Models

        /// <summary>
        /// Pixtral 12B - free tier multimodal model.
        /// </summary>
        public const string Pixtral = "pixtral-12b-2409";

        #endregion

        #region Specialized Models

        /// <summary>
        /// Codestral - optimized for code generation and understanding.
        /// </summary>
        public const string Codestral = "codestral-latest";

        /// <summary>
        /// Mistral Embed - text embedding model.
        /// </summary>
        public const string Embed = "mistral-embed";

        /// <summary>
        /// Mistral Moderation - content moderation model.
        /// </summary>
        public const string Moderation = "mistral-moderation-latest";

        #endregion

        #region Efficient Models

        /// <summary>
        /// Ministral 3B - ultra-efficient small model.
        /// </summary>
        public const string Ministral3B = "ministral-3b-latest";

        /// <summary>
        /// Ministral 8B - efficient model for edge deployment.
        /// </summary>
        public const string Ministral8B = "ministral-8b-latest";

        /// <summary>
        /// Mistral Small - good balance of speed and quality.
        /// </summary>
        public const string Small = "mistral-small-latest";

        #endregion

        #region Legacy Models

        /// <summary>
        /// Mistral Tiny - legacy model, fastest and most cost-effective.
        /// </summary>
        [Obsolete("Consider using Ministral3B or Small for better performance.")]
        public const string Tiny = "mistral-tiny";

        /// <summary>
        /// Mistral Medium - legacy model.
        /// </summary>
        [Obsolete("Consider using Small or Large for better performance.")]
        public const string Medium = "mistral-medium-latest";

        #endregion

        #region Research Models

        /// <summary>
        /// Mistral Nemo - research model for specific use cases.
        /// </summary>
        public const string Nemo = "open-mistral-nemo";

        #endregion

        #region Reasoning Models (Magistral)

        /// <summary>
        /// Magistral Small - reasoning model for efficient chain-of-thought.
        /// </summary>
        public const string MagistralSmall = "magistral-small-latest";

        /// <summary>
        /// Magistral Medium - reasoning model balancing performance and cost.
        /// </summary>
        public const string MagistralMedium = "magistral-medium-latest";

        #endregion
    }

    /// <summary>
    /// Provides constants for message roles.
    /// </summary>
    public static class MessageRoles
    {
        /// <summary>
        /// System role - used for system instructions and context.
        /// Sets the behavior and context for the assistant.
        /// </summary>
        public const string System = "system";

        /// <summary>
        /// User role - represents messages from the user.
        /// Provides requests, questions, or instructions to the assistant.
        /// </summary>
        public const string User = "user";

        /// <summary>
        /// Assistant role - represents responses from the AI assistant.
        /// Can be used at conversation start or to provide context.
        /// </summary>
        public const string Assistant = "assistant";

        /// <summary>
        /// Tool role - used for function calling results.
        /// Provides the output of a tool call back to the model.
        /// </summary>
        public const string Tool = "tool";
    }

    /// <summary>
    /// Constants for prompt mode values.
    /// </summary>
    public static class PromptModes
    {
        /// <summary>
        /// Reasoning mode - enhanced reasoning capabilities with system prompt including
        /// knowledge cutoff date, model capabilities, tone, and safety guidelines.
        /// </summary>
        public const string Reasoning = "reasoning";
    }
}
