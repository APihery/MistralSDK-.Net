using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MistralSDK.Embeddings
{
    /// <summary>
    /// Request for the Mistral Embeddings API.
    /// See <see href="https://docs.mistral.ai/api/endpoint/embeddings"/>.
    /// </summary>
    public class EmbeddingRequest
    {
        /// <summary>
        /// The embedding model to use (e.g. mistral-embed, codestral-embed).
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = EmbeddingModels.MistralEmbed;

        /// <summary>
        /// Text to embed. Can be a single string or array of strings for batch processing.
        /// </summary>
        [JsonPropertyName("input")]
        public object Input { get; set; } = string.Empty;

        /// <summary>
        /// Output format: "float" or "base64". Default "float".
        /// </summary>
        [JsonPropertyName("encoding_format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EncodingFormat { get; set; }

        /// <summary>
        /// Output dimension (codestral-embed: default 1536, max 3072).
        /// </summary>
        [JsonPropertyName("output_dimension")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? OutputDimension { get; set; }

        /// <summary>
        /// Output dtype: "float", "int8", "uint8", "binary", "ubinary". Default "float".
        /// </summary>
        [JsonPropertyName("output_dtype")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? OutputDtype { get; set; }

        /// <summary>
        /// Optional metadata.
        /// </summary>
        [JsonPropertyName("metadata")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object>? Metadata { get; set; }

        #region Fluent Builder Methods

        /// <summary>Sets the output dimension (codestral-embed).</summary>
        public EmbeddingRequest WithDimension(int dimension) { OutputDimension = dimension; return this; }

        /// <summary>Configures for code embeddings (codestral-embed).</summary>
        public EmbeddingRequest ForCode() { Model = EmbeddingModels.CodestralEmbed; return this; }

        /// <summary>Sets the output dtype (float, int8, uint8, etc.).</summary>
        public EmbeddingRequest WithOutputDtype(string dtype) { OutputDtype = dtype; return this; }

        #endregion
    }

    /// <summary>
    /// Embedding model constants.
    /// </summary>
    public static class EmbeddingModels
    {
        /// <summary>Text embeddings - general purpose.</summary>
        public const string MistralEmbed = "mistral-embed";

        /// <summary>Code embeddings - for code search and retrieval.</summary>
        public const string CodestralEmbed = "codestral-embed";
    }

    /// <summary>
    /// Output format for embeddings.
    /// </summary>
    public static class EmbeddingEncodingFormat
    {
        public const string Float = "float";
        public const string Base64 = "base64";
    }

    /// <summary>
    /// Output dtype for embeddings (codestral-embed).
    /// </summary>
    public static class EmbeddingDtype
    {
        public const string Float = "float";
        public const string Int8 = "int8";
        public const string UInt8 = "uint8";
        public const string Binary = "binary";
        public const string UBinary = "ubinary";
    }
}
