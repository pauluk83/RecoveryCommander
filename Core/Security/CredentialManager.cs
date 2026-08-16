/*
 * AUDIT HEADER
 * File: CredentialManager.cs
 * Module: Core / Security
 * Created: 2026-07-20
 * Author: Zane Stanton
 *
 * CHANGELOG:
 * 2026-07-20 - 1.0.0 - Initial credential manager for ISO 27001/SOC 2 compliance.
 *                       Implements secure credential storage using Windows Credential Manager
 *                       with encryption and audit logging for all credential operations.
 */

using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace RecoveryCommander.Core.Security
{
    /// <summary>
    /// Secure credential manager using Windows Credential Manager (CredMgr).
    /// Provides encrypted storage for sensitive credentials with full audit trail.
    /// </summary>
    public static class CredentialManager
    {
        private const string TargetPrefix = "RecoveryCommander_";

        /// <summary>
        /// Stores a credential securely in Windows Credential Manager
        /// </summary>
        public static void StoreCredential(string credentialName, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(credentialName))
                throw new ArgumentException("Credential name cannot be empty", nameof(credentialName));

            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be empty", nameof(username));

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty", nameof(password));

            try
            {
                var targetName = $"{TargetPrefix}{credentialName}";

                var credential = new CREDENTIAL
                {
                    Type = (int)CRED_TYPE.GENERIC,
                    TargetName = targetName,
                    UserName = username,
                    CredentialBlobSize = password.Length * 2,
                    Persist = (int)CRED_PERSIST.LOCAL_MACHINE
                };

                // Allocate unmanaged memory for credential blob
                var passwordBytes = Encoding.Unicode.GetBytes(password);
                credential.CredentialBlob = Marshal.AllocHGlobal(passwordBytes.Length);
                Marshal.Copy(passwordBytes, 0, credential.CredentialBlob, passwordBytes.Length);

                try
                {
                    if (!CredWrite(ref credential, 0))
                    {
                        var error = Marshal.GetLastWin32Error();
                        throw new System.ComponentModel.Win32Exception(error, "Failed to store credential");
                    }

                    AuditLogger.Instance.LogSuccess("CredentialManager", "StoreCredential", targetName);
                }
                finally
                {
                    Marshal.FreeHGlobal(credential.CredentialBlob);
                }
            }
            catch (Exception ex)
            {
                AuditLogger.Instance.LogFailure("CredentialManager", "StoreCredential", ex.Message, credentialName);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a credential from Windows Credential Manager
        /// </summary>
        public static Credential? GetCredential(string credentialName)
        {
            if (string.IsNullOrWhiteSpace(credentialName))
                throw new ArgumentException("Credential name cannot be empty", nameof(credentialName));

            try
            {
                var targetName = $"{TargetPrefix}{credentialName}";

                if (!CredRead(targetName, (int)CRED_TYPE.GENERIC, 0, out var credentialPtr))
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error == 1168) // ERROR_NOT_FOUND
                    {
                        AuditLogger.Instance.LogFailure("CredentialManager", "GetCredential", "Credential not found", credentialName);
                        return null;
                    }
                    throw new System.ComponentModel.Win32Exception(error, "Failed to read credential");
                }

                try
                {
                    var credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);
                    
                    // Read credential blob from unmanaged memory
                    var passwordBytes = new byte[credential.CredentialBlobSize];
                    Marshal.Copy(credential.CredentialBlob, passwordBytes, 0, credential.CredentialBlobSize);
                    var password = Encoding.Unicode.GetString(passwordBytes);

                    AuditLogger.Instance.LogSuccess("CredentialManager", "GetCredential", targetName);

                    return new Credential
                    {
                        Username = credential.UserName,
                        Password = password,
                        TargetName = credential.TargetName
                    };
                }
                finally
                {
                    CredFree(credentialPtr);
                }
            }
            catch (Exception ex)
            {
                AuditLogger.Instance.LogFailure("CredentialManager", "GetCredential", ex.Message, credentialName);
                throw;
            }
        }

        /// <summary>
        /// Deletes a credential from Windows Credential Manager
        /// </summary>
        public static bool DeleteCredential(string credentialName)
        {
            if (string.IsNullOrWhiteSpace(credentialName))
                throw new ArgumentException("Credential name cannot be empty", nameof(credentialName));

            try
            {
                var targetName = $"{TargetPrefix}{credentialName}";

                if (!CredDelete(targetName, (int)CRED_TYPE.GENERIC, 0))
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error == 1168) // ERROR_NOT_FOUND
                    {
                        AuditLogger.Instance.LogFailure("CredentialManager", "DeleteCredential", "Credential not found", credentialName);
                        return false;
                    }
                    throw new System.ComponentModel.Win32Exception(error, "Failed to delete credential");
                }

                AuditLogger.Instance.LogSuccess("CredentialManager", "DeleteCredential", targetName);
                return true;
            }
            catch (Exception ex)
            {
                AuditLogger.Instance.LogFailure("CredentialManager", "DeleteCredential", ex.Message, credentialName);
                throw;
            }
        }

        /// <summary>
        /// Lists all credentials stored by RecoveryCommander
        /// </summary>
        public static string[] ListCredentials()
        {
            try
            {
                if (!CredEnumerate(null, 0, out var count, out var credentials))
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error == 1168) // ERROR_NOT_FOUND
                    {
                        return Array.Empty<string>();
                    }
                    throw new System.ComponentModel.Win32Exception(error, "Failed to enumerate credentials");
                }

                try
                {
                    var result = new string[count];
                    var credentialPtrs = new IntPtr[count];
                    
                    // Copy the array of pointers from unmanaged memory
                    Marshal.Copy(credentials, credentialPtrs, 0, count);
                    
                    for (int i = 0; i < count; i++)
                    {
                        var credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtrs[i]);
                        if (credential.TargetName.StartsWith(TargetPrefix, StringComparison.Ordinal))
                        {
                            result[i] = credential.TargetName.Substring(TargetPrefix.Length);
                        }
                    }

                    AuditLogger.Instance.LogSuccess("CredentialManager", "ListCredentials", $"Found {count} credentials");
                    return result;
                }
                finally
                {
                    CredFree(credentials);
                }
            }
            catch (Exception ex)
            {
                AuditLogger.Instance.LogFailure("CredentialManager", "ListCredentials", ex.Message);
                throw;
            }
        }

        #region Windows Credential Manager P/Invoke

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL
        {
            public int Flags;
            public int Type;
            public string TargetName;
            public string Comment;
            public long LastWritten;
            public int CredentialBlobSize;
            public IntPtr CredentialBlob;
            public int Persist;
            public int AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }

        private enum CRED_TYPE
        {
            GENERIC = 1,
            DOMAIN_PASSWORD = 2,
            DOMAIN_CERTIFICATE = 3,
            DOMAIN_VISIBLE_PASSWORD = 4,
            GENERIC_CERTIFICATE = 5,
            DOMAIN_EXTENDED = 6,
            MAXIMUM = 7,
            MAXIMUM_EX = MAXIMUM + 1000
        }

        private enum CRED_PERSIST
        {
            SESSION = 1,
            LOCAL_MACHINE = 2,
            ENTERPRISE = 3
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredWrite([In] ref CREDENTIAL credential, uint flags);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredRead(string targetName, int type, uint flags, out IntPtr credential);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredDelete(string targetName, int type, uint flags);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredEnumerate(string filter, uint flags, out int count, out IntPtr credentials);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CredFree(IntPtr credential);

        #endregion
    }

    /// <summary>
    /// Represents a stored credential
    /// </summary>
    public sealed class Credential
    {
        public string Username { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string TargetName { get; init; } = string.Empty;

        /// <summary>
        /// Securely clears the password from memory
        /// </summary>
        public void SecureClear()
        {
            if (!string.IsNullOrEmpty(Password))
            {
                var passwordBytes = Encoding.Unicode.GetBytes(Password);
                DataEncryption.SecureClear(passwordBytes);
            }
        }
    }
}
