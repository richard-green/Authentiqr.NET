using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace LCGoogleApps
{
	public class Encryption
	{
		private const string CryptoKey = "496f1a09cce8b429725ceca93d29adaad4bb97bc0870dda39d42c78be8c74f65";
		private const string CryptoIV = "4e3eb5fa6d3c7146d5332a2f58867c95";
		private const string FixedSalt = "c0NjU3MDlmNzRjMjQwYg";
		private const int HashRounds = 300000;

		private static HashAlgorithm Hasher { get; set; }

		/// <summary>
		/// Static constructor
		/// </summary>
		static Encryption()
		{
			Hasher = SHA256.Create();
		}

		private static SymmetricAlgorithm GetCryptoAlgorithm()
		{
			return new AesManaged()
			{
				Key = FromHex(CryptoKey),
				IV = FromHex(CryptoIV)
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

		public static byte[] Encrypt(byte[] data, SymmetricAlgorithm algorithm = null)
		{
			// Check arguments
			if (data == null || data.Length <= 0)
			{
				throw new ArgumentNullException("data");
			}
			if (algorithm == null)
			{
				algorithm = GetCryptoAlgorithm();
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

		public static byte[] Encrypt(string plainText, SymmetricAlgorithm algorithm = null)
		{
			return Encrypt(Encoding.UTF8.GetBytes(plainText), algorithm);
		}

		public static string Decrypt(byte[] cipherText, SymmetricAlgorithm algorithm = null)
		{
			// Check arguments
			if (cipherText == null || cipherText.Length <= 0)
			{
				throw new ArgumentNullException("cipherText");
			}
			if (algorithm == null)
			{
				algorithm = GetCryptoAlgorithm();
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

		public static string ToHex(byte[] data)
		{
			return BitConverter.ToString(data).Replace("-", String.Empty).ToLower();
		}

		public static byte[] FromHex(string hex)
		{
			int NumberChars = hex.Length;
			byte[] bytes = new byte[NumberChars / 2];
			for (int i = 0; i < NumberChars; i += 2)
			{
				bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			}
			return bytes;
		}

		public static string ToBase64(byte[] data)
		{
			return Convert.ToBase64String(data);
		}

		public static byte[] FromBase64(string base64)
		{
			return Convert.FromBase64String(base64);
		}

		public static string GenerateSalt()
		{
			return ToBase64(Hash(DateTime.Now.Ticks.ToString()));
		}

		public static string GeneratePasswordHash(string salt, string password)
		{
			byte[] hash = Hash(salt + FixedSalt + ToBase64(Encrypt(password)));

			for (int i = 1; i < HashRounds; i++)
			{
                hash = Hash(hash);
			}

			return ToBase64(hash);
		}
	}
}
