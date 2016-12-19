using System;

namespace Authentiqr.NET.Code
{
    public class Base64
    {
        public static string Encode(byte[] data)
        {
            return Convert.ToBase64String(data);
        }

        public static byte[] Decode(string base64)
        {
            return Convert.FromBase64String(base64);
        }
    }
}
