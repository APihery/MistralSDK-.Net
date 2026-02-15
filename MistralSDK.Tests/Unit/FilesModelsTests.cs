using Microsoft.VisualStudio.TestTools.UnitTesting;
using MistralSDK.Files;
using System.Text.Json;

namespace MistralSDK.Tests.Unit
{
    [TestClass]
    public class FilesModelsTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        [TestMethod]
        public void MistralFileInfo_DeserializesCorrectly()
        {
            var json = """{"id":"file-123","object":"file","filename":"test.pdf","purpose":"ocr","source":"upload"}""";
            var file = JsonSerializer.Deserialize<MistralFileInfo>(json, JsonOptions);
            Assert.IsNotNull(file);
            Assert.AreEqual("file-123", file.Id);
            Assert.AreEqual("test.pdf", file.Filename);
            Assert.AreEqual("ocr", file.Purpose);
        }

        [TestMethod]
        public void FileListResponse_DeserializesCorrectly()
        {
            var json = """{"data":[{"id":"f1","filename":"a.pdf"}],"object":"list","total":1}""";
            var response = JsonSerializer.Deserialize<FileListResponse>(json, JsonOptions);
            Assert.IsNotNull(response);
            Assert.AreEqual(1, response.Data.Count);
            Assert.AreEqual("f1", response.Data[0].Id);
            Assert.AreEqual(1, response.Total);
        }

        [TestMethod]
        public void FileDeleteResponse_DeserializesCorrectly()
        {
            var json = """{"id":"file-123","object":"file","deleted":true}""";
            var response = JsonSerializer.Deserialize<FileDeleteResponse>(json, JsonOptions);
            Assert.IsNotNull(response);
            Assert.IsTrue(response.Deleted);
            Assert.AreEqual("file-123", response.Id);
        }

        [TestMethod]
        public void FileSignedUrlResponse_DeserializesCorrectly()
        {
            var json = """{"url":"https://example.com/signed-url"}""";
            var response = JsonSerializer.Deserialize<FileSignedUrlResponse>(json, JsonOptions);
            Assert.IsNotNull(response);
            Assert.IsTrue(response.Url.StartsWith("https://"));
        }

        [TestMethod]
        public void FilePurpose_Constants_HaveCorrectValues()
        {
            Assert.AreEqual("ocr", FilePurpose.Ocr);
            Assert.AreEqual("fine-tune", FilePurpose.FineTune);
            Assert.AreEqual("batch", FilePurpose.Batch);
        }
    }
}
