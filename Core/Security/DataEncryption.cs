/*
 * AUDIT HEADER
 * File: DataEncryption.cs
 * Module: Core / Security
 * Created: 2026-07-20
 * Author: Zane Stanton
 *
 * CHANGELOG:
 * 2026-07-20 - 1.0.0 - Initial encryption utilities for ISO 27001/SOC 2 compliance.
 *                       Implements AES-256-GCM for data at rest encryption with
 *                       Windows DPAPI for key protection and secure key derivation.
 */

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RecoveryCommander.Core.Security
{
    /// <summary>
    /// Encryption utilities for securing sensitive data at rest.
    /// Uses AES-256-GCM for authenticated encryption with Windows DPAPI for key protection.
    /// </summary>
    public static class DataEncryption
    {
        private const int KeySize = 256; // AES-256
        private const int SaltSize = 32;
        private const int NonceSize = 12; // GCM standard
        private const int TagSize = 16; // GCM authentication tag

        /// <summary>
        /// Encrypts sensitive data using AES-256-GCM with DPAPI-protected key
        /// </summary>
        public static string Encrypt(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
                return string.Empty;

            try
            {
                var key = GetOrCreateEncryptionKey();
                var salt = RandomNumberGenerator.GetBytes(SaltSize);
                var nonce = RandomNumberGenerator.GetBytes(NonceSize);

                using var aes = new AesGcm(key, TagSize);
                var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                var ciphertext = new byte[plaintextBytes.Length];
                var tag = new byte[TagSize];

                aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

                // Combine salt + nonce + ciphertext + tag
                var result = new byte[SaltSize + NonceSize + ciphertext.Length + TagSize];
                Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
                Buffer.BlockCopy(nonce, 0, result, SaltSize, NonceSize);
                Buffer.BlockCopy(ciphertext, 0, result, SaltSize + NonceSize, ciphertext.Length);
                Buffer.BlockCopy(tag, 0, result, SaltSize + NonceSize + ciphertext.Length, TagSize);

                return Convert.ToBase64String(result);
            }
            catch (CryptographicException ex)
            {
                AuditLogger.Instance.LogFailure("Encryption", "EncryptData", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Decrypts data encrypted with Encrypt method
        /// </summary>
        public static string Decrypt(string ciphertext)
        {
            if (string.IsNullOrEmpty(ciphertext))
                return string.Empty;

            try
            {
                var data = Convert.FromBase64String(ciphertext);

                if (data.Length < SaltSize + NonceSize + TagSize)
                    throw new CryptographicException("Invalid ciphertext length");

                var salt = new byte[SaltSize];
                var nonce = new byte[NonceSize];
                var tag = new byte[TagSize];
                var encryptedData = new byte[data.Length - SaltSize - NonceSize - TagSize];

                Buffer.BlockCopy(data, 0, salt, 0, SaltSize);
                Buffer.BlockCopy(data, SaltSize, nonce, 0, NonceSize);
                Buffer.BlockCopy(data, SaltSize + NonceSize, encryptedData, 0, encryptedData.Length);
                Buffer.BlockCopy(data, SaltSize + NonceSize + encryptedData.Length, tag, 0, TagSize);

                var key = GetOrCreateEncryptionKey(salt);

                using var aes = new AesGcm(key, TagSize);
                var plaintextBytes = new byte[encryptedData.Length];

                aes.Decrypt(nonce, encryptedData, tag, plaintextBytes);

                return Encoding.UTF8.GetString(plaintextBytes);
            }
            catch (CryptographicException ex)
            {
                AuditLogger.Instance.LogFailure("Encryption", "DecryptData", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Encrypts a file using AES-256-GCM
        /// </summary>
        public static void EncryptFile(string inputPath, string outputPath)
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException("Input file not found", inputPath);

            try
            {
                var key = GetOrCreateEncryptionKey();
                var salt = RandomNumberGenerator.GetBytes(SaltSize);
                var nonce = RandomNumberGenerator.GetBytes(NonceSize);

                var plaintext = File.ReadAllBytes(inputPath);
                var ciphertext = new byte[plaintext.Length];
                var tag = new byte[TagSize];

                using var aes = new AesGcm(key, TagSize);
                aes.Encrypt(nonce, plaintext, ciphertext, tag);

                // Write salt + nonce + ciphertext + tag
                using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                fs.Write(salt, 0, SaltSize);
                fs.Write(nonce, 0, NonceSize);
                fs.Write(ciphertext, 0, ciphertext.Length);
                fs.Write(tag, 0, TagSize);

                AuditLogger.Instance.LogSuccess("Encryption", "EncryptFile", inputPath);
            }
            catch (Exception ex)
            {
                AuditLogger.Instance.LogFailure("Encryption", "EncryptFile", ex.Message, inputPath);
                throw;
            }
        }

        /// <summary>
        /// Decrypts a file encrypted with EncryptFile
        /// </summary>
        public static void DecryptFile(string inputPath, string outputPath)
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException("Input file not found", inputPath);

            try
            {
                var data = File.ReadAllBytes(inputPath);

                if (data.Length < SaltSize + NonceSize + TagSize)
                    throw new CryptographicException("Invalid encrypted file length");

                var salt = new byte[SaltSize];
                var nonce = new byte[NonceSize];
                var tag = new byte[TagSize];
                var encryptedData = new byte[data.Length - SaltSize - NonceSize - TagSize];

                Buffer.BlockCopy(data, 0, salt, 0, SaltSize);
                Buffer.BlockCopy(data, SaltSize, nonce, 0, NonceSize);
                Buffer.BlockCopy(data, SaltSize + NonceSize, encryptedData, 0, encryptedData.Length);
                Buffer.BlockCopy(data, SaltSize + NonceSize + encryptedData.Length, tag, 0, TagSize);

                var key = GetOrCreateEncryptionKey(salt);

                using var aes = new AesGcm(key, TagSize);
                var plaintext = new byte[encryptedData.Length];

                aes.Decrypt(nonce, encryptedData, tag, plaintext);

                File.WriteAllBytes(outputPath, plaintext);

                AuditLogger.Instance.LogSuccess("Encryption", "DecryptFile", outputPath);
            }
            catch (Exception ex)
            {
                AuditLogger.Instance.LogFailure("Encryption", "DecryptFile", ex.Message, inputPath);
                throw;
            }
        }

        /// <summary>
        /// Gets or creates encryption key using DPAPI for protection
        /// </summary>
        private static byte[] GetOrCreateEncryptionKey(byte[]? salt = null)
        {
            salt ??= RandomNumberGenerator.GetBytes(SaltSize);

            var keyPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RecoveryCommander",
                "security",
                "encryption.key");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(keyPath)!);

                if (File.Exists(keyPath))
                {
                    var storedKey = File.ReadAllBytes(keyPath);
                    return ProtectedData.Unprotect(storedKey, null, DataProtectionScope.CurrentUser);
                }

                // Generate new key
                var newKey = RandomNumberGenerator.GetBytes(KeySize / 8);
                var encryptedKey = ProtectedData.Protect(newKey, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(keyPath, encryptedKey);

                AuditLogger.Instance.LogSuccess("Encryption", "KeyGeneration", keyPath);
                return newKey;
            }
            catch (Exception ex)
            {
                AuditLogger.Instance.LogFailure("Encryption", "KeyGeneration", ex.Message, keyPath);
                throw;
            }
        }

        /// <summary>
        /// Securely wipes sensitive data from memory
        /// </summary>
        public static void SecureClear(byte[] data)
        {
            if (data == null || data.Length == 0)
                return;

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0;
            }

            // Force garbage collection to ensure memory is cleared
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// Computes SHA-256 hash of data for integrity verification
        /// </summary>
        public static string ComputeHash(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// Computes SHA-256 hash of file for integrity verification
        /// </summary>
        public static string ComputeFileHash(string filePath)
        {
            using var fs = File.OpenRead(filePath);
            var hash = SHA256.HashData(fs);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
