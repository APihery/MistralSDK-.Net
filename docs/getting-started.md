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

## Multi-Turn Conversation (Chat with Context)

The Mistral API maintains context through the message history you send. Here's an example of a complete conversation where the AI remembers previous exchanges:

```csharp
using MistralSDK;
using MistralSDK.ChatCompletion;

var apiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY");
using var client = new MistralClient(apiKey);

// Initialize conversation with a system prompt
var conversation = new List<MessageRequest>
{
    MessageRequest.System("You are a helpful cooking assistant. Be concise and friendly.")
};

// Helper method to chat and display response
async Task<string> ChatAsync(string userMessage)
{
    // Add user message to history
    conversation.Add(MessageRequest.User(userMessage));
    
    var request = new ChatCompletionRequest
    {
        Model = MistralModels.Small,
        Messages = conversation,
        Temperature = 0.7,
        MaxTokens = 300
    };
    
    var response = await client.ChatCompletionAsync(request);
    
    if (response.IsSuccess)
    {
        // Add assistant response to history (maintains context)
        conversation.Add(MessageRequest.Assistant(response.Message));
        return response.Message;
    }
    
    return $"Error: {response.Message}";
}

// Simulate a conversation
Console.WriteLine("🧑 User: I want to make pasta for dinner tonight.");
Console.WriteLine($"🤖 Assistant: {await ChatAsync("I want to make pasta for dinner tonight.")}");
Console.WriteLine();

Console.WriteLine("🧑 User: What ingredients do I need for carbonara?");
Console.WriteLine($"🤖 Assistant: {await ChatAsync("What ingredients do I need for carbonara?")}");
Console.WriteLine();

Console.WriteLine("🧑 User: I don't have guanciale, what can I use instead?");
Console.WriteLine($"🤖 Assistant: {await ChatAsync("I don't have guanciale, what can I use instead?")}");
Console.WriteLine();

Console.WriteLine("🧑 User: Perfect! And how long should I cook the pasta?");
Console.WriteLine($"🤖 Assistant: {await ChatAsync("Perfect! And how long should I cook the pasta?")}");
```

**Example output:**

```
🧑 User: I want to make pasta for dinner tonight.
🤖 Assistant: Great choice! What type of pasta dish are you in the mood for? 
   I can help with carbonara, bolognese, aglio e olio, or something else!

🧑 User: What ingredients do I need for carbonara?
🤖 Assistant: For a classic carbonara (serves 2), you'll need:
   - 200g spaghetti or rigatoni
   - 150g guanciale (cured pork cheek)
   - 3 egg yolks + 1 whole egg
   - 50g Pecorino Romano, finely grated
   - Black pepper
   - Salt for pasta water

🧑 User: I don't have guanciale, what can I use instead?
🤖 Assistant: No problem! You can substitute with:
   1. **Pancetta** (best alternative) - similar flavor profile
   2. **Thick-cut bacon** - more smoky, but works well
   
   Cut into small cubes and render the fat slowly for best results.

🧑 User: Perfect! And how long should I cook the pasta?
🤖 Assistant: Cook your spaghetti for about **1-2 minutes less** than the 
   package says (usually 8-10 min for al dente). It will finish cooking 
   in the pan with the sauce. Save some pasta water before draining!
```

**Key points:**
- The `conversation` list maintains the full message history
- Each exchange is added to the list (user message, then assistant response)
- The AI remembers previous context (it knows you're making carbonara without guanciale)
- Use `MessageRequest.System()`, `User()`, and `Assistant()` factory methods for cleaner code

## Next Steps

- [Configuration Options](configuration.md) - Customize the client behavior
- [Dependency Injection](dependency-injection.md) - Use with ASP.NET Core
- [Error Handling](error-handling.md) - Handle errors gracefully
