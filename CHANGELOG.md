# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [5.0.0] - 2026-02-26

### 🚀 New Features

#### Unified MistralResponse
- All completion APIs (Chat, Agent, FIM, Embeddings, Moderation, Classification) now return `MistralResponse`
- `MistralResponse.Data` holds the strongly-typed response when `IsSuccess` is true
- `MistralResponse.GetData<T>()` for typed access to Data

#### Convenience overloads
- `ChatCompletionAsync(string userMessage, string? model, int? maxTokens)` – simple one-shot chat
- `FimCompletionAsync(string prompt, string? suffix, int? maxTokens, string? model)` – simple FIM
- `EmbeddingsCreateAsync(string text)` and `EmbeddingsCreateAsync(string[] texts)` – simple embeddings
- `ModerateAsync(string text, string? model)` – quick moderation

#### Batch & Fine-Tuning helpers
- `BatchRequest.ForChat(userMessage, maxTokens, customId)` – create chat batch requests easily
- `TrainingFile.From(fileId, weight)` – create training file references
- `BatchJobResponse.IsComplete` – true when job is in terminal state
- `FineTuningJobResponse.IsComplete` – same for fine-tuning jobs
- `BatchJobsListAllAsync()` – paginate all batch jobs
- `FineTuningJobsListAllAsync()` – paginate all fine-tuning jobs
- `BatchJobWaitUntilCompleteAsync(jobId, pollIntervalMs, timeoutMs)` – polling helper
- `FineTuningJobWaitUntilCompleteAsync(jobId, ...)` – same for fine-tuning

#### Files API
- `FilesUploadAsync(Stream, string, FilePurposeType)` – type-safe purpose (FilePurposeType.Ocr, .FineTune, .Batch, .Audio)

#### Workflows & Helpers
- `ChatSession` – Multi-turn conversation with `CompleteAsync()` and `CompleteStreamAsync()`
- `OcrExtractTextAsync(stream, fileName, deleteAfter)` – One-step OCR: upload → extract text → optionally delete file
- `DocumentQa` – OCR + Q&A workflow: load document, then `AskAsync()` with history
- `SimpleRag` – Embeddings + retrieval + chat: add documents, `IndexAsync()`, then `AskAsync()`
- `ConversationHelper.TrimToLastMessages()` – Trim messages to last N exchanges
- `ConversationHelper.TrimToLastN()` – Trim to last N messages
- `ChatContextBuilder` – Fluent API to build prompts with document, instructions, and question
- See [docs/workflows.md](docs/workflows.md)

#### Fluent API
- `FimCompletionRequest`: `WithSuffix`, `WithMaxTokens`, `WithStop`, `WithStops`
- `EmbeddingRequest`: `WithDimension`, `ForCode`, `WithOutputDtype`
- `EmbeddingResponse.GetFirstVector()`, `GetVector(index)`

#### FIM API (Fill-in-the-Middle)
- `FimCompletionAsync(request)` - Code completion with Codestral (prefix + optional suffix)
- `FimCompletionRequest` - model, prompt, suffix, temperature, max_tokens, etc.
- `FimModels.CodestralLatest`, `FimModels.Codestral2404`, `FimModels.Codestral2405`
- Same response format as chat completions (`ChatCompletionResponse`)
- See [docs/fim.md](docs/fim.md)

#### Batch API
- `BatchJobsListAsync(limit, after)` - List batch jobs with pagination
- `BatchJobCreateAsync(request)` - Create batch job (input files or inline requests)
- `BatchJobGetAsync(jobId)` - Get batch job status
- `BatchJobCancelAsync(jobId)` - Cancel a batch job
- Supported endpoints: chat completions, embeddings, FIM, moderations, classifications, OCR
- See [docs/batch.md](docs/batch.md)

#### Fine-Tuning API
- `FineTuningJobsListAsync(limit, after)` - List fine-tuning jobs
- `FineTuningJobCreateAsync(request)` - Create fine-tuning job
- `FineTuningJobGetAsync(jobId)` - Get job status
- `FineTuningJobCancelAsync(jobId)` - Cancel a job
- `FineTuningJobStartAsync(jobId)` - Start a job (when auto_start=false)
- `FineTuningJobCreateRequest`, `TrainingFile`, `CompletionTrainingParameters`
- `FineTuneableModels` constants (open-mistral-7b, mistral-small-latest, etc.)
- See [docs/fine-tuning.md](docs/fine-tuning.md)

#### Agents API
- `AgentCompletionAsync(request)` - Run completions with an agent ID instead of a model
- `AgentCompletionRequest` - agent_id, messages, max_tokens, tools, tool_choice, etc.
- Same response format as chat completions (`ChatCompletionResponse`)
- See [docs/agents.md](docs/agents.md)

#### Embeddings API
- `EmbeddingsCreateAsync(request)` - Create text or code embeddings
- `EmbeddingRequest` - model, input (string or array), output_dimension, output_dtype
- `EmbeddingModels.MistralEmbed`, `EmbeddingModels.CodestralEmbed`
- See [docs/embeddings.md](docs/embeddings.md) and [RAG Quickstart](https://docs.mistral.ai/capabilities/embeddings/rag_quickstart)

#### Classifiers API
- `ModerationsCreateAsync(request)` - Moderate text (POST /v1/moderations)
- `ChatModerationsCreateAsync(request)` - Moderate chat (POST /v1/chat/moderations)
- `ClassificationsCreateAsync(request)` - Classify text (POST /v1/classifications)
- `ChatClassificationsCreateAsync(request)` - Classify chat (POST /v1/chat/classifications)
- `ModerationResponse`, `ClassificationResponse`, `ModerationResult`
- See [docs/classifiers.md](docs/classifiers.md)

#### Models API
- `ModelsListAsync()` - List all models available to the user
- `ModelsRetrieveAsync(modelId)` - Retrieve model information
- `ModelsDeleteAsync(modelId)` - Delete a fine-tuned model
- `ModelsUpdateAsync(modelId, request)` - Update name/description of a fine-tuned model
- `ModelsArchiveAsync(modelId)` - Archive a fine-tuned model
- `ModelsUnarchiveAsync(modelId)` - Unarchive a fine-tuned model
- `ModelCard`, `ModelCapabilities`, `ModelListResponse`, `UpdateFTModelRequest`, etc.
- See [docs/models.md](docs/models.md)

#### Reasoning API (Magistral)
- `MistralModels.MagistralSmall`, `MistralModels.MagistralMedium` - Reasoning models
- `ReasoningHelper.CreateReasoningRequest()` - Build reasoning requests with default system prompt
- `ReasoningHelper.DefaultReasoningSystemPrompt()` - Recommended system prompt for reasoning
- `ContentChunk`, `TextChunk`, `ThinkChunk` - Structured content for messages
- `ContentChunkBuilder` - Build text and thinking chunks
- `MessageRequest.SystemWithChunks()` - System message with structured content
- `MessageContentExtensions.GetContentText()`, `GetAllContentText()`, `GetThinkingText()` - Extract text from response content
- `MessageRequest.Content` and `MessageResponse.Content` - Support string or content chunks (reasoning output)
- `prompt_mode: "reasoning"` or `null` - Control default system prompt

### 📚 Documentation
- New `docs/reasoning.md` - Reasoning & Magistral guide

### 🔄 Backward Compatibility
- `MessageRequest.Content` and `MessageResponse.Content` remain `string` for simple use.
- Use `ContentChunks` for structured content (reasoning). Use `ContentRaw` when building responses from code.

---

## [4.0.0] - 2026-02-15

### 🚀 New Features

#### Audio & Transcription API
- `AudioTranscribeAsync(request)` - Transcribe audio to text
- `AudioTranscribeStreamAsync(request)` - Stream transcription events in real time
- Audio input via `AudioTranscriptionRequestBuilder.FromStream()`, `FromFileId()`, `FromFileUrl()`
- Support for Voxtral models (voxtral-mini-latest, voxtral-small-latest)
- Options: language hint, diarization (speaker ID), timestamps (segment/word), context bias
- `TranscriptionResponse` with text, segments, usage (including prompt_audio_seconds)
- Streaming events: `TranscriptionStreamTextDelta`, `TranscriptionStreamLanguage`, `TranscriptionStreamSegmentDelta`, `TranscriptionStreamDone`
- `FilePurpose.Audio` - Upload audio files for transcription

### 🔒 Security & Validation
- Input validation for file name, URL length (≤2083 chars), language code (2 chars)
- Path traversal prevention in file names
- Comprehensive error handling tests (401, 404, 429, 500)

### 📚 Documentation
- New `docs/audio.md` - Audio & Transcription guide
- Updated API reference with Audio types

### 🧪 Testing
- 18 new unit tests for Audio API (validation, success, errors)
- 12 tests for AudioTranscriptionRequestBuilder

---

## [3.0.0] - 2026-02-13

### 🚀 New Features

#### Files API
- `FilesListAsync()` - List files in the organization
- `FilesUploadAsync(stream, fileName, purpose)` - Upload files for OCR, fine-tuning, or batch
- `FilesRetrieveAsync(fileId)` - Retrieve file metadata
- `FilesDeleteAsync(fileId)` - Delete a file
- `FilesDownloadAsync(fileId)` - Download file content as stream
- `FilesGetSignedUrlAsync(fileId, expiryHours)` - Get signed download URL
- `MistralFileInfo`, `FileListResponse`, `FileDeleteResponse`, `FileSignedUrlResponse` types
- `FilePurpose.Ocr`, `FilePurpose.FineTune`, `FilePurpose.Batch` constants

#### OCR API (Document AI)
- `OcrProcessAsync(request)` - Extract text from PDFs and images
- Document input via `OcrDocument.FromFileId()`, `FromDocumentUrl()`, `FromImageUrl()`, `FromImageBase64()`
- Support for PDFs, images (URL or base64), and uploaded files
- `OcrRequest` with table format (markdown/html), header/footer extraction, page selection
- `OcrResponse` with pages, markdown content, extracted images, usage info
- `OcrModels.MistralOcrLatest` - Latest OCR model
- `OcrTableFormat.Markdown`, `OcrTableFormat.Html` constants

### 📚 Documentation
- New `docs/ocr.md` - OCR and Files API guide
- Updated API reference with OCR and Files types

### 🧪 Testing
- 32 new unit tests for OCR and Files
- Integration tests with PDF and image assets
- All 169 tests passing

---

## [2.1.0] - 2026-02-06

### 🚀 New Features

#### Streaming Support
- New `ChatCompletionStreamAsync()` method returning `IAsyncEnumerable<StreamingChatCompletionChunk>`
- New `ChatCompletionStreamCollectAsync()` for streaming with callback and collected result
- `StreamingChatCompletionChunk` class for individual streaming chunks
- `StreamingChatCompletionResult` class for accumulated streaming results
- Full cancellation support for streaming operations

#### Extended Request Parameters
- `N` - Number of completions to return (input tokens billed once)
- `Stop` - Stop sequence(s) to end generation
- `FrequencyPenalty` - Penalize frequent tokens (0.0-2.0)
- `PresencePenalty` - Encourage vocabulary diversity (0.0-2.0)
- `ResponseFormat` - JSON mode and JSON Schema support

#### JSON Mode & Structured Output
- New `ResponseFormat` class for specifying output format
- `ResponseFormatType.Text` - Standard text output (default)
- `ResponseFormatType.JsonObject` - Guarantees valid JSON output
- `ResponseFormatType.JsonSchema` - Enforces specific JSON schema
- `JsonSchema` class for defining structured output schemas
- Fluent methods: `AsJson()` and `AsJsonSchema(schema)`

#### Enhanced Message Support
- `Prefix` property for prepending content to assistant responses
- `ToolCallId` and `Name` properties for tool/function calling support
- `Tool` message role for function calling results
- Static factory methods: `MessageRequest.System()`, `.User()`, `.Assistant()`, `.Tool()`

#### Updated Models
- Added `MistralModels.PixtralLarge` - Multimodal model
- Added `MistralModels.Saba` - Middle Eastern & South Asian languages
- Added `MistralModels.Ministral3B` - Ultra-efficient small model
- Added `MistralModels.Ministral8B` - Edge deployment model
- Added `MistralModels.Codestral` - Code generation model
- Added `MistralModels.Pixtral` - Free multimodal model
- Added `MistralModels.Embed` - Text embeddings
- Added `MistralModels.Moderation` - Content moderation
- Added `MistralModels.Nemo` - Research model
- Marked `MistralModels.Tiny` and `.Medium` as obsolete

#### Other Additions
- `PromptModes.Reasoning` constant for enhanced reasoning mode
- `MessageRoles.Tool` constant for tool messages
- Fluent builder methods on `ChatCompletionRequest`

### 📚 Documentation
- New streaming documentation page (`docs/streaming.md`)
- Updated getting-started with new parameters
- Updated API reference with all new types
- Improved model documentation with categories

### ⚠️ Breaking Changes
- `Temperature` and `TopP` properties are now nullable (`double?`)
- This allows the API to use its default values when not specified

---

## [2.0.0] - 2026-02-06

### 🚀 Major Changes

#### Interface-Based Design
- Added `IMistralClient` interface for dependency injection and easier mocking in tests
- All public methods now have proper contracts defined in interfaces
- Enables better testability with mock objects

#### HttpClientFactory Support
- Added support for `IHttpClientFactory` to properly manage HttpClient lifecycle
- Prevents socket exhaustion issues in high-throughput scenarios
- New constructor overloads accepting pre-configured HttpClient instances

#### Custom Exceptions
- New `MistralApiException` base class for all API-related exceptions
- `MistralValidationException` for request validation failures
- `MistralAuthenticationException` for authentication errors
- `MistralRateLimitException` for rate limiting scenarios
- `MistralModelNotFoundException` for invalid model errors
- All exceptions include `IsRetryable` and `RetryDelaySeconds` properties

#### Configuration Options
- New `MistralClientOptions` class for comprehensive configuration
- Support for `appsettings.json` configuration binding
- Options include: `TimeoutSeconds`, `MaxRetries`, `ThrowOnError`, `ValidateRequests`, `EnableCaching`

#### Dependency Injection Extensions
- `AddMistralClient()` extension method for `IServiceCollection`
- `AddMistralClientFromEnvironment()` for automatic API key loading
- Configuration section binding support

#### Response Caching
- New `IChatCompletionCache` interface for custom cache implementations
- `MemoryChatCompletionCache` for in-memory caching
- Configurable cache expiration
- Only successful responses are cached

### ✨ Added
- **Request Validation**: `ValidateRequest()` method for client-side validation before API calls
- **Cancellation Support**: All async methods now accept `CancellationToken`
- **TestConfiguration**: Secure API key management via environment variables for tests
- **Test Categories**: Unit and Integration test separation with `[TestCategory]` attribute
- **Mock-based Tests**: New unit tests using Moq for HttpClient mocking
- **Coverlet Integration**: Code coverage collection support

### 🔒 Security Improvements
- API keys are no longer hardcoded in tests
- Use of `MISTRAL_API_KEY` environment variable
- `MISTRAL_ENABLE_INTEGRATION_TESTS` flag for explicit opt-in to integration tests
- Security documentation in README

### 📚 Documentation
- Updated README with comprehensive examples
- Dependency injection usage examples
- Error handling patterns documentation
- Configuration guide
- Test execution instructions

### 🧪 Testing
- 54 passing unit tests
- 9 integration tests (require API key)
- Mock-based tests for offline development
- Separate test categories for CI/CD pipelines

### ⚠️ Breaking Changes
- Minimum .NET version remains .NET 8.0
- Some internal classes reorganized into new namespaces:
  - `MistralSDK.Abstractions` - Interfaces
  - `MistralSDK.Configuration` - Options classes
  - `MistralSDK.Exceptions` - Custom exceptions
  - `MistralSDK.Extensions` - DI extensions
  - `MistralSDK.Caching` - Caching services

### 🔧 Dependencies Added
- `Microsoft.Extensions.Http` 8.0.1
- `Microsoft.Extensions.DependencyInjection.Abstractions` 8.0.2
- `Microsoft.Extensions.Options` 8.0.2
- `Microsoft.Extensions.Caching.Memory` 8.0.1
- `Moq` 4.20.72 (test project)
- `coverlet.collector` 6.0.2 (test project)

---

## [1.0.0] - 2025-08-03

### ✨ Added
- **Core Functionality**
  - Complete Mistral AI chat completion API support
  - Full async/await implementation for all operations
  - Type-safe request and response objects
  - Comprehensive error handling with detailed error messages

- **Client Features**
  - `MistralClient` class with proper resource management (IDisposable)
  - `ChatCompletionAsync` method for sending requests
  - Automatic JSON serialization with snake_case naming
  - Built-in HTTP client with Bearer token authentication
  - Support for all HTTP status codes and error scenarios

- **Request Models**
  - `ChatCompletionRequest` with all Mistral API parameters
  - `MessageRequest` for individual conversation messages
  - Support for system, user, and assistant message roles
  - Configurable parameters: temperature, top_p, max_tokens, safe_prompt, stream
  - Built-in validation with data annotations

- **Response Models**
  - `ChatCompletionResponse` for successful API responses
  - `ChatCompletionErrorResponse` for validation errors
  - `ChatCompletionErrorModelResponse` for model-specific errors
  - `MistralResponse` as standardized wrapper for all responses
  - `UsageInfo` with token counting and cost estimation

- **Error Handling**
  - Comprehensive error parsing for different API error formats
  - User-friendly error messages with retry recommendations
  - HTTP status code mapping
  - Timeout handling for network issues
  - Null reference protection throughout

- **Constants and Utilities**
  - `MistralModels` static class with all available models
  - `MessageRoles` static class for message role constants
  - `FinishReasons` static class for completion reasons
  - `ErrorTypes` and `ModelErrorTypes` for error categorization
  - `ModelErrorCodes` for specific error codes

- **Technical Features**
  - .NET 8.0 support with latest C# features
  - Nullable reference types for better type safety
  - Data annotations for request validation
  - XML documentation for all public APIs
  - Optimized JSON serialization with custom naming policy
  - Proper resource disposal with IDisposable pattern

- **Documentation**
  - Comprehensive XML documentation for all classes and methods
  - Usage examples and best practices
  - Error handling guide with common scenarios
  - Cost estimation examples
  - API reference with all available models and parameters

- **Testing**
  - Complete test suite with MSTest framework
  - Unit tests for all public methods
  - Integration tests for API communication
  - Error scenario testing
  - Validation testing for request objects
  - Multi-model testing
  - Conversation history testing
  - Cost estimation testing

- **Development Features**
  - Clean project structure with proper namespaces
  - Consistent coding style and naming conventions
  - Comprehensive error handling patterns
  - Type-safe constants and enums
  - Proper separation of concerns

- **Security**
  - Secure API key handling
  - Input validation and sanitization
  - Safe error message handling (no sensitive data exposure)
  - Proper resource cleanup

- **Package Information**
  - NuGet package configuration
  - MIT License
  - Comprehensive metadata
  - Source files included for debugging
  - Documentation files included

### 🔧 Technical Details
- **Target Framework**: .NET 8.0
- **Dependencies**: None (uses built-in .NET libraries)
- **License**: MIT
- **Repository**: GitHub with proper Git configuration
- **Documentation**: XML + Markdown
- **Testing**: MSTest with comprehensive coverage

### 📚 Documentation
- Complete API reference
- Usage examples for common scenarios
- Error handling patterns
- Cost estimation guide
- Best practices and recommendations

### 🧪 Testing Coverage
- Basic functionality tests
- Error handling tests
- Validation tests
- Multi-model tests
- Conversation history tests
- Cost estimation tests
- Resource management tests

---

## Version History

### [1.0.0] - Initial Release
- Complete Mistral AI SDK implementation
- Full chat completion support
- Comprehensive error handling
- Complete documentation and testing 