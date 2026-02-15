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
var file = await client.FilesUploadAsync(stream, "document.pdf", FilePurpose.Ocr);

Console.WriteLine($"Uploaded: {file.Id}");
```

**Purpose values:**
- `FilePurpose.Ocr` - For OCR processing
- `FilePurpose.FineTune` - For model fine-tuning
- `FilePurpose.Batch` - For batch jobs

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

### Basic OCR (upload → process)

```csharp
using MistralSDK;
using MistralSDK.Files;
using MistralSDK.Ocr;

// 1. Upload the file
using var stream = File.OpenRead("receipt.jpg");
var file = await client.FilesUploadAsync(stream, "receipt.jpg", FilePurpose.Ocr);

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

## Discussion sur un PDF (Q&A)

Une fois le texte extrait par OCR, vous pouvez lancer une conversation avec l'IA pour poser des questions sur le document :

```csharp
using MistralSDK;
using MistralSDK.ChatCompletion;
using MistralSDK.Files;
using MistralSDK.Ocr;

// 1. Extraire le texte du PDF
using var stream = File.OpenRead("rapport.pdf");
var file = await client.FilesUploadAsync(stream, "rapport.pdf", FilePurpose.Ocr);
var ocrResult = await client.OcrProcessAsync(new OcrRequest
{
    Document = OcrDocument.FromFileId(file.Id),
    Model = OcrModels.MistralOcrLatest
});
var documentText = ocrResult.GetAllMarkdown();
await client.FilesDeleteAsync(file.Id);

// 2. Lancer une conversation sur le document
var conversation = new List<MessageRequest>
{
    MessageRequest.System($"Tu es un assistant qui répond aux questions sur le document suivant. " +
        "Réponds uniquement en te basant sur le contenu fourni.\n\n---\n{documentText}"),
    MessageRequest.User("Quel est le sujet principal de ce document ?")
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
    // Poser une question de suivi
    conversation.Add(MessageRequest.Assistant(response.Message));
    conversation.Add(MessageRequest.User("Peux-tu résumer les points clés ?"));

    var followUp = await client.ChatCompletionAsync(new ChatCompletionRequest
    {
        Model = MistralModels.Small,
        Messages = conversation,
        MaxTokens = 500
    });
    Console.WriteLine(followUp.Message);
}
```

L'IA conserve le contexte du document dans toute la conversation et peut répondre à des questions de suivi.

## Supported formats

- **PDF** - Standard and scanned
- **Images** - PNG, JPEG, AVIF, etc.
- **Documents** - DOCX, PPTX (via URL)

## Next Steps

- [API Reference](api-reference.md) - Full type reference
- [Error Handling](error-handling.md) - Handle API errors
