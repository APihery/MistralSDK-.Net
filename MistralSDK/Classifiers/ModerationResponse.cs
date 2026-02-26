using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MistralSDK.Classifiers
{
    /// <summary>
    /// Response from moderation endpoints (POST /v1/moderations, POST /v1/chat/moderations).
    /// </summary>
    public class ModerationResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("results")]
        public List<ModerationResult> Results { get; set; } = new();
    }

    /// <summary>
    /// Moderation result for a single input.
    /// </summary>
    public class ModerationResult
    {
        /// <summary>
        /// Category flags (sexual, hate_and_discrimination, violence_and_threats, etc.).
        /// </summary>
        [JsonPropertyName("categories")]
        public Dictionary<string, bool> Categories { get; set; } = new();

        /// <summary>
        /// Category scores (0-1).
        /// </summary>
        [JsonPropertyName("category_scores")]
        public Dictionary<string, double> CategoryScores { get; set; } = new();
    }

    /// <summary>
    /// Response from classification endpoints (POST /v1/classifications, POST /v1/chat/classifications).
    /// </summary>
    public class ClassificationResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Classification results. Each item is a map of target name to scores.
        /// </summary>
        [JsonPropertyName("results")]
        public List<Dictionary<string, ClassificationTargetResult>> Results { get; set; } = new();
    }

    /// <summary>
    /// Classification target result with scores.
    /// </summary>
    public class ClassificationTargetResult
    {
        [JsonPropertyName("scores")]
        public Dictionary<string, double> Scores { get; set; } = new();
    }
}
