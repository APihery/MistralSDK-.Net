# Classifiers API

The Mistral SDK supports the [Classifiers API](https://docs.mistral.ai/api/endpoint/classifiers) for content moderation and classification.

## Overview

- **Moderation** – Check text for safety (sexual, hate, violence, etc.)
- **Chat Moderation** – Moderate conversation messages
- **Classification** – Classify text into custom categories
- **Chat Classification** – Classify chat conversations

## Text moderation

```csharp
using MistralSDK;
using MistralSDK.Classifiers;

var client = new MistralClient(apiKey);

var request = new ModerationRequest
{
    Model = ModerationModels.MistralModerationLatest,
    Input = new[] { "Text to check.", "Another text." }
};

var result = await client.ModerationsCreateAsync(request);
var response = result.GetData<ModerationResponse>();

foreach (var r in response!.Results)
{
    foreach (var (category, flagged) in r.Categories)
    {
        if (flagged)
            Console.WriteLine($"Flagged: {category} (score: {r.CategoryScores[category]})");
    }
}
```

## Chat moderation

```csharp
using MistralSDK.ChatCompletion;

var request = new ChatModerationRequest
{
    Model = ModerationModels.MistralModerationLatest,
    Input = new List<List<MessageRequest>>
    {
        new List<MessageRequest>
        {
            MessageRequest.User("User message"),
            MessageRequest.Assistant("Assistant reply")
        }
    }
};

var modResult = await client.ChatModerationsCreateAsync(request);
var modData = modResult.GetData<ModerationResponse>();
```

## Quick moderation (convenience overload)

```csharp
var result = await client.ModerateAsync("Text to check");
var response = result.GetData<ModerationResponse>();
```

## Text classification

```csharp
var request = new ModerationRequest
{
    Model = "your-classifier-model-id",
    Input = "Text to classify"
};

var result = await client.ClassificationsCreateAsync(request);
var response = result.GetData<ClassificationResponse>();

foreach (var res in response!.Results)
{
    foreach (var (target, scores) in res)
    {
        Console.WriteLine($"Target: {target}");
        foreach (var (label, score) in scores.Scores)
            Console.WriteLine($"  {label}: {score}");
    }
}
```

## Chat classification

```csharp
var request = new ChatClassificationRequest
{
    Model = "your-classifier-model-id",
    Input = new InstructRequest
    {
        Messages = new List<MessageRequest>
        {
            MessageRequest.User("User message"),
            MessageRequest.Assistant("Assistant reply")
        }
    }
};

var result = await client.ChatClassificationsCreateAsync(request);
var response = result.GetData<ClassificationResponse>();
```

## Moderation categories

Common categories returned by moderation:

- `sexual`
- `hate_and_discrimination`
- `violence_and_threats`
- `dangerous_and_criminal_content`
- `selfharm`
- `health`
- `financial`
- `law`
- `pii`
