using Authentiqr.Core;
using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;

namespace Authentiqr.NET.Code.EncryptionV3
{
    public class AesDecryptor
    {
        private readonly int Iterations;
        private readonly int SaltSize;
        private readonly int ChecksumSize;

        public AesDecryptor(CryptoConfig config)
        {
            Iterations = config.Iterations;
            SaltSize = config.SaltSize;
            ChecksumSize = config.ChecksumSize;

            if (SaltSize < 8) throw new ArgumentOutOfRangeException(nameof(config), "AesDecryptor: SaltSize must be greater than 8");
            if (ChecksumSize < 0) throw new ArgumentOutOfRangeException(nameof(config), "AesDecryptor: ChecksumSize must not be negative");
            if (ChecksumSize > 20) throw new ArgumentOutOfRangeException(nameof(config), "AesDecryptor: ChecksumSize must not be greated than 20");
            if (Iterations < 0) throw new ArgumentOutOfRangeException(nameof(config), "AesDecryptor: Iterations must not be negative");
        }

        /// <summary>
        /// Decrypts a byte-array that contains a Salt + Encrypted Data + optional Checksum, to the original string
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="ChecksumValidationException">Thrown when the Checksum does not match</exception>
        /// <exception cref="CryptographicException">Thrown if data is corrupted</exception>
        public string Decrypt(byte[] bytes, SecureString password)
        {
            if (bytes == null || bytes.Length < SaltSize) throw new ArgumentOutOfRangeException(nameof(bytes), "AesDecryptor.Decrypt: bytes cannot be null or smaller than the salt size");

            // Extract the Salt
            byte[] saltBytes = bytes.Take(SaltSize).ToArray();

            using var keyDerivationFunction = password.Use(p => new Rfc2898DeriveBytes(p, saltBytes, Iterations, HashAlgorithmName.SHA1));
            // Derive the previous IV from the Key and Salt
            var keyBytes = keyDerivationFunction.GetBytes(32);
            var ivBytes = keyDerivationFunction.GetBytes(16);

            byte[] checksum = null;
            string data = null;

            if (ChecksumSize > 0)
            {
                // Extract the Checksum from the end of the input data
                checksum = bytes.Skip(bytes.Length - ChecksumSize).Take(ChecksumSize).ToArray();

                // Remove the Checksum, left now with Salt + Encrypted Data
                bytes = bytes.Take(bytes.Length - ChecksumSize).ToArray();
            }

            // Remove the Salt, left now with just the Encrypted Data
            var encryptedData = bytes.Skip(SaltSize).ToArray();

            // Create a decrytor to perform the stream transform.
            // The default Cipher Mode is CBC and the Padding is PKCS7 which are both good
            using (var aesManaged = Aes.Create())
            using (var decryptor = aesManaged.CreateDecryptor(keyBytes, ivBytes))
            using (var memoryStream = new MemoryStream(encryptedData))
            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
            using (var streamReader = new StreamReader(cryptoStream))
            {
                // Return the decrypted text from the decrypting stream.
                data = streamReader.ReadToEnd();
            }

            if (ChecksumSize > 0)
            {
                // Now compute the HMAC of the Salt + Encrypted Data, and verify it's correct
                using var hmac = new HMACSHA256(keyBytes);
                using var cipherStream = new MemoryStream(bytes);
                var checksum2 = hmac.ComputeHash(cipherStream).Take(ChecksumSize);
                if (!Enumerable.SequenceEqual(checksum, checksum2))
                {
                    throw new ChecksumValidationException("AesDecryptor.Decrypt: Checksum does not match", data);
                }
            }

            return data;
        }
    }
}
