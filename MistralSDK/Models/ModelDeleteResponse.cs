using System.Text.Json.Serialization;

namespace MistralSDK.Models
{
    /// <summary>
    /// Response from DELETE /v1/models/{model_id}.
    /// </summary>
    public class ModelDeleteResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public string Object { get; set; } = "model";

        [JsonPropertyName("deleted")]
        public bool Deleted { get; set; } = true;
    }
}
