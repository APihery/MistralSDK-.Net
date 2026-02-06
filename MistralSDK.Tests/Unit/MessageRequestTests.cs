using Microsoft.VisualStudio.TestTools.UnitTesting;
using MistralSDK.ChatCompletion;
using System.Text.Json;

namespace MistralSDK.Tests.Unit
{
    /// <summary>
    /// Unit tests for MessageRequest class and factory methods.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class MessageRequestTests
    {
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };

        #region Factory Methods Tests

        [TestMethod]
        public void System_CreatesSystemMessage()
        {
            var message = MessageRequest.System("You are a helpful assistant.");

            Assert.AreEqual(MessageRoles.System, message.Role);
            Assert.AreEqual("You are a helpful assistant.", message.Content);
            Assert.IsTrue(message.IsValid());
        }

        [TestMethod]
        public void User_CreatesUserMessage()
        {
            var message = MessageRequest.User("Hello, how are you?");

            Assert.AreEqual(MessageRoles.User, message.Role);
            Assert.AreEqual("Hello, how are you?", message.Content);
            Assert.IsTrue(message.IsValid());
        }

        [TestMethod]
        public void Assistant_CreatesAssistantMessage()
        {
            var message = MessageRequest.Assistant("I'm doing well, thank you!");

            Assert.AreEqual(MessageRoles.Assistant, message.Role);
            Assert.AreEqual("I'm doing well, thank you!", message.Content);
            Assert.IsFalse(message.Prefix);
            Assert.IsTrue(message.IsValid());
        }

        [TestMethod]
        public void Assistant_WithPrefix_CreatesPrefixMessage()
        {
            var message = MessageRequest.Assistant("Sure, here's the answer:", prefix: true);

            Assert.AreEqual(MessageRoles.Assistant, message.Role);
            Assert.AreEqual("Sure, here's the answer:", message.Content);
            Assert.IsTrue(message.Prefix);
            Assert.IsTrue(message.IsValid());
        }

        [TestMethod]
        public void Tool_CreatesToolMessage()
        {
            var message = MessageRequest.Tool("call_123", "{\"result\": \"success\"}", "my_function");

            Assert.AreEqual(MessageRoles.Tool, message.Role);
            Assert.AreEqual("{\"result\": \"success\"}", message.Content);
            Assert.AreEqual("call_123", message.ToolCallId);
            Assert.AreEqual("my_function", message.Name);
            Assert.IsTrue(message.IsValid());
        }

        [TestMethod]
        public void Tool_WithoutName_IsValid()
        {
            var message = MessageRequest.Tool("call_123", "{\"result\": \"success\"}");

            Assert.AreEqual(MessageRoles.Tool, message.Role);
            Assert.IsNull(message.Name);
            Assert.IsTrue(message.IsValid());
        }

        #endregion

        #region Validation Tests

        [TestMethod]
        public void IsValid_EmptyRole_ReturnsFalse()
        {
            var message = new MessageRequest { Role = "", Content = "Hello" };

            Assert.IsFalse(message.IsValid());
        }

        [TestMethod]
        public void IsValid_InvalidRole_ReturnsFalse()
        {
            var message = new MessageRequest { Role = "invalid_role", Content = "Hello" };

            Assert.IsFalse(message.IsValid());
        }

        [TestMethod]
        public void IsValid_EmptyContent_ReturnsFalse()
        {
            var message = new MessageRequest { Role = MessageRoles.User, Content = "" };

            Assert.IsFalse(message.IsValid());
        }

        [TestMethod]
        public void IsValid_ToolWithoutToolCallId_ReturnsFalse()
        {
            var message = new MessageRequest 
            { 
                Role = MessageRoles.Tool, 
                Content = "result",
                ToolCallId = null 
            };

            Assert.IsFalse(message.IsValid());
        }

        [TestMethod]
        public void IsValid_ToolWithEmptyToolCallId_ReturnsFalse()
        {
            var message = new MessageRequest 
            { 
                Role = MessageRoles.Tool, 
                Content = "result",
                ToolCallId = "" 
            };

            Assert.IsFalse(message.IsValid());
        }

        #endregion

        #region Serialization Tests

        [TestMethod]
        public void Message_Prefix_SerializesCorrectly()
        {
            var message = MessageRequest.Assistant("Hello", prefix: true);

            var json = JsonSerializer.Serialize(message, _jsonOptions);

            Assert.IsTrue(json.Contains("\"prefix\":true"));
        }

        [TestMethod]
        public void Message_PrefixFalse_NotSerialized()
        {
            var message = MessageRequest.Assistant("Hello", prefix: false);

            var json = JsonSerializer.Serialize(message, _jsonOptions);

            // Prefix should not be serialized when false (default)
            Assert.IsFalse(json.Contains("\"prefix\":true"));
        }

        [TestMethod]
        public void Message_ToolCallId_SerializesCorrectly()
        {
            var message = MessageRequest.Tool("call_abc123", "result");

            var json = JsonSerializer.Serialize(message, _jsonOptions);

            Assert.IsTrue(json.Contains("\"tool_call_id\":\"call_abc123\""));
        }

        [TestMethod]
        public void Message_Name_SerializesCorrectly()
        {
            var message = MessageRequest.Tool("call_123", "result", "get_weather");

            var json = JsonSerializer.Serialize(message, _jsonOptions);

            Assert.IsTrue(json.Contains("\"name\":\"get_weather\""));
        }

        [TestMethod]
        public void Message_NullName_NotSerialized()
        {
            var message = MessageRequest.User("Hello");

            var json = JsonSerializer.Serialize(message, _jsonOptions);

            Assert.IsFalse(json.Contains("\"name\""));
        }

        #endregion

        #region MessageRoles Constants Tests

        [TestMethod]
        public void MessageRoles_System_HasCorrectValue()
        {
            Assert.AreEqual("system", MessageRoles.System);
        }

        [TestMethod]
        public void MessageRoles_User_HasCorrectValue()
        {
            Assert.AreEqual("user", MessageRoles.User);
        }

        [TestMethod]
        public void MessageRoles_Assistant_HasCorrectValue()
        {
            Assert.AreEqual("assistant", MessageRoles.Assistant);
        }

        [TestMethod]
        public void MessageRoles_Tool_HasCorrectValue()
        {
            Assert.AreEqual("tool", MessageRoles.Tool);
        }

        #endregion
    }
}
