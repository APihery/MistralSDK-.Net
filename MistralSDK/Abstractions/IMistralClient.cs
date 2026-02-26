using MistralSDK.Agents;
using MistralSDK.Audio;
using MistralSDK.Batch;
using MistralSDK.ChatCompletion;
using MistralSDK.Classifiers;
using MistralSDK.Embeddings;
using MistralSDK.Exceptions;
using MistralSDK.Files;
using MistralSDK.Fim;
using MistralSDK.FineTuning;
using MistralSDK.Models;
using MistralSDK.Ocr;
using System;
using System.Collections.Generic;
using System.IO;
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
        /// Chat completion (convenience overload). Uses default model.
        /// </summary>
        Task<MistralResponse> ChatCompletionAsync(string userMessage, string? model = null, int? maxTokens = null, CancellationToken cancellationToken = default);

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

        #region Embeddings API

        /// <summary>
        /// Creates embeddings for text. Use mistral-embed for text, codestral-embed for code.
        /// </summary>
        /// <param name="request">Embedding request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="MistralResponse"/> with <see cref="EmbeddingResponse"/> in Data when successful.</returns>
        Task<MistralResponse> EmbeddingsCreateAsync(EmbeddingRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates embeddings for a single text (convenience overload).
        /// </summary>
        Task<MistralResponse> EmbeddingsCreateAsync(string text, string? model = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates embeddings for multiple texts (convenience overload).
        /// </summary>
        Task<MistralResponse> EmbeddingsCreateAsync(string[] texts, string? model = null, CancellationToken cancellationToken = default);

        #endregion

        #region Classifiers API

        /// <summary>
        /// Moderates text for safety (POST /v1/moderations).
        /// </summary>
        /// <returns><see cref="MistralResponse"/> with <see cref="ModerationResponse"/> in Data when successful.</returns>
        Task<MistralResponse> ModerationsCreateAsync(ModerationRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Moderates text (convenience overload).
        /// </summary>
        Task<MistralResponse> ModerateAsync(string text, string? model = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Moderates chat messages (POST /v1/chat/moderations).
        /// </summary>
        Task<MistralResponse> ChatModerationsCreateAsync(ChatModerationRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Classifies text (POST /v1/classifications).
        /// </summary>
        /// <returns><see cref="MistralResponse"/> with <see cref="ClassificationResponse"/> in Data when successful.</returns>
        Task<MistralResponse> ClassificationsCreateAsync(ModerationRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Classifies chat (POST /v1/chat/classifications).
        /// </summary>
        Task<MistralResponse> ChatClassificationsCreateAsync(ChatClassificationRequest request, CancellationToken cancellationToken = default);

        #endregion

        #region Agents API

        /// <summary>
        /// Sends an agent completion request to the Mistral AI API.
        /// Uses an agent ID instead of a model. See <see href="https://docs.mistral.ai/api/endpoint/agents"/>.
        /// </summary>
        /// <param name="request">The agent completion request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="MistralResponse"/> with <see cref="ChatCompletionResponse"/> in Data when successful.</returns>
        Task<MistralResponse> AgentCompletionAsync(AgentCompletionRequest request, CancellationToken cancellationToken = default);

        #endregion

        #region Models API

        /// <summary>
        /// Lists all models available to the user.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of models.</returns>
        Task<ModelListResponse> ModelsListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves information about a specific model.
        /// </summary>
        /// <param name="modelId">The model ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Model card.</returns>
        Task<ModelCard> ModelsRetrieveAsync(string modelId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a fine-tuned model.
        /// </summary>
        /// <param name="modelId">The model ID to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Delete result.</returns>
        Task<ModelDeleteResponse> ModelsDeleteAsync(string modelId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a fine-tuned model's name or description.
        /// </summary>
        /// <param name="modelId">The model ID.</param>
        /// <param name="request">Update request (name, description).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Updated model card.</returns>
        Task<ModelCard> ModelsUpdateAsync(string modelId, UpdateFTModelRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Archives a fine-tuned model.
        /// </summary>
        /// <param name="modelId">The model ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Archive result.</returns>
        Task<ArchiveFTModelResponse> ModelsArchiveAsync(string modelId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unarchives a fine-tuned model.
        /// </summary>
        /// <param name="modelId">The model ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Unarchive result.</returns>
        Task<UnarchiveFTModelResponse> ModelsUnarchiveAsync(string modelId, CancellationToken cancellationToken = default);

        #endregion

        #region FIM API

        /// <summary>
        /// Fill-in-the-Middle completion for code (Codestral). See <see href="https://docs.mistral.ai/api/endpoint/fim"/>.
        /// </summary>
        /// <param name="request">FIM request with prompt and optional suffix.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="MistralResponse"/> with <see cref="ChatCompletionResponse"/> in Data when successful.</returns>
        Task<MistralResponse> FimCompletionAsync(FimCompletionRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// FIM completion (convenience overload).
        /// </summary>
        Task<MistralResponse> FimCompletionAsync(string prompt, string? suffix = null, int? maxTokens = null, string? model = null, CancellationToken cancellationToken = default);

        #endregion

        #region Batch API

        /// <summary>
        /// Lists batch jobs with optional filters.
        /// </summary>
        /// <param name="limit">Max jobs to return. Default 20.</param>
        /// <param name="after">Cursor for pagination.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<BatchJobsListResponse> BatchJobsListAsync(int limit = 20, string? after = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a batch job.
        /// </summary>
        /// <param name="request">Batch job creation request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<BatchJobResponse> BatchJobCreateAsync(BatchJobCreateRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a batch job by ID.
        /// </summary>
        /// <param name="jobId">Batch job ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<BatchJobResponse> BatchJobGetAsync(string jobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels a batch job.
        /// </summary>
        /// <param name="jobId">Batch job ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<BatchJobResponse> BatchJobCancelAsync(string jobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all batch jobs (paginates automatically).
        /// </summary>
        Task<IReadOnlyList<BatchJobResponse>> BatchJobsListAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Waits until a batch job completes (success, failed, or cancelled).
        /// </summary>
        /// <param name="jobId">Batch job ID.</param>
        /// <param name="pollIntervalMs">Interval between status checks. Default 5000.</param>
        /// <param name="timeoutMs">Maximum wait time. Default 86400000 (24h).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<BatchJobResponse> BatchJobWaitUntilCompleteAsync(string jobId, int pollIntervalMs = 5000, int timeoutMs = 86400000, CancellationToken cancellationToken = default);

        #endregion

        #region Fine-Tuning API

        /// <summary>
        /// Lists fine-tuning jobs with optional filters.
        /// </summary>
        /// <param name="limit">Max jobs to return. Default 20.</param>
        /// <param name="after">Cursor for pagination.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<FineTuningJobsListResponse> FineTuningJobsListAsync(int limit = 20, string? after = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a fine-tuning job.
        /// </summary>
        /// <param name="request">Fine-tuning job creation request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<FineTuningJobResponse> FineTuningJobCreateAsync(FineTuningJobCreateRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a fine-tuning job by ID.
        /// </summary>
        /// <param name="jobId">Fine-tuning job ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<FineTuningJobResponse> FineTuningJobGetAsync(string jobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels a fine-tuning job.
        /// </summary>
        /// <param name="jobId">Fine-tuning job ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<FineTuningJobResponse> FineTuningJobCancelAsync(string jobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts a fine-tuning job (when created with auto_start=false).
        /// </summary>
        /// <param name="jobId">Fine-tuning job ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<FineTuningJobResponse> FineTuningJobStartAsync(string jobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all fine-tuning jobs (paginates automatically).
        /// </summary>
        Task<IReadOnlyList<FineTuningJobResponse>> FineTuningJobsListAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Waits until a fine-tuning job completes (success, failed, or cancelled).
        /// </summary>
        Task<FineTuningJobResponse> FineTuningJobWaitUntilCompleteAsync(string jobId, int pollIntervalMs = 5000, int timeoutMs = 86400000, CancellationToken cancellationToken = default);

        #endregion

        #region Files API

        /// <summary>
        /// Lists files in the organization.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of files.</returns>
        Task<FileListResponse> FilesListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a file for OCR, fine-tuning, or batch processing.
        /// </summary>
        /// <param name="fileStream">The file stream to upload.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="purpose">Purpose: ocr, fine-tune, batch, or audio. Use <see cref="FilePurpose"/> constants.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The uploaded file info.</returns>
        Task<MistralFileInfo> FilesUploadAsync(Stream fileStream, string fileName, string purpose, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a file with typed purpose (convenience overload).
        /// </summary>
        Task<MistralFileInfo> FilesUploadAsync(Stream fileStream, string fileName, FilePurposeType purpose, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves information about a specific file.
        /// </summary>
        /// <param name="fileId">The file ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>File information.</returns>
        Task<MistralFileInfo> FilesRetrieveAsync(string fileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="fileId">The file ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Delete result.</returns>
        Task<FileDeleteResponse> FilesDeleteAsync(string fileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads file content as a stream.
        /// </summary>
        /// <param name="fileId">The file ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The file content stream.</returns>
        Task<Stream> FilesDownloadAsync(string fileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a signed URL for downloading a file.
        /// </summary>
        /// <param name="fileId">The file ID.</param>
        /// <param name="expiryHours">URL validity in hours. Default 24.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Signed URL response.</returns>
        Task<FileSignedUrlResponse> FilesGetSignedUrlAsync(string fileId, int expiryHours = 24, CancellationToken cancellationToken = default);

        #endregion

        #region OCR API

        /// <summary>
        /// Performs OCR on a document (PDF, image, or uploaded file).
        /// </summary>
        /// <param name="request">The OCR request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>OCR response with extracted text and structure.</returns>
        Task<OcrResponse> OcrProcessAsync(OcrRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// One-step OCR: uploads a file, runs OCR, and returns the extracted text.
        /// Optionally deletes the file after processing.
        /// </summary>
        /// <param name="fileStream">The file stream (PDF or image).</param>
        /// <param name="fileName">The file name (e.g. document.pdf, receipt.jpg).</param>
        /// <param name="deleteAfter">If true, deletes the uploaded file after OCR. Default true.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The extracted text from all pages, or empty string on failure.</returns>
        Task<string> OcrExtractTextAsync(Stream fileStream, string fileName, bool deleteAfter = true, CancellationToken cancellationToken = default);

        #endregion

        #region Audio API

        /// <summary>
        /// Transcribes audio to text.
        /// </summary>
        /// <param name="request">Request with audio from stream, file ID, or URL.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Transcription response with text and segments.</returns>
        Task<TranscriptionResponse> AudioTranscribeAsync(AudioTranscriptionRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams transcription events as they arrive.
        /// </summary>
        /// <param name="request">Request with audio from stream, file ID, or URL.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async enumerable of transcription stream events.</returns>
        IAsyncEnumerable<TranscriptionStreamEvent> AudioTranscribeStreamAsync(AudioTranscriptionRequest request, CancellationToken cancellationToken = default);

        #endregion
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
