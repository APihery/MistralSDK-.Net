# Workflows & Helpers

The Mistral SDK provides high-level workflows and helpers for common patterns: multi-turn chat, document Q&A, and simple RAG.

## ChatSession – Multi-Turn Conversation

`ChatSession` manages conversation history and exposes `CompleteAsync()` and `CompleteStreamAsync()` for simple multi-turn chat.

```csharp
using MistralSDK;
using MistralSDK.Conversation;

using var client = new MistralClient(apiKey);
var session = new ChatSession(client, model: MistralModels.Small)
{
    SystemPrompt = "You are a helpful cooking assistant. Be concise."
};

// First turn
session.AddUser("I want to make carbonara tonight.");
var reply1 = await session.CompleteAsync();
Console.WriteLine(reply1);

// Second turn – context is kept
session.AddUser("What can I use instead of guanciale?");
var reply2 = await session.CompleteAsync();
Console.WriteLine(reply2);

// Streaming
session.AddUser("How long should I cook the pasta?");
await foreach (var chunk in session.CompleteStreamAsync())
    Console.Write(chunk);
```

### Methods

| Method | Description |
|-------|-------------|
| `AddUser(content)` | Adds a user message |
| `AddAssistant(content)` | Adds an assistant message (e.g. after manual handling) |
| `CompleteAsync(addToHistory)` | Sends conversation, returns reply, optionally adds to history |
| `CompleteStreamAsync()` | Streams the response and adds it to history |
| `Clear(keepSystemPrompt)` | Clears history |
| `BuildRequest()` | Builds the underlying `ChatCompletionRequest` |

---

## OcrExtractTextAsync – One-Step OCR

Upload a file, run OCR, and get the extracted text in one call. Optionally deletes the file after processing.

```csharp
using MistralSDK;

using var stream = File.OpenRead("invoice.pdf");
var text = await client.OcrExtractTextAsync(stream, "invoice.pdf", deleteAfter: true);
Console.WriteLine(text);

// Keep the file on Mistral's servers for later use
var text2 = await client.OcrExtractTextAsync(stream2, "report.pdf", deleteAfter: false);
```

---

## DocumentQa – OCR + Q&A with History

Load a document via OCR, then ask questions. Conversation history is kept for follow-up questions.

```csharp
using MistralSDK;
using MistralSDK.Workflows;

var qa = new DocumentQa(client);

// Load from file
using var stream = File.OpenRead("contract.pdf");
await qa.LoadDocumentAsync(stream, "contract.pdf");

// Or load text directly
qa.LoadDocumentText("Your document content here...");

// Ask questions
var answer1 = await qa.AskAsync("What is the termination clause?");
Console.WriteLine(answer1);

var answer2 = await qa.AskAsync("And what about the notice period?");
Console.WriteLine(answer2);  // Context from previous question is used

// Clear history but keep document
qa.ClearHistory();

// Reset everything
qa.Reset();
```

---

## SimpleRag – Embeddings + Retrieval + Chat

Add documents, embed them, then ask questions. The most relevant chunks are retrieved and used as context.

```csharp
using MistralSDK;
using MistralSDK.Workflows;

var rag = new SimpleRag(client);

// Add documents (split by paragraphs by default)
rag.AddDocument("Your first document text...", sourceId: "doc1");
rag.AddDocument("Your second document text...", sourceId: "doc2");

// Or add chunks manually
rag.AddChunk("A specific passage.", "doc1");

// Embed and index
await rag.IndexAsync();

// Ask questions
var answer = await rag.AskAsync("What is the main topic?", topK: 5);
Console.WriteLine(answer);

// Clear and start over
rag.Clear();
```

### AddDocument options

| Parameter | Default | Description |
|-----------|---------|-------------|
| `splitByParagraphs` | true | Split by double newlines |
| `chunkSize` | 500 | Max chars per chunk when not splitting by paragraphs |

---

## ConversationHelper – Trim History

When the conversation exceeds the context window, trim to the last N exchanges.

```csharp
using MistralSDK.Helpers;
using MistralSDK.ChatCompletion;

var messages = session.Messages;
var trimmed = ConversationHelper.TrimToLastMessages(messages, maxExchanges: 10);

// Or trim to last N messages total
var trimmed2 = ConversationHelper.TrimToLastN(messages, maxMessages: 20);

// Get last assistant/user message
var lastReply = ConversationHelper.GetLastAssistantMessage(messages);
var lastQuestion = ConversationHelper.GetLastUserMessage(messages);
```

---

## ChatContextBuilder – Fluent Prompt Building

Build chat requests with document context, instructions, and question.

```csharp
using MistralSDK.Helpers;
using MistralSDK.ChatCompletion;

var documentText = "Your OCR or document content...";

var request = ChatContextBuilder.Create()
    .WithDocument(documentText, instruction: "Answer only from this document.")
    .WithInstruction("Be concise. Respond in French.")
    .WithUserQuestion("What is the main conclusion?")
    .WithModel(MistralModels.Small)
    .WithMaxTokens(500)
    .Build();

var response = await client.ChatCompletionAsync(request);
```

---

## See Also

- [Getting Started](getting-started.md) – Basic chat usage
- [OCR](ocr.md) – Full OCR API
- [Embeddings](embeddings.md) – Embedding API for RAG
