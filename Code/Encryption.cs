using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Authentiqr.NET.Code
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
            return new AesManaged()
            {
                Key = Hex.Decode(CryptoKey),
                IV = Hex.Decode(CryptoIV)
            };
        }

        /// <summary>
        /// Generate a crypto algorithm, using a derived Key and IV. Key and IV will be hashed before usage.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <returns></returns>
        public static SymmetricAlgorithm CreateCryptoAlgorithm(string key, string iv)
        {
            AesManaged algorithm = new AesManaged();
            byte[] keyHash = Hash(key);
            byte[] ivHash = Hash(iv);
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
                throw new ArgumentNullException("data");
            }
            if (algorithm == null)
            {
                throw new ArgumentNullException("algorithm");
            }
            if (algorithm.Key == null || algorithm.Key.Length <= 0)
            {
                throw new ArgumentNullException("algorithm.Key");
            }
            if (algorithm.IV == null || algorithm.IV.Length <= 0)
            {
                throw new ArgumentNullException("algorithm.IV");
            }

            byte[] encryptedData = null;

            try
            {
                // Create a decryptor to perform the stream transform
                ICryptoTransform encryptor = algorithm.CreateEncryptor();

                // Create the streams used for encryption
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        // Write all data to the stream
                        csEncrypt.Write(data, 0, data.Length);
                    }

                    encryptedData = msEncrypt.ToArray();
                }
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
                throw new ArgumentNullException("cipherText");
            }
            if (algorithm == null)
            {
                throw new ArgumentNullException("algorithm");
            }
            if (algorithm.Key == null || algorithm.Key.Length <= 0)
            {
                throw new ArgumentNullException("algorithm.Key");
            }
            if (algorithm.IV == null || algorithm.IV.Length <= 0)
            {
                throw new ArgumentNullException("algorithm.IV");
            }

            string plaintext = null;

            try
            {
                // Create a decrytor to perform the stream transform
                ICryptoTransform decryptor = algorithm.CreateDecryptor();

                // Create the streams used for decryption
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            // Read the decrypted bytes from the decrypting stream and place them in a string
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
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
    }
}
