using Microsoft.VisualStudio.TestTools.UnitTesting;
using MistralSDK.Audio;
using System;
using System.IO;
using System.Text.Json;

namespace MistralSDK.Tests.Unit
{
    [TestClass]
    [TestCategory("Unit")]
    public class AudioModelsTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };

        [TestMethod]
        public void AudioTranscriptionRequestBuilder_FromStream_ValidInput_Succeeds()
        {
            using var stream = new MemoryStream(new byte[100]);
            var request = AudioTranscriptionRequestBuilder.FromStream(stream, "audio.mp3", AudioModels.VoxtralMiniLatest);
            Assert.IsNotNull(request);
            Assert.AreEqual(AudioModels.VoxtralMiniLatest, request.Model);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AudioTranscriptionRequestBuilder_FromStream_NullStream_Throws()
        {
            AudioTranscriptionRequestBuilder.FromStream(null!, "audio.mp3");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AudioTranscriptionRequestBuilder_FromStream_EmptyFileName_Throws()
        {
            using var stream = new MemoryStream();
            AudioTranscriptionRequestBuilder.FromStream(stream, "");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AudioTranscriptionRequestBuilder_FromStream_WhitespaceFileName_Throws()
        {
            using var stream = new MemoryStream();
            AudioTranscriptionRequestBuilder.FromStream(stream, "   ");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AudioTranscriptionRequestBuilder_FromStream_InvalidCharsInFileName_Throws()
        {
            using var stream = new MemoryStream();
            AudioTranscriptionRequestBuilder.FromStream(stream, "audio<>:mp3");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AudioTranscriptionRequestBuilder_FromStream_EmptyModel_Throws()
        {
            using var stream = new MemoryStream();
            AudioTranscriptionRequestBuilder.FromStream(stream, "audio.mp3", "");
        }

        [TestMethod]
        public void AudioTranscriptionRequestBuilder_FromFileId_ValidInput_Succeeds()
        {
            var request = AudioTranscriptionRequestBuilder.FromFileId("file-123", AudioModels.VoxtralMiniLatest);
            Assert.IsNotNull(request);
            Assert.AreEqual(AudioModels.VoxtralMiniLatest, request.Model);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AudioTranscriptionRequestBuilder_FromFileId_Empty_Throws()
        {
            AudioTranscriptionRequestBuilder.FromFileId("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AudioTranscriptionRequestBuilder_FromFileId_Whitespace_Throws()
        {
            AudioTranscriptionRequestBuilder.FromFileId("   ");
        }

        [TestMethod]
        public void AudioTranscriptionRequestBuilder_FromFileUrl_ValidInput_Succeeds()
        {
            var request = AudioTranscriptionRequestBuilder.FromFileUrl("https://example.com/audio.mp3", AudioModels.VoxtralMiniLatest);
            Assert.IsNotNull(request);
            Assert.AreEqual(AudioModels.VoxtralMiniLatest, request.Model);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AudioTranscriptionRequestBuilder_FromFileUrl_Empty_Throws()
        {
            AudioTranscriptionRequestBuilder.FromFileUrl("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AudioTranscriptionRequestBuilder_FromFileUrl_InvalidUrl_Throws()
        {
            AudioTranscriptionRequestBuilder.FromFileUrl("not-a-valid-url");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AudioTranscriptionRequestBuilder_FromFileUrl_UrlTooLong_Throws()
        {
            var longUrl = "https://example.com/" + new string('a', 2100);
            AudioTranscriptionRequestBuilder.FromFileUrl(longUrl);
        }

        [TestMethod]
        public void TranscriptionResponse_DeserializesCorrectly()
        {
            var json = """{"model":"voxtral-mini-2507","text":"Hello world","language":"en","segments":[],"usage":{"prompt_tokens":4,"completion_tokens":10,"total_tokens":14,"prompt_audio_seconds":5}}""";
            var response = JsonSerializer.Deserialize<TranscriptionResponse>(json, JsonOptions);
            Assert.IsNotNull(response);
            Assert.AreEqual("voxtral-mini-2507", response.Model);
            Assert.AreEqual("Hello world", response.Text);
            Assert.AreEqual("en", response.Language);
            Assert.AreEqual(5, response.Usage?.PromptAudioSeconds);
        }

        [TestMethod]
        public void AudioModels_Constants_HaveCorrectValues()
        {
            Assert.AreEqual("voxtral-mini-latest", AudioModels.VoxtralMiniLatest);
            Assert.AreEqual("voxtral-mini-2507", AudioModels.VoxtralMini2507);
            Assert.AreEqual("segment", TimestampGranularity.Segment);
            Assert.AreEqual("word", TimestampGranularity.Word);
        }

        #region Adversarial tests - attempts to break the SDK

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AudioTranscriptionRequestBuilder_FromStream_PathTraversal_Throws()
        {
            using var stream = new MemoryStream();
            AudioTranscriptionRequestBuilder.FromStream(stream, "../../../etc/passwd");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AudioTranscriptionRequestBuilder_FromStream_PathTraversalWindows_Throws()
        {
            using var stream = new MemoryStream();
            AudioTranscriptionRequestBuilder.FromStream(stream, "..\\..\\..\\windows\\system32\\config");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AudioTranscriptionRequestBuilder_FromStream_FileNameTooLong_Throws()
        {
            using var stream = new MemoryStream();
            var longName = new string('a', 256) + ".mp3";
            AudioTranscriptionRequestBuilder.FromStream(stream, longName);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AudioTranscriptionRequestBuilder_FromStream_NullCharInFileName_Throws()
        {
            using var stream = new MemoryStream();
            AudioTranscriptionRequestBuilder.FromStream(stream, "audio\x0.mp3");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AudioTranscriptionRequestBuilder_FromFileUrl_RelativeUrl_Throws()
        {
            AudioTranscriptionRequestBuilder.FromFileUrl("/relative/path/audio.mp3");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AudioTranscriptionRequestBuilder_FromFileUrl_FileProtocol_Throws()
        {
            AudioTranscriptionRequestBuilder.FromFileUrl("file:///C:/local/audio.mp3");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AudioTranscriptionRequestBuilder_FromFileUrl_Whitespace_Throws()
        {
            AudioTranscriptionRequestBuilder.FromFileUrl("   ");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AudioTranscriptionRequestBuilder_FromFileId_Null_Throws()
        {
            AudioTranscriptionRequestBuilder.FromFileId(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AudioTranscriptionRequestBuilder_FromStream_NullModel_Throws()
        {
            using var stream = new MemoryStream();
            AudioTranscriptionRequestBuilder.FromStream(stream, "audio.mp3", null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AudioTranscriptionRequestBuilder_FromStream_WhitespaceModel_Throws()
        {
            using var stream = new MemoryStream();
            AudioTranscriptionRequestBuilder.FromStream(stream, "audio.mp3", "   ");
        }

        #endregion
    }
}
