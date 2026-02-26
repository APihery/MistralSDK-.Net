using System.Linq;
using MistralSDK.ChatCompletion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MistralSDK.Tests.Unit;

[TestClass]
[TestCategory("Unit")]
public class ReasoningTests
{
    [TestMethod]
    public void MistralModels_HasMagistralConstants()
    {
        Assert.AreEqual("magistral-small-latest", MistralModels.MagistralSmall);
        Assert.AreEqual("magistral-medium-latest", MistralModels.MagistralMedium);
    }

    [TestMethod]
    public void ReasoningHelper_DefaultSystemPrompt_ReturnsChunks()
    {
        var chunks = ReasoningHelper.DefaultReasoningSystemPrompt();
        Assert.IsNotNull(chunks);
        Assert.IsTrue(chunks.Count >= 2);
        Assert.IsTrue(chunks.Any(c => c is TextChunk));
        Assert.IsTrue(chunks.Any(c => c is ThinkChunk));
    }

    [TestMethod]
    public void ReasoningHelper_CreateReasoningRequest_WithDefaultPrompt()
    {
        var request = ReasoningHelper.CreateReasoningRequest(
            MistralModels.MagistralMedium,
            "What is 2+2?",
            useDefaultPrompt: true);

        Assert.AreEqual(MistralModels.MagistralMedium, request.Model);
        Assert.AreEqual(PromptModes.Reasoning, request.PromptMode);
        Assert.IsTrue(request.Messages.Count >= 2);
        Assert.AreEqual(MessageRoles.System, request.Messages[0].Role);
        Assert.AreEqual(MessageRoles.User, request.Messages[1].Role);
        Assert.IsNotNull(request.Messages[0].ContentChunks);
        Assert.AreEqual("What is 2+2?", request.Messages[1].Content);
    }

    [TestMethod]
    public void ReasoningHelper_CreateReasoningRequest_WithoutDefaultPrompt()
    {
        var request = ReasoningHelper.CreateReasoningRequest(
            MistralModels.MagistralSmall,
            "Solve x=1",
            useDefaultPrompt: false);

        Assert.AreEqual(1, request.Messages.Count);
        Assert.AreEqual(MessageRoles.User, request.Messages[0].Role);
    }

    [TestMethod]
    public void ContentChunkBuilder_Text_CreatesTextChunk()
    {
        var chunk = ContentChunkBuilder.Text("hello");
        Assert.IsInstanceOfType(chunk, typeof(TextChunk));
        Assert.AreEqual("hello", ((TextChunk)chunk).Text);
    }

    [TestMethod]
    public void ContentChunkBuilder_Thinking_CreatesThinkChunk()
    {
        var chunk = ContentChunkBuilder.Thinking("reasoning here");
        Assert.IsInstanceOfType(chunk, typeof(ThinkChunk));
        Assert.AreEqual(1, ((ThinkChunk)chunk).Thinking.Count);
        Assert.IsInstanceOfType(((ThinkChunk)chunk).Thinking[0], typeof(TextChunk));
    }

    [TestMethod]
    public void ContentChunkBuilder_ExtractAnswerText_SkipsThinking()
    {
        var chunks = new List<ContentChunk>
        {
            ContentChunkBuilder.Thinking("internal thought"),
            ContentChunkBuilder.Text("final answer")
        };
        var result = ContentChunkBuilder.ExtractAnswerText(chunks);
        Assert.AreEqual("final answer", result);
    }

    [TestMethod]
    public void ContentChunkBuilder_ExtractThinkingText_OnlyThinking()
    {
        var chunks = new List<ContentChunk>
        {
            ContentChunkBuilder.Thinking("reasoning trace"),
            ContentChunkBuilder.Text("answer")
        };
        var result = ContentChunkBuilder.ExtractThinkingText(chunks);
        Assert.AreEqual("reasoning trace", result);
    }

    [TestMethod]
    public void MessageContentExtensions_GetContentText_String()
    {
        Assert.AreEqual("hi", MessageContentExtensions.GetContentText("hi"));
        Assert.IsNull(MessageContentExtensions.GetContentText(null));
    }

    [TestMethod]
    public void MessageContentExtensions_GetContentText_Chunks()
    {
        var chunks = new List<ContentChunk> { ContentChunkBuilder.Text("answer") };
        Assert.AreEqual("answer", MessageContentExtensions.GetContentText(chunks));
    }

    [TestMethod]
    public void MessageRequest_SystemWithChunks_SetsContentChunks()
    {
        var chunks = new List<ContentChunk> { ContentChunkBuilder.Text("system") };
        var msg = MessageRequest.SystemWithChunks(chunks);
        Assert.AreEqual(MessageRoles.System, msg.Role);
        Assert.AreSame(chunks, msg.ContentChunks);
        Assert.IsTrue(msg.IsValid());
    }

    [TestMethod]
    public void MessageRequest_ContentAsString_IsValid()
    {
        var msg = MessageRequest.User("hello");
        Assert.IsTrue(msg.IsValid());
        Assert.AreEqual("hello", msg.Content);
    }

    [TestMethod]
    public void MessageContentConverter_SerializesString()
    {
        var msg = MessageRequest.User("test");
        var opts = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower };
        var json = System.Text.Json.JsonSerializer.Serialize(msg, opts);
        Assert.IsTrue(json.Contains("content") && json.Contains("test"), $"Expected content in JSON: {json}");
    }

    #region Pen-test / Adversarial tests

    [TestMethod]
    public void MessageContentConverter_ReadNull_ReturnsNull()
    {
        var json = "{\"role\":\"user\",\"content\":null}";
        var msg = System.Text.Json.JsonSerializer.Deserialize<MessageRequest>(json);
        Assert.IsNotNull(msg);
        Assert.AreEqual(string.Empty, msg.Content);
    }

    [TestMethod]
    public void ContentChunkBuilder_ExtractAllText_NullChunks_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, ContentChunkBuilder.ExtractAllText(null));
        Assert.AreEqual(string.Empty, ContentChunkBuilder.ExtractAnswerText(null));
        Assert.AreEqual(string.Empty, ContentChunkBuilder.ExtractThinkingText(null));
    }

    [TestMethod]
    public void ContentChunkBuilder_ExtractAllText_ThinkChunkWithNullThinking_NoThrow()
    {
        var chunks = new List<ContentChunk> { new ThinkChunk { Thinking = null! } };
        var result = ContentChunkBuilder.ExtractAllText(chunks);
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ReasoningHelper_CreateReasoningRequest_NullModel_Throws()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            ReasoningHelper.CreateReasoningRequest(null!, "hello"));
    }

    [TestMethod]
    public void ReasoningHelper_CreateReasoningRequest_NullMessage_Throws()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            ReasoningHelper.CreateReasoningRequest(MistralModels.MagistralSmall, null!));
    }

    [TestMethod]
    public void MessageContentExtensions_GetContentText_UnknownType_ReturnsNull()
    {
        Assert.IsNull(MessageContentExtensions.GetContentText(42));
        Assert.IsNull(MessageContentExtensions.GetContentText(new object()));
    }

    [TestMethod]
    public void MessageResponse_Content_AlwaysReturnsString()
    {
        var msg = new MessageResponse { ContentRaw = "hello" };
        Assert.AreEqual("hello", msg.Content);
        msg.ContentRaw = new List<ContentChunk> { ContentChunkBuilder.Text("answer") };
        Assert.AreEqual("answer", msg.Content);
    }

    #endregion
}
