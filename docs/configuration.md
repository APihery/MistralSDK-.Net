# Configuration

## MistralClientOptions

The `MistralClientOptions` class provides comprehensive configuration for the client.

```csharp
using MistralSDK.Configuration;

var options = new MistralClientOptions
{
    // Required
    ApiKey = "your-api-key",
    
    // Optional - API settings
    BaseUrl = "https://api.mistral.ai/v1",  // Default
    TimeoutSeconds = 30,                      // Default: 30
    
    // Optional - Retry settings
    MaxRetries = 3,                           // Default: 3
    RetryDelayMilliseconds = 1000,            // Default: 1000
    
    // Optional - Behavior settings
    ValidateRequests = true,                  // Default: true
    ThrowOnError = false,                     // Default: false
    
    // Optional - Caching settings
    EnableCaching = false,                    // Default: false
    CacheExpirationMinutes = 5                // Default: 5
};

using var client = new MistralClient(options);
```

## Options Reference

### ApiKey (Required)

The API key for authentication with Mistral AI.

```csharp
options.ApiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY");
```

**Security Note:** Never hardcode your API key. Use environment variables or secure configuration.

### BaseUrl

The base URL for the Mistral AI API.

```csharp
options.BaseUrl = "https://api.mistral.ai/v1";  // Default
```

### TimeoutSeconds

HTTP request timeout in seconds.

```csharp
options.TimeoutSeconds = 60;  // For longer requests
```

### MaxRetries

Maximum number of retry attempts for transient failures.

```csharp
options.MaxRetries = 5;  // More retries for unreliable networks
```

### RetryDelayMilliseconds

Initial delay between retry attempts (used for exponential backoff).

```csharp
options.RetryDelayMilliseconds = 2000;  // 2 seconds
```

### ValidateRequests

When `true`, requests are validated before sending to the API.

```csharp
options.ValidateRequests = true;  // Recommended

// Validation checks:
// - Model is not empty
// - At least one message exists
// - Message roles are valid
// - Temperature is between 0.0 and 2.0
// - TopP is between 0.0 and 1.0
```

### ThrowOnError

When `true`, API errors throw exceptions instead of returning error responses.

```csharp
// Default behavior (ThrowOnError = false)
var response = await client.ChatCompletionAsync(request);
if (!response.IsSuccess)
{
    Console.WriteLine($"Error: {response.Message}");
}

// With ThrowOnError = true
options.ThrowOnError = true;
try
{
    var response = await client.ChatCompletionAsync(request);
    Console.WriteLine(response.Message);
}
catch (MistralApiException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

### EnableCaching

When `true`, successful responses are cached.

```csharp
options.EnableCaching = true;
options.CacheExpirationMinutes = 10;
```

See [Caching](caching.md) for more details.

## Configuration Sources

### Environment Variables

```csharp
var options = new MistralClientOptions
{
    ApiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY") 
             ?? throw new InvalidOperationException("API key not set")
};
```

### appsettings.json (ASP.NET Core)

```json
{
  "MistralApi": {
    "ApiKey": "",
    "BaseUrl": "https://api.mistral.ai/v1",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "ValidateRequests": true,
    "ThrowOnError": false,
    "EnableCaching": false,
    "CacheExpirationMinutes": 5
  }
}
```

```csharp
// In Program.cs
builder.Services.AddMistralClient(
    builder.Configuration.GetSection("MistralApi")
);
```

### User Secrets (Development)

```bash
dotnet user-secrets init
dotnet user-secrets set "MistralApi:ApiKey" "your-api-key"
```

## Constructors

### Simple Constructor

```csharp
// Just API key
using var client = new MistralClient("your-api-key");
```

### Options Constructor

```csharp
// With options object
var options = new MistralClientOptions { ApiKey = "your-api-key" };
using var client = new MistralClient(options);
```

### IOptions Constructor (DI)

```csharp
// With IOptions<MistralClientOptions> (injected)
public class MyService
{
    private readonly IMistralClient _client;
    
    public MyService(IMistralClient client)
    {
        _client = client;
    }
}
```

### HttpClient Constructor (Advanced)

```csharp
// With pre-configured HttpClient (from IHttpClientFactory)
var httpClient = httpClientFactory.CreateClient("MistralApi");
using var client = new MistralClient(httpClient, options);
```

## Next Steps

- [Dependency Injection](dependency-injection.md)
- [Error Handling](error-handling.md)
