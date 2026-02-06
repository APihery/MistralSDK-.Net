using MistralSDK.ChatCompletion;
using MistralSDK.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MistralSDK.Abstractions
{
    /// <summary>
    /// Defines the contract for a Mistral AI API client.
    /// This interface enables dependency injection and unit testing with mocks.
    /// </summary>
    public interface IMistralClient : IDisposable
    {
        /// <summary>
        /// Sends a chat completion request to the Mistral AI API.
        /// </summary>
        /// <param name="request">The chat completion request containing the model, messages, and parameters.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A <see cref="MistralResponse"/> object containing the API response or error information.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
        /// <exception cref="MistralApiException">Thrown when the API returns an error.</exception>
        Task<MistralResponse> ChatCompletionAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a streaming chat completion request to the Mistral AI API.
        /// Tokens are returned as they are generated.
        /// </summary>
        /// <param name="request">The chat completion request. The Stream property will be automatically set to true.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>An async enumerable of streaming chunks.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
        /// <exception cref="MistralApiException">Thrown when the API returns an error.</exception>
        IAsyncEnumerable<StreamingChatCompletionChunk> ChatCompletionStreamAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a streaming chat completion request and collects all chunks into a single result.
        /// Useful when you want streaming behavior but need the complete response.
        /// </summary>
        /// <param name="request">The chat completion request.</param>
        /// <param name="onChunk">Optional callback invoked for each chunk received.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The complete streaming result with all accumulated content.</returns>
        Task<StreamingChatCompletionResult> ChatCompletionStreamCollectAsync(
            ChatCompletionRequest request, 
            Action<StreamingChatCompletionChunk>? onChunk = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a chat completion request without sending it to the API.
        /// </summary>
        /// <param name="request">The request to validate.</param>
        /// <returns>A validation result indicating whether the request is valid.</returns>
        ValidationResult ValidateRequest(ChatCompletionRequest request);
    }

    /// <summary>
    /// Represents the result of a validation operation.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets a value indicating whether the validation passed.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the collection of validation errors, if any.
        /// </summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResult"/> class.
        /// </summary>
        /// <param name="isValid">Whether the validation passed.</param>
        /// <param name="errors">The collection of validation errors.</param>
        public ValidationResult(bool isValid, IReadOnlyList<string>? errors = null)
        {
            IsValid = isValid;
            Errors = errors ?? Array.Empty<string>();
        }

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <returns>A successful validation result.</returns>
        public static ValidationResult Success() => new ValidationResult(true);

        /// <summary>
        /// Creates a failed validation result with the specified errors.
        /// </summary>
        /// <param name="errors">The validation errors.</param>
        /// <returns>A failed validation result.</returns>
        public static ValidationResult Failure(params string[] errors) => new ValidationResult(false, errors);
    }
}
