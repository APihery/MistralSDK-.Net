# Error Handling

MistralSDK provides two approaches to error handling: response objects and exceptions.

## Response-Based Error Handling (Default)

By default, errors are returned in the response object.

```csharp
var response = await client.ChatCompletionAsync(request);

if (response.IsSuccess)
{
    Console.WriteLine($"Success: {response.Message}");
}
else
{
    Console.WriteLine($"Error {response.StatusCode}: {response.Message}");
}
```

### Response Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsSuccess` | `bool` | Whether the request succeeded |
| `StatusCode` | `int` | HTTP status code |
| `Message` | `string` | Response content or error message |
| `Model` | `string?` | Model used (success only) |
| `Usage` | `UsageInfo?` | Token usage (success only) |

### Common Status Codes

| Code | Meaning | Action |
|------|---------|--------|
| 200 | Success | Process response |
| 400 | Bad Request | Check request parameters |
| 401 | Unauthorized | Check API key |
| 403 | Forbidden | Check permissions |
| 404 | Not Found | Check model name |
| 422 | Validation Error | Check request format |
| 429 | Rate Limited | Wait and retry |
| 500 | Server Error | Retry later |

## Exception-Based Error Handling

Enable exceptions with `ThrowOnError = true`:

```csharp
var options = new MistralClientOptions
{
    ApiKey = apiKey,
    ThrowOnError = true
};

using var client = new MistralClient(options);

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

## Exception Types

### MistralApiException (Base)

Base exception for all API errors.

```csharp
catch (MistralApiException ex)
{
    Console.WriteLine($"Status: {ex.StatusCode}");
    Console.WriteLine($"Message: {ex.Message}");
    Console.WriteLine($"Type: {ex.ErrorType}");
    Console.WriteLine($"Code: {ex.ErrorCode}");
    Console.WriteLine($"Retryable: {ex.IsRetryable}");
    Console.WriteLine($"Retry after: {ex.RetryDelaySeconds}s");
}
```

### MistralValidationException

Thrown when request validation fails.

```csharp
catch (MistralValidationException ex)
{
    Console.WriteLine("Validation errors:");
    foreach (var error in ex.ValidationErrors)
    {
        Console.WriteLine($"  - {error}");
    }
}
```

### MistralAuthenticationException

Thrown when authentication fails (invalid API key).

```csharp
catch (MistralAuthenticationException ex)
{
    Console.WriteLine("Authentication failed. Check your API key.");
}
```

### MistralRateLimitException

Thrown when rate limit is exceeded.

```csharp
catch (MistralRateLimitException ex)
{
    Console.WriteLine($"Rate limited. Retry after {ex.RetryDelaySeconds}s");
    await Task.Delay(TimeSpan.FromSeconds(ex.RetryDelaySeconds ?? 60));
    // Retry...
}
```

### MistralModelNotFoundException

Thrown when the specified model is not found.

```csharp
catch (MistralModelNotFoundException ex)
{
    Console.WriteLine($"Model '{ex.ModelId}' not found");
}
```

## Complete Error Handling Example

```csharp
var options = new MistralClientOptions
{
    ApiKey = apiKey,
    ThrowOnError = true,
    ValidateRequests = true
};

using var client = new MistralClient(options);

try
{
    var response = await client.ChatCompletionAsync(request);
    Console.WriteLine(response.Message);
}
catch (MistralValidationException ex)
{
    // Request validation failed (client-side)
    Console.WriteLine("Invalid request:");
    foreach (var error in ex.ValidationErrors)
    {
        Console.WriteLine($"  - {error}");
    }
}
catch (MistralAuthenticationException ex)
{
    // Invalid or expired API key
    Console.WriteLine("Please check your API key");
}
catch (MistralRateLimitException ex)
{
    // Too many requests
    Console.WriteLine($"Rate limited. Wait {ex.RetryDelaySeconds}s");
}
catch (MistralModelNotFoundException ex)
{
    // Invalid model name
    Console.WriteLine($"Model not found: {ex.ModelId}");
}
catch (MistralApiException ex) when (ex.IsRetryable)
{
    // Transient error - can retry
    Console.WriteLine($"Temporary error. Retry in {ex.RetryDelaySeconds}s");
}
catch (MistralApiException ex)
{
    // Other API errors
    Console.WriteLine($"API error: {ex.Message}");
}
catch (OperationCanceledException)
{
    // Request was cancelled
    Console.WriteLine("Request cancelled");
}
catch (Exception ex)
{
    // Unexpected errors
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## Retryable Errors

Some errors are transient and can be retried:

```csharp
catch (MistralApiException ex) when (ex.IsRetryable)
{
    // These status codes are retryable:
    // - 429 Too Many Requests
    // - 500 Internal Server Error
    // - 502 Bad Gateway
    // - 503 Service Unavailable
    // - 504 Gateway Timeout
    
    var delay = ex.RetryDelaySeconds ?? 5;
    await Task.Delay(TimeSpan.FromSeconds(delay));
    
    // Retry the request...
}
```

## Client-Side Validation

Enable `ValidateRequests` to catch errors before sending:

```csharp
options.ValidateRequests = true;

// Or validate manually:
var validationResult = client.ValidateRequest(request);

if (!validationResult.IsValid)
{
    Console.WriteLine("Validation errors:");
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
}
```

## Cancellation

Use `CancellationToken` to cancel long-running requests:

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    var response = await client.ChatCompletionAsync(request, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Request timed out or was cancelled");
}
```

## Next Steps

- [Caching](caching.md)
- [Testing](testing.md)
