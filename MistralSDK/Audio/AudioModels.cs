using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace MistralSDK.Audio
{
    #region Request

    /// <summary>
    /// Request for audio transcription.
    /// Provide audio via stream (FromStream), file ID (FromFileId), or URL (FromFileUrl).
    /// </summary>
    public class AudioTranscriptionRequest
    {
        /// <summary>Model to use. Default is voxtral-mini-latest.</summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = AudioModels.VoxtralMiniLatest;

        /// <summary>Language of the audio (e.g. "en"). Improves accuracy when known.</summary>
        [JsonPropertyName("language")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Language { get; set; }

        /// <summary>Temperature for sampling. Null uses model default.</summary>
        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Temperature { get; set; }

        /// <summary>Enable speaker diarization (who said what).</summary>
        [JsonPropertyName("diarize")]
        public bool Diarize { get; set; }

        /// <summary>Context bias: up to 100 words/phrases to guide spelling of names, terms.</summary>
        [JsonPropertyName("context_bias")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? ContextBias { get; set; }

        /// <summary>Timestamp granularities: "segment" and/or "word".</summary>
        [JsonPropertyName("timestamp_granularities")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? TimestampGranularities { get; set; }

        /// <summary>Audio stream (when using FromStream). Internal use.</summary>
        [JsonIgnore]
        internal Stream? AudioStream { get; set; }

        /// <summary>File name for stream upload. Internal use.</summary>
        [JsonIgnore]
        internal string? FileName { get; set; }

        /// <summary>File ID from Files API. Set when using FromFileId.</summary>
        [JsonIgnore]
        internal string? FileId { get; set; }

        /// <summary>URL of audio file. Set when using FromFileUrl.</summary>
        [JsonIgnore]
        internal string? FileUrl { get; set; }
    }

    /// <summary>
    /// Factory for creating AudioTranscriptionRequest with different audio sources.
    /// </summary>
    public static class AudioTranscriptionRequestBuilder
    {
        /// <summary>Create request with audio from a stream.</summary>
        public static AudioTranscriptionRequest FromStream(Stream audioStream, string fileName, string model = AudioModels.VoxtralMiniLatest)
        {
            if (audioStream == null)
                throw new ArgumentNullException(nameof(audioStream));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name is required.", nameof(fileName));
            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Model is required.", nameof(model));
            ValidateAudioFileName(fileName);
            return new AudioTranscriptionRequest { AudioStream = audioStream, FileName = fileName, Model = model };
        }

        /// <summary>Create request with audio from an uploaded file ID.</summary>
        public static AudioTranscriptionRequest FromFileId(string fileId, string model = AudioModels.VoxtralMiniLatest)
        {
            if (string.IsNullOrWhiteSpace(fileId))
                throw new ArgumentException("File ID is required.", nameof(fileId));
            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Model is required.", nameof(model));
            return new AudioTranscriptionRequest { FileId = fileId, Model = model };
        }

        /// <summary>Create request with audio from a URL.</summary>
        public static AudioTranscriptionRequest FromFileUrl(string fileUrl, string model = AudioModels.VoxtralMiniLatest)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                throw new ArgumentException("File URL is required.", nameof(fileUrl));
            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Model is required.", nameof(model));
            if (fileUrl.Length > 2083)
                throw new ArgumentException("File URL must not exceed 2083 characters.", nameof(fileUrl));
            if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri) || !uri.IsAbsoluteUri)
                throw new ArgumentException("File URL must be a valid absolute URL.", nameof(fileUrl));
            if (!uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) &&
                !uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("File URL must use http or https protocol.", nameof(fileUrl));
            return new AudioTranscriptionRequest { FileUrl = fileUrl, Model = model };
        }

        private static void ValidateAudioFileName(string fileName)
        {
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            if (fileName.IndexOfAny(invalidChars) >= 0)
                throw new ArgumentException($"File name contains invalid characters.", nameof(fileName));
            if (fileName.Length > 255)
                throw new ArgumentException("File name must not exceed 255 characters.", nameof(fileName));
        }
    }

    /// <summary>Audio model constants.</summary>
    public static class AudioModels
    {
        /// <summary>Voxtral Mini for transcription (latest).</summary>
        public const string VoxtralMiniLatest = "voxtral-mini-latest";

        /// <summary>Voxtral Mini specific version for transcription.</summary>
        public const string VoxtralMini2507 = "voxtral-mini-2507";

        /// <summary>Voxtral Mini Transcribe.</summary>
        public const string VoxtralMini2602 = "voxtral-mini-2602";

        /// <summary>Voxtral Small for chat with audio.</summary>
        public const string VoxtralSmallLatest = "voxtral-small-latest";
    }

    /// <summary>Timestamp granularity for transcription.</summary>
    public static class TimestampGranularity
    {
        public const string Segment = "segment";
        public const string Word = "word";
    }

    #endregion

    #region Response

    /// <summary>
    /// Response from the audio transcription API.
    /// </summary>
    public class TranscriptionResponse
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("segments")]
        public List<TranscriptionSegmentChunk> Segments { get; set; } = new();

        [JsonPropertyName("usage")]
        public TranscriptionUsageInfo? Usage { get; set; }
    }

    /// <summary>Single segment in a transcription (with optional timestamps).</summary>
    public class TranscriptionSegmentChunk
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("start")]
        public double Start { get; set; }

        [JsonPropertyName("end")]
        public double End { get; set; }

        [JsonPropertyName("score")]
        public double? Score { get; set; }

        [JsonPropertyName("speaker_id")]
        public string? SpeakerId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "transcription_segment";
    }

    /// <summary>Usage information for transcription.</summary>
    public class TranscriptionUsageInfo
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }

        [JsonPropertyName("prompt_audio_seconds")]
        public int? PromptAudioSeconds { get; set; }
    }

    #endregion

    #region Streaming

    /// <summary>Base type for transcription stream events.</summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(TranscriptionStreamTextDelta), "transcription.text.delta")]
    [JsonDerivedType(typeof(TranscriptionStreamLanguage), "transcription.language")]
    [JsonDerivedType(typeof(TranscriptionStreamSegmentDelta), "transcription.segment")]
    [JsonDerivedType(typeof(TranscriptionStreamDone), "transcription.done")]
    public abstract class TranscriptionStreamEvent
    {
        [JsonPropertyName("type")]
        public abstract string Type { get; }
    }

    /// <summary>Text delta in streaming transcription.</summary>
    public class TranscriptionStreamTextDelta : TranscriptionStreamEvent
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        public override string Type => "transcription.text.delta";
    }

    /// <summary>Detected language in streaming.</summary>
    public class TranscriptionStreamLanguage : TranscriptionStreamEvent
    {
        [JsonPropertyName("audio_language")]
        public string AudioLanguage { get; set; } = string.Empty;

        public override string Type => "transcription.language";
    }

    /// <summary>Segment with timestamps in streaming.</summary>
    public class TranscriptionStreamSegmentDelta : TranscriptionStreamEvent
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("start")]
        public double Start { get; set; }

        [JsonPropertyName("end")]
        public double End { get; set; }

        [JsonPropertyName("speaker_id")]
        public string? SpeakerId { get; set; }

        public override string Type => "transcription.segment";
    }

    /// <summary>Final event when streaming transcription is done.</summary>
    public class TranscriptionStreamDone : TranscriptionStreamEvent
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("segments")]
        public List<TranscriptionSegmentChunk> Segments { get; set; } = new();

        [JsonPropertyName("usage")]
        public TranscriptionUsageInfo? Usage { get; set; }

        public override string Type => "transcription.done";
    }

    /// <summary>SSE event wrapper for transcription stream.</summary>
    public class TranscriptionStreamEvents
    {
        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public TranscriptionStreamEvent? Data { get; set; }
    }

    #endregion
}
