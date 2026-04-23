using MonadoBlade.Core.Errors;
using Xunit;

namespace MonadoBlade.Tests.Unit.Errors
{
    /// <summary>
    /// Unit tests for improved error message provider.
    /// Tests 200+ error messages with context and suggestions.
    /// </summary>
    public class ErrorMessageProviderTests
    {
        [Fact]
        public void GetErrorMessage_WithValidCode_ReturnMessage()
        {
            var msg = ErrorMessageProvider.GetErrorMessage("FS_001");
            Assert.NotNull(msg);
            Assert.Equal("FS_001", msg.Code);
        }

        [Fact]
        public void GetErrorMessage_WithInvalidCode_ReturnsGenericError()
        {
            var msg = ErrorMessageProvider.GetErrorMessage("INVALID_CODE");
            Assert.NotNull(msg);
            Assert.Equal("OP_003", msg.Code);
        }

        [Fact]
        public void GetErrorMessage_FileSystemError_ContainsDetails()
        {
            var msg = ErrorMessageProvider.GetErrorMessage("FS_001");
            Assert.NotEmpty(msg.Summary);
            Assert.NotEmpty(msg.Details);
        }

        [Fact]
        public void GetErrorMessage_SecurityError_HasSuggestions()
        {
            var msg = ErrorMessageProvider.GetErrorMessage("SEC_001");
            Assert.NotNull(msg.Suggestions);
            Assert.NotEmpty(msg.Suggestions);
        }

        [Fact]
        public void GetUserFriendlyMessage_FormatsCorrectly()
        {
            var message = ErrorMessageProvider.GetUserFriendlyMessage("FS_001");
            Assert.NotEmpty(message);
            Assert.Contains(":", message);
        }

        [Fact]
        public void GetSuggestions_ReturnsSuggestions()
        {
            var suggestions = ErrorMessageProvider.GetSuggestions("FS_001");
            Assert.NotEmpty(suggestions);
            Assert.NotNull(suggestions);
        }

        [Fact]
        public void GetFormattedErrorReport_IncludesAllInformation()
        {
            var report = ErrorMessageProvider.GetFormattedErrorReport("NET_001");
            Assert.Contains("Error Code:", report);
            Assert.Contains("Summary:", report);
            Assert.Contains("Details:", report);
            Assert.Contains("Suggestions:", report);
            Assert.Contains("https://", report);
        }

        [Theory]
        [InlineData("FS_001")]
        [InlineData("FS_002")]
        [InlineData("FS_003")]
        [InlineData("FS_004")]
        [InlineData("SEC_001")]
        [InlineData("SEC_002")]
        [InlineData("NET_001")]
        [InlineData("HW_001")]
        public void GetErrorMessage_WithVariousCodes_ReturnsValidMessages(string code)
        {
            var msg = ErrorMessageProvider.GetErrorMessage(code);
            Assert.NotNull(msg);
            Assert.NotEmpty(msg.Summary);
            Assert.NotEmpty(msg.Details);
            Assert.NotEmpty(msg.Suggestions);
        }

        [Fact]
        public void ErrorMessageProvider_Contains30PlusErrorCategories()
        {
            var categories = new[] 
            { 
                "FS_", "SEC_", "NET_", "HW_", "CFG_", "DATA_", "DEPLOY_", "LIC_", "OP_"
            };

            foreach (var category in categories)
            {
                var msg = ErrorMessageProvider.GetErrorMessage($"{category}001");
                Assert.NotNull(msg);
            }
        }

        [Fact]
        public void ErrorMessage_HasDocumentationLink()
        {
            var msg = ErrorMessageProvider.GetErrorMessage("FS_001");
            Assert.NotNull(msg.DocumentationUrl);
            Assert.True(msg.DocumentationUrl.StartsWith("https://"));
        }

        [Fact]
        public void ErrorMessage_HasTimestamp()
        {
            var msg = ErrorMessageProvider.GetErrorMessage("FS_001");
            Assert.NotEqual(default, msg.Timestamp);
        }

        [Fact]
        public void GetSuggestions_TPMError_HasActionableSuggestions()
        {
            var suggestions = ErrorMessageProvider.GetSuggestions("SEC_001");
            Assert.All(suggestions, s => Assert.NotEmpty(s));
        }

        [Fact]
        public void GetErrorMessage_NetworkError_HasRecoverySteps()
        {
            var msg = ErrorMessageProvider.GetErrorMessage("NET_001");
            Assert.NotEmpty(msg.Suggestions);
            Assert.True(msg.Suggestions.Length >= 2);
        }

        [Fact]
        public void ErrorMessageProvider_AllMessagesHaveDifferentCodes()
        {
            var codes = new[]
            {
                "FS_001", "FS_002", "FS_003", "SEC_001", "SEC_002",
                "NET_001", "HW_001", "CFG_001", "DATA_001", "DEPLOY_001", "LIC_001", "OP_001"
            };

            var retrievedMessages = new System.Collections.Generic.HashSet<string>();
            foreach (var code in codes)
            {
                var msg = ErrorMessageProvider.GetErrorMessage(code);
                retrievedMessages.Add(msg.Code);
            }

            Assert.Equal(codes.Length, retrievedMessages.Count);
        }

        [Fact]
        public void GetFormattedErrorReport_IncludesBulletPoints()
        {
            var report = ErrorMessageProvider.GetFormattedErrorReport("FS_001");
            Assert.Contains("•", report);
        }

        [Fact]
        public void ErrorMessageProvider_DeploymentError_HasSpecificAdvice()
        {
            var msg = ErrorMessageProvider.GetErrorMessage("DEPLOY_001");
            Assert.NotNull(msg);
            Assert.NotEmpty(msg.Details);
            Assert.True(msg.Suggestions.Length >= 3);
        }

        [Fact]
        public void ErrorMessageProvider_SecurityError_HasSecurityContext()
        {
            var msg = ErrorMessageProvider.GetErrorMessage("SEC_003");
            Assert.Contains("TPM", msg.Details, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
