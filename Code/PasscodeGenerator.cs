using Authentiqr.NET.Code.Encode;
using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;

namespace Authentiqr.NET.Code
{
    public class PasscodeGenerator
    {
        // Original Java code from: http://blog.jcuff.net/2011/02/cli-java-based-google-authenticator.html
        // Converted to C# by Richard Green

        private int PassCodeLength { get; set; }
        private int Interval { get; set; }
        private int PinModulo { get; set; }

        public PasscodeGenerator()
        {
            Interval = 30;
            PassCodeLength = 6;
            PinModulo = (int)Math.Pow(10, PassCodeLength);
        }

        public string GenerateTimeoutCode(SecureString password)
        {
            return password.Use(p => GenerateTimeoutCode(p));
        }

        public string GenerateTimeoutCode(string password)
        {
            byte[] key = Base32.FromBase32String(password);
            string keyStr = Hex.Encode(key);
            HMACSHA1 Mac = new HMACSHA1();
            Mac.Key = key;

            byte[] challenge = Reverse(BitConverter.GetBytes(CurrentInterval));
            byte[] hash = Mac.ComputeHash(challenge);

            string hashStr = Hex.Encode(hash);

            // Dynamically truncate the hash
            // OffsetBits are the low order bits of the last byte of the hash
            int offset = hash[hash.Length - 1] & 0xF;
            // Grab a positive integer value starting at the given offset.
            int result = HashToInt(hash, offset);
            int truncatedHash = result & 0x7FFFFFFF;
            int pinValue = truncatedHash % PinModulo;

            return pinValue.ToString(new String('0', 6));
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

        private static long UnixSeconds(DateTime stamp)
        {
            return (long)(stamp.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;
        }

        public long CurrentInterval
        {
            get
            {
                return UnixSeconds(DateTime.Now) / Interval;
            }
        }
    }
}
