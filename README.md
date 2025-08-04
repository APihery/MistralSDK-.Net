# Mistral AI SDK for .NET

A comprehensive .NET SDK for interacting with the Mistral AI API, providing easy-to-use methods for chat completions and other AI services.

## Features

- ✅ Full support for Mistral AI chat completion API
- ✅ Support for all Mistral models (Tiny, Small, Medium, Large)
- ✅ Comprehensive error handling and validation
- ✅ Type-safe request and response objects
- ✅ Full async/await support
- ✅ Resource management with IDisposable
- ✅ Built-in cost estimation
- ✅ XML documentation for all public APIs

## Quick Start

```csharp
using MistralSDK;
using MistralSDK.ChatCompletion;

// Initialize the client
var client = new MistralClient("your-api-key");

// Create a chat completion request
var request = new ChatCompletionRequest
{
    Model = MistralModels.MistralTiny,
    Messages = new List<MessageRequest>
    {
        new MessageRequest { Role = MessageRoles.User, Content = "Hello, how are you?" }
    },
    MaxTokens = 100,
    Temperature = 0.7
};

// Send the request
var response = await client.ChatCompletionAsync(request);

if (response.IsSuccess)
{
    Console.WriteLine($"Response: {response.Message}");
    Console.WriteLine($"Model: {response.Model}");
    Console.WriteLine($"Usage: {response.Usage?.GetEstimatedCost():C}");
}
else
{
    Console.WriteLine($"Error: {response.Message}");
}
```

## Installation

```bash
dotnet add package MistralSDK
```

## Requirements

- .NET 8.0 or later
- Valid Mistral AI API key

## Configuration

Get your API key from [Mistral AI Console](https://console.mistral.ai/).

## Testing

Run the test suite:

```bash
dotnet test
```

## Documentation

For detailed documentation and examples, see the [CHANGELOG.md](CHANGELOG.md) file.

## Roadmap

- [ ] Support for embeddings API
- [ ] Support for fine-tuning API
- [ ] Streaming responses
- [ ] Rate limiting and retry policies
- [ ] Additional model support

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details. 