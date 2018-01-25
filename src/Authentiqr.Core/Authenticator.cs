using Authentiqr.Core.Encode;
using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;

namespace Authentiqr.Core
{
    public class Authenticator
    {
        // Original Java code from: http://blog.jcuff.net/2011/02/cli-java-based-google-authenticator.html
        // Converted to C# by Richard Green

        private int PassCodeLength;
        private int Interval;
        private int PinModulo;
        private Func<DateTime> Now;

        public Authenticator() : this(() => DateTime.Now)
        {
        }

        public Authenticator(Func<DateTime> now)
        {
            Interval = 30;
            PassCodeLength = 6;
            PinModulo = (int)Math.Pow(10, PassCodeLength);
            Now = now;
        }

        /// <summary>
        /// Generate a code based on a Base32-encoded password
        /// </summary>
        /// <param name="password">Base32-encoded</param>
        /// <returns>6-digit code string</returns>
        public string GenerateCode(SecureString password) => password.Use(pwd => GenerateCode(pwd, CurrentInterval));

        /// <summary>
        /// Generate a code based on a Base32-encoded password
        /// </summary>
        /// <param name="password">Base32-encoded</param>
        /// <returns>6-digit code string</returns>
        public string GenerateCode(string password) => GenerateCode(password, CurrentInterval);

        /// <summary>
        /// Validate a code given the Base32-encoded password used to generate it
        /// </summary>
        /// <param name="code">6-digit code string</param>
        /// <param name="password">Base32-encoded</param>
        /// <param name="maxIntervals">Number of intervals to check ahead and backwards in time if the code is not found in the current time window</param>
        /// <returns></returns>
        public bool ValidateCode(string code, SecureString password, int maxIntervals = 2) => password.Use(p => ValidateCode(code, p, maxIntervals));

        /// <summary>
        /// Validate a code given the Base32-encoded password used to generate it
        /// </summary>
        /// <param name="code">6-digit code string</param>
        /// <param name="password">Base32-encoded</param>
        /// <param name="maxIntervals">Number of intervals to check forward and backwards in time around the current time window</param>
        /// <returns></returns>
        public bool ValidateCode(string code, string password, int maxIntervals = 2)
        {
            var currentInterval = CurrentInterval - maxIntervals;

            for (int i = 0; i <= maxIntervals * maxIntervals; i++)
            {
                if (code == GenerateCode(password, currentInterval)) return true;
                currentInterval++;
            }

            return false;
        }

        private string GenerateCode(string password, long currentInterval)
        {
            byte[] key = Base32.FromBase32String(password);
            string keyStr = Hex.Encode(key);
            HMACSHA1 Mac = new HMACSHA1();
            Mac.Key = key;

            byte[] challenge = Reverse(BitConverter.GetBytes(currentInterval));
            byte[] hash = Mac.ComputeHash(challenge);

            string hashStr = Hex.Encode(hash);

            // Dynamically truncate the hash
            // OffsetBits are the low order bits of the last byte of the hash
            int offset = hash[hash.Length - 1] & 0xF;
            // Grab a positive integer value starting at the given offset.
            int result = HashToInt(hash, offset);
            int truncatedHash = result & 0x7FFFFFFF;
            int pinValue = truncatedHash % PinModulo;

            return pinValue.ToString(new String('0', PassCodeLength));
        }

        private int HashToInt(byte[] bytes, int start)
        {
            using (BinaryReader input = new BinaryReader(new MemoryStream(bytes, start, bytes.Length - start)))
            {
                byte a = input.ReadByte();
                byte b = input.ReadByte();
                byte c = input.ReadByte();
                byte d = input.ReadByte();

                return ((int)a << 24) | ((int)b << 16) | ((int)c << 8) | (int)d;
            }
        }

        private byte[] Reverse(byte[] array)
        {
            byte[] result = new byte[array.Length];
            int ix = 0;
            for (int i = array.Length - 1; i >= 0; i--)
            {
                result[ix++] = array[i];
            }
            return result;
        }

        private long CurrentInterval => UnixSeconds(Now()) / Interval;

        private long UnixSeconds(DateTime stamp)
        {
            return (long)(stamp.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;
        }
    }
}
