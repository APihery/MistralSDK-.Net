# Caching

MistralSDK includes an optional response caching mechanism to reduce API calls for repeated identical requests.

## Enabling Caching

```csharp
var options = new MistralClientOptions
{
    ApiKey = apiKey,
    EnableCaching = true,
    CacheExpirationMinutes = 5  // Cache entries expire after 5 minutes
};

using var client = new MistralClient(options);
```

## How It Works

1. When a request is made, a cache key is generated from the request parameters
2. If a cached response exists and hasn't expired, it's returned immediately
3. If no cache exists, the API is called and the response is cached
4. Only successful responses (HTTP 2xx) are cached

## Cache Key Generation

The cache key is generated from:
- Model name
- Messages (role and content)
- Temperature
- TopP
- MaxTokens
- SafePrompt
- RandomSeed

**Note:** Two requests with identical parameters will share the same cache entry.

## When to Use Caching

### Good Use Cases

- **Repeated queries**: Same questions asked frequently
- **Development/Testing**: Reduce API calls during development
- **Static content**: Generating the same content repeatedly
- **Low temperature requests**: Deterministic responses (temperature ≈ 0)

### When NOT to Use Caching

- **Creative content**: High temperature, varied responses expected
- **Conversations**: Each message should be unique
- **Time-sensitive data**: Content that changes frequently
- **User-specific responses**: Personalized content

## Cache Interface

You can implement custom caching by implementing `IChatCompletionCache`:

```csharp
public interface IChatCompletionCache
{
    Task<MistralResponse?> GetAsync(
        ChatCompletionRequest request, 
        CancellationToken cancellationToken = default);
    
    Task SetAsync(
        ChatCompletionRequest request, 
        MistralResponse response, 
        CancellationToken cancellationToken = default);
    
    void Clear();
}
```

## Built-in Cache Implementation

### MemoryChatCompletionCache

In-memory cache using `IMemoryCache`:

```csharp
using MistralSDK.Caching;

// Standalone usage
var cache = new MemoryChatCompletionCache(options);

// With DI
services.AddSingleton<IChatCompletionCache, MemoryChatCompletionCache>();
```

## Custom Cache Implementation

### Redis Example

```csharp
public class RedisChatCompletionCache : IChatCompletionCache
{
    private readonly IDistributedCache _cache;
    private readonly MistralClientOptions _options;

    public RedisChatCompletionCache(
        IDistributedCache cache, 
        IOptions<MistralClientOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public async Task<MistralResponse?> GetAsync(
        ChatCompletionRequest request, 
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableCaching) return null;
        
        var key = GenerateKey(request);
        var cached = await _cache.GetStringAsync(key, cancellationToken);
        
        if (cached == null) return null;
        
        return JsonSerializer.Deserialize<MistralResponse>(cached);
    }

    public async Task SetAsync(
        ChatCompletionRequest request, 
        MistralResponse response, 
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableCaching || !response.IsSuccess) return;
        
        var key = GenerateKey(request);
        var json = JsonSerializer.Serialize(response);
        
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = 
                TimeSpan.FromMinutes(_options.CacheExpirationMinutes)
        };
        
        await _cache.SetStringAsync(key, json, cacheOptions, cancellationToken);
    }

    public void Clear()
    {
        // Redis doesn't support clear all easily
        // Consider using key patterns or separate database
    }

    private string GenerateKey(ChatCompletionRequest request)
    {
        // Generate a unique key based on request parameters
        var json = JsonSerializer.Serialize(request);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return $"mistral:chat:{Convert.ToBase64String(hash)}";
    }
}
```

## Cache Considerations

### Memory Usage

The in-memory cache stores entire response objects. For high-volume applications, consider:
- Setting appropriate expiration times
- Monitoring memory usage
- Using distributed cache for multi-instance deployments

### Cache Invalidation

The cache uses time-based expiration. There's no automatic invalidation based on content changes.

### Thread Safety

The built-in `MemoryChatCompletionCache` is thread-safe.

## Next Steps

- [Testing](testing.md)
- [API Reference](api-reference.md)
