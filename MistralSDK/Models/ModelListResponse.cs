using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MistralSDK.Models
{
    /// <summary>
    /// Response from GET /v1/models - list of available models.
    /// </summary>
    public class ModelListResponse
    {
        [JsonPropertyName("object")]
        public string Object { get; set; } = "list";

        [JsonPropertyName("data")]
        public List<ModelCard> Data { get; set; } = new();
    }
}
