using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MistralSDK.Fim
{
    /// <summary>
    /// Request for Fill-in-the-Middle (FIM) completion.
    /// Used for code completion with Codestral. See <see href="https://docs.mistral.ai/api/endpoint/fim"/>.
    /// </summary>
    public class FimCompletionRequest
    {
        /// <summary>
        /// Model with FIM support (e.g. codestral-latest).
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = FimModels.CodestralLatest;

        /// <summary>
        /// The text/code to complete (prefix).
        /// </summary>
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Optional suffix. When given prompt and suffix, the model fills what is between them.
        /// </summary>
        [JsonPropertyName("suffix")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Suffix { get; set; }

        /// <summary>
        /// Sampling temperature (0-1.5). Default varies by model.
        /// </summary>
        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Temperature { get; set; }

        /// <summary>
        /// Top-p nucleus sampling (0-1). Default 1.0.
        /// </summary>
        [JsonPropertyName("top_p")]
        public double TopP { get; set; } = 1.0;

        /// <summary>
        /// Maximum tokens to generate.
        /// </summary>
        [JsonPropertyName("max_tokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Minimum tokens to generate.
        /// </summary>
        [JsonPropertyName("min_tokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MinTokens { get; set; }

        /// <summary>
        /// Whether to stream the response. Default false.
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        /// <summary>
        /// Stop sequences.
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
        /// Optional metadata.
        /// </summary>
        [JsonPropertyName("metadata")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object>? Metadata { get; set; }

        #region Fluent Builder Methods

        /// <summary>Sets the suffix for fill-in-the-middle.</summary>
        public FimCompletionRequest WithSuffix(string suffix) { Suffix = suffix; return this; }

        /// <summary>Sets the maximum tokens to generate.</summary>
        public FimCompletionRequest WithMaxTokens(int maxTokens) { MaxTokens = maxTokens; return this; }

        /// <summary>Sets a single stop sequence.</summary>
        public FimCompletionRequest WithStop(string stop) { Stop = stop; return this; }

        /// <summary>Sets multiple stop sequences.</summary>
        public FimCompletionRequest WithStops(params string[] stops) { Stop = stops; return this; }

        #endregion
    }

    /// <summary>
    /// FIM model constants.
    /// </summary>
    public static class FimModels
    {
        public const string CodestralLatest = "codestral-latest";
        public const string Codestral2404 = "codestral-2404";
        public const string Codestral2405 = "codestral-2405";
    }
}
