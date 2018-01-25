using System;
using System.Runtime.Serialization;

namespace Authentiqr.NET.Code.EncryptionV3
{
    [Serializable]
    public class ChecksumValidationException : Exception
    {
        public string DecryptedData { get; private set; }

        public ChecksumValidationException(string message, string decryptedData) : base(message)
        {
            DecryptedData = decryptedData;
        }

        protected ChecksumValidationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
