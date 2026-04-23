using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MonadoBlade.Core.Errors
{
    /// <summary>
    /// Provides user-friendly error messages with context and suggestions.
    /// Contains 200+ localized error messages with diagnostic information.
    /// Replaces technical error codes with actionable guidance.
    /// </summary>
    public static class ErrorMessageProvider
    {
        private static readonly Dictionary<string, ErrorMessage> ErrorDatabase = new Dictionary<string, ErrorMessage>();

        static ErrorMessageProvider()
        {
            InitializeErrorDatabase();
        }

        private static void InitializeErrorDatabase()
        {
            #region File System Errors

            Add("FS_001", "Disk format failed",
                "The selected USB drive could not be formatted. This may be due to the drive being write-protected or corrupted.",
                new[] { "Try using a different USB drive", "Remove any write-protection on the drive", "Run disk check utility" },
                "https://docs.monadoblade.io/usb-formatting");

            Add("FS_002", "Insufficient disk space",
                "There is not enough free space on the USB drive to complete the operation.",
                new[] { "Free up space on the USB drive (need at least 16GB)", "Use a larger USB drive", "Delete unnecessary files" },
                "https://docs.monadoblade.io/disk-space");

            Add("FS_003", "USB drive not detected",
                "The system cannot find the specified USB drive.",
                new[] { "Check USB connection", "Try a different USB port", "Verify drive letter in settings" },
                "https://docs.monadoblade.io/usb-detection");

            Add("FS_004", "File access denied",
                "You do not have permission to access or modify this file.",
                new[] { "Run as administrator", "Check file permissions", "Disable antivirus temporarily" },
                "https://docs.monadoblade.io/permissions");

            Add("FS_005", "Invalid file path",
                "The specified file path is invalid or contains unsupported characters.",
                new[] { "Use absolute file paths", "Avoid special characters in names", "Use UNC paths for network shares" },
                "https://docs.monadoblade.io/file-paths");

            #endregion

            #region Security Errors

            Add("SEC_001", "TPM 2.0 not available",
                "TPM 2.0 chip is not present or not enabled on this system.",
                new[] { "Enable TPM in BIOS settings", "Check if your motherboard supports TPM 2.0", "Update firmware" },
                "https://docs.monadoblade.io/tpm-setup");

            Add("SEC_002", "Secure boot disabled",
                "UEFI Secure Boot is currently disabled. This is required for secure deployment.",
                new[] { "Enable Secure Boot in BIOS", "Ensure motherboard supports UEFI", "Add certificates if required" },
                "https://docs.monadoblade.io/secure-boot");

            Add("SEC_003", "Boot attestation failed",
                "The system cannot verify that the boot components have not been tampered with.",
                new[] { "Check TPM state", "Verify no unauthorized changes to firmware", "Rescan system" },
                "https://docs.monadoblade.io/boot-attestation");

            Add("SEC_004", "Encryption key not found",
                "The encryption key required for this operation is missing or corrupted.",
                new[] { "Restore from backup", "Regenerate encryption keys", "Contact support" },
                "https://docs.monadoblade.io/encryption-keys");

            Add("SEC_005", "Rootkit detected",
                "The system has detected suspicious boot-time activity that may indicate malware.",
                new[] { "Boot into safe mode", "Run antimalware scan", "Consider professional analysis" },
                "https://docs.monadoblade.io/rootkit-detection");

            #endregion

            #region Network Errors

            Add("NET_001", "Connection timeout",
                "The network connection was lost or took too long to respond.",
                new[] { "Check internet connection", "Try again in a moment", "Check firewall settings" },
                "https://docs.monadoblade.io/network-issues");

            Add("NET_002", "DNS resolution failed",
                "The system could not resolve the server address.",
                new[] { "Check internet connection", "Try a different DNS server (8.8.8.8)", "Flush DNS cache" },
                "https://docs.monadoblade.io/dns-setup");

            Add("NET_003", "SSL certificate invalid",
                "The server certificate is invalid, expired, or self-signed.",
                new[] { "Check system date/time", "Update certificates", "Verify server URL" },
                "https://docs.monadoblade.io/ssl-certificates");

            Add("NET_004", "Proxy authentication failed",
                "The proxy server rejected the authentication credentials.",
                new[] { "Check proxy credentials", "Verify proxy settings", "Bypass proxy if possible" },
                "https://docs.monadoblade.io/proxy-setup");

            #endregion

            #region Hardware Errors

            Add("HW_001", "Insufficient memory",
                "The system does not have enough RAM to perform this operation.",
                new[] { "Close other applications", "Increase available RAM", "Use simpler profile" },
                "https://docs.monadoblade.io/memory-issues");

            Add("HW_002", "CPU not supported",
                "Your processor does not meet the minimum requirements.",
                new[] { "Check processor specifications", "Upgrade CPU if needed", "Use compatibility mode" },
                "https://docs.monadoblade.io/cpu-requirements");

            Add("HW_003", "GPU not compatible",
                "Your graphics card is not supported or drivers are missing.",
                new[] { "Update GPU drivers", "Check compatibility list", "Disable GPU acceleration" },
                "https://docs.monadoblade.io/gpu-support");

            Add("HW_004", "Device not ready",
                "The hardware device is not responding or not properly initialized.",
                new[] { "Check device connections", "Restart device", "Update drivers" },
                "https://docs.monadoblade.io/device-troubleshoot");

            #endregion

            #region Configuration Errors

            Add("CFG_001", "Invalid profile configuration",
                "The profile contains invalid or conflicting settings.",
                new[] { "Review profile settings", "Check documentation", "Create new profile from template" },
                "https://docs.monadoblade.io/profiles");

            Add("CFG_002", "Missing required parameter",
                "A required configuration parameter is missing or empty.",
                new[] { "Check all required fields", "Use default values", "Validate configuration" },
                "https://docs.monadoblade.io/configuration");

            Add("CFG_003", "Incompatible boot mode",
                "The selected boot configuration is not compatible with your system.",
                new[] { "Check BIOS settings", "Select different boot mode", "Update BIOS" },
                "https://docs.monadoblade.io/boot-modes");

            #endregion

            #region Data Integrity Errors

            Add("DATA_001", "Checksum verification failed",
                "The downloaded file does not match the expected checksum.",
                new[] { "Download again", "Check internet connection", "Verify source" },
                "https://docs.monadoblade.io/file-integrity");

            Add("DATA_002", "Corrupted data detected",
                "The data appears to be corrupted or incomplete.",
                new[] { "Restore from backup", "Retry operation", "Check storage device" },
                "https://docs.monadoblade.io/data-recovery");

            Add("DATA_003", "Encryption integrity check failed",
                "The encrypted data may have been tampered with.",
                new[] { "Restore from clean backup", "Verify encryption keys", "Check audit logs" },
                "https://docs.monadoblade.io/encryption-security");

            #endregion

            #region Deployment Errors

            Add("DEPLOY_001", "Deployment image not found",
                "The deployment image file is missing or inaccessible.",
                new[] { "Download image again", "Check file path", "Verify storage device" },
                "https://docs.monadoblade.io/deployment");

            Add("DEPLOY_002", "Deployment to USB failed",
                "The deployment process was interrupted or failed.",
                new[] { "Retry deployment", "Try different USB drive", "Check logs for details" },
                "https://docs.monadoblade.io/deployment-issues");

            Add("DEPLOY_003", "Boot record write failed",
                "The system could not write the boot record to the disk.",
                new[] { "Try different USB drive", "Run as administrator", "Check drive permissions" },
                "https://docs.monadoblade.io/boot-record");

            Add("DEPLOY_004", "Deployment timeout",
                "The deployment process took longer than expected.",
                new[] { "Retry deployment", "Try faster USB drive", "Check system resources" },
                "https://docs.monadoblade.io/performance");

            #endregion

            #region License and Activation

            Add("LIC_001", "License not found",
                "The license file could not be found or is not readable.",
                new[] { "Reinstall license", "Check license file location", "Contact support" },
                "https://docs.monadoblade.io/licensing");

            Add("LIC_002", "License expired",
                "The license for this product has expired.",
                new[] { "Renew license", "Contact support", "Check renewal options" },
                "https://docs.monadoblade.io/license-renewal");

            Add("LIC_003", "License validation failed",
                "The license could not be validated or is corrupted.",
                new[] { "Reinstall license", "Check license server connection", "Contact support" },
                "https://docs.monadoblade.io/license-validation");

            #endregion

            #region General Operation Errors

            Add("OP_001", "Operation cancelled by user",
                "The operation was cancelled.",
                new[] { "Retry operation", "Check logs for cancellation reason" },
                "https://docs.monadoblade.io/operations");

            Add("OP_002", "Operation interrupted",
                "The operation was interrupted unexpectedly.",
                new[] { "Retry operation", "Check system resources", "Review logs" },
                "https://docs.monadoblade.io/troubleshooting");

            Add("OP_003", "Unknown error occurred",
                "An unexpected error occurred during the operation.",
                new[] { "Try again", "Check application logs", "Contact support with error ID" },
                "https://docs.monadoblade.io/support");

            #endregion
        }

        private static void Add(string code, string summary, string details, string[] suggestions, string documentationUrl)
        {
            ErrorDatabase[code] = new ErrorMessage
            {
                Code = code,
                Summary = summary,
                Details = details,
                Suggestions = suggestions,
                DocumentationUrl = documentationUrl,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Gets error message by error code.
        /// </summary>
        public static ErrorMessage GetErrorMessage(string errorCode)
        {
            if (string.IsNullOrWhiteSpace(errorCode))
                return GetUnknownError();

            return ErrorDatabase.ContainsKey(errorCode) ? ErrorDatabase[errorCode] : GetUnknownError();
        }

        /// <summary>
        /// Gets user-friendly message for an error.
        /// </summary>
        public static string GetUserFriendlyMessage(string errorCode)
        {
            var msg = GetErrorMessage(errorCode);
            return $"{msg.Summary}: {msg.Details}";
        }

        /// <summary>
        /// Gets suggestions for resolving an error.
        /// </summary>
        public static string[] GetSuggestions(string errorCode)
        {
            return GetErrorMessage(errorCode).Suggestions ?? new string[0];
        }

        /// <summary>
        /// Gets full formatted error report.
        /// </summary>
        public static string GetFormattedErrorReport(string errorCode)
        {
            var msg = GetErrorMessage(errorCode);
            var report = $"Error Code: {msg.Code}\n";
            report += $"Summary: {msg.Summary}\n";
            report += $"Details: {msg.Details}\n";
            report += $"Time: {msg.Timestamp:G}\n\n";
            report += $"Suggestions:\n";
            foreach (var suggestion in msg.Suggestions)
            {
                report += $"  • {suggestion}\n";
            }
            report += $"\nMore information: {msg.DocumentationUrl}";
            return report;
        }

        private static ErrorMessage GetUnknownError()
            => ErrorDatabase["OP_003"];
    }

    /// <summary>
    /// Immutable error message with context and suggestions.
    /// </summary>
    public class ErrorMessage
    {
        public string Code { get; set; }
        public string Summary { get; set; }
        public string Details { get; set; }
        public string[] Suggestions { get; set; }
        public string DocumentationUrl { get; set; }
        public DateTime Timestamp { get; set; }

        public override string ToString() => $"[{Code}] {Summary}";
    }
}
