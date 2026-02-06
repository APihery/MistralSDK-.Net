using Microsoft.VisualStudio.TestTools.UnitTesting;
using MistralSDK.ChatCompletion;

namespace MistralSDK.Tests.Unit
{
    /// <summary>
    /// Unit tests for MistralModels constants.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class MistralModelsTests
    {
        #region Premier Models

        [TestMethod]
        public void MistralModels_Large_HasCorrectValue()
        {
            Assert.AreEqual("mistral-large-latest", MistralModels.Large);
        }

        [TestMethod]
        public void MistralModels_PixtralLarge_HasCorrectValue()
        {
            Assert.AreEqual("pixtral-large-latest", MistralModels.PixtralLarge);
        }

        [TestMethod]
        public void MistralModels_Saba_HasCorrectValue()
        {
            Assert.AreEqual("mistral-saba-latest", MistralModels.Saba);
        }

        #endregion

        #region Efficient Models

        [TestMethod]
        public void MistralModels_Small_HasCorrectValue()
        {
            Assert.AreEqual("mistral-small-latest", MistralModels.Small);
        }

        [TestMethod]
        public void MistralModels_Ministral8B_HasCorrectValue()
        {
            Assert.AreEqual("ministral-8b-latest", MistralModels.Ministral8B);
        }

        [TestMethod]
        public void MistralModels_Ministral3B_HasCorrectValue()
        {
            Assert.AreEqual("ministral-3b-latest", MistralModels.Ministral3B);
        }

        #endregion

        #region Specialized Models

        [TestMethod]
        public void MistralModels_Codestral_HasCorrectValue()
        {
            Assert.AreEqual("codestral-latest", MistralModels.Codestral);
        }

        [TestMethod]
        public void MistralModels_Embed_HasCorrectValue()
        {
            Assert.AreEqual("mistral-embed", MistralModels.Embed);
        }

        [TestMethod]
        public void MistralModels_Moderation_HasCorrectValue()
        {
            Assert.AreEqual("mistral-moderation-latest", MistralModels.Moderation);
        }

        [TestMethod]
        public void MistralModels_Pixtral_HasCorrectValue()
        {
            Assert.AreEqual("pixtral-12b-2409", MistralModels.Pixtral);
        }

        [TestMethod]
        public void MistralModels_Nemo_HasCorrectValue()
        {
            Assert.AreEqual("open-mistral-nemo", MistralModels.Nemo);
        }

        #endregion

        #region Legacy Models

        [TestMethod]
        public void MistralModels_Tiny_HasCorrectValue()
        {
            #pragma warning disable CS0618 // Obsolete warning
            Assert.AreEqual("mistral-tiny", MistralModels.Tiny);
            #pragma warning restore CS0618
        }

        [TestMethod]
        public void MistralModels_Medium_HasCorrectValue()
        {
            #pragma warning disable CS0618 // Obsolete warning
            Assert.AreEqual("mistral-medium-latest", MistralModels.Medium);
            #pragma warning restore CS0618
        }

        #endregion

        #region PromptModes Tests

        [TestMethod]
        public void PromptModes_Reasoning_HasCorrectValue()
        {
            Assert.AreEqual("reasoning", PromptModes.Reasoning);
        }

        #endregion

        #region ResponseFormatType Tests

        [TestMethod]
        public void ResponseFormatType_Text_HasCorrectValue()
        {
            Assert.AreEqual("text", ResponseFormatType.Text);
        }

        [TestMethod]
        public void ResponseFormatType_JsonObject_HasCorrectValue()
        {
            Assert.AreEqual("json_object", ResponseFormatType.JsonObject);
        }

        [TestMethod]
        public void ResponseFormatType_JsonSchema_HasCorrectValue()
        {
            Assert.AreEqual("json_schema", ResponseFormatType.JsonSchema);
        }

        #endregion
    }
}
