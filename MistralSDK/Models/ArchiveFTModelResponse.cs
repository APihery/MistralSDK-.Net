using System.Text.Json.Serialization;

namespace MistralSDK.Models
{
    /// <summary>
    /// Response from POST /v1/fine_tuning/models/{model_id}/archive.
    /// </summary>
    public class ArchiveFTModelResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public string Object { get; set; } = "model";

        [JsonPropertyName("archived")]
        public bool Archived { get; set; } = true;
    }

    /// <summary>
    /// Response from DELETE /v1/fine_tuning/models/{model_id}/archive.
    /// </summary>
    public class UnarchiveFTModelResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public string Object { get; set; } = "model";

        [JsonPropertyName("archived")]
        public bool Archived { get; set; } = false;
    }
}
