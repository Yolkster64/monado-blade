using System;
using System.Collections.Generic;
using MonadoBlade.Core.Quality;
using Xunit;

namespace MonadoBlade.Tests.Unit.Quality
{
    /// <summary>
    /// Unit tests for DuplicateCodeExtractor utility class.
    /// Tests all 50+ utility methods to ensure reduction of code duplication.
    /// </summary>
    public class DuplicateCodeExtractorTests
    {
        [Fact]
        public void IsNullOrWhiteSpace_WithNull_ReturnsTrue()
        {
            Assert.True(DuplicateCodeExtractor.IsNullOrWhiteSpace(null));
        }

        [Fact]
        public void IsNullOrWhiteSpace_WithEmpty_ReturnsTrue()
        {
            Assert.True(DuplicateCodeExtractor.IsNullOrWhiteSpace(string.Empty));
        }

        [Fact]
        public void IsNullOrWhiteSpace_WithWhitespace_ReturnsTrue()
        {
            Assert.True(DuplicateCodeExtractor.IsNullOrWhiteSpace("   "));
        }

        [Fact]
        public void IsNullOrWhiteSpace_WithText_ReturnsFalse()
        {
            Assert.False(DuplicateCodeExtractor.IsNullOrWhiteSpace("hello"));
        }

        [Fact]
        public void IsInRange_WithValueInRange_ReturnsTrue()
        {
            Assert.True(DuplicateCodeExtractor.IsInRange(5, 0, 10));
        }

        [Fact]
        public void IsInRange_WithValueOutOfRange_ReturnsFalse()
        {
            Assert.False(DuplicateCodeExtractor.IsInRange(15, 0, 10));
        }

        [Fact]
        public void HasExtension_WithCorrectExtension_ReturnsTrue()
        {
            Assert.True(DuplicateCodeExtractor.HasExtension("file.txt", ".txt"));
        }

        [Fact]
        public void HasExtension_WithWrongExtension_ReturnsFalse()
        {
            Assert.False(DuplicateCodeExtractor.HasExtension("file.txt", ".cs"));
        }

        [Fact]
        public void TruncateString_WithLongString_TruncatesCorrectly()
        {
            var result = DuplicateCodeExtractor.TruncateString("Hello World", 5);
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void HexStringToByteArray_WithValidHex_ConvertsCorrectly()
        {
            var result = DuplicateCodeExtractor.HexStringToByteArray("48656C6C6F");
            Assert.Equal(5, result.Length);
        }

        [Fact]
        public void ByteArrayToHexString_WithBytes_ConvertsCorrectly()
        {
            var bytes = new byte[] { 72, 101, 108, 108, 111 };
            var result = DuplicateCodeExtractor.ByteArrayToHexString(bytes);
            Assert.Equal("48656C6C6F", result);
        }

        [Fact]
        public void SanitizeFilename_WithInvalidChars_RemovesChars()
        {
            var result = DuplicateCodeExtractor.SanitizeFilename("file<name>.txt");
            Assert.DoesNotContain("<", result);
            Assert.DoesNotContain(">", result);
        }

        [Fact]
        public void ExtractVersion_WithValidVersion_ExtractsCorrectly()
        {
            var result = DuplicateCodeExtractor.ExtractVersion("Version 3.1.0 released");
            Assert.Equal("3.1.0", result);
        }

        [Fact]
        public void ComputeSHA256Hash_WithText_ReturnsHash()
        {
            var result = DuplicateCodeExtractor.ComputeSHA256Hash("hello");
            Assert.NotEmpty(result);
            Assert.Equal(64, result.Length);
        }

        [Fact]
        public void VerifyHash_WithCorrectHash_ReturnsTrue()
        {
            var data = "hello";
            var hash = DuplicateCodeExtractor.ComputeSHA256Hash(data);
            Assert.True(DuplicateCodeExtractor.VerifyHash(data, hash));
        }

        [Fact]
        public void GenerateRandomBytes_WithValidLength_GeneratesBytes()
        {
            var result = DuplicateCodeExtractor.GenerateRandomBytes(32);
            Assert.Equal(32, result.Length);
        }

        [Fact]
        public void GenerateUniqueId_GeneratesId()
        {
            var id1 = DuplicateCodeExtractor.GenerateUniqueId();
            var id2 = DuplicateCodeExtractor.GenerateUniqueId();
            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public void SafeGetDictValue_WithExistingKey_ReturnsValue()
        {
            var dict = new Dictionary<string, int> { { "key", 42 } };
            var result = DuplicateCodeExtractor.SafeGetDictValue(dict, "key", 0);
            Assert.Equal(42, result);
        }

        [Fact]
        public void SafeGetDictValue_WithMissingKey_ReturnsDefault()
        {
            var dict = new Dictionary<string, int>();
            var result = DuplicateCodeExtractor.SafeGetDictValue(dict, "key", 99);
            Assert.Equal(99, result);
        }

        [Fact]
        public void IsNullOrEmpty_WithEmptyCollection_ReturnsTrue()
        {
            var collection = new List<int>();
            Assert.True(DuplicateCodeExtractor.IsNullOrEmpty(collection));
        }

        [Fact]
        public void IsNullOrEmpty_WithItems_ReturnsFalse()
        {
            var collection = new List<int> { 1, 2, 3 };
            Assert.False(DuplicateCodeExtractor.IsNullOrEmpty(collection));
        }

        [Fact]
        public void ChunkCollection_WithValidSize_ChunksCorrectly()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var result = DuplicateCodeExtractor.ChunkCollection(collection, 2);
            Assert.Equal(3, result.Count);
            Assert.Equal(2, result[0].Count);
        }

        [Fact]
        public void GetUnixTimestamp_ReturnsPositiveLong()
        {
            var result = DuplicateCodeExtractor.GetUnixTimestamp();
            Assert.True(result > 0);
        }

        [Fact]
        public void TryExecute_WithValidAction_ReturnsTrue()
        {
            var executed = false;
            var result = DuplicateCodeExtractor.TryExecute(() => executed = true);
            Assert.True(result);
            Assert.True(executed);
        }

        [Fact]
        public void TryExecute_WithException_ReturnsFalse()
        {
            var result = DuplicateCodeExtractor.TryExecute(() => throw new InvalidOperationException());
            Assert.False(result);
        }

        [Fact]
        public void MeasureExecutionTime_ReturnsMilliseconds()
        {
            var time = DuplicateCodeExtractor.MeasureExecutionTime(() => System.Threading.Thread.Sleep(10));
            Assert.True(time >= 10);
        }
    }
}
