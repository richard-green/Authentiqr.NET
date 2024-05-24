using System;

namespace Authentiqr.NET.Code.EncryptionV3
{
    [Serializable]
    public class ChecksumValidationException(string message, string decryptedData) : Exception(message)
    {
        public string DecryptedData { get; private set; } = decryptedData;
    }
}
