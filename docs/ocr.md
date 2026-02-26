# OCR & Files API

The Mistral SDK supports Document AI (OCR) and file management for extracting text from PDFs and images.

## Files API

### Upload a file

Upload files for OCR, fine-tuning, or batch processing:

```csharp
using MistralSDK;
using MistralSDK.Files;

var client = new MistralClient(apiKey);

using var stream = File.OpenRead("document.pdf");
var file = await client.FilesUploadAsync(stream, "document.pdf", FilePurposeType.Ocr);

Console.WriteLine($"Uploaded: {file.Id}");
```

**Purpose values** (use `FilePurposeType` for type-safe overload):
- `FilePurposeType.Ocr` - For OCR processing
- `FilePurposeType.FineTune` - For model fine-tuning
- `FilePurposeType.Batch` - For batch jobs
- `FilePurposeType.Audio` - For audio transcription

**File size limit:** 512 MB per file (API limit).

### List files

```csharp
var list = await client.FilesListAsync();
foreach (var f in list.Data)
{
    Console.WriteLine($"{f.Filename} - {f.Purpose}");
}
```

### Retrieve, delete, download

```csharp
var file = await client.FilesRetrieveAsync(fileId);
var content = await client.FilesDownloadAsync(fileId);
var signedUrl = await client.FilesGetSignedUrlAsync(fileId, expiryHours: 24);
await client.FilesDeleteAsync(fileId);
```

## OCR API

### Document input options

| Method | Use case |
|--------|----------|
| `OcrDocument.FromFileId(id)` | Use an uploaded file |
| `OcrDocument.FromDocumentUrl(url)` | PDF from a public URL |
| `OcrDocument.FromImageUrl(url)` | Image from URL (http/https or data URI) |
| `OcrDocument.FromImageBase64(base64, mimeType)` | Image from base64 |

### One-step OCR (OcrExtractTextAsync)

The simplest way to extract text: upload, OCR, and get text in one call. The file is deleted by default after processing.

```csharp
using MistralSDK;

using var stream = File.OpenRead("receipt.jpg");
var text = await client.OcrExtractTextAsync(stream, "receipt.jpg", deleteAfter: true);
Console.WriteLine(text);

// Keep the file on Mistral's servers for later use
var text2 = await client.OcrExtractTextAsync(stream2, "report.pdf", deleteAfter: false);
```

### Basic OCR (upload → process)

For more control (e.g. table format, specific pages), use the full workflow:

```csharp
using MistralSDK;
using MistralSDK.Files;
using MistralSDK.Ocr;

// 1. Upload the file
using var stream = File.OpenRead("receipt.jpg");
var file = await client.FilesUploadAsync(stream, "receipt.jpg", FilePurposeType.Ocr);

// 2. Run OCR
var result = await client.OcrProcessAsync(new OcrRequest
{
    Document = OcrDocument.FromFileId(file.Id),
    Model = OcrModels.MistralOcrLatest
});

// 3. Get extracted text
Console.WriteLine(result.GetAllMarkdown());

// 4. Clean up
await client.FilesDeleteAsync(file.Id);
```

### OCR from image URL

```csharp
var result = await client.OcrProcessAsync(new OcrRequest
{
    Document = OcrDocument.FromDocumentUrl("https://example.com/document.pdf"),
    Model = OcrModels.MistralOcrLatest
});
```

### OCR from base64 image

```csharp
var bytes = await File.ReadAllBytesAsync("scan.jpg");
var base64 = Convert.ToBase64String(bytes);

var result = await client.OcrProcessAsync(new OcrRequest
{
    Document = OcrDocument.FromImageBase64(base64, "image/jpeg"),
    Model = OcrModels.MistralOcrLatest
});
```

### Options

| Parameter | Type | Description |
|-----------|------|-------------|
| `TableFormat` | string | `OcrTableFormat.Markdown` or `OcrTableFormat.Html` |
| `ExtractHeader` | bool | Extract header separately |
| `ExtractFooter` | bool | Extract footer separately |
| `IncludeImageBase64` | bool? | Include images in response |
| `Pages` | List<int> | Specific pages to process (0-based) |

```csharp
var result = await client.OcrProcessAsync(new OcrRequest
{
    Document = OcrDocument.FromFileId(fileId),
    Model = OcrModels.MistralOcrLatest,
    TableFormat = OcrTableFormat.Markdown,
    ExtractHeader = true,
    Pages = new List<int> { 0, 1, 2 }
});
```

### Response structure

```csharp
result.Pages        // List of OcrPage
result.Model        // Model used
result.UsageInfo    // Pages processed, size
result.GetAllMarkdown()  // Concatenated markdown
```

Each `OcrPage` has:
- `Index` - Page number
- `Markdown` - Extracted text/content
- `Images` - Extracted images with bounding boxes
- `Header`, `Footer` - When extraction enabled

## Document Q&A

### Using DocumentQa (recommended)

The `DocumentQa` workflow loads a document via OCR and keeps conversation history for follow-up questions:

```csharp
using MistralSDK;
using MistralSDK.Workflows;

var qa = new DocumentQa(client);

// Load from file
using var stream = File.OpenRead("report.pdf");
await qa.LoadDocumentAsync(stream, "report.pdf");

// Ask questions
var answer1 = await qa.AskAsync("What is the main subject of this document?");
Console.WriteLine(answer1);

var answer2 = await qa.AskAsync("Can you summarize the key points?");
Console.WriteLine(answer2);  // Context from previous question is used

qa.ClearHistory();  // Clear history but keep document
qa.Reset();         // Reset everything
```

### Manual approach

For full control, use OCR + chat completion directly:

```csharp
using MistralSDK;
using MistralSDK.ChatCompletion;
using MistralSDK.Files;
using MistralSDK.Ocr;

// 1. Extract text from PDF
using var stream = File.OpenRead("report.pdf");
var file = await client.FilesUploadAsync(stream, "report.pdf", FilePurposeType.Ocr);
var ocrResult = await client.OcrProcessAsync(new OcrRequest
{
    Document = OcrDocument.FromFileId(file.Id),
    Model = OcrModels.MistralOcrLatest
});
var documentText = ocrResult.GetAllMarkdown();
await client.FilesDeleteAsync(file.Id);

// 2. Start a conversation about the document
var conversation = new List<MessageRequest>
{
    MessageRequest.System($"You are an assistant that answers questions about the following document. " +
        "Answer only based on the content provided.\n\n---\n{documentText}"),
    MessageRequest.User("What is the main subject of this document?")
};

var response = await client.ChatCompletionAsync(new ChatCompletionRequest
{
    Model = MistralModels.Small,
    Messages = conversation,
    MaxTokens = 500
});

if (response.IsSuccess)
{
    Console.WriteLine(response.Message);
    conversation.Add(MessageRequest.Assistant(response.Message));
    conversation.Add(MessageRequest.User("Can you summarize the key points?"));
    var followUp = await client.ChatCompletionAsync(new ChatCompletionRequest
    {
        Model = MistralModels.Small,
        Messages = conversation,
        MaxTokens = 500
    });
    Console.WriteLine(followUp.Message);
}
```

See [Workflows](workflows.md) for more helpers.

## Supported formats

- **PDF** - Standard and scanned
- **Images** - PNG, JPEG, AVIF, etc.
- **Documents** - DOCX, PPTX (via URL)

## Next Steps

- [Workflows](workflows.md) - DocumentQa, OcrExtractTextAsync, and more
- [API Reference](api-reference.md) - Full type reference
- [Error Handling](error-handling.md) - Handle API errors
