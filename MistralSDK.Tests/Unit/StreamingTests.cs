using Microsoft.VisualStudio.TestTools.UnitTesting;
using MistralSDK.ChatCompletion;
using System.Collections.Generic;
using System.Text.Json;

namespace MistralSDK.Tests.Unit
{
    /// <summary>
    /// Unit tests for streaming response classes.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class StreamingTests
    {
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };

        #region StreamingChatCompletionChunk Tests

        [TestMethod]
        public void StreamingChunk_GetContent_ReturnsContentFromFirstChoice()
        {
            var chunk = new StreamingChatCompletionChunk
            {
                Id = "test-id",
                Model = "mistral-small-latest",
                Choices = new List<StreamingChoice>
                {
                    new StreamingChoice
                    {
                        Index = 0,
                        Delta = new DeltaMessage { Content = "Hello" }
                    }
                }
            };

            Assert.AreEqual("Hello", chunk.GetContent());
        }

        [TestMethod]
        public void StreamingChunk_GetContent_EmptyChoices_ReturnsEmptyString()
        {
            var chunk = new StreamingChatCompletionChunk
            {
                Id = "test-id",
                Choices = new List<StreamingChoice>()
            };

            Assert.AreEqual(string.Empty, chunk.GetContent());
        }

        [TestMethod]
        public void StreamingChunk_GetContent_NullDelta_ReturnsEmptyString()
        {
            var chunk = new StreamingChatCompletionChunk
            {
                Id = "test-id",
                Choices = new List<StreamingChoice>
                {
                    new StreamingChoice { Index = 0, Delta = null }
                }
            };

            Assert.AreEqual(string.Empty, chunk.GetContent());
        }

        [TestMethod]
        public void StreamingChunk_GetContent_NullContent_ReturnsEmptyString()
        {
            var chunk = new StreamingChatCompletionChunk
            {
                Id = "test-id",
                Choices = new List<StreamingChoice>
                {
                    new StreamingChoice
                    {
                        Index = 0,
                        Delta = new DeltaMessage { Content = null }
                    }
                }
            };

            Assert.AreEqual(string.Empty, chunk.GetContent());
        }

        [TestMethod]
        public void StreamingChunk_IsComplete_TrueWhenFinishReasonSet()
        {
            var chunk = new StreamingChatCompletionChunk
            {
                Choices = new List<StreamingChoice>
                {
                    new StreamingChoice
                    {
                        Index = 0,
                        FinishReason = "stop"
                    }
                }
            };

            Assert.IsTrue(chunk.IsComplete);
        }

        [TestMethod]
        public void StreamingChunk_IsComplete_FalseWhenNoFinishReason()
        {
            var chunk = new StreamingChatCompletionChunk
            {
                Choices = new List<StreamingChoice>
                {
                    new StreamingChoice
                    {
                        Index = 0,
                        Delta = new DeltaMessage { Content = "Hello" },
                        FinishReason = null
                    }
                }
            };

            Assert.IsFalse(chunk.IsComplete);
        }

        [TestMethod]
        public void StreamingChunk_Deserialization_Works()
        {
            var json = @"{
                ""id"": ""cmpl-test"",
                ""object"": ""chat.completion.chunk"",
                ""model"": ""mistral-small-latest"",
                ""created"": 1234567890,
                ""choices"": [{
                    ""index"": 0,
                    ""delta"": {
                        ""role"": ""assistant"",
                        ""content"": ""Hello""
                    },
                    ""finish_reason"": null
                }]
            }";

            var chunk = JsonSerializer.Deserialize<StreamingChatCompletionChunk>(json, _jsonOptions);

            Assert.IsNotNull(chunk);
            Assert.AreEqual("cmpl-test", chunk.Id);
            Assert.AreEqual("mistral-small-latest", chunk.Model);
            Assert.AreEqual("Hello", chunk.GetContent());
            Assert.IsFalse(chunk.IsComplete);
        }

        [TestMethod]
        public void StreamingChunk_FinalChunk_HasUsage()
        {
            var json = @"{
                ""id"": ""cmpl-test"",
                ""model"": ""mistral-small-latest"",
                ""choices"": [{
                    ""index"": 0,
                    ""delta"": {},
                    ""finish_reason"": ""stop""
                }],
                ""usage"": {
                    ""prompt_tokens"": 10,
                    ""completion_tokens"": 20,
                    ""total_tokens"": 30
                }
            }";

            var chunk = JsonSerializer.Deserialize<StreamingChatCompletionChunk>(json, _jsonOptions);

            Assert.IsNotNull(chunk);
            Assert.IsTrue(chunk.IsComplete);
            Assert.IsNotNull(chunk.Usage);
            Assert.AreEqual(10, chunk.Usage.PromptTokens);
            Assert.AreEqual(20, chunk.Usage.CompletionTokens);
            Assert.AreEqual(30, chunk.Usage.TotalTokens);
        }

        #endregion

        #region StreamingChatCompletionResult Tests

        [TestMethod]
        public void StreamingResult_ChunkCount_ReturnsCorrectCount()
        {
            var result = new StreamingChatCompletionResult
            {
                Chunks = new List<StreamingChatCompletionChunk>
                {
                    new StreamingChatCompletionChunk(),
                    new StreamingChatCompletionChunk(),
                    new StreamingChatCompletionChunk()
                }
            };

            Assert.AreEqual(3, result.ChunkCount);
        }

        [TestMethod]
        public void StreamingResult_DefaultValues_AreCorrect()
        {
            var result = new StreamingChatCompletionResult();

            Assert.AreEqual(string.Empty, result.Id);
            Assert.AreEqual(string.Empty, result.Model);
            Assert.AreEqual(string.Empty, result.Content);
            Assert.IsNull(result.FinishReason);
            Assert.IsNull(result.Usage);
            Assert.IsNotNull(result.Chunks);
            Assert.AreEqual(0, result.ChunkCount);
        }

        #endregion

        #region DeltaMessage Tests

        [TestMethod]
        public void DeltaMessage_Deserialization_Works()
        {
            var json = @"{
                ""role"": ""assistant"",
                ""content"": ""Hello world""
            }";

            var delta = JsonSerializer.Deserialize<DeltaMessage>(json, _jsonOptions);

            Assert.IsNotNull(delta);
            Assert.AreEqual("assistant", delta.Role);
            Assert.AreEqual("Hello world", delta.Content);
        }

        [TestMethod]
        public void DeltaMessage_EmptyDelta_Deserializes()
        {
            var json = "{}";

            var delta = JsonSerializer.Deserialize<DeltaMessage>(json, _jsonOptions);

            Assert.IsNotNull(delta);
            Assert.IsNull(delta.Role);
            Assert.IsNull(delta.Content);
        }

        #endregion

        #region Request Stream Property Tests

        [TestMethod]
        public void Request_Stream_DefaultIsFalse()
        {
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest> { MessageRequest.User("Test") }
            };

            Assert.IsFalse(request.Stream);
        }

        [TestMethod]
        public void Request_Stream_SerializesCorrectly()
        {
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest> { MessageRequest.User("Test") },
                Stream = true
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);

            Assert.IsTrue(json.Contains("\"stream\":true"));
        }

        #endregion
    }
}
