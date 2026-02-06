using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MistralSDK.ChatCompletion;
using MistralSDK.Configuration;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MistralSDK.Caching
{
    /// <summary>
    /// In-memory implementation of the chat completion cache.
    /// Uses <see cref="IMemoryCache"/> for storage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This cache is useful for reducing API calls for identical requests.
    /// It is particularly effective for:
    /// - Repeated queries with the same parameters
    /// - Development and testing scenarios
    /// - Low-latency requirements where cache hits are acceptable
    /// </para>
    /// <para>
    /// Note: Caching is only appropriate when the same input should produce
    /// the same output. For creative or varied responses (high temperature),
    /// caching may not be desirable.
    /// </para>
    /// </remarks>
    public class MemoryChatCompletionCache : IChatCompletionCache, IDisposable
    {
        private readonly IMemoryCache _cache;
        private readonly MistralClientOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly bool _ownsCache;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance with the specified options.
        /// </summary>
        /// <param name="options">The client options containing cache configuration.</param>
        public MemoryChatCompletionCache(MistralClientOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _cache = new MemoryCache(new MemoryCacheOptions());
            _ownsCache = true;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
        }

        /// <summary>
        /// Initializes a new instance with the specified options (IOptions pattern).
        /// </summary>
        /// <param name="options">The client options wrapper.</param>
        public MemoryChatCompletionCache(IOptions<MistralClientOptions> options)
            : this(options?.Value ?? throw new ArgumentNullException(nameof(options)))
        {
        }

        /// <summary>
        /// Initializes a new instance with an existing cache and options.
        /// </summary>
        /// <param name="cache">The memory cache to use.</param>
        /// <param name="options">The client options containing cache configuration.</param>
        public MemoryChatCompletionCache(IMemoryCache cache, MistralClientOptions options)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _ownsCache = false;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
        }

        /// <summary>
        /// Initializes a new instance with an existing cache and options (IOptions pattern).
        /// </summary>
        /// <param name="cache">The memory cache to use.</param>
        /// <param name="options">The client options wrapper.</param>
        public MemoryChatCompletionCache(IMemoryCache cache, IOptions<MistralClientOptions> options)
            : this(cache, options?.Value ?? throw new ArgumentNullException(nameof(options)))
        {
        }

        /// <inheritdoc />
        public Task<MistralResponse?> GetAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
        {
            if (!_options.EnableCaching)
            {
                return Task.FromResult<MistralResponse?>(null);
            }

            var key = GenerateCacheKey(request);
            _cache.TryGetValue(key, out MistralResponse? response);
            return Task.FromResult(response);
        }

        /// <inheritdoc />
        public Task SetAsync(ChatCompletionRequest request, MistralResponse response, CancellationToken cancellationToken = default)
        {
            if (!_options.EnableCaching)
            {
                return Task.CompletedTask;
            }

            // Only cache successful responses
            if (!response.IsSuccess)
            {
                return Task.CompletedTask;
            }

            var key = GenerateCacheKey(request);
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheExpirationMinutes),
                SlidingExpiration = TimeSpan.FromMinutes(_options.CacheExpirationMinutes / 2.0)
            };

            _cache.Set(key, response, cacheOptions);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (_ownsCache && _cache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0); // Remove all entries
            }
        }

        /// <summary>
        /// Generates a cache key based on the request parameters.
        /// </summary>
        /// <param name="request">The chat completion request.</param>
        /// <returns>A unique cache key for the request.</returns>
        private string GenerateCacheKey(ChatCompletionRequest request)
        {
            // Create a deterministic representation of the request
            var keyData = new
            {
                request.Model,
                Messages = request.Messages?.Select(m => new { m.Role, m.Content }).ToList(),
                request.Temperature,
                request.TopP,
                request.MaxTokens,
                request.SafePrompt,
                request.RandomSeed // RandomSeed is part of the key for deterministic caching
            };

            var json = JsonSerializer.Serialize(keyData, _jsonOptions);
            
            // Generate a hash for a shorter, fixed-length key
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            var hash = Convert.ToBase64String(hashBytes);
            
            return $"mistral:chat:{hash}";
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && _ownsCache && _cache is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
