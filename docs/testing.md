# Testing

MistralSDK is designed for testability with interface-based design and mock-friendly architecture.

## Test Categories

Tests are organized into two categories:

| Category | Description | API Key Required |
|----------|-------------|------------------|
| `Unit` | Isolated tests with mocked dependencies | No |
| `Integration` | Real API calls | Yes |

## Running Tests

### All Tests

```bash
dotnet test
```

### Unit Tests Only

```bash
dotnet test --filter "TestCategory=Unit"
```

### Integration Tests

```bash
# Set environment variables
set MISTRAL_API_KEY=your-api-key
set MISTRAL_ENABLE_INTEGRATION_TESTS=true

# Run tests
dotnet test --filter "TestCategory=Integration"
```

### With Code Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Using Batch Files

The project includes batch files for convenience:

| File | Description |
|------|-------------|
| `test.bat` | Run unit tests |
| `test-integration.bat` | Run integration tests (reads API key from `api-key.txt`) |
| `test-all.bat` | Run all tests |
| `test-coverage.bat` | Run tests with coverage |

## Mocking IMistralClient

### With Moq

```csharp
using Moq;
using MistralSDK.Abstractions;
using MistralSDK.ChatCompletion;

[TestClass]
public class ChatServiceTests
{
    private Mock<IMistralClient> _mockClient;
    private ChatService _service;

    [TestInitialize]
    public void Setup()
    {
        _mockClient = new Mock<IMistralClient>();
        _service = new ChatService(_mockClient.Object);
    }

    [TestMethod]
    public async Task GetResponse_Success_ReturnsMessage()
    {
        // Arrange
        _mockClient
            .Setup(x => x.ChatCompletionAsync(
                It.IsAny<ChatCompletionRequest>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MistralResponse(200)
            {
                IsSuccess = true,
                Message = "Hello! I'm doing well.",
                Model = "mistral-small-latest"
            });

        // Act
        var result = await _service.GetResponseAsync("Hello");

        // Assert
        Assert.AreEqual("Hello! I'm doing well.", result);
    }

    [TestMethod]
    public async Task GetResponse_Error_ThrowsException()
    {
        // Arrange
        _mockClient
            .Setup(x => x.ChatCompletionAsync(
                It.IsAny<ChatCompletionRequest>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MistralResponse(500)
            {
                IsSuccess = false,
                Message = "Internal server error"
            });

        // Act & Assert
        await Assert.ThrowsExceptionAsync<Exception>(() => 
            _service.GetResponseAsync("Hello"));
    }
}
```

### Verifying Request Parameters

```csharp
[TestMethod]
public async Task SendMessage_UsesCorrectModel()
{
    // Arrange
    ChatCompletionRequest capturedRequest = null;
    
    _mockClient
        .Setup(x => x.ChatCompletionAsync(
            It.IsAny<ChatCompletionRequest>(), 
            It.IsAny<CancellationToken>()))
        .Callback<ChatCompletionRequest, CancellationToken>((req, ct) => 
            capturedRequest = req)
        .ReturnsAsync(new MistralResponse(200) { IsSuccess = true });

    // Act
    await _service.GetResponseAsync("Hello");

    // Assert
    Assert.AreEqual(MistralModels.Small, capturedRequest.Model);
    Assert.AreEqual(1, capturedRequest.Messages.Count);
    Assert.AreEqual("Hello", capturedRequest.Messages[0].Content);
}
```

## Mocking HttpClient

For lower-level testing, mock the HttpMessageHandler:

```csharp
using Moq;
using Moq.Protected;

[TestClass]
public class MistralClientTests
{
    private Mock<HttpMessageHandler> _mockHandler;
    private HttpClient _httpClient;

    [TestInitialize]
    public void Setup()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://api.mistral.ai/v1")
        };
    }

    [TestMethod]
    public async Task ChatCompletion_ReturnsSuccess()
    {
        // Arrange
        var responseJson = @"{
            ""id"": ""test"",
            ""model"": ""mistral-small-latest"",
            ""choices"": [{
                ""index"": 0,
                ""message"": {
                    ""role"": ""assistant"",
                    ""content"": ""Hello!""
                },
                ""finish_reason"": ""stop""
            }],
            ""usage"": {
                ""prompt_tokens"": 10,
                ""completion_tokens"": 5,
                ""total_tokens"": 15
            }
        }";

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var options = new MistralClientOptions { ApiKey = "test-key" };
        using var client = new MistralClient(_httpClient, options);

        // Act
        var response = await client.ChatCompletionAsync(new ChatCompletionRequest
        {
            Model = MistralModels.Small,
            Messages = new List<MessageRequest>
            {
                new() { Role = MessageRoles.User, Content = "Hi" }
            }
        });

        // Assert
        Assert.IsTrue(response.IsSuccess);
        Assert.AreEqual("Hello!", response.Message);
    }
}
```

## Integration Testing

### Test Configuration

Use `TestConfiguration` for secure API key handling:

```csharp
[TestClass]
public class IntegrationTests
{
    private MistralClient _client;

    [TestInitialize]
    public void Setup()
    {
        if (!TestConfiguration.IsIntegrationTestEnabled())
        {
            return;
        }

        _client = new MistralClient(TestConfiguration.GetApiKeyOrThrow());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task RealApiCall_Works()
    {
        if (!TestConfiguration.IsIntegrationTestEnabled())
        {
            Assert.Inconclusive("Integration tests disabled");
        }

        var request = new ChatCompletionRequest
        {
            Model = MistralModels.Small,
            Messages = new List<MessageRequest>
            {
                new() { Role = MessageRoles.User, Content = "Say hello" }
            },
            MaxTokens = 10
        };

        var response = await _client.ChatCompletionAsync(request);

        Assert.IsTrue(response.IsSuccess);
        Assert.IsNotNull(response.Message);
    }
}
```

## ASP.NET Core Testing

### WebApplicationFactory

```csharp
public class ChatApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ChatApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real client with mock
                services.RemoveAll<IMistralClient>();
                
                var mockClient = new Mock<IMistralClient>();
                mockClient
                    .Setup(x => x.ChatCompletionAsync(
                        It.IsAny<ChatCompletionRequest>(), default))
                    .ReturnsAsync(new MistralResponse(200) 
                    { 
                        IsSuccess = true, 
                        Message = "Test" 
                    });
                
                services.AddSingleton(mockClient.Object);
            });
        });
    }

    [Fact]
    public async Task ChatEndpoint_ReturnsSuccess()
    {
        var client = _factory.CreateClient();
        
        var response = await client.PostAsJsonAsync("/api/chat", "Hello");
        
        response.EnsureSuccessStatusCode();
    }
}
```

## Best Practices

1. **Use interfaces**: Always depend on `IMistralClient`, not `MistralClient`
2. **Isolate tests**: Mock external dependencies
3. **Test edge cases**: Error responses, timeouts, cancellation
4. **Use test categories**: Separate unit and integration tests
5. **Secure API keys**: Use environment variables, not code

## Next Steps

- [API Reference](api-reference.md)
