# Dependency Injection

MistralSDK provides full support for dependency injection in ASP.NET Core and other DI frameworks.

## ASP.NET Core Integration

### Basic Setup

```csharp
// Program.cs
using MistralSDK.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add MistralClient to DI container
builder.Services.AddMistralClient(options =>
{
    options.ApiKey = builder.Configuration["MistralApi:ApiKey"];
});

var app = builder.Build();
```

### Configuration from appsettings.json

```json
// appsettings.json
{
  "MistralApi": {
    "ApiKey": "",
    "BaseUrl": "https://api.mistral.ai/v1",
    "TimeoutSeconds": 60,
    "MaxRetries": 3,
    "ValidateRequests": true,
    "ThrowOnError": false
  }
}
```

```csharp
// Program.cs
builder.Services.AddMistralClient(
    builder.Configuration.GetSection("MistralApi")
);
```

### Using Environment Variables

```csharp
// Automatically loads from MISTRAL_API_KEY environment variable
builder.Services.AddMistralClientFromEnvironment();

// Or specify a custom environment variable name
builder.Services.AddMistralClientFromEnvironment("MY_CUSTOM_API_KEY");
```

## Using the Client

### In a Controller

```csharp
using MistralSDK.Abstractions;
using MistralSDK.ChatCompletion;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IMistralClient _mistralClient;

    public ChatController(IMistralClient mistralClient)
    {
        _mistralClient = mistralClient;
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] string message)
    {
        var request = new ChatCompletionRequest
        {
            Model = MistralModels.Small,
            Messages = new List<MessageRequest>
            {
                new MessageRequest 
                { 
                    Role = MessageRoles.User, 
                    Content = message 
                }
            }
        };

        var response = await _mistralClient.ChatCompletionAsync(request);

        if (response.IsSuccess)
        {
            return Ok(new { response = response.Message });
        }

        return StatusCode(response.StatusCode, new { error = response.Message });
    }
}
```

### In a Service

```csharp
public interface IChatService
{
    Task<string> GetResponseAsync(string userMessage);
}

public class ChatService : IChatService
{
    private readonly IMistralClient _mistralClient;
    private readonly ILogger<ChatService> _logger;

    public ChatService(IMistralClient mistralClient, ILogger<ChatService> logger)
    {
        _mistralClient = mistralClient;
        _logger = logger;
    }

    public async Task<string> GetResponseAsync(string userMessage)
    {
        var request = new ChatCompletionRequest
        {
            Model = MistralModels.Small,
            Messages = new List<MessageRequest>
            {
                new MessageRequest 
                { 
                    Role = MessageRoles.User, 
                    Content = userMessage 
                }
            }
        };

        var response = await _mistralClient.ChatCompletionAsync(request);

        if (response.IsSuccess)
        {
            _logger.LogInformation("Chat completed. Tokens: {Tokens}", 
                response.Usage?.TotalTokens);
            return response.Message;
        }

        _logger.LogError("Chat failed: {Error}", response.Message);
        throw new Exception(response.Message);
    }
}
```

```csharp
// Register the service
builder.Services.AddScoped<IChatService, ChatService>();
```

### In a Minimal API

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMistralClientFromEnvironment();

var app = builder.Build();

app.MapPost("/chat", async (string message, IMistralClient client) =>
{
    var request = new ChatCompletionRequest
    {
        Model = MistralModels.Small,
        Messages = new List<MessageRequest>
        {
            new MessageRequest { Role = MessageRoles.User, Content = message }
        }
    };

    var response = await client.ChatCompletionAsync(request);
    
    return response.IsSuccess 
        ? Results.Ok(response.Message) 
        : Results.Problem(response.Message);
});

app.Run();
```

## HttpClientFactory Integration

The SDK uses `IHttpClientFactory` internally, which provides:

- Proper HttpClient lifecycle management
- Automatic connection pooling
- No socket exhaustion issues
- Polly integration support

### Custom HttpClient Configuration

```csharp
builder.Services.AddHttpClient<IMistralClient, MistralClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<MistralClientOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
})
.AddPolicyHandler(GetRetryPolicy());  // Add Polly retry policy

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
```

## Service Lifetime

The `IMistralClient` is registered as a **typed HttpClient**, which means:
- A new instance is created for each request
- The underlying HttpClient is pooled and reused
- No need to worry about disposal in your code

## Testing with DI

```csharp
// In your test setup
var mockClient = new Mock<IMistralClient>();
mockClient
    .Setup(x => x.ChatCompletionAsync(It.IsAny<ChatCompletionRequest>(), default))
    .ReturnsAsync(new MistralResponse(200) 
    { 
        IsSuccess = true, 
        Message = "Test response" 
    });

services.AddSingleton(mockClient.Object);
```

## Next Steps

- [Error Handling](error-handling.md)
- [Caching](caching.md)
- [Testing](testing.md)
