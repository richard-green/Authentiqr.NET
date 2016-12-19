using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Authentiqr.NET.Code
{
    public static class SecureStringExtensions
    {
        /// <summary>
        /// Decrypts the SecureString and provides the contents to an Action for short-term use
        /// </summary>
        /// <param name="secureString"></param>
        /// <param name="Do"></param>
        public static void Use(this SecureString secureString, Action<string> Do)
        {
            IntPtr bstr = IntPtr.Zero;

            try
            {
                bstr = Marshal.SecureStringToBSTR(secureString);
                Do(Marshal.PtrToStringBSTR(bstr));
            }
            finally
            {
                Marshal.ZeroFreeBSTR(bstr);
            }
        }

        /// <summary>
        /// Decrypts the SecureString and provides the contents to a Func for short-term use
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="secureString"></param>
        /// <param name="Do"></param>
        /// <returns></returns>
        public static T Use<T>(this SecureString secureString, Func<string, T> Do)
        {
            IntPtr bstr = IntPtr.Zero;

            try
            {
                bstr = Marshal.SecureStringToBSTR(secureString);
                return Do(Marshal.PtrToStringBSTR(bstr));
            }
            finally
            {
                Marshal.ZeroFreeBSTR(bstr);
            }
        }
    }
}
