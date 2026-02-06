using MistralSDK.ChatCompletion;
using System.Threading;
using System.Threading.Tasks;

namespace MistralSDK.Caching
{
    /// <summary>
    /// Defines the contract for caching chat completion responses.
    /// </summary>
    public interface IChatCompletionCache
    {
        /// <summary>
        /// Attempts to get a cached response for the given request.
        /// </summary>
        /// <param name="request">The chat completion request.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The cached response, or null if not found.</returns>
        Task<MistralResponse?> GetAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores a response in the cache.
        /// </summary>
        /// <param name="request">The chat completion request (used as the key).</param>
        /// <param name="response">The response to cache.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        Task SetAsync(ChatCompletionRequest request, MistralResponse response, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes all cached entries.
        /// </summary>
        void Clear();
    }
}
