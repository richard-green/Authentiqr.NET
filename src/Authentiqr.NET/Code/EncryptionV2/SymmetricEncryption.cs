using Authentiqr.Core;
using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;

namespace Authentiqr.NET.Code.EncryptionV2
{
    class SymmetricEncryption
    {
        private const int SaltSize = 32;

        public static byte[] Encrypt(byte[] plainText, SecureString password)
        {
            return password.Use(ss => Encrypt(plainText, ss));
        }

        /// <summary>
        /// Encrypts the plainText input using the given Key.
        /// A 128 bit random salt will be generated and prepended to the ciphertext before it is returned.
        /// </summary>
        /// <param name="plainText">The plain text to encrypt.</param>
        /// <param name="password">The plain text encryption key.</param>
        /// <returns>The salt and the ciphertext</returns>
        public static byte[] Encrypt(byte[] plainText, string password)
        {
            if (plainText == null || plainText.Length == 0) throw new ArgumentNullException("plainText");
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException("password");

            // Derive a new Salt, Key, and IV from the password
            using (var keyDerivationFunction = new Rfc2898DeriveBytes(password, SaltSize))
            {
                var saltBytes = keyDerivationFunction.Salt;
                var keyBytes = keyDerivationFunction.GetBytes(32);
                var ivBytes = keyDerivationFunction.GetBytes(16);

                using (var aesManaged = new AesManaged())
                using (var encryptor = aesManaged.CreateEncryptor(keyBytes, ivBytes))
                using (var memoryStream = new MemoryStream())
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    using (var streamWriter = new BinaryWriter(cryptoStream))
                    {
                        streamWriter.Write(plainText);
                    }

                    var cipherTextBytes = memoryStream.ToArray();
                    Array.Resize(ref saltBytes, saltBytes.Length + cipherTextBytes.Length);
                    Array.Copy(cipherTextBytes, 0, saltBytes, SaltSize, cipherTextBytes.Length);

                    return saltBytes;
                }
            }
        }

        public static byte[] Decrypt(byte[] cipherText, SecureString password)
        {
            return password.Use(ss => Decrypt(cipherText, ss));
        }

        /// <summary>
        /// Decrypts the ciphertext using the Key.
        /// </summary>
        /// <param name="ciphertext">The ciphertext to decrypt.</param>
        /// <param name="password">The plain text encryption key.</param>
        /// <returns>The decrypted text.</returns>
        public static byte[] Decrypt(byte[] cipherText, string password)
        {
            if (cipherText == null || cipherText.Length == 0) throw new ArgumentNullException("cipherText");
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException("password");

            // Extract the salt from our ciphertext
            var saltBytes = cipherText.Take(SaltSize).ToArray();
            var ciphertextBytes = cipherText.Skip(SaltSize).ToArray();

            using (var keyDerivationFunction = new Rfc2898DeriveBytes(password, saltBytes))
            {
                // Derive the previous Key and IV from the password and salt
                var keyBytes = keyDerivationFunction.GetBytes(32);
                var ivBytes = keyDerivationFunction.GetBytes(16);

                using (var aesManaged = new AesManaged())
                using (var decryptor = aesManaged.CreateDecryptor(keyBytes, ivBytes))
                using (var memoryStream = new MemoryStream(ciphertextBytes))
                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                using (var outputStream = new StreamReader(cryptoStream))
                using (var outMemoryStream = new MemoryStream())
                {
                    outputStream.BaseStream.CopyTo(outMemoryStream);
                    return outMemoryStream.ToArray();
                }
            }
        }
    }
}
