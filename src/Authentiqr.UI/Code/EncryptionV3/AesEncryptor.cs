using Authentiqr.Core;
using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;

namespace Authentiqr.NET.Code.EncryptionV3
{
    public class AesEncryptor
    {
        private int Iterations;
        private int SaltSize;
        private int ChecksumSize;

        public AesEncryptor(CryptoConfig config)
        {
            Iterations = config.Iterations;
            SaltSize = config.SaltSize;
            ChecksumSize = config.ChecksumSize;

            if (SaltSize < 8) throw new ArgumentOutOfRangeException("AesEncryptor: SaltSize must be greater than 8");
            if (ChecksumSize < 0) throw new ArgumentOutOfRangeException("AesEncryptor: ChecksumSize must not be negative");
            if (ChecksumSize > 20) throw new ArgumentOutOfRangeException("AesEncryptor: ChecksumSize must not be greated than 20");
            if (Iterations <= 0) throw new ArgumentOutOfRangeException("AesEncryptor: Iterations must greater than zero");
        }

        /// <summary>
        /// Encrypts a string to a byte-array, consisting of the Salt + Encrypted Data + optional Checksum
        /// </summary>
        /// <param name="data"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public byte[] Encrypt(string data, SecureString password)
        {
            if (String.IsNullOrEmpty(data)) throw new ArgumentOutOfRangeException("AesEncryptor.Encrypt: data cannot be null or empty");

            // Derive a new Salt and IV from the Key
            using (var keyDerivationFunction = password.Use(p => new Rfc2898DeriveBytes(p, SaltSize, Iterations)))
            {
                var saltBytes = keyDerivationFunction.Salt;
                var keyBytes = keyDerivationFunction.GetBytes(32);
                var ivBytes = keyDerivationFunction.GetBytes(16);

                // Create an encryptor to perform the stream transform.
                // Create the streams used for encryption.
                using (var aesManaged = new AesManaged())
                using (var encryptor = aesManaged.CreateEncryptor(keyBytes, ivBytes))
                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    using (var streamWriter = new StreamWriter(cryptoStream))
                    {
                        // Send the data through the StreamWriter, through the CryptoStream, to the underlying MemoryStream
                        streamWriter.Write(data);
                    }

                    // Read the Encrypted Data from the memory stream
                    var cipherTextBytes = memoryStream.ToArray();

                    // Append Encrypted Data to the Salt
                    AppendBytes(ref saltBytes, cipherTextBytes);

                    if (ChecksumSize > 0)
                    {
                        // Compute the HMAC of the Salt + Encrypted Data, and append to result
                        using (var hmac = new HMACSHA256(keyBytes))
                        using (var cipherStream = new MemoryStream(saltBytes))
                        {
                            var checksum = hmac.ComputeHash(cipherStream);
                            AppendBytes(ref saltBytes, checksum, ChecksumSize);
                        }
                    }

                    return saltBytes;
                }
            }
        }

        private void AppendBytes(ref byte[] original, byte[] additional, int? amount = null)
        {
            var originalLength = original.Length;
            var additionalLength = amount.GetValueOrDefault(additional.Length);
            Array.Resize(ref original, originalLength + additionalLength);
            Array.Copy(additional, 0, original, originalLength, additionalLength);
        }
    }
}
