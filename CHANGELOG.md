# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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