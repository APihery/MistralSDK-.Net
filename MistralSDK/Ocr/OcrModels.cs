using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MistralSDK.ChatCompletion;

namespace MistralSDK.Ocr
{
    #region Document Input (Polymorphic)

    /// <summary>
    /// Base class for OCR document inputs.
    /// Use the static factory methods to create instances.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(OcrFileDocument), "file")]
    [JsonDerivedType(typeof(OcrDocumentUrlDocument), "document_url")]
    [JsonDerivedType(typeof(OcrImageUrlDocument), "image_url")]
    public abstract class OcrDocument
    {
        /// <summary>Creates a document input from an uploaded file ID.</summary>
        public static OcrDocument FromFileId(string fileId)
        {
            if (string.IsNullOrWhiteSpace(fileId))
                throw new ArgumentException("File ID is required.", nameof(fileId));
            return new OcrFileDocument { FileId = fileId };
        }

        /// <summary>Creates a document input from a PDF/document URL.</summary>
        public static OcrDocument FromDocumentUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Document URL is required.", nameof(url));
            return new OcrDocumentUrlDocument { DocumentUrl = url };
        }

        /// <summary>Creates a document input from an image URL (http/https or data URI).</summary>
        public static OcrDocument FromImageUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Image URL is required.", nameof(url));
            return new OcrImageUrlDocument { ImageUrl = new OcrImageUrl { Url = url } };
        }

        /// <summary>Creates a document input from base64-encoded image data.</summary>
        public static OcrDocument FromImageBase64(string base64Data, string mimeType = "image/jpeg")
        {
            if (string.IsNullOrWhiteSpace(base64Data))
                throw new ArgumentException("Base64 data is required.", nameof(base64Data));
            var url = $"data:{mimeType};base64,{base64Data}";
            return FromImageUrl(url);
        }
    }

    /// <summary>Document from an uploaded file ID.</summary>
    public class OcrFileDocument : OcrDocument
    {
        [JsonPropertyName("file_id")]
        public string FileId { get; set; } = string.Empty;
    }

    /// <summary>Document from a PDF/document URL.</summary>
    public class OcrDocumentUrlDocument : OcrDocument
    {
        [JsonPropertyName("document_url")]
        public string DocumentUrl { get; set; } = string.Empty;
    }

    /// <summary>Document from an image URL.</summary>
    public class OcrImageUrlDocument : OcrDocument
    {
        [JsonPropertyName("image_url")]
        public OcrImageUrl ImageUrl { get; set; } = new();
    }

    /// <summary>Image URL (http, https, or data URI).</summary>
    public class OcrImageUrl
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    #endregion

    #region OCR Request

    /// <summary>
    /// Request for the Mistral OCR API.
    /// </summary>
    public class OcrRequest
    {
        /// <summary>Document to process. Use OcrDocument.FromFileId/FromDocumentUrl/FromImageUrl.</summary>
        [JsonPropertyName("document")]
        public OcrDocument Document { get; set; } = null!;

        /// <summary>Model to use. Default is "mistral-ocr-latest".</summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = OcrModels.MistralOcrLatest;

        /// <summary>Table format: "markdown" or "html".</summary>
        [JsonPropertyName("table_format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TableFormat { get; set; }

        /// <summary>Extract header content separately.</summary>
        [JsonPropertyName("extract_header")]
        public bool ExtractHeader { get; set; }

        /// <summary>Extract footer content separately.</summary>
        [JsonPropertyName("extract_footer")]
        public bool ExtractFooter { get; set; }

        /// <summary>Include base64-encoded images in response.</summary>
        [JsonPropertyName("include_image_base64")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IncludeImageBase64 { get; set; }

        /// <summary>Max images to extract.</summary>
        [JsonPropertyName("image_limit")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ImageLimit { get; set; }

        /// <summary>Minimum size of images to extract (height/width in pixels).</summary>
        [JsonPropertyName("image_min_size")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ImageMinSize { get; set; }

        /// <summary>Specific pages to process (0-based).</summary>
        [JsonPropertyName("pages")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<int>? Pages { get; set; }

        /// <summary>Optional prompt for document annotation (structured extraction).</summary>
        [JsonPropertyName("document_annotation_prompt")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DocumentAnnotationPrompt { get; set; }

        /// <summary>Response format for document annotation.</summary>
        [JsonPropertyName("document_annotation_format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ResponseFormat? DocumentAnnotationFormat { get; set; }

        /// <summary>Response format for bbox annotation.</summary>
        [JsonPropertyName("bbox_annotation_format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ResponseFormat? BboxAnnotationFormat { get; set; }
    }

    /// <summary>
    /// OCR model constants.
    /// </summary>
    public static class OcrModels
    {
        /// <summary>Latest Mistral OCR model.</summary>
        public const string MistralOcrLatest = "mistral-ocr-latest";
    }

    /// <summary>
    /// Table format for OCR output.
    /// </summary>
    public static class OcrTableFormat
    {
        public const string Markdown = "markdown";
        public const string Html = "html";
    }

    #endregion

    #region OCR Response

    /// <summary>
    /// Response from the Mistral OCR API.
    /// </summary>
    public class OcrResponse
    {
        /// <summary>List of processed pages with extracted content.</summary>
        [JsonPropertyName("pages")]
        public List<OcrPage> Pages { get; set; } = new();

        /// <summary>Model used for OCR.</summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>Document-level annotation if requested.</summary>
        [JsonPropertyName("document_annotation")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DocumentAnnotation { get; set; }

        /// <summary>Usage information.</summary>
        [JsonPropertyName("usage_info")]
        public OcrUsageInfo? UsageInfo { get; set; }

        /// <summary>Combined markdown from all pages.</summary>
        public string GetAllMarkdown() => string.Join("\n\n", Pages.ConvertAll(p => p.Markdown ?? string.Empty));

        /// <summary>Full text content (strip markdown formatting for plain text).</summary>
        public string GetPlainText() => GetAllMarkdown();
    }

    /// <summary>
    /// OCR result for a single page.
    /// </summary>
    public class OcrPage
    {
        /// <summary>Page index (1-based).</summary>
        [JsonPropertyName("index")]
        public int Index { get; set; }

        /// <summary>Extracted content in markdown format.</summary>
        [JsonPropertyName("markdown")]
        public string? Markdown { get; set; }

        /// <summary>Extracted images with bounding boxes.</summary>
        [JsonPropertyName("images")]
        public List<OcrExtractedImage> Images { get; set; } = new();

        /// <summary>Extracted tables.</summary>
        [JsonPropertyName("tables")]
        public List<OcrExtractedTable>? Tables { get; set; }

        /// <summary>Hyperlinks detected in the page.</summary>
        [JsonPropertyName("hyperlinks")]
        public List<OcrHyperlink>? Hyperlinks { get; set; }

        /// <summary>Header content when extract_header is true.</summary>
        [JsonPropertyName("header")]
        public string? Header { get; set; }

        /// <summary>Footer content when extract_footer is true.</summary>
        [JsonPropertyName("footer")]
        public string? Footer { get; set; }

        /// <summary>Page dimensions.</summary>
        [JsonPropertyName("dimensions")]
        public OcrDimensions? Dimensions { get; set; }
    }

    /// <summary>
    /// Extracted image from a document page.
    /// </summary>
    public class OcrExtractedImage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("top_left_x")]
        public int TopLeftX { get; set; }

        [JsonPropertyName("top_left_y")]
        public int TopLeftY { get; set; }

        [JsonPropertyName("bottom_right_x")]
        public int BottomRightX { get; set; }

        [JsonPropertyName("bottom_right_y")]
        public int BottomRightY { get; set; }

        [JsonPropertyName("image_base64")]
        public string? ImageBase64 { get; set; }
    }

    /// <summary>
    /// Extracted table from a document page.
    /// </summary>
    public class OcrExtractedTable
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("format")]
        public string? Format { get; set; }
    }

    /// <summary>
    /// Hyperlink detected in a document.
    /// </summary>
    public class OcrHyperlink
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    /// <summary>
    /// Page dimensions.
    /// </summary>
    public class OcrDimensions
    {
        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("dpi")]
        public int? Dpi { get; set; }
    }

    /// <summary>
    /// OCR usage information.
    /// </summary>
    public class OcrUsageInfo
    {
        [JsonPropertyName("pages_processed")]
        public int? PagesProcessed { get; set; }

        [JsonPropertyName("doc_size_bytes")]
        public long? DocSizeBytes { get; set; }
    }

    #endregion
}
