using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Security;
using RecoveryCommander.Core.Security;

namespace RecoveryCommander.Core
{
    /// <summary>
    /// Security helper methods for input validation and sanitization
    /// Enhanced for ISO 27001/SOC 2 compliance with comprehensive validation
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
                if (fullPath.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase))
                    return false;
                
                if (fullPath.Contains(':', StringComparison.Ordinal))
                {
                    // Allow only the drive letter colon (e.g., C:\)
                    if (fullPath.IndexOf(':', StringComparison.Ordinal) != 1 || fullPath.AsSpan(2).IndexOf(':') != -1)
                        return false;
                }

                // Check for path traversal attempts
                if (path.Contains("..", StringComparison.Ordinal) || path.Contains('~', StringComparison.Ordinal))
                {
                    if (fullPath.Contains("..", StringComparison.Ordinal)) return false;
                }
                
                // Validate the path doesn't contain invalid characters
                var invalidChars = Path.GetInvalidPathChars();
                if (fullPath.IndexOfAny(invalidChars) >= 0)
                    return false;
                
                sanitizedPath = fullPath;
                return true;
            }
            catch (ArgumentException) { return false; }
            catch (PathTooLongException) { return false; }
            catch (NotSupportedException) { return false; }
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
                
                // SSRF protection: Basic static check for loopback and private IPs.
                // Note: Comprehensive protection is handled at the network layer in ServiceContainer.cs
                // to mitigate DNS rebinding, but this provides a fast-fail for literal local URLs.
                if (IsLocalOrPrivateHost(uri.Host))
                    return false;
                
                validUri = uri;
                return true;
            }
            catch (UriFormatException) { return false; }
        }

        private static bool IsLocalOrPrivateHost(string host)
        {
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase)) return true;

            if (System.Net.IPAddress.TryParse(host, out var ip))
            {
                if (System.Net.IPAddress.IsLoopback(ip)) return true;

                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    var bytes = ip.GetAddressBytes();
                    if (bytes[0] == 10) return true; // 10.0.0.0/8
                    if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true; // 172.16.0.0/12
                    if (bytes[0] == 192 && bytes[1] == 168) return true; // 192.168.0.0/16
                    if (bytes[0] == 169 && bytes[1] == 254) return true; // 169.254.0.0/16 (Link-local)
                }
                else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast) return true;
                    // Unique Local Address (fc00::/7)
                    var bytes = ip.GetAddressBytes();
                    if ((bytes[0] & 0xfe) == 0xfc) return true;
                }
            }

            return false;
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
            var dangerousChars = new[] { '&', '|', ';', '`', '$', '(', ')', '<', '>', '\n', '\r', '"', '\'' };
            var sanitized = new string(arguments.Where(c => !dangerousChars.Contains(c)).ToArray());
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
            var escaped = argument.Replace("\"", "\"\"", StringComparison.Ordinal);
            return $"\"{escaped}\"";
        }

        /// <summary>
        /// Escapes an argument for use with ProcessStartInfo.Arguments (Win32 CreateProcess rules)
        /// </summary>
        public static string EscapeProcessArgument(string argument)
        {
            if (string.IsNullOrEmpty(argument)) return "\"\"";

            if (!argument.Contains(' ', StringComparison.Ordinal) && !argument.Contains('\t', StringComparison.Ordinal) && !argument.Contains('"', StringComparison.Ordinal))
                return argument;

            var escaped = new System.Text.StringBuilder();
            escaped.Append('"');
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
            escaped.Append('"');
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
            var name = fileName.Replace("/", "", StringComparison.Ordinal).Replace("\\", "", StringComparison.Ordinal);
            
            // Remove invalid filename characters
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                name = name.Replace(c.ToString(), "", StringComparison.Ordinal);
            }

            // Block reserved Windows filenames (CON, PRN, etc.)
            string[] reservedNames = { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
            string nameWithoutExt = Path.GetFileNameWithoutExtension(name).ToUpperInvariant();
            if (reservedNames.Contains(nameWithoutExt, StringComparer.OrdinalIgnoreCase))
                return false;
            
            // Check for path traversal attempts
            if (name.Contains("..", StringComparison.Ordinal) || name.Contains('~'))
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
            if (string.IsNullOrWhiteSpace(fileName) || allowedExtensions == null || allowedExtensions.Length == 0)
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
                return false;

            // Remove the dot
            extension = extension.Substring(1);

            foreach (var allowed in allowedExtensions)
            {
                if (!string.IsNullOrEmpty(allowed) && extension.Equals(allowed, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Validates input against SQL injection patterns
        /// </summary>
        public static bool ContainsSqlInjection(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var sqlPatterns = new[]
            {
                "' OR '", "' --", "'/*", "*/'", "xp_", "exec(", "execute(",
                "sp_", "select ", "insert ", "update ", "delete ", "drop ",
                "union ", "create ", "alter ", "truncate ", "declare ",
                "--", "/*", "*/", ";--", ";/*", "1=1", "1 = 1"
            };

            var lowerInput = input.ToLowerInvariant();
            foreach (var pattern in sqlPatterns)
            {
                if (lowerInput.Contains(pattern.ToLowerInvariant()))
                {
                    AuditLogger.Instance.LogFailure("InputValidation", "SQLInjectionDetected", $"Pattern: {pattern}", input);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Validates input against XSS patterns
        /// </summary>
        public static bool ContainsXss(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var xssPatterns = new[]
            {
                "<script", "</script>", "javascript:", "onerror=", "onload=",
                "onmouseover=", "onfocus=", "onblur=", "onclick=",
                "ondblclick=", "onmousedown=", "onmouseup=", "onkeydown=",
                "onkeypress=", "onkeyup=", "eval(", "expression(",
                "vbscript:", "data:", "fromCharCode", "document.cookie",
                "document.write", "innerHTML", "outerHTML"
            };

            var lowerInput = input.ToLowerInvariant();
            foreach (var pattern in xssPatterns)
            {
                if (lowerInput.Contains(pattern.ToLowerInvariant()))
                {
                    AuditLogger.Instance.LogFailure("InputValidation", "XSSDetected", $"Pattern: {pattern}", input);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sanitizes input to prevent XSS attacks
        /// </summary>
        public static string SanitizeForXss(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var sanitized = input
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal)
                .Replace("\"", "&quot;", StringComparison.Ordinal)
                .Replace("'", "&#x27;", StringComparison.Ordinal)
                .Replace("/", "&#x2F;", StringComparison.Ordinal);

            return sanitized;
        }

        /// <summary>
        /// Validates email format
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates phone number format (basic validation)
        /// </summary>
        public static bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // Allow digits, spaces, hyphens, parentheses, plus sign
            var cleaned = phone.Replace(" ", "", StringComparison.Ordinal)
                             .Replace("-", "", StringComparison.Ordinal)
                             .Replace("(", "", StringComparison.Ordinal)
                             .Replace(")", "", StringComparison.Ordinal)
                             .Replace("+", "", StringComparison.Ordinal);

            return cleaned.Length >= 10 && cleaned.Length <= 15 && cleaned.All(char.IsDigit);
        }

        /// <summary>
        /// Validates that a string meets minimum entropy requirements
        /// </summary>
        public static bool HasSufficientEntropy(string input, int minimumEntropy = 40)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var uniqueChars = input.Distinct().Count();
            var entropy = input.Length * Math.Log2(uniqueChars);

            return entropy >= minimumEntropy;
        }

        /// <summary>
        /// Validates password strength
        /// </summary>
        public static bool IsStrongPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
                return false;

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

            return hasUpper && hasLower && hasDigit && hasSpecial;
        }

        /// <summary>
        /// Validates that input doesn't exceed maximum length
        /// </summary>
        public static bool IsValidLength(string input, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(input))
                return true;

            return input.Length <= maxLength;
        }

        /// <summary>
        /// Comprehensive input validation for user-provided data
        /// </summary>
        public static bool ValidateUserInput(string input, int maxLength = 1000, bool allowHtml = false)
        {
            if (string.IsNullOrWhiteSpace(input))
                return true;

            // Check length
            if (!IsValidLength(input, maxLength))
            {
                AuditLogger.Instance.LogFailure("InputValidation", "LengthExceeded", $"Length: {input.Length}, Max: {maxLength}");
                return false;
            }

            // Check for SQL injection
            if (ContainsSqlInjection(input))
                return false;

            // Check for XSS unless HTML is explicitly allowed
            if (!allowHtml && ContainsXss(input))
                return false;

            return true;
        }
    }
}



