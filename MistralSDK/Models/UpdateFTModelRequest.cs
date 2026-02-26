using System.Text.Json.Serialization;

namespace MistralSDK.Models
{
    /// <summary>
    /// Request body for PATCH /v1/fine_tuning/models/{model_id}.
    /// </summary>
    public class UpdateFTModelRequest
    {
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }
    }
}
