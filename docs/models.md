# Models API

The Mistral SDK supports the [Models API](https://docs.mistral.ai/api/endpoint/models) for listing, retrieving, and managing models (including fine-tuned models).

## List models

```csharp
using MistralSDK;
using MistralSDK.Models;

var client = new MistralClient(apiKey);

var list = await client.ModelsListAsync();

foreach (var model in list.Data)
{
    Console.WriteLine($"{model.Id} - {model.Type} - context: {model.MaxContextLength}");
    Console.WriteLine($"  Chat: {model.Capabilities.CompletionChat}, Vision: {model.Capabilities.Vision}");
}
```

## Retrieve a model

```csharp
var model = await client.ModelsRetrieveAsync("mistral-small-latest");

Console.WriteLine(model.Id);
Console.WriteLine(model.MaxContextLength);
Console.WriteLine(model.Capabilities.FunctionCalling);
```

## Delete a fine-tuned model

```csharp
var result = await client.ModelsDeleteAsync("ft:open-mistral-7b:587a6b29:20240514:7e773925");

Console.WriteLine($"Deleted: {result.Deleted}, ID: {result.Id}");
```

## Update a fine-tuned model

```csharp
var updateRequest = new UpdateFTModelRequest
{
    Name = "My Custom Model",
    Description = "Fine-tuned for customer support"
};

var updated = await client.ModelsUpdateAsync("ft:open-mistral-7b:587a6b29:20240514:7e773925", updateRequest);
```

## Archive / Unarchive

```csharp
// Archive
var archived = await client.ModelsArchiveAsync("ft:open-mistral-7b:587a6b29:20240514:7e773925");
Console.WriteLine($"Archived: {archived.Archived}");

// Unarchive
var unarchived = await client.ModelsUnarchiveAsync("ft:open-mistral-7b:587a6b29:20240514:7e773925");
Console.WriteLine($"Archived: {unarchived.Archived}");
```

## Model card

`ModelCard` includes:

| Property | Description |
|----------|-------------|
| `Id` | Model identifier |
| `Type` | "base" or "fine-tuned" |
| `Capabilities` | CompletionChat, Vision, FunctionCalling, etc. |
| `MaxContextLength` | Context window size |
| `Name` | Optional display name |
| `Description` | Optional description |
| `Job` | Fine-tuning job ID (fine-tuned only) |
| `Root` | Root model (fine-tuned only) |
| `Archived` | Archive status (fine-tuned only) |
