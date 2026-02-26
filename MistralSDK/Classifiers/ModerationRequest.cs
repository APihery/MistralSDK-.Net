using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MistralSDK.Classifiers
{
    /// <summary>
    /// Request for moderation (POST /v1/moderations) or classification (POST /v1/classifications).
    /// Text to classify - single string or array of strings.
    /// </summary>
    public class ModerationRequest
    {
        /// <summary>
        /// Model to use (e.g. mistral-moderation-latest).
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = ModerationModels.MistralModerationLatest;

        /// <summary>
        /// Text to moderate. Single string or array of strings.
        /// </summary>
        [JsonPropertyName("input")]
        public object Input { get; set; } = string.Empty;

        /// <summary>
        /// Optional metadata.
        /// </summary>
        [JsonPropertyName("metadata")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Request for chat moderation (POST /v1/chat/moderations).
    /// </summary>
    public class ChatModerationRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = ModerationModels.MistralModerationLatest;

        /// <summary>
        /// Chat messages to moderate. Array of messages or array of arrays.
        /// </summary>
        [JsonPropertyName("input")]
        public object Input { get; set; } = new List<object>();
    }

    /// <summary>
    /// Request for chat classification (POST /v1/chat/classifications).
    /// </summary>
    public class ChatClassificationRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Chat to classify. InstructRequest (messages) or array of InstructRequest.
        /// </summary>
        [JsonPropertyName("input")]
        public object Input { get; set; } = new object();
    }

    /// <summary>
    /// Instruct request - messages for chat classification.
    /// </summary>
    public class InstructRequest
    {
        [JsonPropertyName("messages")]
        public List<MistralSDK.ChatCompletion.MessageRequest> Messages { get; set; } = new();
    }

    /// <summary>
    /// Moderation model constants.
    /// </summary>
    public static class ModerationModels
    {
        public const string MistralModerationLatest = "mistral-moderation-latest";
    }
}
