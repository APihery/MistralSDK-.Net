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
| `ChatCompletionAsync(request, ct)` | `Task<MistralResponse>` | Send a chat completion request |
| `ChatCompletionStreamAsync(request, ct)` | `IAsyncEnumerable<StreamingChatCompletionChunk>` | Stream chat completion |
| `ChatCompletionStreamCollectAsync(request, onChunk, ct)` | `Task<StreamingChatCompletionResult>` | Stream and collect result |
| `ValidateRequest(request)` | `ValidationResult` | Validate a request |
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
| `ChatCompletionAsync(request, ct)` | `Task<MistralResponse>` | Send request |
| `ChatCompletionStreamAsync(request, ct)` | `IAsyncEnumerable<StreamingChatCompletionChunk>` | Stream request |
| `ChatCompletionStreamCollectAsync(request, onChunk, ct)` | `Task<StreamingChatCompletionResult>` | Stream and collect |
| `ValidateRequest(request)` | `ValidationResult` | Validate request |

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
| `Temperature` | `double?` | null | Randomness (0.0-2.0) |
| `TopP` | `double?` | null | Nucleus sampling (0.0-1.0) |
| `MaxTokens` | `int?` | null | Max tokens to generate |
| `N` | `int?` | null | Number of completions |
| `Stop` | `object?` | null | Stop sequence(s) |
| `FrequencyPenalty` | `double?` | null | Penalize frequent tokens (0.0-2.0) |
| `PresencePenalty` | `double?` | null | Encourage diversity (0.0-2.0) |
| `ResponseFormat` | `ResponseFormat?` | null | JSON mode or schema |
| `SafePrompt` | `bool` | false | Enable safety filters |
| `RandomSeed` | `int?` | null | Seed for reproducibility |
| `Stream` | `bool` | false | Stream response |
| `PromptMode` | `string?` | null | Prompt mode (e.g., "reasoning") |

#### Fluent Methods

| Method | Description |
|--------|-------------|
| `WithStop(string)` | Set single stop sequence |
| `WithStops(params string[])` | Set multiple stop sequences |
| `AsJson()` | Enable JSON mode |
| `AsJsonSchema(JsonSchema)` | Enable JSON schema mode |

### ResponseFormat

Specifies the output format.

```csharp
public class ResponseFormat
```

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `string` | Format type (text, json_object, json_schema) |
| `JsonSchema` | `JsonSchema?` | Schema for json_schema type |

### ResponseFormatType

Constants for response format types.

| Constant | Value | Description |
|----------|-------|-------------|
| `Text` | `"text"` | Standard text output |
| `JsonObject` | `"json_object"` | JSON mode |
| `JsonSchema` | `"json_schema"` | JSON with schema |

### JsonSchema

JSON schema definition for structured output.

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Schema name |
| `Description` | `string?` | Schema description |
| `Strict` | `bool` | Enable strict mode |
| `Schema` | `Dictionary<string, object>` | Schema definition |

### MessageRequest

A message in the conversation.

```csharp
public class MessageRequest
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Role` | `string` | Message role |
| `Content` | `string` | Message content |
| `Prefix` | `bool` | Prepend to assistant response |
| `ToolCallId` | `string?` | Tool call ID (for tool role) |
| `Name` | `string?` | Tool name (for tool role) |

#### Static Factory Methods

| Method | Description |
|--------|-------------|
| `System(content)` | Create system message |
| `User(content)` | Create user message |
| `Assistant(content, prefix)` | Create assistant message |
| `Tool(toolCallId, content, name)` | Create tool result message |

### MistralModels

Available model constants.

#### Premier Models

| Constant | Value | Description |
|----------|-------|-------------|
| `Large` | `"mistral-large-latest"` | Flagship model |
| `PixtralLarge` | `"pixtral-large-latest"` | Multimodal |
| `Saba` | `"mistral-saba-latest"` | ME/SA languages |

#### Efficient Models

| Constant | Value | Description |
|----------|-------|-------------|
| `Small` | `"mistral-small-latest"` | Balanced |
| `Ministral8B` | `"ministral-8b-latest"` | Edge deployment |
| `Ministral3B` | `"ministral-3b-latest"` | Ultra-efficient |

#### Specialized Models

| Constant | Value | Description |
|----------|-------|-------------|
| `Codestral` | `"codestral-latest"` | Code generation |
| `Pixtral` | `"pixtral-12b-2409"` | Free multimodal |
| `Embed` | `"mistral-embed"` | Embeddings |
| `Moderation` | `"mistral-moderation-latest"` | Content moderation |
| `Nemo` | `"open-mistral-nemo"` | Research model |

### MessageRoles

Message role constants.

| Constant | Value | Description |
|----------|-------|-------------|
| `System` | `"system"` | System instructions |
| `User` | `"user"` | User messages |
| `Assistant` | `"assistant"` | AI responses |
| `Tool` | `"tool"` | Tool results |

### PromptModes

Prompt mode constants.

| Constant | Value | Description |
|----------|-------|-------------|
| `Reasoning` | `"reasoning"` | Enhanced reasoning |

### StreamingChatCompletionChunk

A chunk in a streaming response.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Completion ID |
| `Model` | `string` | Model used |
| `Choices` | `List<StreamingChoice>` | Delta choices |
| `Usage` | `UsageInfo?` | Usage (final chunk) |
| `IsComplete` | `bool` | Is final chunk |

| Method | Returns | Description |
|--------|---------|-------------|
| `GetContent()` | `string` | Get content delta |

### StreamingChatCompletionResult

Complete result after streaming.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Completion ID |
| `Model` | `string` | Model used |
| `Content` | `string` | Complete content |
| `FinishReason` | `string?` | Why generation stopped |
| `Usage` | `UsageInfo?` | Token usage |
| `Chunks` | `List<...>` | All chunks |
| `ChunkCount` | `int` | Number of chunks |

### UsageInfo

Token usage information.

| Property | Type | Description |
|----------|------|-------------|
| `PromptTokens` | `int` | Input tokens |
| `CompletionTokens` | `int` | Output tokens |
| `TotalTokens` | `int` | Total tokens |

| Method | Returns | Description |
|--------|---------|-------------|
| `GetEstimatedCost(model)` | `decimal` | Estimated cost in USD |

---

## MistralSDK.Configuration

### MistralClientOptions

Configuration options for the client.

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

| Property | Type | Description |
|----------|------|-------------|
| `StatusCode` | `HttpStatusCode` | HTTP status |
| `ErrorType` | `string?` | API error type |
| `ErrorCode` | `string?` | API error code |
| `IsRetryable` | `bool` | Can be retried |
| `RetryDelaySeconds` | `int?` | Suggested retry delay |

### Derived Exceptions

| Exception | Description |
|-----------|-------------|
| `MistralValidationException` | Request validation failed |
| `MistralAuthenticationException` | Invalid API key |
| `MistralRateLimitException` | Rate limit exceeded |
| `MistralModelNotFoundException` | Model not found |

---

## MistralSDK.Extensions

### ServiceCollectionExtensions

DI registration extensions.

| Method | Description |
|--------|-------------|
| `AddMistralClient(Action<MistralClientOptions>)` | Register with action |
| `AddMistralClient(IConfigurationSection)` | Register from config |
| `AddMistralClientFromEnvironment(string?)` | Register from env var |

---

## MistralSDK.Caching

### IChatCompletionCache

Cache interface.

| Method | Returns | Description |
|--------|---------|-------------|
| `GetAsync(request, ct)` | `Task<MistralResponse?>` | Get cached response |
| `SetAsync(request, response, ct)` | `Task` | Cache response |
| `Clear()` | `void` | Clear all entries |

### MemoryChatCompletionCache

In-memory cache implementation using `IMemoryCache`.
