using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MistralSDK.ChatCompletion
{
    /// <summary>
    /// Represents a streaming chunk from the Mistral AI API.
    /// </summary>
    public class StreamingChatCompletionChunk
    {
        /// <summary>
        /// Gets or sets the unique identifier for the completion.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the object type. Always "chat.completion.chunk" for streaming.
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model identifier used for the completion.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the chunk was created (Unix timestamp).
        /// </summary>
        [JsonPropertyName("created")]
        public long Created { get; set; }

        /// <summary>
        /// Gets or sets the list of delta choices in this chunk.
        /// </summary>
        [JsonPropertyName("choices")]
        public List<StreamingChoice> Choices { get; set; } = new List<StreamingChoice>();

        /// <summary>
        /// Gets or sets the usage information (only in the final chunk).
        /// </summary>
        [JsonPropertyName("usage")]
        public UsageInfo? Usage { get; set; }

        /// <summary>
        /// Gets the content delta from the first choice, if available.
        /// </summary>
        /// <returns>The content delta or empty string.</returns>
        public string GetContent()
        {
            return Choices?.Count > 0 ? Choices[0].Delta?.Content ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Gets a value indicating whether this is the final chunk.
        /// </summary>
        public bool IsComplete => Choices?.Count > 0 && Choices[0].FinishReason != null;
    }

    /// <summary>
    /// Represents a choice in a streaming response.
    /// </summary>
    public class StreamingChoice
    {
        /// <summary>
        /// Gets or sets the index of this choice.
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the delta content for this choice.
        /// </summary>
        [JsonPropertyName("delta")]
        public DeltaMessage? Delta { get; set; }

        /// <summary>
        /// Gets or sets the reason why generation finished.
        /// </summary>
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    /// <summary>
    /// Represents a delta message in streaming responses.
    /// Contains only the new content since the last chunk.
    /// </summary>
    public class DeltaMessage
    {
        /// <summary>
        /// Gets or sets the role of the message sender.
        /// Usually only present in the first chunk.
        /// </summary>
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        /// <summary>
        /// Gets or sets the content delta.
        /// This is the new text since the last chunk.
        /// </summary>
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        /// <summary>
        /// Gets or sets the tool calls in this delta.
        /// </summary>
        [JsonPropertyName("tool_calls")]
        public List<ToolCall>? ToolCalls { get; set; }
    }

    /// <summary>
    /// Represents a complete streaming response after all chunks have been received.
    /// </summary>
    public class StreamingChatCompletionResult
    {
        /// <summary>
        /// Gets or sets the unique identifier for the completion.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model identifier used for the completion.
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the complete accumulated content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the reason why generation finished.
        /// </summary>
        public string? FinishReason { get; set; }

        /// <summary>
        /// Gets or sets the usage information.
        /// </summary>
        public UsageInfo? Usage { get; set; }

        /// <summary>
        /// Gets or sets the list of all chunks received.
        /// </summary>
        public List<StreamingChatCompletionChunk> Chunks { get; set; } = new List<StreamingChatCompletionChunk>();

        /// <summary>
        /// Gets the number of chunks received.
        /// </summary>
        public int ChunkCount => Chunks.Count;
    }
}
