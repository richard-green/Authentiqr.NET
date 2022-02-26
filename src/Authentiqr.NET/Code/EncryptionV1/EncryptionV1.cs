using Authentiqr.Core;
using Authentiqr.Core.Encode;
using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Authentiqr.NET.Code.EncryptionV1
{
    public class EncryptionV1
    {
        private const string CryptoKey = "496f1a09cce8b429725ceca93d29adaad4bb97bc0870dda39d42c78be8c74f65";
        private const string CryptoIV = "4e3eb5fa6d3c7146d5332a2f58867c95";
        private const string FixedSalt = "c0NjU3MDlmNzRjMjQwYg";
        private const int HashRounds = 300000;

        private static HashAlgorithm Hasher { get; set; }

        /// <summary>
        /// Static constructor
        /// </summary>
        static EncryptionV1()
        {
            Hasher = SHA256.Create();
        }

        private static SymmetricAlgorithm GetCryptoAlgorithm()
        {
            var algorithm = Aes.Create();
            algorithm.Key = Hex.Decode(CryptoKey);
            algorithm.IV = Hex.Decode(CryptoIV);
            return algorithm;
        }

        /// <summary>
        /// Generate a crypto algorithm, using a derived Key and IV. Key and IV will be hashed before usage.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <returns></returns>
        public static SymmetricAlgorithm CreateCryptoAlgorithm(string key, string iv)
        {
            var algorithm = Aes.Create();
            var keyHash = Hash(key);
            var ivHash = Hash(iv);
            algorithm.Key = keyHash;
            algorithm.IV = ivHash.Take(algorithm.BlockSize / 8).ToArray();
            return algorithm;
        }

        /// <summary>
        /// Generate a crypto algorithm, using a derived Key and IV. Key and IV will be hashed before usage.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <returns></returns>
        public static SymmetricAlgorithm CreateCryptoAlgorithm(SecureString key, SecureString iv)
        {
            var algorithm = Aes.Create();
            var keyHash = Hash(key);
            var ivHash = Hash(iv);
            algorithm.Key = keyHash;
            algorithm.IV = ivHash.Take(algorithm.BlockSize / 8).ToArray();
            return algorithm;
        }

        public static byte[] Encrypt(string plainText, SymmetricAlgorithm algorithm)
        {
            return Encrypt(Encoding.UTF8.GetBytes(plainText), algorithm);
        }

        public static byte[] Encrypt(byte[] data, SymmetricAlgorithm algorithm)
        {
            // Check arguments
            if (data == null || data.Length <= 0)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (algorithm == null)
            {
                throw new ArgumentNullException(nameof(algorithm));
            }
            if (algorithm.Key == null || algorithm.Key.Length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(algorithm), "algorithm.Key is null or empty");
            }
            if (algorithm.IV == null || algorithm.IV.Length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(algorithm), "algorithm.IV is null or empty");
            }

            byte[] encryptedData = null;

            try
            {
                // Create a decryptor to perform the stream transform
                ICryptoTransform encryptor = algorithm.CreateEncryptor();

                // Create the streams used for encryption
                using var msEncrypt = new MemoryStream();
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    // Write all data to the stream
                    csEncrypt.Write(data, 0, data.Length);
                }

                encryptedData = msEncrypt.ToArray();
            }
            catch (Exception)
            {
                encryptedData = null;
            }
            finally
            {
                algorithm.Clear();
            }

            // Return the encrypted bytes from the memory stream
            return encryptedData;
        }

        public static string Decrypt(byte[] cipherText, SymmetricAlgorithm algorithm)
        {
            // Check arguments
            if (cipherText == null || cipherText.Length <= 0)
            {
                throw new ArgumentNullException(nameof(cipherText));
            }
            if (algorithm == null)
            {
                throw new ArgumentNullException(nameof(algorithm));
            }
            if (algorithm.Key == null || algorithm.Key.Length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(algorithm), "algorithm.Key is null or empty");
            }
            if (algorithm.IV == null || algorithm.IV.Length <= 0)
            {
                throw new ArgumentNullException(nameof(algorithm), "algorithm.IV is null or empty");
            }

            string plaintext = null;

            try
            {
                // Create a decrytor to perform the stream transform
                ICryptoTransform decryptor = algorithm.CreateDecryptor();

                // Create the streams used for decryption
                using var msDecrypt = new MemoryStream(cipherText);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);
                // Read the decrypted bytes from the decrypting stream and place them in a string
                plaintext = srDecrypt.ReadToEnd();
            }
            finally
            {
                algorithm.Clear();
            }

            return plaintext;
        }

        public static byte[] Hash(string data)
        {
            return Hasher.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        public static byte[] Hash(SecureString data)
        {
            return data.Use(s =>
            {
                return Hasher.ComputeHash(Encoding.UTF8.GetBytes(s));
            });
        }

        public static byte[] Hash(byte[] data)
        {
            return Hasher.ComputeHash(data);
        }

        public static string GeneratePasswordHash(string salt, string password)
        {
            byte[] hash = Hash(salt + FixedSalt + Base64.Encode(Encrypt(password, GetCryptoAlgorithm())));

            for (int i = 1; i < HashRounds; i++)
            {
                hash = Hash(hash);
            }

            return Base64.Encode(hash);
        }

        public static SecureString GeneratePasswordHash(SecureString salt, SecureString password)
        {
            return salt.Use(s =>
            {
                return password.Use(p =>
                {
                    return new SecureString().AppendChars(GeneratePasswordHash(s, p));
                });
            });
        }
    }
}
