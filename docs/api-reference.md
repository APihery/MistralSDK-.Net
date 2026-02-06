# API Reference

Complete reference for all public types in MistralSDK.

## Namespaces

| Namespace | Description |
|-----------|-------------|
| `MistralSDK` | Core client and response types |
| `MistralSDK.Abstractions` | Interfaces |
| `MistralSDK.ChatCompletion` | Request/response models |
| `MistralSDK.Configuration` | Configuration options |
| `MistralSDK.Exceptions` | Custom exceptions |
| `MistralSDK.Extensions` | DI extensions |
| `MistralSDK.Caching` | Caching interfaces |

---

## MistralSDK

### MistralClient

Main client for interacting with the Mistral AI API.

```csharp
public class MistralClient : IMistralClient, IDisposable
```

#### Constructors

| Constructor | Description |
|-------------|-------------|
| `MistralClient(string apiKey)` | Create with API key |
| `MistralClient(MistralClientOptions options)` | Create with options |
| `MistralClient(HttpClient httpClient, MistralClientOptions options)` | Create with HttpClient |

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `ChatCompletionAsync(ChatCompletionRequest request, CancellationToken ct)` | `Task<MistralResponse>` | Send a chat completion request |
| `ValidateRequest(ChatCompletionRequest request)` | `ValidationResult` | Validate a request |
| `Dispose()` | `void` | Release resources |

### MistralResponse

Standardized response from the API.

```csharp
public class MistralResponse
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `StatusCode` | `int` | HTTP status code |
| `Message` | `string` | Response content or error message |
| `IsSuccess` | `bool` | Whether the request succeeded |
| `Model` | `string?` | Model used (success only) |
| `Usage` | `UsageInfo?` | Token usage (success only) |

---

## MistralSDK.Abstractions

### IMistralClient

Interface for the Mistral client.

```csharp
public interface IMistralClient : IDisposable
```

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `ChatCompletionAsync(ChatCompletionRequest request, CancellationToken ct)` | `Task<MistralResponse>` | Send request |
| `ValidateRequest(ChatCompletionRequest request)` | `ValidationResult` | Validate request |

### ValidationResult

Result of request validation.

```csharp
public class ValidationResult
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsValid` | `bool` | Whether validation passed |
| `Errors` | `IReadOnlyList<string>` | Validation errors |

---

## MistralSDK.ChatCompletion

### ChatCompletionRequest

Request for chat completion.

```csharp
public class ChatCompletionRequest
```

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Model` | `string` | Required | Model identifier |
| `Messages` | `List<MessageRequest>` | Required | Conversation messages |
| `Temperature` | `double` | 0.7 | Randomness (0.0-2.0) |
| `TopP` | `double` | 1.0 | Nucleus sampling (0.0-1.0) |
| `MaxTokens` | `int?` | null | Max tokens to generate |
| `SafePrompt` | `bool` | false | Enable safety filters |
| `RandomSeed` | `int?` | null | Seed for reproducibility |
| `Stream` | `bool` | false | Stream response |

### MessageRequest

A message in the conversation.

```csharp
public class MessageRequest
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Role` | `string` | Message role (system/user/assistant) |
| `Content` | `string` | Message content |

### MistralModels

Available model constants.

```csharp
public static class MistralModels
```

| Constant | Value |
|----------|-------|
| `Tiny` | `"mistral-tiny"` |
| `Small` | `"mistral-small-latest"` |
| `Medium` | `"mistral-medium-latest"` |
| `Large` | `"mistral-large-latest"` |

### MessageRoles

Message role constants.

```csharp
public static class MessageRoles
```

| Constant | Value |
|----------|-------|
| `System` | `"system"` |
| `User` | `"user"` |
| `Assistant` | `"assistant"` |

### UsageInfo

Token usage information.

```csharp
public class UsageInfo
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `PromptTokens` | `int` | Input tokens |
| `CompletionTokens` | `int` | Output tokens |
| `TotalTokens` | `int` | Total tokens |

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetEstimatedCost(string model)` | `decimal` | Estimated cost in USD |

---

## MistralSDK.Configuration

### MistralClientOptions

Configuration options for the client.

```csharp
public class MistralClientOptions
```

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ApiKey` | `string` | Required | API key |
| `BaseUrl` | `string` | `"https://api.mistral.ai/v1"` | API base URL |
| `TimeoutSeconds` | `int` | 30 | Request timeout |
| `MaxRetries` | `int` | 3 | Max retry attempts |
| `RetryDelayMilliseconds` | `int` | 1000 | Retry delay |
| `ValidateRequests` | `bool` | true | Validate before send |
| `ThrowOnError` | `bool` | false | Throw on errors |
| `EnableCaching` | `bool` | false | Enable caching |
| `CacheExpirationMinutes` | `int` | 5 | Cache TTL |

---

## MistralSDK.Exceptions

### MistralApiException

Base exception for API errors.

```csharp
public class MistralApiException : Exception
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `StatusCode` | `HttpStatusCode` | HTTP status |
| `ErrorType` | `string?` | API error type |
| `ErrorCode` | `string?` | API error code |
| `IsRetryable` | `bool` | Can be retried |
| `RetryDelaySeconds` | `int?` | Suggested retry delay |

### MistralValidationException

Validation failure exception.

```csharp
public class MistralValidationException : MistralApiException
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `ValidationErrors` | `IReadOnlyList<string>` | Validation errors |

### MistralAuthenticationException

Authentication failure exception.

```csharp
public class MistralAuthenticationException : MistralApiException
```

### MistralRateLimitException

Rate limit exceeded exception.

```csharp
public class MistralRateLimitException : MistralApiException
```

### MistralModelNotFoundException

Model not found exception.

```csharp
public class MistralModelNotFoundException : MistralApiException
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `ModelId` | `string` | The model that wasn't found |

---

## MistralSDK.Extensions

### ServiceCollectionExtensions

DI registration extensions.

```csharp
public static class ServiceCollectionExtensions
```

#### Methods

| Method | Description |
|--------|-------------|
| `AddMistralClient(Action<MistralClientOptions>)` | Register with action |
| `AddMistralClient(IConfigurationSection)` | Register from config |
| `AddMistralClientFromEnvironment(string?)` | Register from env var |

---

## MistralSDK.Caching

### IChatCompletionCache

Cache interface.

```csharp
public interface IChatCompletionCache
```

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetAsync(ChatCompletionRequest, CancellationToken)` | `Task<MistralResponse?>` | Get cached response |
| `SetAsync(ChatCompletionRequest, MistralResponse, CancellationToken)` | `Task` | Cache response |
| `Clear()` | `void` | Clear all entries |

### MemoryChatCompletionCache

In-memory cache implementation.

```csharp
public class MemoryChatCompletionCache : IChatCompletionCache, IDisposable
```
