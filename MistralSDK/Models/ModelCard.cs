using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MistralSDK.Models
{
    /// <summary>
    /// Represents a Mistral model (base or fine-tuned).
    /// </summary>
    public class ModelCard
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public string Object { get; set; } = "model";

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("owned_by")]
        public string OwnedBy { get; set; } = "mistralai";

        [JsonPropertyName("capabilities")]
        public ModelCapabilities Capabilities { get; set; } = new();

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("max_context_length")]
        public int MaxContextLength { get; set; } = 32768;

        [JsonPropertyName("aliases")]
        public List<string> Aliases { get; set; } = new();

        [JsonPropertyName("deprecation")]
        public string? Deprecation { get; set; }

        [JsonPropertyName("deprecation_replacement_model")]
        public string? DeprecationReplacementModel { get; set; }

        [JsonPropertyName("default_model_temperature")]
        public double? DefaultModelTemperature { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "base";

        /// <summary>Fine-tuned model: job ID.</summary>
        [JsonPropertyName("job")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Job { get; set; }

        /// <summary>Fine-tuned model: root model.</summary>
        [JsonPropertyName("root")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Root { get; set; }

        /// <summary>Fine-tuned model: archived status.</summary>
        [JsonPropertyName("archived")]
        public bool Archived { get; set; }
    }
}
