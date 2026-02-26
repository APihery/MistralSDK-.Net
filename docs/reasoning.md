# Reasoning (Magistral)

The Mistral SDK supports the [Reasoning API](https://docs.mistral.ai/capabilities/reasoning) for chain-of-thought reasoning using Magistral models. These models generate logical reasoning steps before producing the final answer.

## Overview

- **Reasoning models** – Magistral Small and Medium for complex tasks (math, coding, analysis)
- **Structured output** – Separate thinking traces and final answer
- **prompt_mode** – Use default system prompt or provide your own
- **Content chunks** – Build messages with `text` and `thinking` chunks

## Models

| Constant | Description |
|----------|-------------|
| `MistralModels.MagistralSmall` | Smaller reasoning model, efficient |
| `MistralModels.MagistralMedium` | More powerful, balances performance and cost |

## Basic usage

### Simple reasoning (with default system prompt)

```csharp
using MistralSDK;
using MistralSDK.ChatCompletion;

var client = new MistralClient(apiKey);

var request = ReasoningHelper.CreateReasoningRequest(
    MistralModels.MagistralMedium,
    "John is one of 4 children. The first sister is 4 years old. Next year, " +
    "the second sister will be twice as old as the first sister. How old is John?");

var response = await client.ChatCompletionAsync(request);

if (response.IsSuccess)
{
    var content = response.Message;  // Final answer (thinking excluded)
    Console.WriteLine(content);
}
```

### Manual request with prompt_mode

```csharp
var request = new ChatCompletionRequest
{
    Model = MistralModels.MagistralMedium,
    Messages = new List<MessageRequest>
    {
        MessageRequest.User("Solve: 2x + 5 = 15")
    },
    PromptMode = PromptModes.Reasoning
};

var response = await client.ChatCompletionAsync(request);
```

### Without default system prompt

```csharp
var request = ReasoningHelper.CreateReasoningRequest(
    MistralModels.MagistralSmall,
    "What is 17 * 23?",
    useDefaultPrompt: false);

request.PromptMode = null;  // No system prompt
```

## Structured content (thinking + text)

For reasoning models, the response `content` can be an array of chunks:

- `type: "thinking"` – Reasoning traces (internal monologue)
- `type: "text"` – Final answer

### Extracting content from the response

```csharp
var data = response.GetData<ChatCompletionResponse>();
var choice = data?.Choices?[0]?.Message;
if (choice == null) return;

// Final answer (string - works for simple and reasoning responses)
var answer = choice.Content;

// For reasoning: raw chunks (thinking + text)
var chunks = choice.ContentChunks;
if (chunks != null)
{
    var thinking = MessageContentExtensions.GetThinkingText(chunks);
    var fullText = MessageContentExtensions.GetAllContentText(chunks);
}
```

### Building messages with structured content

You can send system messages with `text` and `thinking` chunks:

```csharp
var systemChunks = new List<ContentChunk>
{
    ContentChunkBuilder.Text("You are a math tutor. Think step by step."),
    ContentChunkBuilder.Thinking("Consider the problem structure first."),
    ContentChunkBuilder.Text("Then provide the final answer.")
};

var request = new ChatCompletionRequest
{
    Model = MistralModels.MagistralMedium,
    Messages = new List<MessageRequest>
    {
        MessageRequest.SystemWithChunks(systemChunks),
        MessageRequest.User("Solve x² - 5x + 6 = 0")
    },
    PromptMode = PromptModes.Reasoning
};
```

## Default reasoning system prompt

`ReasoningHelper.DefaultReasoningSystemPrompt()` returns the recommended system prompt:

1. Instructions to draft thinking before answering
2. A `thinking` chunk template
3. Instructions for the final answer

Use it when you want optimal reasoning behavior:

```csharp
var systemPrompt = ReasoningHelper.DefaultReasoningSystemPrompt();
var messages = new List<MessageRequest>
{
    MessageRequest.SystemWithChunks(systemPrompt),
    MessageRequest.User("Your question here")
};
```

## Streaming

Reasoning models support streaming. The delta `content` is typically plain text; thinking and answer may be interleaved in the stream.

```csharp
var request = ReasoningHelper.CreateReasoningRequest(
    MistralModels.MagistralMedium,
    "Explain the Pythagorean theorem.");
request.Stream = true;

await foreach (var chunk in client.ChatCompletionStreamAsync(request))
{
    var text = chunk.GetContent();
    if (!string.IsNullOrEmpty(text))
        Console.Write(text);
}
```

## prompt_mode values

| Value | Effect |
|-------|--------|
| `PromptModes.Reasoning` | Use default reasoning system prompt |
| `null` | No system prompt (provide your own or none) |

## See also

- [Mistral Reasoning docs](https://docs.mistral.ai/capabilities/reasoning)
- [Chat Completions](api-reference.md#chat-completions)
