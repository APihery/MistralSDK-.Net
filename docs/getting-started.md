# Getting Started

## Prerequisites

- .NET 8.0 or later
- A Mistral AI API key ([Get one here](https://console.mistral.ai/))

## Installation

Add the MistralSDK to your project:

```bash
# Using dotnet CLI
dotnet add reference path/to/MistralSDK.csproj

# Or add the project reference in your .csproj
<ProjectReference Include="..\MistralSDK\MistralSDK.csproj" />
```

## Your First Request

### 1. Set up your API key

Store your API key securely in an environment variable:

```bash
# Windows (Command Prompt)
set MISTRAL_API_KEY=your-api-key-here

# Windows (PowerShell)
$env:MISTRAL_API_KEY="your-api-key-here"

# Linux/macOS
export MISTRAL_API_KEY="your-api-key-here"
```

### 2. Write your first code

```csharp
using MistralSDK;
using MistralSDK.ChatCompletion;

// Get API key from environment
var apiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY");

// Create the client
using var client = new MistralClient(apiKey);

// Create a request
var request = new ChatCompletionRequest
{
    Model = MistralModels.Small,
    Messages = new List<MessageRequest>
    {
        new MessageRequest 
        { 
            Role = MessageRoles.User, 
            Content = "Hello! What is the capital of France?" 
        }
    }
};

// Send the request
var response = await client.ChatCompletionAsync(request);

// Handle the response
if (response.IsSuccess)
{
    Console.WriteLine($"Response: {response.Message}");
    Console.WriteLine($"Tokens used: {response.Usage?.TotalTokens}");
}
else
{
    Console.WriteLine($"Error: {response.Message}");
}
```

## Available Models

### Premier Models
| Constant | Model Name | Description |
|----------|------------|-------------|
| `MistralModels.Large` | `mistral-large-latest` | Flagship model, top-tier reasoning |
| `MistralModels.PixtralLarge` | `pixtral-large-latest` | Multimodal with image understanding |
| `MistralModels.Saba` | `mistral-saba-latest` | Middle Eastern & South Asian languages |

### Efficient Models
| Constant | Model Name | Description |
|----------|------------|-------------|
| `MistralModels.Small` | `mistral-small-latest` | Good balance of speed and quality |
| `MistralModels.Ministral8B` | `ministral-8b-latest` | Efficient for edge deployment |
| `MistralModels.Ministral3B` | `ministral-3b-latest` | Ultra-efficient small model |

### Specialized Models
| Constant | Model Name | Description |
|----------|------------|-------------|
| `MistralModels.Codestral` | `codestral-latest` | Optimized for code generation |
| `MistralModels.Pixtral` | `pixtral-12b-2409` | Free multimodal model |
| `MistralModels.Embed` | `mistral-embed` | Text embeddings |

## Message Roles

| Constant | Role | Description |
|----------|------|-------------|
| `MessageRoles.System` | `system` | System instructions |
| `MessageRoles.User` | `user` | User messages |
| `MessageRoles.Assistant` | `assistant` | AI responses |

## Request Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Model` | `string` | Required | The model to use |
| `Messages` | `List<MessageRequest>` | Required | Conversation messages |
| `Temperature` | `double?` | null | Randomness (0.0-2.0), recommended 0.0-0.7 |
| `TopP` | `double?` | null | Nucleus sampling (0.0-1.0) |
| `MaxTokens` | `int?` | null | Maximum tokens to generate |
| `N` | `int?` | null | Number of completions to return |
| `Stop` | `object?` | null | Stop sequence(s) |
| `FrequencyPenalty` | `double?` | null | Penalize frequent tokens (0.0-2.0) |
| `PresencePenalty` | `double?` | null | Encourage diversity (0.0-2.0) |
| `ResponseFormat` | `ResponseFormat?` | null | JSON mode or JSON schema |
| `SafePrompt` | `bool` | false | Enable safety filters |
| `RandomSeed` | `int?` | null | Seed for reproducibility |
| `Stream` | `bool` | false | Enable streaming response |

## Next Steps

- [Configuration Options](configuration.md) - Customize the client behavior
- [Dependency Injection](dependency-injection.md) - Use with ASP.NET Core
- [Error Handling](error-handling.md) - Handle errors gracefully
