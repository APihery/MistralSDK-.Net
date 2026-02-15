using Microsoft.VisualStudio.TestTools.UnitTesting;
using MistralSDK;
using MistralSDK.Configuration;
using MistralSDK.Files;
using MistralSDK.Ocr;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MistralSDK.Tests
{
    /// <summary>
    /// Integration tests for OCR and Files API.
    /// Requires MISTRAL_API_KEY and api-key.txt for integration tests.
    /// </summary>
    [TestClass]
    public class OcrAndFilesTests
    {
        private MistralClient? _client;
        private string _ressourcesPath = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            if (!TestConfiguration.IsIntegrationTestEnabled())
                return;

            _client = new MistralClient(new MistralClientOptions
            {
                ApiKey = TestConfiguration.GetApiKeyOrThrow(),
                TimeoutSeconds = 120,
                ValidateRequests = true
            });

            _ressourcesPath = Path.Combine(AppContext.BaseDirectory, "Ressources");
        }

        [TestCleanup]
        public void TestCleanup() => _client?.Dispose();

        private void SkipIfIntegrationDisabled()
        {
            if (!TestConfiguration.IsIntegrationTestEnabled())
                Assert.Inconclusive("Set MISTRAL_API_KEY and api-key.txt for integration tests.");
            Assert.IsNotNull(_client);
        }

        private string GetTestFilePath(string fileName)
        {
            var path = Path.Combine(_ressourcesPath, fileName);
            if (!File.Exists(path))
                Assert.Inconclusive($"Test file not found: {path}");
            return path;
        }

        #region Files API Integration Tests

        [TestMethod]
        [TestCategory("Integration")]
        public async Task Files_List_ReturnsResponse()
        {
            SkipIfIntegrationDisabled();
            var response = await _client!.FilesListAsync();
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Data);
            Assert.AreEqual("list", response.Object);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task Files_UploadAndDelete_OcrFile_Succeeds()
        {
            SkipIfIntegrationDisabled();
            var filePath = GetTestFilePath("test_ocr_image_1.jpg");
            using var stream = File.OpenRead(filePath);

            var uploaded = await _client!.FilesUploadAsync(stream, "test_ocr_image_1.jpg", FilePurpose.Ocr);
            Assert.IsNotNull(uploaded);
            Assert.IsFalse(string.IsNullOrEmpty(uploaded.Id));
            Assert.AreEqual("ocr", uploaded.Purpose);

            var deleted = await _client.FilesDeleteAsync(uploaded.Id);
            Assert.IsTrue(deleted.Deleted);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task Files_Upload_InvalidPurpose_Throws()
        {
            SkipIfIntegrationDisabled();
            var filePath = GetTestFilePath("test_ocr_image_1.jpg");
            using var stream = File.OpenRead(filePath);
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _client!.FilesUploadAsync(stream, "test.jpg", "invalid-purpose"));
        }

        #endregion

        #region OCR API Integration Tests

        [TestMethod]
        [TestCategory("Integration")]
        public async Task Ocr_WithImageUrl_FromUploadedFile_ExtractsText()
        {
            SkipIfIntegrationDisabled();
            var filePath = GetTestFilePath("test_ocr_image_1.jpg");
            string? fileId = null;
            try
            {
                using (var stream = File.OpenRead(filePath))
                    fileId = (await _client!.FilesUploadAsync(stream, "test_ocr.jpg", FilePurpose.Ocr)).Id;

                var request = new OcrRequest
                {
                    Document = OcrDocument.FromFileId(fileId),
                    Model = OcrModels.MistralOcrLatest
                };
                var response = await _client!.OcrProcessAsync(request);
                Assert.IsNotNull(response);
                Assert.IsTrue(response.Pages.Count > 0);
                var text = response.GetAllMarkdown();
                Assert.IsFalse(string.IsNullOrWhiteSpace(text));
                // Pangram should be extracted
                Assert.IsTrue(text.Contains("fox", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("quick", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("dog", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                if (!string.IsNullOrEmpty(fileId))
                    await _client!.FilesDeleteAsync(fileId);
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task Ocr_WithPdfFile_ExtractsText()
        {
            SkipIfIntegrationDisabled();
            var filePath = GetTestFilePath("test_ocr_fr_en.pdf");
            string? fileId = null;
            try
            {
                using (var stream = File.OpenRead(filePath))
                    fileId = (await _client!.FilesUploadAsync(stream, "test_ocr_fr_en.pdf", FilePurpose.Ocr)).Id;

                var request = new OcrRequest
                {
                    Document = OcrDocument.FromFileId(fileId),
                    Model = OcrModels.MistralOcrLatest
                };
                var response = await _client!.OcrProcessAsync(request);
                Assert.IsNotNull(response);
                Assert.IsTrue(response.Pages.Count > 0);
                var text = response.GetAllMarkdown();
                Assert.IsFalse(string.IsNullOrWhiteSpace(text));
                // Expected content from the test PDF
                Assert.IsTrue(text.Contains("renard", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("fox", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("OCR", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                if (!string.IsNullOrEmpty(fileId))
                    await _client!.FilesDeleteAsync(fileId);
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task Ocr_WithLocalImageBase64_ExtractsText()
        {
            SkipIfIntegrationDisabled();
            var filePath = GetTestFilePath("test_ocr_image_1.jpg");
            var bytes = await File.ReadAllBytesAsync(filePath);
            var base64 = Convert.ToBase64String(bytes);

            var request = new OcrRequest
            {
                Document = OcrDocument.FromImageBase64(base64, "image/jpeg"),
                Model = OcrModels.MistralOcrLatest
            };
            var response = await _client!.OcrProcessAsync(request);
            Assert.IsNotNull(response);
            Assert.IsTrue(response.Pages.Count > 0);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task Ocr_ScannedPdf_ProcessesWithoutCrash()
        {
            SkipIfIntegrationDisabled();
            var filePath = GetTestFilePath("test_ocr_pdf_1.pdf");
            string? fileId = null;
            try
            {
                using (var stream = File.OpenRead(filePath))
                    fileId = (await _client!.FilesUploadAsync(stream, "test_ocr_pdf_1.pdf", FilePurpose.Ocr)).Id;

                var request = new OcrRequest
                {
                    Document = OcrDocument.FromFileId(fileId),
                    Model = OcrModels.MistralOcrLatest
                };
                var response = await _client!.OcrProcessAsync(request);
                Assert.IsNotNull(response);
                Assert.IsTrue(response.Pages.Count >= 0);
            }
            finally
            {
                if (!string.IsNullOrEmpty(fileId))
                    await _client!.FilesDeleteAsync(fileId);
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        [ExpectedException(typeof(ArgumentException))]
        public async Task Ocr_NullDocument_Throws()
        {
            SkipIfIntegrationDisabled();
            await _client!.OcrProcessAsync(new OcrRequest { Document = null! });
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task Files_Retrieve_InvalidFileId_ThrowsMistralApiException()
        {
            SkipIfIntegrationDisabled();
            await Assert.ThrowsExceptionAsync<MistralSDK.Exceptions.MistralApiException>(() =>
                _client!.FilesRetrieveAsync("file-id-inexistant-12345"));
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task Ocr_WithInvalidDocumentUrl_ThrowsMistralApiException()
        {
            SkipIfIntegrationDisabled();
            var request = new OcrRequest
            {
                Document = OcrDocument.FromDocumentUrl("https://invalid-domain-that-does-not-exist-xyz123.com/document.pdf"),
                Model = OcrModels.MistralOcrLatest
            };
            await Assert.ThrowsExceptionAsync<MistralSDK.Exceptions.MistralApiException>(() =>
                _client!.OcrProcessAsync(request));
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task Ocr_WithInvalidFileId_ThrowsMistralApiException()
        {
            SkipIfIntegrationDisabled();
            var request = new OcrRequest
            {
                Document = OcrDocument.FromFileId("file-id-inexistant-12345"),
                Model = OcrModels.MistralOcrLatest
            };
            await Assert.ThrowsExceptionAsync<MistralSDK.Exceptions.MistralApiException>(() =>
                _client!.OcrProcessAsync(request));
        }

        #endregion
    }
}
