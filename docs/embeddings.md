# Embeddings & RAG

The Mistral SDK supports the [Embeddings API](https://docs.mistral.ai/api/endpoint/embeddings) for text and code embeddings, and patterns for [RAG (Retrieval-Augmented Generation)](https://docs.mistral.ai/capabilities/embeddings/rag_quickstart).

## Overview

- **Text Embeddings** – `mistral-embed` for general-purpose text (1024 dimensions)
- **Code Embeddings** – `codestral-embed` for code search and retrieval (up to 3072 dimensions)
- **RAG** – Combine embeddings with chat completion for retrieval-augmented generation

## Text embeddings

```csharp
using MistralSDK;
using MistralSDK.Embeddings;

var client = new MistralClient(apiKey);

var request = new EmbeddingRequest
{
    Model = EmbeddingModels.MistralEmbed,
    Input = new[] { "Embed this sentence.", "As well as this one." }
};

var result = await client.EmbeddingsCreateAsync(request);
var response = result.GetData<EmbeddingResponse>();

foreach (var item in response!.Data)
{
    Console.WriteLine($"Index {item.Index}: {item.Embedding.Count} dimensions");
}
```

## Code embeddings

```csharp
var request = new EmbeddingRequest
{
    Model = EmbeddingModels.CodestralEmbed,
    Input = "def two_sum(nums, target): ...",
    OutputDimension = 512,  // Optional: reduce from default 1536
    OutputDtype = EmbeddingDtype.Float
};

var result = await client.EmbeddingsCreateAsync(request);
var response = result.GetData<EmbeddingResponse>();
```

## Single text input (convenience overload)

```csharp
// Simple overload for a single text
var result = await client.EmbeddingsCreateAsync("Single text to embed");
var vector = result.GetData<EmbeddingResponse>()!.GetFirstVector();

// Or with full request
var request = new EmbeddingRequest
{
    Model = EmbeddingModels.MistralEmbed,
    Input = "Single text to embed"
};
var res = await client.EmbeddingsCreateAsync(request);
var vec = res.GetData<EmbeddingResponse>()!.Data[0].Embedding;
```

## RAG pattern

### Using SimpleRag (recommended)

The `SimpleRag` workflow handles embeddings, retrieval, and chat in one place:

```csharp
using MistralSDK;
using MistralSDK.Workflows;

var rag = new SimpleRag(client);

// Add documents (split by paragraphs by default)
rag.AddDocument("Your first document text...", sourceId: "doc1");
rag.AddDocument("Your second document text...", sourceId: "doc2");

// Embed and index
await rag.IndexAsync();

// Ask questions – top-k chunks are retrieved automatically
var answer = await rag.AskAsync("What is the main topic?", topK: 5);
Console.WriteLine(answer);

rag.Clear();  // Clear and start over
```

See [Workflows](workflows.md) for full details.

### Manual RAG pattern

For full control, combine embeddings with chat completion:

1. **Embed** documents and store in a vector database
2. **Embed** the user question
3. **Retrieve** similar document chunks (e.g. cosine similarity)
4. **Generate** answer using chat completion with retrieved context

```csharp
// 1. Embed document chunks
var docRequest = new EmbeddingRequest
{
    Model = EmbeddingModels.MistralEmbed,
    Input = chunks  // List<string> of text chunks
};
var docResult = await client.EmbeddingsCreateAsync(docRequest);
var docEmbeddings = docResult.GetData<EmbeddingResponse>()!;

// 2. Embed the question
var questionResult = await client.EmbeddingsCreateAsync("What were the main points?");
var questionEmbedding = questionResult.GetData<EmbeddingResponse>()!.GetFirstVector();

// 3. Find similar chunks (e.g. cosine similarity, FAISS, etc.)
// ... your vector search logic ...

// 4. Generate with context
var chatRequest = new ChatCompletionRequest
{
    Model = MistralModels.Small,
    Messages = new List<MessageRequest>
    {
        MessageRequest.System("Answer based on the context provided."),
        MessageRequest.User($"Context:\n{retrievedContext}\n\nQuestion: {question}")
    }
};
var answer = await client.ChatCompletionAsync(chatRequest);
```

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Model` | mistral-embed (text) or codestral-embed (code) |
| `Input` | string or string[] |
| `EncodingFormat` | "float" or "base64" |
| `OutputDimension` | For codestral-embed: 1–3072, default 1536 |
| `OutputDtype` | float, int8, uint8, binary, ubinary |

## Models

| Constant | Use case |
|-----------|----------|
| `EmbeddingModels.MistralEmbed` | General text, RAG, similarity |
| `EmbeddingModels.CodestralEmbed` | Code search, code analytics |

## Next Steps

- [Workflows](workflows.md) - SimpleRag, ChatSession, and more
