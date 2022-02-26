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

        private readonly int PassCodeLength;
        private readonly int Interval;
        private readonly int PinModulo;
        private readonly Func<DateTime> Now;

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
            var key = Base32.FromBase32String(password);
            var mac = new HMACSHA1
            {
                Key = key
            };

            var challenge = Reverse(BitConverter.GetBytes(currentInterval));
            var hash = mac.ComputeHash(challenge);

            // Dynamically truncate the hash
            // OffsetBits are the low order bits of the last byte of the hash
            var offset = hash[^1] & 0xF;
            // Grab a positive integer value starting at the given offset.
            var result = HashToInt(hash, offset);
            var truncatedHash = result & 0x7FFFFFFF;
            var pinValue = truncatedHash % PinModulo;

            return pinValue.ToString(new string('0', PassCodeLength));
        }

        private static int HashToInt(byte[] bytes, int start)
        {
            using var input = new BinaryReader(new MemoryStream(bytes, start, bytes.Length - start));
            var a = input.ReadByte();
            var b = input.ReadByte();
            var c = input.ReadByte();
            var d = input.ReadByte();

            return (a << 24) | (b << 16) | (c << 8) | d;
        }

        private static byte[] Reverse(byte[] array)
        {
            var result = new byte[array.Length];
            var ix = 0;
            for (int i = array.Length - 1; i >= 0; i--)
            {
                result[ix++] = array[i];
            }
            return result;
        }

        private long CurrentInterval => UnixSeconds(Now()) / Interval;

        private static long UnixSeconds(DateTime stamp)
        {
            return (long)(stamp.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;
        }
    }
}
