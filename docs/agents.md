# Agents API

The Mistral SDK supports the [Agents API](https://docs.mistral.ai/api/endpoint/agents) for running completions with pre-configured agents instead of raw models.

## Overview

- **Agent completion** – Use an agent ID instead of a model for chat completions
- **Unified response** – Returns `MistralResponse` with `ChatCompletionResponse` in `Data` when successful
- **Tools & tool choice** – Agents can use tools and tool_choice parameters

## Agent completion

```csharp
using MistralSDK;
using MistralSDK.Agents;
using MistralSDK.ChatCompletion;

var client = new MistralClient(apiKey);

var request = new AgentCompletionRequest
{
    AgentId = "<your-agent-id>",
    Messages = new List<MessageRequest>
    {
        MessageRequest.User("Who is the best French painter? Answer in one short sentence.")
    }
};

var result = await client.AgentCompletionAsync(request);

if (result.IsSuccess)
{
    var data = result.GetData<ChatCompletionResponse>();
    Console.WriteLine(data!.GetFirstChoiceContent());
    Console.WriteLine($"Model: {data.Model}");
    Console.WriteLine($"Tokens: {data.Usage?.TotalTokens}");
}
else
{
    Console.WriteLine($"Error: {result.Message}");
}
```

## Optional parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `MaxTokens` | int? | Maximum tokens to generate |
| `Stream` | bool | Enable streaming (default: false) |
| `Stop` | string/array | Stop sequences |
| `RandomSeed` | int? | Deterministic sampling |
| `ResponseFormat` | ResponseFormat | JSON mode or schema |
| `Tools` | List | Tools the agent may call |
| `ToolChoice` | object | "auto", "none", "required" |
| `PresencePenalty` | double | 0–2, default 0 |
| `FrequencyPenalty` | double | 0–2, default 0 |
| `N` | int? | Number of completions |
| `ParallelToolCalls` | bool | Default true |
| `PromptMode` | string | e.g. "reasoning" |

## Response

The response is a `MistralResponse`. When `IsSuccess` is true, use `GetData<ChatCompletionResponse>()` to access:

- `Choices` – Completion choices with message content
- `Model` – Model used by the agent
- `Usage` – Token usage (prompt_tokens, completion_tokens, total_tokens)
