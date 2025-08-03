# Mistral AI SDK for .NET

A comprehensive .NET SDK for interacting with the Mistral AI API, providing easy-to-use methods for chat completions and other AI services.

## ✨ Features

- ✅ **Chat Completions**: Generate text completions using Mistral's advanced language models
- ✅ **Multiple Models**: Support for all Mistral models (Tiny, Small, Medium, Large)
- ✅ **Error Handling**: Comprehensive error handling with detailed error messages
- ✅ **Type Safety**: Full type safety with strongly-typed request and response objects
- ✅ **Async Support**: Full async/await support for all operations
- ✅ **Resource Management**: Proper disposal of resources with IDisposable implementation
- ✅ **Validation**: Built-in request validation with data annotations
- ✅ **Documentation**: Comprehensive XML documentation for all public APIs

> 📋 **For detailed feature information and version history, see [CHANGELOG.md](CHANGELOG.md)**

## 🚀 Quick Start

### Installation

```bash
dotnet add package MistralSDK
```

### Basic Usage

```csharp
using MistralSDK;
using MistralSDK.ChatCompletion;

// Initialize the client with your API key
using var client = new MistralClient("your-api-key-here");

// Create a chat completion request
var request = new ChatCompletionRequest
{
    Model = MistralModels.Small,
    Messages = new List<MessageRequest>
    {
        new MessageRequest
        {
            Role = MessageRoles.User,
            Content = "Hello! How are you today?"
        }
    },
    Temperature = 0.7,
    MaxTokens = 100
};

// Send the request
var response = await client.ChatCompletionAsync(request);

// Check if the request was successful
if (response.IsSuccess)
{
    Console.WriteLine($"Response: {response.Message}");
    Console.WriteLine($"Model used: {response.Model}");
    Console.WriteLine($"Tokens used: {response.Usage?.TotalTokens}");
}
else
{
    Console.WriteLine($"Error: {response.Message}");
}
```

## 📚 Documentation

### API Reference

#### MistralClient
```csharp
public MistralClient(string apiKey)
public async Task<MistralResponse> ChatCompletionAsync(ChatCompletionRequest request)
```

#### ChatCompletionRequest
```csharp
public string Model { get; set; }                    // Required: Model identifier
public List<MessageRequest> Messages { get; set; }   // Required: Conversation messages
public double Temperature { get; set; }              // Optional: Randomness (0.0-2.0)
public double TopP { get; set; }                     // Optional: Nucleus sampling (0.0-1.0)
public int? MaxTokens { get; set; }                  // Optional: Maximum tokens to generate
public bool SafePrompt { get; set; }                 // Optional: Enable safety filters
public bool Stream { get; set; }                     // Optional: Enable streaming
```

#### MistralResponse
```csharp
public int StatusCode { get; set; }                  // HTTP status code
public string Message { get; set; }                  // Response content or error message
public bool IsSuccess { get; set; }                  // Whether the request was successful
public string? Model { get; set; }                   // Model used (successful responses)
public UsageInfo? Usage { get; set; }                // Token usage information
```

### Available Models

```csharp
MistralModels.Tiny    // Fastest, most cost-effective
MistralModels.Small   // Good balance of speed and quality
MistralModels.Medium  // High quality with good performance
MistralModels.Large   // Highest quality, best for complex tasks
```

### Message Roles

```csharp
MessageRoles.System    // System instructions and context
MessageRoles.User      // Messages from the user
MessageRoles.Assistant // Responses from the AI assistant
```

## 💡 Examples

### Conversation Management

```csharp
var conversation = new List<MessageRequest>
{
    new MessageRequest { Role = MessageRoles.System, Content = "You are a helpful assistant." },
    new MessageRequest { Role = MessageRoles.User, Content = "What is the capital of France?" }
};

var request = new ChatCompletionRequest
{
    Model = MistralModels.Small,
    Messages = conversation
};

var response = await client.ChatCompletionAsync(request);

if (response.IsSuccess)
{
    // Add assistant response to conversation
    conversation.Add(new MessageRequest 
    { 
        Role = MessageRoles.Assistant, 
        Content = response.Message 
    });
}
```

### Error Handling

```csharp
var response = await client.ChatCompletionAsync(request);

if (!response.IsSuccess)
{
    switch (response.StatusCode)
    {
        case 401:
            Console.WriteLine("Authentication failed. Check your API key.");
            break;
        case 429:
            Console.WriteLine("Rate limit exceeded. Try again later.");
            break;
        default:
            Console.WriteLine($"Error: {response.Message}");
            break;
    }
}
```

### Cost Estimation

```csharp
if (response.Usage != null && response.Model != null)
{
    var cost = response.Usage.GetEstimatedCost(response.Model);
    Console.WriteLine($"Estimated cost: ${cost:F6}");
}
```

## 🧪 Testing

The SDK includes a comprehensive test suite covering:
- Basic functionality
- Error handling
- Validation
- Multi-model testing
- Conversation history
- Cost estimation

Run tests with:
```bash
dotnet test
```

## 📋 Requirements

- .NET 8.0 or later
- Valid Mistral AI API key

## 🔧 Configuration

The SDK uses built-in .NET libraries and requires no additional dependencies.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📞 Support

- 📖 [CHANGELOG.md](CHANGELOG.md) - Detailed feature information and version history
- 🔗 [Mistral AI API Documentation](https://docs.mistral.ai/)
- 🐛 [GitHub Issues](https://github.com/yourusername/mistral-sdk-dotnet/issues)

## 🎯 Roadmap

Future versions will include:
- Streaming support
- Function calling
- Embeddings
- Fine-tuning support
- Rate limiting and retry logic

---

**Version**: 1.0.0 | **Last Updated**: December 2024 