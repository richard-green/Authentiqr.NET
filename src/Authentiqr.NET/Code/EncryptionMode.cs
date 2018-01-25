using System.ComponentModel;

namespace Authentiqr.NET.Code
{
    public enum EncryptionMode
    {
        [Description("User SID encryption - Not recommended!")]
        Basic = 0,

        [Description("Pattern encryption")]
        Pattern = 1,

        [Description("Password encryption")]
        Password = 2,

        [Description("Combined pattern and password encryption")]
        PatternAndPassword = 3
    }
}
