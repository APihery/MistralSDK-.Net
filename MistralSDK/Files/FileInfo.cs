using System.Text.Json.Serialization;

namespace MistralSDK.Files
{
    /// <summary>
    /// Represents a file in the Mistral AI Files API.
    /// </summary>
    public class MistralFileInfo
    {
        /// <summary>Unique identifier of the file.</summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>Object type, always "file".</summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = "file";

        /// <summary>Size of the file in bytes.</summary>
        [JsonPropertyName("bytes")]
        public int? Bytes { get; set; }

        /// <summary>UNIX timestamp when the file was created.</summary>
        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        /// <summary>Name of the uploaded file.</summary>
        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;

        /// <summary>Purpose of the file: "fine-tune", "batch", or "ocr".</summary>
        [JsonPropertyName("purpose")]
        public string Purpose { get; set; } = string.Empty;

        /// <summary>Sample type for batch files.</summary>
        [JsonPropertyName("sample_type")]
        public string? SampleType { get; set; }

        /// <summary>Source: "upload", "repository", or "mistral".</summary>
        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        /// <summary>Whether the file has been deleted.</summary>
        [JsonPropertyName("deleted")]
        public bool? Deleted { get; set; }

        /// <summary>Number of lines (for JSONL files).</summary>
        [JsonPropertyName("num_lines")]
        public int? NumLines { get; set; }

        /// <summary>MIME type of the file.</summary>
        [JsonPropertyName("mimetype")]
        public string? Mimetype { get; set; }

        /// <summary>File signature.</summary>
        [JsonPropertyName("signature")]
        public string? Signature { get; set; }
    }

    /// <summary>
    /// Response from listing files.
    /// </summary>
    public class FileListResponse
    {
        /// <summary>List of files.</summary>
        [JsonPropertyName("data")]
        public List<MistralFileInfo> Data { get; set; } = new();

        /// <summary>Object type, always "list".</summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = "list";

        /// <summary>Total number of files.</summary>
        [JsonPropertyName("total")]
        public int? Total { get; set; }
    }

    /// <summary>
    /// Response from deleting a file.
    /// </summary>
    public class FileDeleteResponse
    {
        /// <summary>ID of the deleted file.</summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>Object type.</summary>
        [JsonPropertyName("object")]
        public string Object { get; set; } = "file";

        /// <summary>Whether the file was deleted.</summary>
        [JsonPropertyName("deleted")]
        public bool Deleted { get; set; }
    }

    /// <summary>
    /// Response from getting a signed URL.
    /// </summary>
    public class FileSignedUrlResponse
    {
        /// <summary>The signed URL for downloading the file.</summary>
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    /// <summary>
    /// Purpose for uploaded files.
    /// </summary>
    public static class FilePurpose
    {
        /// <summary>For fine-tuning models.</summary>
        public const string FineTune = "fine-tune";

        /// <summary>For batch processing.</summary>
        public const string Batch = "batch";

        /// <summary>For OCR processing.</summary>
        public const string Ocr = "ocr";

        /// <summary>For audio transcription.</summary>
        public const string Audio = "audio";
    }
}
