using MistralSDK.ChatCompletion;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MistralSDK.Agents
{
    /// <summary>
    /// Request for agent completion from the Mistral AI API.
    /// Uses an agent ID instead of a model. See <see href="https://docs.mistral.ai/api/endpoint/agents"/>.
    /// </summary>
    public class AgentCompletionRequest
    {
        /// <summary>
        /// The ID of the agent to use for this completion.
        /// </summary>
        [JsonPropertyName("agent_id")]
        public string AgentId { get; set; } = string.Empty;

        /// <summary>
        /// The prompt(s) to generate completions for, encoded as a list of messages with role and content.
        /// </summary>
        [JsonPropertyName("messages")]
        public List<MessageRequest> Messages { get; set; } = new List<MessageRequest>();

        /// <summary>
        /// Maximum number of tokens to generate. Default varies by agent.
        /// </summary>
        [JsonPropertyName("max_tokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Whether to stream the response. Default is false.
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        /// <summary>
        /// Stop generation if this token (or one of these tokens) is detected.
        /// </summary>
        [JsonPropertyName("stop")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Stop { get; set; }

        /// <summary>
        /// Random seed for deterministic results.
        /// </summary>
        [JsonPropertyName("random_seed")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? RandomSeed { get; set; }

        /// <summary>
        /// Optional metadata to attach to the request.
        /// </summary>
        [JsonPropertyName("metadata")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Response format (text, json_object, json_schema).
        /// </summary>
        [JsonPropertyName("response_format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ResponseFormat? ResponseFormat { get; set; }

        /// <summary>
        /// Tools the agent may call.
        /// </summary>
        [JsonPropertyName("tools")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<object>? Tools { get; set; }

        /// <summary>
        /// Tool choice: "auto", "none", "required", or a specific tool.
        /// </summary>
        [JsonPropertyName("tool_choice")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? ToolChoice { get; set; }

        /// <summary>
        /// Presence penalty (0 to 2). Default 0.
        /// </summary>
        [JsonPropertyName("presence_penalty")]
        public double PresencePenalty { get; set; } = 0;

        /// <summary>
        /// Frequency penalty (0 to 2). Default 0.
        /// </summary>
        [JsonPropertyName("frequency_penalty")]
        public double FrequencyPenalty { get; set; } = 0;

        /// <summary>
        /// Number of completions to return. Default 1.
        /// </summary>
        [JsonPropertyName("n")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? N { get; set; }

        /// <summary>
        /// Whether to allow parallel tool calls. Default true.
        /// </summary>
        [JsonPropertyName("parallel_tool_calls")]
        public bool ParallelToolCalls { get; set; } = true;

        /// <summary>
        /// Prompt mode. Use "reasoning" for reasoning models.
        /// </summary>
        [JsonPropertyName("prompt_mode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PromptMode { get; set; }
    }
}
