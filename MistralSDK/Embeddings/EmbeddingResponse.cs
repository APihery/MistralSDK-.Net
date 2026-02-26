using MistralSDK.ChatCompletion;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MistralSDK.Embeddings
{
    /// <summary>
    /// Response from the Embeddings API.
    /// </summary>
    public class EmbeddingResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public string Object { get; set; } = "list";

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public List<EmbeddingData> Data { get; set; } = new();

        [JsonPropertyName("usage")]
        public UsageInfo? Usage { get; set; }

        /// <summary>Gets the first embedding vector, or empty list if none.</summary>
        public IReadOnlyList<double> GetFirstVector() => Data?.Count > 0 ? Data[0].Embedding : new List<double>();

        /// <summary>Gets the embedding vector at the specified index.</summary>
        public IReadOnlyList<double>? GetVector(int index) => Data?.Count > index ? Data[index].Embedding : null;
    }

    /// <summary>
    /// A single embedding result.
    /// </summary>
    public class EmbeddingData
    {
        [JsonPropertyName("object")]
        public string Object { get; set; } = "embedding";

        [JsonPropertyName("embedding")]
        public List<double> Embedding { get; set; } = new();

        [JsonPropertyName("index")]
        public int Index { get; set; }
    }
}
