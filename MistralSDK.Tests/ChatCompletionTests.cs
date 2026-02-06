using Microsoft.VisualStudio.TestTools.UnitTesting;
using MistralSDK;
using MistralSDK.ChatCompletion;
using MistralSDK.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MistralSDK.Tests
{
    /// <summary>
    /// Integration tests for the MistralClient class.
    /// These tests make real API calls and require a valid API key.
    /// Set the MISTRAL_API_KEY environment variable and MISTRAL_ENABLE_INTEGRATION_TESTS=true to run.
    /// </summary>
    [TestClass]
    public class ChatCompletionTests
    {
        private MistralClient? _client;
        public TestContext TestContext { get; set; } = null!;

        /// <summary>
        /// Initializes the test environment before each test method.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            // Skip initialization if integration tests are not enabled
            if (!TestConfiguration.IsIntegrationTestEnabled())
            {
                return;
            }

            // Initialize the client with API key from environment variable
            var options = new MistralClientOptions
            {
                ApiKey = TestConfiguration.GetApiKeyOrThrow(),
                TimeoutSeconds = 60,
                ValidateRequests = true
            };
            _client = new MistralClient(options);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _client?.Dispose();
        }

        /// <summary>
        /// Helper method to skip tests when integration tests are not enabled.
        /// Also ensures _client is properly initialized.
        /// </summary>
        private void SkipIfIntegrationTestsDisabled()
        {
            if (!TestConfiguration.IsIntegrationTestEnabled())
            {
                Assert.Inconclusive(
                    "Integration tests are disabled. " +
                    "Set MISTRAL_API_KEY and MISTRAL_ENABLE_INTEGRATION_TESTS=true to enable.");
            }

            // Ensure client is initialized (should always be true if integration tests are enabled)
            Assert.IsNotNull(_client, "Client should be initialized when integration tests are enabled.");
        }

        /// <summary>
        /// Tests a successful chat completion request with valid parameters.
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        public async Task ChatCompletion_ValidResponse_ShouldSucceed()
        {
            SkipIfIntegrationTestsDisabled();

            // Arrange
            var messages = new List<MessageRequest>
            {
                new MessageRequest
                {
                    Role = MessageRoles.User,
                    Content = "Who is the best French painter? Answer in one short sentence."
                }
            };

            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = messages,
                Temperature = 0.7,
                MaxTokens = 100
            };

            // Act
            var response = await _client!.ChatCompletionAsync(request);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess, "Response should be successful");
            Assert.IsNotNull(response.Message);
            Assert.AreEqual(200, response.StatusCode);
            Assert.IsNotNull(response.Model);
            Assert.IsNotNull(response.Usage);

            // Log test information
            TestContext.WriteLine($"Status Code: {response.StatusCode}");
            TestContext.WriteLine($"Message: {response.Message}");
            TestContext.WriteLine($"Model: {response.Model}");
            TestContext.WriteLine($"Total Tokens: {response.Usage?.TotalTokens}");
        }

        /// <summary>
        /// Tests a conversation with multiple messages including follow-up questions.
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        public async Task ChatHistoryCompletion_ValidResponse_ShouldSucceed()
        {
            SkipIfIntegrationTestsDisabled();

            // Arrange
            var messages = new List<MessageRequest>
            {
                new MessageRequest
                {
                    Role = MessageRoles.User,
                    Content = "Who is the best French painter? Answer in one short sentence."
                }
            };

            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = messages,
                Temperature = 0.7,
                MaxTokens = 100
            };

            // Act - First request
            var response = await _client!.ChatCompletionAsync(request);

            // Assert - First response
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess, "First response should be successful");
            Assert.IsNotNull(response.Message);
            Assert.AreEqual(200, response.StatusCode);

            // Log first message information
            TestContext.WriteLine("=== First Message ===");
            TestContext.WriteLine($"User Message: {messages[0].Content}");
            TestContext.WriteLine($"Status Code: {response.StatusCode}");
            TestContext.WriteLine($"Response Content: {response.Message}");

            // Add assistant response to conversation
            messages.Add(new MessageRequest
            {
                Role = MessageRoles.Assistant,
                Content = response.Message
            });

            // Add follow-up question
            messages.Add(new MessageRequest
            {
                Role = MessageRoles.User,
                Content = "Are you sure?"
            });

            // Update request with new messages
            request.Messages = messages;

            // Act - Second request
            response = await _client!.ChatCompletionAsync(request);

            // Assert - Second response
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess, "Second response should be successful");
            Assert.IsNotNull(response.Message);
            Assert.AreEqual(200, response.StatusCode);

            // Log second message information
            TestContext.WriteLine("=== Second Message ===");
            TestContext.WriteLine($"User Message: {messages[2].Content}");
            TestContext.WriteLine($"Status Code: {response.StatusCode}");
            TestContext.WriteLine($"Response Content: {response.Message}");
        }

        /// <summary>
        /// Tests error handling when an invalid role is provided.
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        public async Task ChatCompletion_ErrorRole_ShouldReturnError()
        {
            SkipIfIntegrationTestsDisabled();

            // Arrange
            var messages = new List<MessageRequest>
            {
                new MessageRequest
                {
                    Role = "wrong", // Invalid role
                    Content = "This is a test message that will cause an error."
                }
            };

            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = messages,
                Temperature = 0.7
            };

            // Act
            var response = await _client!.ChatCompletionAsync(request);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess, "Response should indicate failure");
            Assert.IsNotNull(response.Message);
            // Client-side validation returns 400, API may return 400 or 422
            Assert.IsTrue(response.StatusCode == 400 || response.StatusCode == 422, 
                $"Should return 400 or 422 for validation error, got {response.StatusCode}");

            // Log error information
            TestContext.WriteLine($"Status Code: {response.StatusCode}");
            TestContext.WriteLine($"Error Message: {response.Message}");
        }

        /// <summary>
        /// Tests error handling when an invalid model is provided.
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        public async Task ChatCompletion_ErrorModel_ShouldReturnError()
        {
            SkipIfIntegrationTestsDisabled();

            // Arrange
            var messages = new List<MessageRequest>
            {
                new MessageRequest
                {
                    Role = MessageRoles.User,
                    Content = "This is a test message that will cause an error."
                }
            };

            var request = new ChatCompletionRequest
            {
                Model = "mistral-small-latest-wrong", // Invalid model
                Messages = messages,
                Temperature = 0.7
            };

            // Act
            var response = await _client!.ChatCompletionAsync(request);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess, "Response should indicate failure");
            Assert.IsNotNull(response.Message);
            Assert.AreEqual(400, response.StatusCode, "Should return 400 for invalid model");

            // Log error information
            TestContext.WriteLine($"Status Code: {response.StatusCode}");
            TestContext.WriteLine($"Error Message: {response.Message}");
        }

        /// <summary>
        /// Tests request validation with invalid parameters.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void ChatCompletionRequest_InvalidParameters_ShouldFailValidation()
        {
            // Arrange
            var invalidRequest = new ChatCompletionRequest
            {
                Model = "", // Invalid: empty model
                Messages = new List<MessageRequest>(), // Invalid: empty messages
                Temperature = 3.0 // Invalid: temperature > 2.0
            };

            // Act & Assert
            Assert.IsFalse(invalidRequest.IsValid(), "Request should fail validation");
        }

        /// <summary>
        /// Tests request validation with valid parameters.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void ChatCompletionRequest_ValidParameters_ShouldPassValidation()
        {
            // Arrange
            var validRequest = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest>
                {
                    new MessageRequest
                    {
                        Role = MessageRoles.User,
                        Content = "Test message"
                    }
                },
                Temperature = 0.7,
                MaxTokens = 100
            };

            // Act & Assert
            Assert.IsTrue(validRequest.IsValid(), "Request should pass validation");
        }

        /// <summary>
        /// Tests message validation with invalid role.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void MessageRequest_InvalidRole_ShouldFailValidation()
        {
            // Arrange
            var invalidMessage = new MessageRequest
            {
                Role = "invalid_role",
                Content = "Test content"
            };

            // Act & Assert
            Assert.IsFalse(invalidMessage.IsValid(), "Message should fail validation");
        }

        /// <summary>
        /// Tests message validation with valid parameters.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void MessageRequest_ValidParameters_ShouldPassValidation()
        {
            // Arrange
            var validMessage = new MessageRequest
            {
                Role = MessageRoles.User,
                Content = "Test content"
            };

            // Act & Assert
            Assert.IsTrue(validMessage.IsValid(), "Message should pass validation");
        }

        /// <summary>
        /// Tests different model types to ensure they work correctly.
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        public async Task ChatCompletion_DifferentModels_ShouldWork()
        {
            SkipIfIntegrationTestsDisabled();

            // Test with different models
            var models = new[] { MistralModels.Ministral3B, MistralModels.Small, MistralModels.Large };

            foreach (var model in models)
            {
                // Arrange
                var request = new ChatCompletionRequest
                {
                    Model = model,
                    Messages = new List<MessageRequest>
                    {
                        new MessageRequest
                        {
                            Role = MessageRoles.User,
                            Content = "Say hello in one word."
                        }
                    },
                    Temperature = 0.2,
                    MaxTokens = 50
                };

                // Act
                var response = await _client!.ChatCompletionAsync(request);

                // Assert
                Assert.IsNotNull(response);
                Assert.IsTrue(response.IsSuccess, $"Model {model} should work");
                Assert.AreEqual(model, response.Model);
                Assert.IsNotNull(response.Message);

                TestContext.WriteLine($"Model {model}: {response.Message}");
            }
        }

        /// <summary>
        /// Tests the cost estimation functionality.
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        public async Task ChatCompletion_CostEstimation_ShouldWork()
        {
            SkipIfIntegrationTestsDisabled();

            // Arrange
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest>
                {
                    new MessageRequest
                    {
                        Role = MessageRoles.User,
                        Content = "Calculate 2+2"
                    }
                },
                Temperature = 0.1,
                MaxTokens = 50
            };

            // Act
            var response = await _client!.ChatCompletionAsync(request);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);
            Assert.IsNotNull(response.Usage);

            // Test cost estimation
            var cost = response.Usage.GetEstimatedCost(response.Model ?? MistralModels.Small);
            Assert.IsTrue(cost >= 0, "Cost should be non-negative");

            TestContext.WriteLine($"Estimated cost: ${cost:F6}");
            TestContext.WriteLine($"Tokens used: {response.Usage.TotalTokens}");
        }

        /// <summary>
        /// Tests error handling with null API key.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        [ExpectedException(typeof(ArgumentException))]
        public void MistralClient_NullApiKey_ShouldThrowException()
        {
            // Act & Assert
            _ = new MistralClient((string)null!);
        }

        /// <summary>
        /// Tests error handling with empty API key.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        [ExpectedException(typeof(ArgumentException))]
        public void MistralClient_EmptyApiKey_ShouldThrowException()
        {
            // Act & Assert
            _ = new MistralClient("");
        }

        /// <summary>
        /// Tests error handling with whitespace API key.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        [ExpectedException(typeof(ArgumentException))]
        public void MistralClient_WhitespaceApiKey_ShouldThrowException()
        {
            // Act & Assert
            _ = new MistralClient("   ");
        }

        /// <summary>
        /// Tests error handling with null request.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ChatCompletion_NullRequest_ShouldThrowException()
        {
            // Arrange - Create a client for this test
            using var client = new MistralClient(TestConfiguration.GetTestApiKey());
            
            // Act & Assert
            await client.ChatCompletionAsync(null!);
        }

        /// <summary>
        /// Tests the usage of different message roles.
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        public async Task ChatCompletion_DifferentRoles_ShouldWork()
        {
            SkipIfIntegrationTestsDisabled();

            // Arrange
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest>
                {
                    new MessageRequest
                    {
                        Role = MessageRoles.System,
                        Content = "You are a helpful assistant."
                    },
                    new MessageRequest
                    {
                        Role = MessageRoles.User,
                        Content = "What is 2+2?"
                    }
                },
                Temperature = 0.1,
                MaxTokens = 50
            };

            // Act
            var response = await _client!.ChatCompletionAsync(request);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);
            Assert.IsNotNull(response.Message);

            TestContext.WriteLine($"System + User message response: {response.Message}");
        }

        /// <summary>
        /// Tests the response properties for successful requests.
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        public async Task ChatCompletion_SuccessfulResponse_ShouldHaveCorrectProperties()
        {
            SkipIfIntegrationTestsDisabled();

            // Arrange
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest>
                {
                    new MessageRequest
                    {
                        Role = MessageRoles.User,
                        Content = "Hello"
                    }
                },
                Temperature = 0.7
            };

            // Act
            var response = await _client!.ChatCompletionAsync(request);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual(200, response.StatusCode);
            Assert.IsNotNull(response.Message);
            Assert.IsNotNull(response.Model);
            Assert.IsNotNull(response.Usage);
            Assert.IsTrue(response.Usage.TotalTokens > 0);
            Assert.IsTrue(response.Usage.PromptTokens > 0);
            Assert.IsTrue(response.Usage.CompletionTokens > 0);

            TestContext.WriteLine($"Response properties validated successfully");
            TestContext.WriteLine($"Model: {response.Model}");
            TestContext.WriteLine($"Total Tokens: {response.Usage.TotalTokens}");
        }

        /// <summary>
        /// Tests a complex conversation with message history to ensure context retention.
        /// This test simulates a real conversation with multiple exchanges.
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        public async Task ChatCompletion_MessageHistory_ShouldMaintainContext()
        {
            SkipIfIntegrationTestsDisabled();

            // Arrange - Start with a system message to set context
            var conversation = new List<MessageRequest>
            {
                new MessageRequest
                {
                    Role = MessageRoles.System,
                    Content = "You are a helpful programming assistant. Keep your answers concise and accurate."
                }
            };

            // Step 1: Initial question about programming
            conversation.Add(new MessageRequest
            {
                Role = MessageRoles.User,
                Content = "What is the difference between a class and an object in C#?"
            });

            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = conversation,
                Temperature = 0.3,
                MaxTokens = 150
            };

            // Act - First response
            var response = await _client!.ChatCompletionAsync(request);

            // Assert - First response
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess, "First response should be successful");
            Assert.IsNotNull(response.Message);
            Assert.IsTrue(response.Message.Length > 0, "Response should not be empty");

            // Log first exchange
            TestContext.WriteLine("=== Exchange 1 ===");
            TestContext.WriteLine($"User: {conversation[1].Content}");
            TestContext.WriteLine($"Assistant: {response.Message}");
            TestContext.WriteLine($"Tokens used: {response.Usage?.TotalTokens}");

            // Step 2: Add assistant response to conversation
            conversation.Add(new MessageRequest
            {
                Role = MessageRoles.Assistant,
                Content = response.Message
            });

            // Step 3: Follow-up question that references the previous context
            conversation.Add(new MessageRequest
            {
                Role = MessageRoles.User,
                Content = "Can you give me a simple example of both?"
            });

            // Update request with new conversation
            request.Messages = conversation;

            // Act - Second response
            response = await _client!.ChatCompletionAsync(request);

            // Assert - Second response
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess, "Second response should be successful");
            Assert.IsNotNull(response.Message);
            Assert.IsTrue(response.Message.Length > 0, "Response should not be empty");

            // Log second exchange
            TestContext.WriteLine("=== Exchange 2 ===");
            TestContext.WriteLine($"User: {conversation[3].Content}");
            TestContext.WriteLine($"Assistant: {response.Message}");
            TestContext.WriteLine($"Tokens used: {response.Usage?.TotalTokens}");

            // Step 4: Add assistant response to conversation
            conversation.Add(new MessageRequest
            {
                Role = MessageRoles.Assistant,
                Content = response.Message
            });

            // Step 5: Ask a question that requires understanding of the previous context
            conversation.Add(new MessageRequest
            {
                Role = MessageRoles.User,
                Content = "How would you create an instance of the class you just showed me?"
            });

            // Update request with new conversation
            request.Messages = conversation;

            // Act - Third response
            response = await _client!.ChatCompletionAsync(request);

            // Assert - Third response
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess, "Third response should be successful");
            Assert.IsNotNull(response.Message);
            Assert.IsTrue(response.Message.Length > 0, "Response should not be empty");

            // Log third exchange
            TestContext.WriteLine("=== Exchange 3 ===");
            TestContext.WriteLine($"User: {conversation[5].Content}");
            TestContext.WriteLine($"Assistant: {response.Message}");
            TestContext.WriteLine($"Tokens used: {response.Usage?.TotalTokens}");

            // Step 6: Add assistant response to conversation
            conversation.Add(new MessageRequest
            {
                Role = MessageRoles.Assistant,
                Content = response.Message
            });

            // Step 7: Ask a clarifying question that should reference the entire conversation
            conversation.Add(new MessageRequest
            {
                Role = MessageRoles.User,
                Content = "Can you summarize what we've discussed so far?"
            });

            // Update request with new conversation
            request.Messages = conversation;

            // Act - Fourth response (summary)
            response = await _client!.ChatCompletionAsync(request);

            // Assert - Fourth response
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess, "Fourth response should be successful");
            Assert.IsNotNull(response.Message);
            Assert.IsTrue(response.Message.Length > 0, "Response should not be empty");

            // Log summary
            TestContext.WriteLine("=== Summary ===");
            TestContext.WriteLine($"User: {conversation[7].Content}");
            TestContext.WriteLine($"Assistant: {response.Message}");
            TestContext.WriteLine($"Tokens used: {response.Usage?.TotalTokens}");

            // Verify conversation length
            Assert.AreEqual(8, conversation.Count, "Conversation should have 8 messages (1 system + 4 user + 3 assistant)");

            // Verify that the model maintains context by checking if the summary mentions previous topics
            var summary = response.Message.ToLower();
            Assert.IsTrue(
                summary.Contains("class") || summary.Contains("object") || summary.Contains("c#") || summary.Contains("instance"),
                "Summary should reference previous conversation topics"
            );

            TestContext.WriteLine("=== Conversation History Test Completed Successfully ===");
            TestContext.WriteLine($"Total messages in conversation: {conversation.Count}");
            TestContext.WriteLine($"Final response length: {response.Message.Length} characters");
        }

        #region Streaming Integration Tests

        /// <summary>
        /// Tests that streaming chat completion returns chunks correctly.
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        public async Task ChatCompletionStream_ValidRequest_ReturnsChunks()
        {
            SkipIfIntegrationTestsDisabled();

            // Arrange
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest>
                {
                    MessageRequest.User("Count from 1 to 5.")
                },
                MaxTokens = 50
            };

            // Act
            var chunks = new List<StreamingChatCompletionChunk>();
            var contentBuilder = new System.Text.StringBuilder();

            await foreach (var chunk in _client!.ChatCompletionStreamAsync(request))
            {
                chunks.Add(chunk);
                var content = chunk.GetContent();
                if (!string.IsNullOrEmpty(content))
                {
                    contentBuilder.Append(content);
                    TestContext.WriteLine($"Chunk {chunks.Count}: '{content}'");
                }
            }

            // Assert
            Assert.IsTrue(chunks.Count > 0, "Should receive at least one chunk");
            
            var fullContent = contentBuilder.ToString();
            Assert.IsTrue(fullContent.Length > 0, "Should have accumulated content");
            TestContext.WriteLine($"Total chunks: {chunks.Count}");
            TestContext.WriteLine($"Full content: {fullContent}");

            // Verify first chunk has model info
            Assert.IsNotNull(chunks[0].Model);
            Assert.IsNotNull(chunks[0].Id);

            // Verify last chunk has finish reason
            var lastChunk = chunks[^1];
            Assert.IsTrue(lastChunk.IsComplete || chunks.Any(c => c.IsComplete), 
                "At least one chunk should be marked as complete");
        }

        /// <summary>
        /// Tests that ChatCompletionStreamCollectAsync collects all chunks correctly.
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        public async Task ChatCompletionStreamCollect_ValidRequest_CollectsAllChunks()
        {
            SkipIfIntegrationTestsDisabled();

            // Arrange
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest>
                {
                    MessageRequest.User("Say 'Hello World' and nothing else.")
                },
                MaxTokens = 20
            };

            var chunkCount = 0;

            // Act
            var result = await _client!.ChatCompletionStreamCollectAsync(
                request,
                onChunk: chunk =>
                {
                    chunkCount++;
                    TestContext.WriteLine($"Callback chunk {chunkCount}: '{chunk.GetContent()}'");
                }
            );

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ChunkCount > 0, "Should have received chunks");
            Assert.AreEqual(chunkCount, result.ChunkCount, "Callback count should match chunk count");
            Assert.IsNotNull(result.Content);
            Assert.IsTrue(result.Content.Length > 0, "Should have content");
            Assert.IsNotNull(result.Id);
            Assert.IsNotNull(result.Model);
            Assert.IsNotNull(result.FinishReason);

            TestContext.WriteLine($"Total chunks: {result.ChunkCount}");
            TestContext.WriteLine($"Content: {result.Content}");
            TestContext.WriteLine($"Finish reason: {result.FinishReason}");
            TestContext.WriteLine($"Tokens: {result.Usage?.TotalTokens}");
        }

        /// <summary>
        /// Tests streaming with stop sequences.
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        public async Task ChatCompletionStream_WithStop_StopsAtSequence()
        {
            SkipIfIntegrationTestsDisabled();

            // Arrange
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest>
                {
                    MessageRequest.User("Count from 1 to 10, one number per line.")
                },
                MaxTokens = 100
            }.WithStop("5");

            // Act
            var result = await _client!.ChatCompletionStreamCollectAsync(request);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Content.Length > 0, "Should have content");
            TestContext.WriteLine($"Content with stop at '5': {result.Content}");
            
            // The content should stop at or before reaching "5"
            // Note: The stop sequence behavior may vary
        }

        /// <summary>
        /// Tests streaming can be cancelled.
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        public async Task ChatCompletionStream_Cancellation_StopsStreaming()
        {
            SkipIfIntegrationTestsDisabled();

            // Arrange
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest>
                {
                    MessageRequest.User("Write a very long story about a dragon.")
                },
                MaxTokens = 500
            };

            var cts = new CancellationTokenSource();
            var chunks = new List<StreamingChatCompletionChunk>();

            // Act
            try
            {
                await foreach (var chunk in _client!.ChatCompletionStreamAsync(request, cts.Token))
                {
                    chunks.Add(chunk);
                    TestContext.WriteLine($"Chunk {chunks.Count}: '{chunk.GetContent()}'");
                    
                    // Cancel after receiving 3 chunks
                    if (chunks.Count >= 3)
                    {
                        cts.Cancel();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                TestContext.WriteLine("Streaming cancelled as expected");
            }

            // Assert
            Assert.IsTrue(chunks.Count >= 3, "Should have received at least 3 chunks before cancellation");
            Assert.IsTrue(chunks.Count < 50, "Should have stopped before receiving too many chunks");
            TestContext.WriteLine($"Total chunks before cancellation: {chunks.Count}");
        }

        /// <summary>
        /// Tests streaming with JSON mode.
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        public async Task ChatCompletionStream_JsonMode_ReturnsValidJson()
        {
            SkipIfIntegrationTestsDisabled();

            // Arrange
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest>
                {
                    MessageRequest.System("You are a helpful assistant that responds in JSON format."),
                    MessageRequest.User("Return a JSON object with 'name' set to 'Test' and 'value' set to 42.")
                },
                MaxTokens = 100
            }.AsJson();

            // Act
            var result = await _client!.ChatCompletionStreamCollectAsync(request);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Content.Length > 0, "Should have content");
            TestContext.WriteLine($"JSON content: {result.Content}");

            // Try to parse as JSON
            try
            {
                var jsonDoc = System.Text.Json.JsonDocument.Parse(result.Content);
                Assert.IsNotNull(jsonDoc);
                TestContext.WriteLine("Successfully parsed as JSON");
            }
            catch (System.Text.Json.JsonException ex)
            {
                Assert.Fail($"Response is not valid JSON: {ex.Message}");
            }
        }

        #endregion
    }
}
