using System;

namespace Authentiqr.NET.Code
{
    public class Hex
    {
        public static string Encode(byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", String.Empty).ToLower();
        }

        public static byte[] Decode(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }
    }
}
