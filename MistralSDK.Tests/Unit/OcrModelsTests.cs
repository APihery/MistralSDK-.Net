using Microsoft.VisualStudio.TestTools.UnitTesting;
using MistralSDK.Ocr;
using System;
using System.Text.Json;

namespace MistralSDK.Tests.Unit
{
    [TestClass]
    public class OcrModelsTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };

        [TestMethod]
        public void OcrDocument_FromFileId_SerializesCorrectly()
        {
            var doc = OcrDocument.FromFileId("file-123");
            var json = JsonSerializer.Serialize(new { document = doc }, JsonOptions);
            Assert.IsTrue(json.Contains("\"file_id\":\"file-123\""));
            Assert.IsTrue(json.Contains("\"type\":\"file\""));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void OcrDocument_FromFileId_Empty_Throws()
        {
            OcrDocument.FromFileId("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void OcrDocument_FromFileId_Whitespace_Throws()
        {
            OcrDocument.FromFileId("   ");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void OcrDocument_FromFileId_Null_Throws()
        {
            OcrDocument.FromFileId(null!);
        }

        [TestMethod]
        public void OcrDocument_FromDocumentUrl_SerializesCorrectly()
        {
            var doc = OcrDocument.FromDocumentUrl("https://example.com/doc.pdf");
            var json = JsonSerializer.Serialize(new { document = doc }, JsonOptions);
            Assert.IsTrue(json.Contains("\"document_url\":\"https://example.com/doc.pdf\""));
            Assert.IsTrue(json.Contains("\"type\":\"document_url\""));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void OcrDocument_FromDocumentUrl_Empty_Throws()
        {
            OcrDocument.FromDocumentUrl("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void OcrDocument_FromDocumentUrl_Whitespace_Throws()
        {
            OcrDocument.FromDocumentUrl("   ");
        }

        [TestMethod]
        public void OcrDocument_FromImageUrl_SerializesCorrectly()
        {
            var doc = OcrDocument.FromImageUrl("https://example.com/image.jpg");
            var json = JsonSerializer.Serialize(new { document = doc }, JsonOptions);
            Assert.IsTrue(json.Contains("\"image_url\""));
            Assert.IsTrue(json.Contains("\"url\":\"https://example.com/image.jpg\""));
            Assert.IsTrue(json.Contains("\"type\":\"image_url\""));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void OcrDocument_FromImageUrl_Empty_Throws()
        {
            OcrDocument.FromImageUrl("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void OcrDocument_FromImageUrl_Whitespace_Throws()
        {
            OcrDocument.FromImageUrl("   ");
        }

        [TestMethod]
        public void OcrDocument_FromImageBase64_SerializesCorrectly()
        {
            var doc = OcrDocument.FromImageBase64("YWJjZGVm", "image/png");
            var json = JsonSerializer.Serialize(new { document = doc }, JsonOptions);
            Assert.IsTrue(json.Contains("data:image/png;base64,YWJjZGVm"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void OcrDocument_FromImageBase64_Empty_Throws()
        {
            OcrDocument.FromImageBase64("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void OcrDocument_FromImageBase64_Whitespace_Throws()
        {
            OcrDocument.FromImageBase64("   ");
        }

        [TestMethod]
        public void OcrRequest_SerializesWithAllOptionalFields()
        {
            var request = new OcrRequest
            {
                Document = OcrDocument.FromImageUrl("https://test.com/img.jpg"),
                Model = OcrModels.MistralOcrLatest,
                TableFormat = OcrTableFormat.Markdown,
                ExtractHeader = true,
                ExtractFooter = true,
                IncludeImageBase64 = true,
                ImageLimit = 10,
                Pages = new System.Collections.Generic.List<int> { 0, 1 }
            };
            var json = JsonSerializer.Serialize(request, JsonOptions);
            Assert.IsTrue(json.Contains("mistral-ocr-latest"));
            Assert.IsTrue(json.Contains("markdown"));
        }

        [TestMethod]
        public void OcrResponse_GetAllMarkdown_ConcatenatesPages()
        {
            var response = new OcrResponse
            {
                Pages = new System.Collections.Generic.List<OcrPage>
                {
                    new OcrPage { Markdown = "Page 1" },
                    new OcrPage { Markdown = "Page 2" }
                }
            };
            var result = response.GetAllMarkdown();
            Assert.IsTrue(result.Contains("Page 1"));
            Assert.IsTrue(result.Contains("Page 2"));
        }

        [TestMethod]
        public void OcrResponse_DeserializesCorrectly()
        {
            var json = """{"pages":[{"index":1,"markdown":"Hello"}],"model":"mistral-ocr-latest","usage_info":{"pages_processed":1}}""";
            var response = JsonSerializer.Deserialize<OcrResponse>(json, JsonOptions);
            Assert.IsNotNull(response);
            Assert.AreEqual(1, response.Pages.Count);
            Assert.AreEqual("Hello", response.Pages[0].Markdown);
            Assert.AreEqual("mistral-ocr-latest", response.Model);
        }

        [TestMethod]
        public void OcrModels_MistralOcrLatest_HasCorrectValue()
        {
            Assert.AreEqual("mistral-ocr-latest", OcrModels.MistralOcrLatest);
        }

        [TestMethod]
        public void OcrTableFormat_Constants_HaveCorrectValues()
        {
            Assert.AreEqual("markdown", OcrTableFormat.Markdown);
            Assert.AreEqual("html", OcrTableFormat.Html);
        }
    }
}
