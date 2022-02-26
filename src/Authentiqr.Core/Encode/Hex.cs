using System;

namespace Authentiqr.Core.Encode
{
    public class Hex
    {
        public static string Encode(byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", string.Empty).ToLower();
        }

        public static byte[] Decode(string hex)
        {
            var length = hex.Length;
            var bytes = new byte[length / 2];
            for (int i = 0; i < length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }
    }
}
