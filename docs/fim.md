# FIM (Fill-in-the-Middle)

The Mistral SDK supports the [FIM API](https://docs.mistral.ai/api/endpoint/fim) for code completion with Codestral. FIM allows the model to fill in code between a prefix (prompt) and optional suffix.

## Overview

- **Endpoint** – `POST /v1/fim/completions`
- **Models** – Codestral (`codestral-latest`, `codestral-2404`, `codestral-2405`)
- **Response** – Same format as chat completions (`ChatCompletionResponse`)

## Basic usage

```csharp
using MistralSDK;
using MistralSDK.Fim;

var client = new MistralClient(apiKey);

// Convenience overload
var result = await client.FimCompletionAsync("def fibonacci(n):\n    if n <= 1:\n        return n\n    return ", maxTokens: 100);
var content = result.Message;  // or result.GetData<ChatCompletionResponse>()!.GetFirstChoiceContent()

// Full request
var request = new FimCompletionRequest
{
    Model = FimModels.CodestralLatest,
    Prompt = "def fibonacci(n):\n    if n <= 1:\n        return n\n    return ",
    MaxTokens = 100
};
var res = await client.FimCompletionAsync(request);
var text = res.GetData<ChatCompletionResponse>()!.GetFirstChoiceContent();
```

## Prefix + suffix (fill-in-the-middle)

When you provide both `prompt` and `suffix`, the model fills what is between them:

```csharp
var request = new FimCompletionRequest
{
    Model = FimModels.CodestralLatest,
    Prompt = "def greet(name):\n    ",
    Suffix = "\n    return message",
    MaxTokens = 50,
    Temperature = 0.2
};

var result = await client.FimCompletionAsync(request);
var data = result.GetData<ChatCompletionResponse>();
```

## Parameters

| Property      | Type   | Description                                      |
|---------------|--------|--------------------------------------------------|
| `Model`       | string | Codestral model (e.g. `codestral-latest`)        |
| `Prompt`      | string | Prefix text/code to complete                     |
| `Suffix`      | string | Optional suffix for fill-in-the-middle           |
| `Temperature` | double | Sampling temperature (0–1.5)                     |
| `TopP`        | double | Top-p nucleus sampling (default 1.0)             |
| `MaxTokens`   | int    | Maximum tokens to generate                       |
| `MinTokens`   | int    | Minimum tokens to generate                       |
| `Stop`        | object | Stop sequences                                   |
| `RandomSeed`  | int    | Seed for deterministic results                   |

## Model constants

```csharp
FimModels.CodestralLatest  // codestral-latest
FimModels.Codestral2404    // codestral-2404
FimModels.Codestral2405    // codestral-2405
```
