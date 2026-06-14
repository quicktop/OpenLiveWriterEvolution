// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace OpenLiveWriter.CoreServices
{
    public class CryptHelper
    {
        // Portable encrypted blob layout: [16 IV][32 HMAC-SHA256(IV+ciphertext)][N ciphertext]
        private const int IV_LENGTH = 16;
        private const int HMAC_LENGTH = 32;
        private const int KEY_FILE_LENGTH = 64; // 32 AES-256 + 32 HMAC-SHA256

        public static byte[] Encrypt(string str)
        {
            if (ApplicationEnvironment.IsPortableMode)
            {
                byte[] keyMaterial = GetOrCreatePortableKey();
                byte[] aesKey  = new byte[32];
                byte[] hmacKey = new byte[32];
                Buffer.BlockCopy(keyMaterial, 0,  aesKey,  0, 32);
                Buffer.BlockCopy(keyMaterial, 32, hmacKey, 0, 32);

                byte[] plaintext = new UnicodeEncoding(false, false).GetBytes(str);
                try
                {
                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = aesKey;
                        aes.GenerateIV();
                        byte[] iv = aes.IV;

                        byte[] ciphertext;
                        using (ICryptoTransform encryptor = aes.CreateEncryptor())
                            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

                        // Encrypt-then-MAC: HMAC covers IV + ciphertext
                        byte[] hmac = ComputeHmac(hmacKey, iv, ciphertext);

                        byte[] result = new byte[IV_LENGTH + HMAC_LENGTH + ciphertext.Length];
                        Buffer.BlockCopy(iv,         0, result, 0,                          IV_LENGTH);
                        Buffer.BlockCopy(hmac,       0, result, IV_LENGTH,                  HMAC_LENGTH);
                        Buffer.BlockCopy(ciphertext, 0, result, IV_LENGTH + HMAC_LENGTH,    ciphertext.Length);
                        return result;
                    }
                }
                finally
                {
                    ZeroFill(plaintext);
                    ZeroFill(aesKey);
                    ZeroFill(hmacKey);
                    ZeroFill(keyMaterial);
                }
            }
            else
            {
                byte[] bytes = null;
                byte[] bytesPlusNull = null;
                try
                {
                    bytes = new UnicodeEncoding(false, false).GetBytes(str);
                    bytesPlusNull = new byte[bytes.Length + 2];
                    Array.Copy(bytes, bytesPlusNull, bytes.Length);
                    byte[] encrypted = ProtectedData.Protect(bytesPlusNull, null, DataProtectionScope.CurrentUser);
                    return encrypted;
                }
                finally
                {
                    ZeroFill(bytes);
                    ZeroFill(bytesPlusNull);
                }
            }
        }

        public static string Decrypt(byte[] val)
        {
            if (ApplicationEnvironment.IsPortableMode)
            {
                if (val.Length <= IV_LENGTH + HMAC_LENGTH)
                    throw new ArgumentException("Encrypted value is too short");

                byte[] keyMaterial = GetOrCreatePortableKey();
                byte[] aesKey  = new byte[32];
                byte[] hmacKey = new byte[32];
                Buffer.BlockCopy(keyMaterial, 0,  aesKey,  0, 32);
                Buffer.BlockCopy(keyMaterial, 32, hmacKey, 0, 32);

                byte[] clearBytes = null;
                try
                {
                    byte[] iv         = new byte[IV_LENGTH];
                    byte[] storedHmac = new byte[HMAC_LENGTH];
                    int ciphertextLength = val.Length - IV_LENGTH - HMAC_LENGTH;
                    byte[] ciphertext = new byte[ciphertextLength];

                    Buffer.BlockCopy(val, 0,                       iv,         0, IV_LENGTH);
                    Buffer.BlockCopy(val, IV_LENGTH,                storedHmac, 0, HMAC_LENGTH);
                    Buffer.BlockCopy(val, IV_LENGTH + HMAC_LENGTH,  ciphertext, 0, ciphertextLength);

                    // Verify HMAC before decrypting to detect tampering
                    byte[] expectedHmac = ComputeHmac(hmacKey, iv, ciphertext);
                    if (!ConstantTimeEquals(storedHmac, expectedHmac))
                        throw new CryptographicException("Portable settings integrity check failed: data may have been tampered with.");

                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = aesKey;
                        aes.IV  = iv;
                        using (ICryptoTransform decryptor = aes.CreateDecryptor())
                        {
                            clearBytes = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                            return Encoding.Unicode.GetString(clearBytes);
                        }
                    }
                }
                finally
                {
                    ZeroFill(clearBytes);
                    ZeroFill(aesKey);
                    ZeroFill(hmacKey);
                    ZeroFill(keyMaterial);
                }
            }
            else
            {
                byte[] clearBytes = null;
                try
                {
                    clearBytes = ProtectedData.Unprotect(val, null, DataProtectionScope.CurrentUser);
                    if (clearBytes.Length < 2
                        || (clearBytes.Length % 2) != 0
                        || clearBytes[clearBytes.Length - 1] != 0
                        || clearBytes[clearBytes.Length - 2] != 0)
                    {
                        throw new ArgumentException("Value could not be decrypted");
                    }

                    return Encoding.Unicode.GetString(clearBytes, 0, clearBytes.Length - 2);
                }
                finally
                {
                    ZeroFill(clearBytes);
                }
            }
        }

        /// <summary>
        /// Returns the 64-byte key material for portable mode.
        /// First 32 bytes = AES-256 key, last 32 bytes = HMAC-SHA256 key.
        /// Creates and saves the key file on first call.
        /// NOTE: Security is bounded by physical access to the UserData folder.
        /// Anyone with access to UserData also has the key file. This provides
        /// obfuscation against casual inspection, not protection against a
        /// determined attacker who has access to the portable drive.
        /// </summary>
        private static byte[] GetOrCreatePortableKey()
        {
            string keyPath = Path.Combine(ApplicationEnvironment.InstallationDirectory, "UserData", "encryption.key");
            if (File.Exists(keyPath))
            {
                byte[] stored = File.ReadAllBytes(keyPath);
                if (stored.Length == KEY_FILE_LENGTH)
                    return stored;
            }

            // First run: generate 64 bytes of cryptographically random key material
            byte[] keyMaterial = new byte[KEY_FILE_LENGTH];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(keyMaterial);
            File.WriteAllBytes(keyPath, keyMaterial);
            return keyMaterial;
        }

        private static byte[] ComputeHmac(byte[] hmacKey, byte[] iv, byte[] ciphertext)
        {
            using (var hmac = new HMACSHA256(hmacKey))
            {
                hmac.TransformBlock(iv, 0, iv.Length, null, 0);
                hmac.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                return hmac.Hash;
            }
        }

        // Constant-time comparison to prevent timing attacks on HMAC verification
        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static void ZeroFill(byte[] buffer)
        {
            if (buffer == null)
                return;
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = 0;
        }
    }
}
