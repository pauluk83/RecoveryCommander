using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Security;

namespace RecoveryCommander.Core
{
    /// <summary>
    /// Security helper methods for input validation and sanitization
    /// </summary>
    public static class SecurityHelpers
    {
        /// <summary>
        /// Validates and sanitizes a file path to prevent path traversal attacks
        /// </summary>
        public static bool IsValidFilePath(string path, out string sanitizedPath)
        {
            sanitizedPath = string.Empty;
            
            if (string.IsNullOrWhiteSpace(path))
                return false;
            
            try
            {
                // Get full path to resolve relative paths and check for traversal
                var fullPath = Path.GetFullPath(path);
                
                // Check if the path is within an allowed directory (best practice)
                // For this app, we generally allow most paths but we should block relative traversal
                if (fullPath.Contains("..") || fullPath.Contains("~"))
                    return false;
                
                // Blocks access to sensitive system paths if needed
                // string[] blockedPaths = { "C:\\Windows\\System32\\config", "C:\\Users\\Default" };
                // if (blockedPaths.Any(bp => fullPath.StartsWith(bp, StringComparison.OrdinalIgnoreCase))) return false;

                // Validate the path doesn't contain invalid characters
                var invalidChars = Path.GetInvalidPathChars();
                if (fullPath.IndexOfAny(invalidChars) >= 0)
                    return false;
                
                sanitizedPath = fullPath;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        /// <summary>
        /// Validates a URL to ensure it's safe to download from
        /// </summary>
        public static bool IsValidDownloadUrl(string url, out Uri? validUri)
        {
            validUri = null;
            
            if (string.IsNullOrWhiteSpace(url))
                return false;
            
            try
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    return false;
                
                // Only allow HTTP/HTTPS
                if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                    return false;
                
                // Block localhost and private IP ranges (including IPv6)
                string host = uri.Host.ToLowerInvariant();
                if (host == "localhost" || 
                    host == "127.0.0.1" || 
                    host == "[::1]" ||
                    host.StartsWith("192.168.") ||
                    host.StartsWith("10.") ||
                    host.StartsWith("169.254.") || // Link-local
                    Regex.IsMatch(host, @"^172\.(1[6-9]|2[0-9]|3[01])\.")) // 172.16.0.0/12
                    return false;
                
                validUri = uri;
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Sanitizes command arguments to prevent command injection
        /// </summary>
        public static string SanitizeCommandArguments(string arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
                return string.Empty;
            
            // Remove potentially dangerous characters that could be used for injection
            var dangerousChars = new[] { '&', '|', ';', '`', '$', '(', ')', '<', '>', '\n', '\r' };
            var sanitized = arguments;
            
            foreach (var c in dangerousChars)
            {
                sanitized = sanitized.Replace(c.ToString(), string.Empty);
            }
            
            // Remove command chaining patterns specifically
            sanitized = Regex.Replace(sanitized, @"\s*&&\s*", " ", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"\s*\|\|\s*", " ", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"\s*;\s*", " ", RegexOptions.IgnoreCase);
            
            return sanitized.Trim();
        }
        
        /// <summary>
        /// Validates a file name to prevent directory traversal and invalid characters
        /// </summary>
        public static bool IsValidFileName(string fileName, out string sanitizedFileName)
        {
            sanitizedFileName = string.Empty;
            
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            // Remove path separators to ensure it's just a filename
            var name = fileName.Replace("/", "").Replace("\\", "");
            
            // Remove invalid filename characters
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                name = name.Replace(c.ToString(), "");
            }

            // Block reserved Windows filenames (CON, PRN, etc.)
            string[] reservedNames = { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
            string nameWithoutExt = Path.GetFileNameWithoutExtension(name).ToUpperInvariant();
            if (reservedNames.Contains(nameWithoutExt))
                return false;
            
            // Check for path traversal attempts
            if (name.Contains("..") || name.Contains("~"))
                return false;
            
            if (string.IsNullOrWhiteSpace(name))
                return false;
            
            sanitizedFileName = name;
            return true;
        }
        
        /// <summary>
        /// Validates that a file extension is allowed
        /// </summary>
        public static bool IsAllowedFileExtension(string fileName, string[] allowedExtensions)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;
            
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
                return false;
            
            // Remove the dot
            extension = extension.Substring(1);
            
            foreach (var allowed in allowedExtensions)
            {
                if (extension.Equals(allowed, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            
            return false;
        }
    }
}



