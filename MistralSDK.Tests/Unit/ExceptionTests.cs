using Microsoft.VisualStudio.TestTools.UnitTesting;
using MistralSDK.Exceptions;
using System.Net;

namespace MistralSDK.Tests.Unit
{
    /// <summary>
    /// Unit tests for the custom exception classes.
    /// </summary>
    [TestClass]
    public class ExceptionTests
    {
        #region MistralApiException Tests

        [TestMethod]
        public void MistralApiException_WithMessage_SetsProperties()
        {
            var exception = new MistralApiException("Test error message");

            Assert.AreEqual("Test error message", exception.Message);
            Assert.AreEqual(HttpStatusCode.InternalServerError, exception.StatusCode);
            Assert.IsFalse(exception.IsRetryable);
        }

        [TestMethod]
        public void MistralApiException_WithStatusCode_SetsProperties()
        {
            var exception = new MistralApiException("Error", HttpStatusCode.BadRequest);

            Assert.AreEqual(HttpStatusCode.BadRequest, exception.StatusCode);
            Assert.IsFalse(exception.IsRetryable);
        }

        [TestMethod]
        public void MistralApiException_429TooManyRequests_IsRetryable()
        {
            var exception = new MistralApiException("Rate limited", HttpStatusCode.TooManyRequests);

            Assert.IsTrue(exception.IsRetryable);
            Assert.AreEqual(60, exception.RetryDelaySeconds);
        }

        [TestMethod]
        public void MistralApiException_503ServiceUnavailable_IsRetryable()
        {
            var exception = new MistralApiException("Service unavailable", HttpStatusCode.ServiceUnavailable);

            Assert.IsTrue(exception.IsRetryable);
            Assert.AreEqual(30, exception.RetryDelaySeconds);
        }

        [TestMethod]
        public void MistralApiException_500InternalServerError_IsRetryable()
        {
            var exception = new MistralApiException("Server error", HttpStatusCode.InternalServerError);

            Assert.IsTrue(exception.IsRetryable);
            Assert.AreEqual(5, exception.RetryDelaySeconds);
        }

        [TestMethod]
        public void MistralApiException_400BadRequest_IsNotRetryable()
        {
            var exception = new MistralApiException("Bad request", HttpStatusCode.BadRequest);

            Assert.IsFalse(exception.IsRetryable);
            Assert.IsNull(exception.RetryDelaySeconds);
        }

        [TestMethod]
        public void MistralApiException_WithErrorType_SetsProperties()
        {
            var exception = new MistralApiException(
                "Rate limit exceeded", 
                HttpStatusCode.TooManyRequests, 
                "rate_limit_error", 
                "rate_limit_exceeded");

            Assert.AreEqual("rate_limit_error", exception.ErrorType);
            Assert.AreEqual("rate_limit_exceeded", exception.ErrorCode);
            Assert.IsTrue(exception.IsRetryable);
        }

        [TestMethod]
        public void MistralApiException_WithInnerException_SetsProperties()
        {
            var inner = new TimeoutException("Request timed out");
            var exception = new MistralApiException("Timeout error", inner);

            Assert.AreEqual(inner, exception.InnerException);
            Assert.IsTrue(exception.IsRetryable); // Timeout exceptions are retryable
        }

        #endregion

        #region MistralValidationException Tests

        [TestMethod]
        public void MistralValidationException_WithSingleError_SetsProperties()
        {
            var exception = new MistralValidationException("Model is required");

            Assert.AreEqual(1, exception.ValidationErrors.Count);
            Assert.AreEqual("Model is required", exception.ValidationErrors[0]);
            Assert.IsTrue(exception.Message.Contains("Model is required"));
        }

        [TestMethod]
        public void MistralValidationException_WithMultipleErrors_SetsProperties()
        {
            var errors = new[] { "Model is required", "Messages cannot be empty" };
            var exception = new MistralValidationException(errors);

            Assert.AreEqual(2, exception.ValidationErrors.Count);
            Assert.IsTrue(exception.Message.Contains("Model is required"));
            Assert.IsTrue(exception.Message.Contains("Messages cannot be empty"));
        }

        #endregion

        #region MistralAuthenticationException Tests

        [TestMethod]
        public void MistralAuthenticationException_SetsUnauthorizedStatus()
        {
            var exception = new MistralAuthenticationException("Invalid API key");

            Assert.AreEqual(HttpStatusCode.Unauthorized, exception.StatusCode);
            Assert.AreEqual("Invalid API key", exception.Message);
        }

        #endregion

        #region MistralRateLimitException Tests

        [TestMethod]
        public void MistralRateLimitException_SetsProperties()
        {
            var exception = new MistralRateLimitException("Too many requests");

            Assert.AreEqual(HttpStatusCode.TooManyRequests, exception.StatusCode);
            Assert.AreEqual("rate_limit_error", exception.ErrorType);
            Assert.IsTrue(exception.IsRetryable);
        }

        #endregion

        #region MistralModelNotFoundException Tests

        [TestMethod]
        public void MistralModelNotFoundException_SetsProperties()
        {
            var exception = new MistralModelNotFoundException("mistral-unknown");

            Assert.AreEqual(HttpStatusCode.NotFound, exception.StatusCode);
            Assert.AreEqual("mistral-unknown", exception.ModelId);
            Assert.IsTrue(exception.Message.Contains("mistral-unknown"));
            Assert.AreEqual("model_not_found", exception.ErrorType);
        }

        #endregion
    }
}
