using System.Text.Json.Serialization;

namespace MistralSDK.Models
{
    /// <summary>
    /// Capabilities of a Mistral model.
    /// </summary>
    public class ModelCapabilities
    {
        [JsonPropertyName("completion_chat")]
        public bool CompletionChat { get; set; }

        [JsonPropertyName("completion_fim")]
        public bool CompletionFim { get; set; }

        [JsonPropertyName("function_calling")]
        public bool FunctionCalling { get; set; }

        [JsonPropertyName("fine_tuning")]
        public bool FineTuning { get; set; }

        [JsonPropertyName("vision")]
        public bool Vision { get; set; }

        [JsonPropertyName("ocr")]
        public bool Ocr { get; set; }

        [JsonPropertyName("classification")]
        public bool Classification { get; set; }

        [JsonPropertyName("moderation")]
        public bool Moderation { get; set; }

        [JsonPropertyName("audio")]
        public bool Audio { get; set; }

        [JsonPropertyName("audio_transcription")]
        public bool AudioTranscription { get; set; }
    }
}
