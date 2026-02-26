<p align="center">
  <img src="https://raw.githubusercontent.com/APihery/MistralSDK-.Net/main/Ressources/Logo_MistralSDK.png" alt="MistralSDK Logo" width="200"/>
</p>

<h1 align="center">Mistral AI SDK for .NET</h1>

<p align="center">
  A simple and powerful .NET SDK for the <a href="https://mistral.ai/">Mistral AI API</a>
</p>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#quick-start">Quick Start</a> •
  <a href="#documentation">Documentation</a> •
  <a href="#license">License</a>
</p>

---

## Features

- Chat completion API with full parameter support
- **Agents API** - Run completions with pre-configured agents
- **Models API** - List, retrieve, update, archive fine-tuned models
- **Embeddings** - Text (mistral-embed) and code (codestral-embed) embeddings for RAG
- **FIM** - Fill-in-the-middle code completion (Codestral)
- **Batch API** - Asynchronous batch inference for chat, embeddings, FIM, etc.
- **Fine-Tuning** - Create and manage fine-tuning jobs
- **Classifiers** - Moderation and classification (text & chat)
- **Reasoning** - Chain-of-thought with Magistral models (math, coding)
- **Audio & Transcription** - Transcribe audio to text (Voxtral models)
- **OCR / Document AI** - Extract text from PDFs and images
- **Files API** - Upload, list, download files for OCR/fine-tuning/audio
- **Streaming responses** with `IAsyncEnumerable`
- All Mistral models (Large, Small, Codestral, Pixtral...)
- JSON mode and JSON Schema support
- Stop sequences, penalties, and sampling controls
- Async/await with cancellation support
- Dependency Injection ready
- Custom exception handling
- Response caching

## Installation

```bash
# Via NuGet (recommandé)
dotnet add package MistralSDK.Net

# Ou par référence projet
dotnet add reference path/to/MistralSDK.csproj
```

## Quick Start

```csharp
using MistralSDK;
using MistralSDK.ChatCompletion;

// Create client
var apiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY");
using var client = new MistralClient(apiKey);

// Send a request
var request = new ChatCompletionRequest
{
    Model = MistralModels.Small,
    Messages = new List<MessageRequest>
    {
        new MessageRequest { Role = MessageRoles.User, Content = "Hello!" }
    }
};

var response = await client.ChatCompletionAsync(request);

if (response.IsSuccess)
{
    Console.WriteLine(response.Message);
}
```

## OCR (Extract text from documents)

```csharp
using MistralSDK;

// One-step: upload, OCR, get text (file deleted by default)
using var stream = File.OpenRead("document.pdf");
var text = await client.OcrExtractTextAsync(stream, "document.pdf");
Console.WriteLine(text);
```

For document Q&A with conversation history, use `DocumentQa`:

```csharp
using MistralSDK.Workflows;

var qa = new DocumentQa(client);
await qa.LoadDocumentAsync(File.OpenRead("contract.pdf"), "contract.pdf");
Console.WriteLine(await qa.AskAsync("What is the termination clause?"));
```

See the [OCR documentation](https://github.com/APihery/MistralSDK-.Net/blob/main/docs/ocr.md) and [Workflows](https://github.com/APihery/MistralSDK-.Net/blob/main/docs/workflows.md) for more examples.

## Audio transcription

```csharp
using MistralSDK;
using MistralSDK.Audio;

// From URL
var request = AudioTranscriptionRequestBuilder.FromFileUrl("https://example.com/audio.mp3");
var result = await client.AudioTranscribeAsync(request);
Console.WriteLine(result.Text);

// Or from a local file
using var stream = File.OpenRead("meeting.mp3");
var req = AudioTranscriptionRequestBuilder.FromStream(stream, "meeting.mp3");
var transcript = await client.AudioTranscribeAsync(req);
```

See the [Audio documentation](https://github.com/APihery/MistralSDK-.Net/blob/main/docs/audio.md) for streaming, diarization, and timestamps.

## Streaming

```csharp
await foreach (var chunk in client.ChatCompletionStreamAsync(request))
{
    Console.Write(chunk.GetContent());
}
```

## With Dependency Injection

```csharp
// Register
builder.Services.AddMistralClient(options =>
{
    options.ApiKey = builder.Configuration["MistralApi:ApiKey"];
});

// Use
public class MyService
{
    private readonly IMistralClient _client;
    
    public MyService(IMistralClient client) => _client = client;
}
```

## Available Models

| Constant | Description |
|----------|-------------|
| `MistralModels.Large` | Flagship model, top-tier reasoning |
| `MistralModels.Small` | Good balance of speed and quality |
| `MistralModels.Ministral8B` | Efficient for edge deployment |
| `MistralModels.Codestral` | Optimized for code generation |

## Multi-Turn Conversation

Use `ChatSession` for simple multi-turn chat with automatic history:

```csharp
using MistralSDK.Conversation;

var session = new ChatSession(client) { SystemPrompt = "You are a helpful assistant." };
session.AddUser("What is the capital of France?");
Console.WriteLine(await session.CompleteAsync());

session.AddUser("And what's the population?");
Console.WriteLine(await session.CompleteAsync());  // Remembers we're talking about Paris
```

See the [Workflows documentation](https://github.com/APihery/MistralSDK-.Net/blob/main/docs/workflows.md) for ChatSession, DocumentQa, and SimpleRag.

## Documentation

For detailed documentation, see the [docs](https://github.com/APihery/MistralSDK-.Net/tree/main/docs) folder:

- [Getting Started](https://github.com/APihery/MistralSDK-.Net/blob/main/docs/getting-started.md) - First steps and chat examples
- [Workflows & Helpers](https://github.com/APihery/MistralSDK-.Net/blob/main/docs/workflows.md) - ChatSession, DocumentQa, SimpleRag
- [Audio & Transcription](https://github.com/APihery/MistralSDK-.Net/blob/main/docs/audio.md) - Transcribe audio to text
- [OCR & Files](https://github.com/APihery/MistralSDK-.Net/blob/main/docs/ocr.md) - Document AI and file management
- [Configuration](https://github.com/APihery/MistralSDK-.Net/blob/main/docs/configuration.md)
- [Streaming](https://github.com/APihery/MistralSDK-.Net/blob/main/docs/streaming.md)
- [Dependency Injection](https://github.com/APihery/MistralSDK-.Net/blob/main/docs/dependency-injection.md)
- [Error Handling](https://github.com/APihery/MistralSDK-.Net/blob/main/docs/error-handling.md)
- [Caching](https://github.com/APihery/MistralSDK-.Net/blob/main/docs/caching.md)
- [Testing](https://github.com/APihery/MistralSDK-.Net/blob/main/docs/testing.md)
- [API Reference](https://github.com/APihery/MistralSDK-.Net/blob/main/docs/api-reference.md)

## Requirements

- .NET 8.0+
- Mistral AI API key ([Get one here](https://console.mistral.ai/))

## License

MIT License - see [LICENSE](https://github.com/APihery/MistralSDK-.Net/blob/main/LICENSE) for details.
