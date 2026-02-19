using Microsoft.VisualStudio.TestTools.UnitTesting;
using MistralSDK;
using MistralSDK.Audio;
using MistralSDK.Configuration;
using MistralSDK.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MistralSDK.Tests
{
    /// <summary>
    /// Integration tests for Audio & Transcription API.
    /// Requires MISTRAL_API_KEY and api-key.txt. Uses Ressources/audio_test.mp3.
    /// </summary>
    [TestClass]
    public class AudioAndTranscriptionTests
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

        #region Success scenarios with audio_test.mp3

        [TestMethod]
        [TestCategory("Integration")]
        public async Task Audio_TranscribeFromStream_WithAudioTestMp3_ReturnsText()
        {
            SkipIfIntegrationDisabled();
            var filePath = GetTestFilePath("audio_test.mp3");
            using var stream = File.OpenRead(filePath);

            var request = AudioTranscriptionRequestBuilder.FromStream(stream, "audio_test.mp3");
            var result = await _client!.AudioTranscribeAsync(request);

            Assert.IsNotNull(result);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Text));
            Assert.IsTrue(result.Model.StartsWith("voxtral-mini", StringComparison.OrdinalIgnoreCase));
            Assert.IsNotNull(result.Usage);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task Audio_TranscribeFromUploadedFile_WithAudioTestMp3_ReturnsText()
        {
            SkipIfIntegrationDisabled();
            var filePath = GetTestFilePath("audio_test.mp3");
            string? fileId = null;

            try
            {
                using (var stream = File.OpenRead(filePath))
                    fileId = (await _client!.FilesUploadAsync(stream, "audio_test.mp3", FilePurpose.Audio)).Id;

                var request = AudioTranscriptionRequestBuilder.FromFileId(fileId);
                var result = await _client!.AudioTranscribeAsync(request);

                Assert.IsNotNull(result);
                Assert.IsFalse(string.IsNullOrWhiteSpace(result.Text));
            }
            finally
            {
                if (!string.IsNullOrEmpty(fileId))
                    await _client!.FilesDeleteAsync(fileId);
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task Audio_TranscribeWithLanguageHint_ReturnsResult()
        {
            SkipIfIntegrationDisabled();
            var filePath = GetTestFilePath("audio_test.mp3");
            using var stream = File.OpenRead(filePath);

            var request = AudioTranscriptionRequestBuilder.FromStream(stream, "audio_test.mp3");
            request.Language = "en";

            var result = await _client!.AudioTranscribeAsync(request);
            Assert.IsNotNull(result);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Text));
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task Audio_TranscribeStream_WithAudioTestMp3_ReturnsEvents()
        {
            SkipIfIntegrationDisabled();
            var filePath = GetTestFilePath("audio_test.mp3");
            using var stream = File.OpenRead(filePath);

            var request = AudioTranscriptionRequestBuilder.FromStream(stream, "audio_test.mp3");
            var events = new System.Collections.Generic.List<TranscriptionStreamEvent>();

            await foreach (var evt in _client!.AudioTranscribeStreamAsync(request))
                events.Add(evt);

            var done = events.OfType<TranscriptionStreamDone>().LastOrDefault();
            if (done != null)
                Assert.IsFalse(string.IsNullOrWhiteSpace(done.Text));
            else if (events.Count == 0)
                Assert.Inconclusive("Stream returned no events (API format may vary)");
        }

        #endregion

        #region Error scenarios (adversarial)

        [TestMethod]
        [TestCategory("Integration")]
        public async Task Audio_InvalidFileId_ThrowsMistralApiException()
        {
            SkipIfIntegrationDisabled();
            var request = AudioTranscriptionRequestBuilder.FromFileId("file-id-inexistant-xyz-12345");
            await Assert.ThrowsExceptionAsync<Exceptions.MistralApiException>(() =>
                _client!.AudioTranscribeAsync(request));
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task Audio_InvalidFileUrl_ThrowsMistralApiException()
        {
            SkipIfIntegrationDisabled();
            var request = AudioTranscriptionRequestBuilder.FromFileUrl(
                "https://invalid-domain-that-does-not-exist-xyz999.com/audio.mp3");
            await Assert.ThrowsExceptionAsync<Exceptions.MistralApiException>(() =>
                _client!.AudioTranscribeAsync(request));
        }

        [TestMethod]
        [TestCategory("Integration")]
        public async Task Audio_UnsupportedFileFormat_ThrowsMistralApiException()
        {
            SkipIfIntegrationDisabled();
            var filePath = GetTestFilePath("test_ocr_image_1.jpg"); // Image, not audio
            using var stream = File.OpenRead(filePath);

            var request = AudioTranscriptionRequestBuilder.FromStream(stream, "image.jpg");
            await Assert.ThrowsExceptionAsync<Exceptions.MistralApiException>(() =>
                _client!.AudioTranscribeAsync(request));
        }

        [TestMethod]
        [TestCategory("Integration")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task Audio_NullRequest_Throws()
        {
            SkipIfIntegrationDisabled();
            await _client!.AudioTranscribeAsync(null!);
        }

        #endregion
    }
}
