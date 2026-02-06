using Microsoft.VisualStudio.TestTools.UnitTesting;
using MistralSDK.ChatCompletion;
using System.Collections.Generic;
using System.Text.Json;

namespace MistralSDK.Tests.Unit
{
    /// <summary>
    /// Unit tests for ChatCompletionRequest and related classes.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class ChatCompletionRequestTests
    {
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };

        #region Basic Request Tests

        [TestMethod]
        public void IsValid_ValidRequest_ReturnsTrue()
        {
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest>
                {
                    MessageRequest.User("Hello")
                }
            };

            Assert.IsTrue(request.IsValid());
        }

        [TestMethod]
        public void IsValid_EmptyModel_ReturnsFalse()
        {
            var request = new ChatCompletionRequest
            {
                Model = "",
                Messages = new List<MessageRequest>
                {
                    MessageRequest.User("Hello")
                }
            };

            Assert.IsFalse(request.IsValid());
        }

        [TestMethod]
        public void IsValid_NoMessages_ReturnsFalse()
        {
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest>()
            };

            Assert.IsFalse(request.IsValid());
        }

        #endregion

        #region New Parameters Tests

        [TestMethod]
        public void Request_WithN_SerializesCorrectly()
        {
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest> { MessageRequest.User("Test") },
                N = 3
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);

            Assert.IsTrue(json.Contains("\"n\":3"));
        }

        [TestMethod]
        public void Request_WithStopString_SerializesCorrectly()
        {
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest> { MessageRequest.User("Test") }
            }.WithStop("END");

            var json = JsonSerializer.Serialize(request, _jsonOptions);

            Assert.IsTrue(json.Contains("\"stop\":\"END\""));
        }

        [TestMethod]
        public void Request_WithStopArray_SerializesCorrectly()
        {
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest> { MessageRequest.User("Test") }
            }.WithStops("END", "STOP", "###");

            var json = JsonSerializer.Serialize(request, _jsonOptions);

            Assert.IsTrue(json.Contains("\"stop\":[\"END\",\"STOP\",\"###\"]"));
        }

        [TestMethod]
        public void Request_WithFrequencyPenalty_SerializesCorrectly()
        {
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest> { MessageRequest.User("Test") },
                FrequencyPenalty = 0.5
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);

            Assert.IsTrue(json.Contains("\"frequency_penalty\":0.5"));
        }

        [TestMethod]
        public void Request_WithPresencePenalty_SerializesCorrectly()
        {
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest> { MessageRequest.User("Test") },
                PresencePenalty = 1.0
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);

            Assert.IsTrue(json.Contains("\"presence_penalty\":1"));
        }

        [TestMethod]
        public void IsValid_InvalidFrequencyPenalty_ReturnsFalse()
        {
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest> { MessageRequest.User("Test") },
                FrequencyPenalty = 2.5 // Invalid: max is 2.0
            };

            Assert.IsFalse(request.IsValid());
        }

        [TestMethod]
        public void IsValid_InvalidPresencePenalty_ReturnsFalse()
        {
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest> { MessageRequest.User("Test") },
                PresencePenalty = -0.5 // Invalid: min is 0.0
            };

            Assert.IsFalse(request.IsValid());
        }

        [TestMethod]
        public void IsValid_InvalidN_ReturnsFalse()
        {
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest> { MessageRequest.User("Test") },
                N = 0 // Invalid: min is 1
            };

            Assert.IsFalse(request.IsValid());
        }

        #endregion

        #region ResponseFormat Tests

        [TestMethod]
        public void Request_AsJson_SetsResponseFormat()
        {
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest> { MessageRequest.User("Test") }
            }.AsJson();

            Assert.IsNotNull(request.ResponseFormat);
            Assert.AreEqual(ResponseFormatType.JsonObject, request.ResponseFormat.Type);
        }

        [TestMethod]
        public void Request_AsJsonSchema_SetsResponseFormat()
        {
            var schema = new JsonSchema
            {
                Name = "Person",
                Description = "A person object",
                Schema = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["name"] = new Dictionary<string, object> { ["type"] = "string" },
                        ["age"] = new Dictionary<string, object> { ["type"] = "integer" }
                    }
                }
            };

            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest> { MessageRequest.User("Test") }
            }.AsJsonSchema(schema);

            Assert.IsNotNull(request.ResponseFormat);
            Assert.AreEqual(ResponseFormatType.JsonSchema, request.ResponseFormat.Type);
            Assert.IsNotNull(request.ResponseFormat.JsonSchema);
            Assert.AreEqual("Person", request.ResponseFormat.JsonSchema.Name);
        }

        [TestMethod]
        public void ResponseFormat_JsonObject_SerializesCorrectly()
        {
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest> { MessageRequest.User("Test") }
            }.AsJson();

            var json = JsonSerializer.Serialize(request, _jsonOptions);

            Assert.IsTrue(json.Contains("\"response_format\""));
            Assert.IsTrue(json.Contains("\"type\":\"json_object\""));
        }

        #endregion

        #region Temperature and TopP Tests

        [TestMethod]
        public void Request_NullTemperature_NotSerialized()
        {
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest> { MessageRequest.User("Test") },
                Temperature = null
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);

            // Temperature should be serialized as null or not present
            // The API will use its default value
            Assert.IsTrue(json.Contains("\"temperature\":null") || !json.Contains("\"temperature\""));
        }

        [TestMethod]
        public void Request_WithTemperature_SerializesCorrectly()
        {
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest> { MessageRequest.User("Test") },
                Temperature = 0.3
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);

            Assert.IsTrue(json.Contains("\"temperature\":0.3"));
        }

        [TestMethod]
        public void IsValid_InvalidTemperature_ReturnsFalse()
        {
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest> { MessageRequest.User("Test") },
                Temperature = 2.5 // Invalid: max is 2.0
            };

            Assert.IsFalse(request.IsValid());
        }

        [TestMethod]
        public void IsValid_InvalidTopP_ReturnsFalse()
        {
            var request = new ChatCompletionRequest
            {
                Model = MistralModels.Small,
                Messages = new List<MessageRequest> { MessageRequest.User("Test") },
                TopP = 1.5 // Invalid: max is 1.0
            };

            Assert.IsFalse(request.IsValid());
        }

        #endregion
    }
}
