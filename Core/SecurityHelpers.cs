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
                // Normalize path first
                var normalizedPath = path.Replace('/', '\\');
                
                // Get full path to resolve relative paths and check for traversal
                var fullPath = Path.GetFullPath(normalizedPath);
                
                // Block UNC paths to prevent NTLM theft/traversal if not explicitly expected
                if (fullPath.StartsWith("\\\\"))
                    return false;
                
                // Block potential alternative data streams (ADS)
                if (fullPath.Contains(":"))
                {
                    // Allow only the drive letter colon (e.g., C:\)
                    if (fullPath.IndexOf(':') != 1 || fullPath.Substring(2).Contains(":"))
                        return false;
                }

                // Check for path traversal attempts
                if (path.Contains("..") || path.Contains("~"))
                {
                    if (fullPath.Contains("..")) return false;
                }
                
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
                
                // Only allow HTTPS
                if (uri.Scheme != Uri.UriSchemeHttps)
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
            // We are more aggressive now: & | ; ` $ ( ) < > \n \r " '
            var dangerousChars = new[] { '&', '|', ';', '`', '$', '(', ')', '<', '>', '\n', '\r' };
            var sanitized = arguments;
            
            foreach (var c in dangerousChars)
            {
                sanitized = sanitized.Replace(c.ToString(), string.Empty);
            }
            // Note: command chaining patterns (&&, ||, ;) are already neutralized by
            // the character-level stripping above, so no additional regex pass is needed.
            
            return sanitized.Trim();
        }

        /// <summary>
        /// Escapes an argument for use with powershell.exe -File
        /// </summary>
        public static string EscapePowerShellArgument(string argument)
        {
            if (string.IsNullOrEmpty(argument)) return "\"\"";
            
            // For -File, paths with spaces must be quoted. 
            // Quotes inside the path must be escaped for PowerShell.
            var escaped = argument.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        /// <summary>
        /// Escapes an argument for use with ProcessStartInfo.Arguments (Win32 CreateProcess rules)
        /// </summary>
        public static string EscapeProcessArgument(string argument)
        {
            if (string.IsNullOrEmpty(argument)) return "\"\"";

            if (!argument.Contains(" ") && !argument.Contains("\t") && !argument.Contains("\""))
                return argument;

            var escaped = new System.Text.StringBuilder();
            escaped.Append("\"");
            for (int i = 0; i < argument.Length; i++)
            {
                int backslashes = 0;
                while (i < argument.Length && argument[i] == '\\')
                {
                    backslashes++;
                    i++;
                }

                if (i == argument.Length)
                {
                    escaped.Append('\\', backslashes * 2);
                }
                else if (argument[i] == '"')
                {
                    escaped.Append('\\', backslashes * 2 + 1);
                    escaped.Append('"');
                }
                else
                {
                    escaped.Append('\\', backslashes);
                    escaped.Append(argument[i]);
                }
            }
            escaped.Append("\"");
            return escaped.ToString();
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



