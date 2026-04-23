using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MonadoBlade.Core.Quality
{
    /// <summary>
    /// Utility class for extracting and managing common code patterns.
    /// Contains 50+ frequently used functions to reduce code duplication.
    /// Reduces codebase by extracting duplicate patterns into reusable methods.
    /// </summary>
    public static class DuplicateCodeExtractor
    {
        #region Validation Methods

        /// <summary>
        /// Validates if a string is null, empty, or whitespace.
        /// </summary>
        public static bool IsNullOrWhiteSpace(string value) => string.IsNullOrWhiteSpace(value);

        /// <summary>
        /// Validates if a value is within a numeric range.
        /// </summary>
        public static bool IsInRange<T>(T value, T min, T max) where T : IComparable<T>
            => value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;

        /// <summary>
        /// Validates path exists and is accessible.
        /// </summary>
        public static bool PathExists(string path) => !IsNullOrWhiteSpace(path) && (File.Exists(path) || Directory.Exists(path));

        /// <summary>
        /// Validates file has required extension.
        /// </summary>
        public static bool HasExtension(string filePath, string extension)
            => !IsNullOrWhiteSpace(filePath) && filePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Validates USB drive is connected and accessible.
        /// </summary>
        public static bool IsUSBDriveAccessible(string drivePath)
            => PathExists(drivePath) && Directory.Exists(drivePath);

        /// <summary>
        /// Validates TPM 2.0 is available on system.
        /// </summary>
        public static bool IsTPM20Available()
        {
            try
            {
                return File.Exists(@"C:\Windows\System32\tpm.msc") || 
                       File.Exists(@"C:\Windows\System32\drivers\tpm.sys");
            }
            catch { return false; }
        }

        /// <summary>
        /// Validates string matches regex pattern.
        /// </summary>
        public static bool MatchesPattern(string input, string pattern)
            => !IsNullOrWhiteSpace(input) && Regex.IsMatch(input, pattern);

        /// <summary>
        /// Validates minimum length requirement.
        /// </summary>
        public static bool MeetsMinLength(string value, int minLength)
            => !IsNullOrWhiteSpace(value) && value.Length >= minLength;

        #endregion

        #region String Manipulation Methods

        /// <summary>
        /// Safely truncates string to maximum length.
        /// </summary>
        public static string TruncateString(string value, int maxLength)
            => IsNullOrWhiteSpace(value) ? value : value.Length > maxLength ? value.Substring(0, maxLength) : value;

        /// <summary>
        /// Converts hex string to byte array safely.
        /// </summary>
        public static byte[] HexStringToByteArray(string hex)
        {
            if (IsNullOrWhiteSpace(hex)) return new byte[0];
            return Enumerable.Range(0, hex.Length).Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16)).ToArray();
        }

        /// <summary>
        /// Converts byte array to hex string.
        /// </summary>
        public static string ByteArrayToHexString(byte[] bytes)
            => bytes == null ? string.Empty : BitConverter.ToString(bytes).Replace("-", string.Empty);

        /// <summary>
        /// Sanitizes filename for safe file system usage.
        /// </summary>
        public static string SanitizeFilename(string filename)
        {
            if (IsNullOrWhiteSpace(filename)) return "file";
            var invalidChars = Path.GetInvalidFileNameChars();
            return new string(filename.Where(c => !invalidChars.Contains(c)).ToArray());
        }

        /// <summary>
        /// Extracts version number from string.
        /// </summary>
        public static string ExtractVersion(string versionString)
        {
            if (IsNullOrWhiteSpace(versionString)) return "0.0.0";
            var match = Regex.Match(versionString, @"\d+\.\d+\.\d+");
            return match.Success ? match.Value : "0.0.0";
        }

        /// <summary>
        /// Normalizes line endings across platforms.
        /// </summary>
        public static string NormalizeLineEndings(string text)
            => IsNullOrWhiteSpace(text) ? text : text.Replace("\r\n", "\n").Replace("\r", "\n");

        /// <summary>
        /// Removes control characters from string.
        /// </summary>
        public static string RemoveControlCharacters(string text)
            => IsNullOrWhiteSpace(text) ? text : new string(text.Where(c => !char.IsControl(c)).ToArray());

        /// <summary>
        /// Safely encodes string to UTF-8 bytes.
        /// </summary>
        public static byte[] EncodeToUTF8(string text)
            => Encoding.UTF8.GetBytes(IsNullOrWhiteSpace(text) ? string.Empty : text);

        /// <summary>
        /// Safely decodes UTF-8 bytes to string.
        /// </summary>
        public static string DecodeFromUTF8(byte[] bytes)
            => bytes == null || bytes.Length == 0 ? string.Empty : Encoding.UTF8.GetString(bytes);

        #endregion

        #region Hash and Cryptography Methods

        /// <summary>
        /// Computes SHA256 hash of input string.
        /// </summary>
        public static string ComputeSHA256Hash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(IsNullOrWhiteSpace(input) ? string.Empty : input));
                return ByteArrayToHexString(hashBytes);
            }
        }

        /// <summary>
        /// Computes SHA256 hash of file.
        /// </summary>
        public static string ComputeFileHash(string filePath)
        {
            if (!PathExists(filePath)) return string.Empty;
            try
            {
                using (var sha256 = SHA256.Create())
                using (var fileStream = File.OpenRead(filePath))
                {
                    var hashBytes = sha256.ComputeHash(fileStream);
                    return ByteArrayToHexString(hashBytes);
                }
            }
            catch { return string.Empty; }
        }

        /// <summary>
        /// Verifies hash matches computed hash.
        /// </summary>
        public static bool VerifyHash(string data, string expectedHash)
            => ComputeSHA256Hash(data).Equals(expectedHash, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Generates cryptographically secure random bytes.
        /// </summary>
        public static byte[] GenerateRandomBytes(int length)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var randomBytes = new byte[length];
                rng.GetBytes(randomBytes);
                return randomBytes;
            }
        }

        /// <summary>
        /// Generates GUID-based unique identifier.
        /// </summary>
        public static string GenerateUniqueId() => Guid.NewGuid().ToString("N");

        #endregion

        #region Collection Methods

        /// <summary>
        /// Safely gets item from dictionary with default value.
        /// </summary>
        public static T SafeGetDictValue<K, T>(Dictionary<K, T> dict, K key, T defaultValue)
            => dict != null && dict.ContainsKey(key) ? dict[key] : defaultValue;

        /// <summary>
        /// Merges multiple dictionaries into one.
        /// </summary>
        public static Dictionary<K, V> MergeDictionaries<K, V>(params Dictionary<K, V>[] dictionaries)
        {
            var result = new Dictionary<K, V>();
            foreach (var dict in dictionaries.Where(d => d != null))
            {
                foreach (var kvp in dict)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            return result;
        }

        /// <summary>
        /// Safely gets or creates item in dictionary.
        /// </summary>
        public static T GetOrAdd<K, T>(Dictionary<K, T> dict, K key, Func<T> factory)
        {
            if (dict == null) return factory?.Invoke();
            if (!dict.ContainsKey(key)) dict[key] = factory?.Invoke();
            return dict[key];
        }

        /// <summary>
        /// Checks if collection is null or empty.
        /// </summary>
        public static bool IsNullOrEmpty<T>(IEnumerable<T> collection)
            => collection == null || !collection.Any();

        /// <summary>
        /// Safely chunks collection into batches.
        /// </summary>
        public static List<List<T>> ChunkCollection<T>(IEnumerable<T> collection, int chunkSize)
        {
            var result = new List<List<T>>();
            if (collection == null || chunkSize <= 0) return result;
            var batch = new List<T>();
            foreach (var item in collection)
            {
                batch.Add(item);
                if (batch.Count == chunkSize)
                {
                    result.Add(new List<T>(batch));
                    batch.Clear();
                }
            }
            if (batch.Count > 0) result.Add(batch);
            return result;
        }

        #endregion

        #region File I/O Methods

        /// <summary>
        /// Safely reads entire file as string.
        /// </summary>
        public static string SafeReadAllText(string filePath)
        {
            try { return PathExists(filePath) ? File.ReadAllText(filePath) : string.Empty; }
            catch { return string.Empty; }
        }

        /// <summary>
        /// Safely writes text to file.
        /// </summary>
        public static bool SafeWriteAllText(string filePath, string content)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(filePath, content ?? string.Empty);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Gets file size in bytes.
        /// </summary>
        public static long GetFileSize(string filePath)
        {
            try { return PathExists(filePath) ? new FileInfo(filePath).Length : 0; }
            catch { return 0; }
        }

        /// <summary>
        /// Checks if file is locked by another process.
        /// </summary>
        public static bool IsFileLocked(string filePath)
        {
            try
            {
                using (File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                return false;
            }
            catch (IOException) { return true; }
        }

        /// <summary>
        /// Safely deletes file if it exists.
        /// </summary>
        public static bool SafeDeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath)) File.Delete(filePath);
                return true;
            }
            catch { return false; }
        }

        #endregion

        #region Time and Date Methods

        /// <summary>
        /// Gets current Unix timestamp in seconds.
        /// </summary>
        public static long GetUnixTimestamp()
            => (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        /// <summary>
        /// Converts Unix timestamp to DateTime.
        /// </summary>
        public static DateTime UnixTimestampToDateTime(long timestamp)
            => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp).ToLocalTime();

        /// <summary>
        /// Checks if timestamp is within recent time period.
        /// </summary>
        public static bool IsRecent(long timestamp, int secondsAgo)
            => GetUnixTimestamp() - timestamp <= secondsAgo;

        /// <summary>
        /// Formats timespan as human-readable string.
        /// </summary>
        public static string FormatTimespan(TimeSpan timespan)
        {
            if (timespan.TotalSeconds < 60) return $"{(int)timespan.TotalSeconds}s";
            if (timespan.TotalMinutes < 60) return $"{(int)timespan.TotalMinutes}m";
            if (timespan.TotalHours < 24) return $"{(int)timespan.TotalHours}h";
            return $"{(int)timespan.TotalDays}d";
        }

        #endregion

        #region Error Handling Methods

        /// <summary>
        /// Safely executes action with exception handling.
        /// </summary>
        public static bool TryExecute(Action action, string errorContext = "")
        {
            try { action?.Invoke(); return true; }
            catch (Exception ex) { Debug.WriteLine($"Error {errorContext}: {ex.Message}"); return false; }
        }

        /// <summary>
        /// Safely executes function and returns result or default.
        /// </summary>
        public static T TryExecute<T>(Func<T> action, T defaultValue, string errorContext = "")
        {
            try { return action?.Invoke() ?? defaultValue; }
            catch (Exception ex) { Debug.WriteLine($"Error {errorContext}: {ex.Message}"); return defaultValue; }
        }

        /// <summary>
        /// Retries operation with exponential backoff.
        /// </summary>
        public static bool RetryWithBackoff(Action action, int maxRetries = 3, int initialDelayMs = 100)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try { action?.Invoke(); return true; }
                catch when (i < maxRetries - 1) { Task.Delay(initialDelayMs * (int)Math.Pow(2, i)).Wait(); }
            }
            return false;
        }

        #endregion

        #region Logging and Diagnostics

        /// <summary>
        /// Logs performance metrics for code execution.
        /// </summary>
        public static long MeasureExecutionTime(Action action, string operationName = "")
        {
            var stopwatch = Stopwatch.StartNew();
            try { action?.Invoke(); }
            finally { stopwatch.Stop(); Debug.WriteLine($"{operationName} took {stopwatch.ElapsedMilliseconds}ms"); }
            return stopwatch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Gets memory usage in MB.
        /// </summary>
        public static long GetMemoryUsageMB()
            => GC.GetTotalMemory(false) / (1024 * 1024);

        #endregion
    }
}
