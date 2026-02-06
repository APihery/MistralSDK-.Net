using Microsoft.VisualStudio.TestTools.UnitTesting;
using MistralSDK.Configuration;
using System;

namespace MistralSDK.Tests.Unit
{
    /// <summary>
    /// Unit tests for the MistralClientOptions class.
    /// </summary>
    [TestClass]
    public class MistralClientOptionsTests
    {
        [TestMethod]
        public void Options_DefaultValues_AreCorrect()
        {
            var options = new MistralClientOptions();

            Assert.AreEqual(string.Empty, options.ApiKey);
            Assert.AreEqual("https://api.mistral.ai/v1", options.BaseUrl);
            Assert.AreEqual(30, options.TimeoutSeconds);
            Assert.AreEqual(3, options.MaxRetries);
            Assert.AreEqual(1000, options.RetryDelayMilliseconds);
            Assert.IsFalse(options.EnableCaching);
            Assert.AreEqual(5, options.CacheExpirationMinutes);
            Assert.IsFalse(options.ThrowOnError);
            Assert.IsTrue(options.ValidateRequests);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Validate_EmptyApiKey_ThrowsArgumentException()
        {
            var options = new MistralClientOptions
            {
                ApiKey = ""
            };

            options.Validate();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Validate_WhitespaceApiKey_ThrowsArgumentException()
        {
            var options = new MistralClientOptions
            {
                ApiKey = "   "
            };

            options.Validate();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Validate_EmptyBaseUrl_ThrowsArgumentException()
        {
            var options = new MistralClientOptions
            {
                ApiKey = "valid-key",
                BaseUrl = ""
            };

            options.Validate();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Validate_InvalidBaseUrl_ThrowsArgumentException()
        {
            var options = new MistralClientOptions
            {
                ApiKey = "valid-key",
                BaseUrl = "not-a-valid-url"
            };

            options.Validate();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Validate_NegativeTimeout_ThrowsArgumentException()
        {
            var options = new MistralClientOptions
            {
                ApiKey = "valid-key",
                TimeoutSeconds = -1
            };

            options.Validate();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Validate_ZeroTimeout_ThrowsArgumentException()
        {
            var options = new MistralClientOptions
            {
                ApiKey = "valid-key",
                TimeoutSeconds = 0
            };

            options.Validate();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Validate_NegativeMaxRetries_ThrowsArgumentException()
        {
            var options = new MistralClientOptions
            {
                ApiKey = "valid-key",
                MaxRetries = -1
            };

            options.Validate();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Validate_NegativeRetryDelay_ThrowsArgumentException()
        {
            var options = new MistralClientOptions
            {
                ApiKey = "valid-key",
                RetryDelayMilliseconds = -1
            };

            options.Validate();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Validate_ZeroCacheExpiration_ThrowsArgumentException()
        {
            var options = new MistralClientOptions
            {
                ApiKey = "valid-key",
                CacheExpirationMinutes = 0
            };

            options.Validate();
        }

        [TestMethod]
        public void Validate_ValidOptions_DoesNotThrow()
        {
            var options = new MistralClientOptions
            {
                ApiKey = "valid-key",
                BaseUrl = "https://api.mistral.ai/v1",
                TimeoutSeconds = 30,
                MaxRetries = 3,
                RetryDelayMilliseconds = 1000,
                CacheExpirationMinutes = 5
            };

            // Should not throw
            options.Validate();
        }

        [TestMethod]
        public void Validate_CustomValidOptions_DoesNotThrow()
        {
            var options = new MistralClientOptions
            {
                ApiKey = "my-custom-key",
                BaseUrl = "https://custom.api.com/v2",
                TimeoutSeconds = 120,
                MaxRetries = 5,
                RetryDelayMilliseconds = 2000,
                EnableCaching = true,
                CacheExpirationMinutes = 10,
                ThrowOnError = true,
                ValidateRequests = false
            };

            // Should not throw
            options.Validate();
        }

        [TestMethod]
        public void Validate_HttpBaseUrl_DoesNotThrow()
        {
            var options = new MistralClientOptions
            {
                ApiKey = "valid-key",
                BaseUrl = "http://localhost:8080/api"
            };

            // Should not throw (HTTP is allowed for local development)
            options.Validate();
        }
    }
}
