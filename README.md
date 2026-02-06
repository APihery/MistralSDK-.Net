# Mistral AI SDK for .NET

A comprehensive .NET SDK for interacting with the Mistral AI API, providing easy-to-use methods for chat completions and other AI services.

## Features

- ✅ Full support for Mistral AI chat completion API
- ✅ Support for all Mistral models (Tiny, Small, Medium, Large)
- ✅ Comprehensive error handling with custom exceptions
- ✅ Type-safe request and response objects
- ✅ Full async/await support with cancellation tokens
- ✅ Resource management with IDisposable
- ✅ Built-in cost estimation
- ✅ XML documentation for all public APIs
- ✅ **Interface-based design** (`IMistralClient`) for easy mocking and testing
- ✅ **IHttpClientFactory support** for proper HttpClient lifecycle management
- ✅ **Dependency Injection support** for ASP.NET Core and other DI frameworks
- ✅ **Request validation** before sending to the API
- ✅ **Optional response caching** to reduce API calls
- ✅ **Configurable options** via `MistralClientOptions`

## Quick Start

### Basic Usage

```csharp
using MistralSDK;
using MistralSDK.ChatCompletion;

// Initialize the client with API key from environment variable
var apiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY");
using var client = new MistralClient(apiKey);

// Create a chat completion request
var request = new ChatCompletionRequest
{
    Model = MistralModels.Small,
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
    Console.WriteLine($"Estimated cost: ${response.Usage?.GetEstimatedCost(response.Model):F6}");
}
else
{
    Console.WriteLine($"Error ({response.StatusCode}): {response.Message}");
}
```

### Using Dependency Injection (ASP.NET Core)

```csharp
// In Program.cs or Startup.cs
using MistralSDK.Extensions;

// Option 1: Configure with action
builder.Services.AddMistralClient(options =>
{
    options.ApiKey = builder.Configuration["MistralApi:ApiKey"];
    options.TimeoutSeconds = 60;
    options.MaxRetries = 3;
});

// Option 2: Configure from appsettings.json
builder.Services.AddMistralClient(builder.Configuration.GetSection("MistralApi"));

// Option 3: Use environment variable
builder.Services.AddMistralClientFromEnvironment();
```

```csharp
// In your service or controller
public class ChatService
{
    private readonly IMistralClient _mistralClient;

    public ChatService(IMistralClient mistralClient)
    {
        _mistralClient = mistralClient;
    }

    public async Task<string> GetResponseAsync(string userMessage)
    {
        var request = new ChatCompletionRequest
        {
            Model = MistralModels.Small,
            Messages = new List<MessageRequest>
            {
                new MessageRequest { Role = MessageRoles.User, Content = userMessage }
            }
        };

        var response = await _mistralClient.ChatCompletionAsync(request);
        return response.IsSuccess ? response.Message : $"Error: {response.Message}";
    }
}
```

### Using Custom Options

```csharp
using MistralSDK;
using MistralSDK.Configuration;

var options = new MistralClientOptions
{
    ApiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY"),
    BaseUrl = "https://api.mistral.ai/v1",
    TimeoutSeconds = 60,
    MaxRetries = 3,
    ValidateRequests = true,
    ThrowOnError = true,  // Throw exceptions instead of returning error responses
    EnableCaching = true,
    CacheExpirationMinutes = 5
};

using var client = new MistralClient(options);
```

## Installation

```bash
dotnet add package MistralSDK
```

## Requirements

- .NET 8.0 or later
- Valid Mistral AI API key

## Configuration

### API Key Security

**Important:** Never commit your API key to source control!

1. Get your API key from [Mistral AI Console](https://console.mistral.ai/)
2. Store it securely using one of these methods:

```bash
# Environment variable (recommended for development)
export MISTRAL_API_KEY="your-api-key"

# Or on Windows
set MISTRAL_API_KEY=your-api-key
```

```json
// appsettings.json (use with user secrets in development)
{
  "MistralApi": {
    "ApiKey": "", // Leave empty, use user secrets or environment variables
    "BaseUrl": "https://api.mistral.ai/v1",
    "TimeoutSeconds": 30
  }
}
```

```bash
# User secrets (development only)
dotnet user-secrets set "MistralApi:ApiKey" "your-api-key"
```

## Error Handling

### Using Response Objects

```csharp
var response = await client.ChatCompletionAsync(request);

if (response.IsSuccess)
{
    Console.WriteLine(response.Message);
}
else
{
    Console.WriteLine($"Error {response.StatusCode}: {response.Message}");
}
```

### Using Exceptions

```csharp
var options = new MistralClientOptions
{
    ApiKey = apiKey,
    ThrowOnError = true  // Enable exception throwing
};

using var client = new MistralClient(options);

try
{
    var response = await client.ChatCompletionAsync(request);
    Console.WriteLine(response.Message);
}
catch (MistralValidationException ex)
{
    Console.WriteLine($"Validation failed: {string.Join(", ", ex.ValidationErrors)}");
}
catch (MistralAuthenticationException ex)
{
    Console.WriteLine("Invalid API key");
}
catch (MistralRateLimitException ex)
{
    Console.WriteLine($"Rate limited. Retry after {ex.RetryDelaySeconds} seconds");
}
catch (MistralApiException ex)
{
    Console.WriteLine($"API error: {ex.Message}");
    if (ex.IsRetryable)
    {
        Console.WriteLine($"Retry after {ex.RetryDelaySeconds} seconds");
    }
}
```

## Testing

### Run All Tests

```bash
dotnet test
```

### Run Unit Tests Only

```bash
dotnet test --filter "TestCategory=Unit"
```

### Run Integration Tests

Integration tests require a valid API key:

```bash
# Set environment variables
export MISTRAL_API_KEY="your-api-key"
export MISTRAL_ENABLE_INTEGRATION_TESTS="true"

# Run integration tests
dotnet test --filter "TestCategory=Integration"
```

### Run Tests with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Available Models

```csharp
MistralModels.Tiny    // "mistral-tiny" - Fastest, most cost-effective
MistralModels.Small   // "mistral-small-latest" - Good balance
MistralModels.Medium  // "mistral-medium-latest" - High quality
MistralModels.Large   // "mistral-large-latest" - Best quality
```

## Roadmap

- [ ] Support for embeddings API
- [ ] Support for fine-tuning API
- [ ] Streaming responses
- [ ] Automatic retry with exponential backoff
- [ ] Additional model support
- [ ] Function calling support

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details. 