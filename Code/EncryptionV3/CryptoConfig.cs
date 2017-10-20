namespace Authentiqr.NET.Code.EncryptionV3
{
    public class CryptoConfig
    {
        /// <summary>
        /// Number of iterations to derive a strong password
        /// </summary>
        public int Iterations { get; set; }

        /// <summary>
        /// Size of salt to prepend to data prior to encryption, in bytes, minimum of 8
        /// </summary>
        public int SaltSize { get; set; }

        /// <summary>
        /// Size of checksum to append to data, in bytes, valid range of 0-20
        /// </summary>
        public int ChecksumSize { get; set; }
    }
}
