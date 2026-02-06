# Streaming

MistralSDK supports streaming responses, allowing you to receive tokens as they are generated rather than waiting for the complete response.

## Why Use Streaming?

- **Lower perceived latency**: Users see content immediately
- **Real-time feedback**: Show typing indicators or progressive content
- **Early termination**: Cancel generation if the response is going off-track
- **Memory efficiency**: Process tokens as they arrive

## Basic Streaming

Use `ChatCompletionStreamAsync` to receive an `IAsyncEnumerable` of chunks:

```csharp
var request = new ChatCompletionRequest
{
    Model = MistralModels.Small,
    Messages = new List<MessageRequest>
    {
        MessageRequest.User("Write a short story about a robot.")
    }
};

await foreach (var chunk in client.ChatCompletionStreamAsync(request))
{
    // Get the content delta from this chunk
    var content = chunk.GetContent();
    Console.Write(content); // No newline - build up the response
}
Console.WriteLine(); // Final newline
```

## Streaming with Callback

Use `ChatCompletionStreamCollectAsync` to stream and collect the final result:

```csharp
var result = await client.ChatCompletionStreamCollectAsync(
    request,
    onChunk: chunk =>
    {
        // Called for each chunk
        Console.Write(chunk.GetContent());
    }
);

// Access complete response
Console.WriteLine($"\n\nTotal chunks: {result.ChunkCount}");
Console.WriteLine($"Complete content length: {result.Content.Length}");
Console.WriteLine($"Finish reason: {result.FinishReason}");
Console.WriteLine($"Tokens used: {result.Usage?.TotalTokens}");
```

## StreamingChatCompletionChunk Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique completion identifier |
| `Model` | `string` | Model used |
| `Choices` | `List<StreamingChoice>` | Delta choices |
| `Usage` | `UsageInfo?` | Token usage (final chunk only) |
| `IsComplete` | `bool` | Whether this is the final chunk |

### Getting Content

```csharp
// Simple way - get content from first choice
var content = chunk.GetContent();

// Detailed way - access delta directly
if (chunk.Choices?.Count > 0)
{
    var delta = chunk.Choices[0].Delta;
    var role = delta?.Role;      // Usually only in first chunk
    var text = delta?.Content;   // Text delta
}
```

## StreamingChatCompletionResult

When using `ChatCompletionStreamCollectAsync`:

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Completion identifier |
| `Model` | `string` | Model used |
| `Content` | `string` | Complete accumulated content |
| `FinishReason` | `string?` | Why generation stopped |
| `Usage` | `UsageInfo?` | Token usage |
| `Chunks` | `List<...>` | All received chunks |
| `ChunkCount` | `int` | Number of chunks |

## Cancellation

Streaming supports cancellation tokens:

```csharp
var cts = new CancellationTokenSource();

// Cancel after 5 seconds
cts.CancelAfter(TimeSpan.FromSeconds(5));

try
{
    await foreach (var chunk in client.ChatCompletionStreamAsync(request, cts.Token))
    {
        Console.Write(chunk.GetContent());
        
        // Or cancel based on content
        if (chunk.GetContent().Contains("stop word"))
        {
            cts.Cancel();
        }
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("\nStreaming cancelled.");
}
```

## Error Handling

Errors during streaming throw exceptions:

```csharp
try
{
    await foreach (var chunk in client.ChatCompletionStreamAsync(request))
    {
        Console.Write(chunk.GetContent());
    }
}
catch (MistralApiException ex)
{
    Console.WriteLine($"API Error: {ex.Message}");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Network Error: {ex.Message}");
}
```

## ASP.NET Core Example

Streaming to a web client:

```csharp
[HttpPost("stream")]
public async IAsyncEnumerable<string> StreamChat(
    [FromBody] string message,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    var request = new ChatCompletionRequest
    {
        Model = MistralModels.Small,
        Messages = new List<MessageRequest>
        {
            MessageRequest.User(message)
        }
    };

    await foreach (var chunk in _client.ChatCompletionStreamAsync(request, cancellationToken))
    {
        yield return chunk.GetContent();
    }
}
```

## Server-Sent Events (SSE)

For real-time web applications:

```csharp
[HttpGet("sse")]
public async Task StreamSSE([FromQuery] string message, CancellationToken cancellationToken)
{
    Response.ContentType = "text/event-stream";
    
    var request = new ChatCompletionRequest
    {
        Model = MistralModels.Small,
        Messages = new List<MessageRequest>
        {
            MessageRequest.User(message)
        }
    };

    await foreach (var chunk in _client.ChatCompletionStreamAsync(request, cancellationToken))
    {
        var content = chunk.GetContent();
        if (!string.IsNullOrEmpty(content))
        {
            await Response.WriteAsync($"data: {content}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
    
    await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
}
```

## Performance Tips

1. **Use streaming for long responses**: The latency benefit increases with response length
2. **Avoid heavy processing per chunk**: Keep chunk handling lightweight
3. **Buffer if needed**: For UI updates, consider buffering a few chunks before rendering
4. **Monitor cancellation**: Check `cancellationToken.IsCancellationRequested` in long operations

## Next Steps

- [Configuration](configuration.md) - Configure streaming timeouts
- [Error Handling](error-handling.md) - Handle streaming errors
