using Microsoft.Win32;
using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;

namespace Authentiqr.NET.Code
{
    public class Settings
    {
        public AccountData Accounts { get; private set; } = new AccountData();
        public int AccountWindowTop { get; set; } = 200;
        public int AccountWindowLeft { get; set; } = 200;
        public int PatternWindowTop { get; set; } = 100;
        public int PatternWindowLeft { get; set; } = 100;
        public bool StartupPrompt { get; set; } = true;
        public int EncryptionVersion { get; private set; } = 1;
        public EncryptionMode EncryptionMode { get; private set; } = EncryptionMode.Basic;

        private string encryptedData;
        private string pattern;
        private SecureString password;

        public void LoadSettings()
        {
            var settingsVersion = ((int?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "SettingsVersion", 0)).GetValueOrDefault(0);

            switch (settingsVersion)
            {
                case 1:
                    AccountWindowTop = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "AccountWindowTop", AccountWindowTop));
                    AccountWindowLeft = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "AccountWindowLeft", AccountWindowLeft));
                    PatternWindowTop = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "PatternWindowTop", PatternWindowTop));
                    PatternWindowLeft = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "PatternWindowLeft", PatternWindowLeft));
                    StartupPrompt = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "StartupPrompt", 1)) == 1;
                    EncryptionVersion = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "EncryptionVersion", 1));
                    EncryptionMode = (EncryptionMode)((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "EncryptionMode", EncryptionMode.Basic));
                    encryptedData = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "AccountData", null);
                    break;

                case 0:
                    var patternEnabled = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "PatternEnabled", 0)) == 1;
                    PatternWindowTop = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "PatternWindowTop", PatternWindowTop));
                    PatternWindowLeft = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "PatternWindowLeft", PatternWindowLeft));
                    StartupPrompt = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "StartupPrompt", 1)) == 1;
                    EncryptionMode = patternEnabled ? EncryptionMode.Pattern : EncryptionMode.Basic;
                    encryptedData = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "encAccounts", null);
                    Migrate(0);
                    break;
            }
        }

        public void SaveSettings()
        {
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "SettingsVersion", 1, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "AccountWindowTop", AccountWindowTop, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "AccountWindowLeft", AccountWindowLeft, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "PatternWindowTop", PatternWindowTop, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "PatternWindowLeft", PatternWindowLeft, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "StartupPrompt", StartupPrompt ? 1 : 0, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "EncryptionVersion", EncryptionVersion, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "EncryptionMode", EncryptionMode, RegistryValueKind.DWord);
        }

        public void SaveAccounts()
        {
            encryptedData = Encrypt(Accounts.Data);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "AccountData", encryptedData, RegistryValueKind.String);
        }

        public void Unlock()
        {
            Accounts = AccountData.Parse(Decrypt(encryptedData));
        }

        public void SetPattern(string pattern)
        {
            this.EncryptionMode = EncryptionMode.Pattern;
            this.pattern = pattern;
        }

        public void SetPassword(SecureString password)
        {
            this.EncryptionMode = EncryptionMode.Password;
            this.password = password;
        }

        private string Encrypt(string data)
        {
            if (String.IsNullOrEmpty(data)) return String.Empty;

            switch (EncryptionMode)
            {
                case EncryptionMode.Basic:
                case EncryptionMode.Pattern:
                    return Base64.Encode(Encryption.Encrypt(data, CreateAlgorithm()));
                default:
                    throw new NotImplementedException("Encryption mode not supported: " + EncryptionMode);
            }
        }

        private string Decrypt(string encData)
        {
            if (String.IsNullOrEmpty(encData)) return String.Empty;

            switch (EncryptionMode)
            {
                case EncryptionMode.Basic:
                case EncryptionMode.Pattern:
                    return Encryption.Decrypt(Base64.Decode(encData), CreateAlgorithm());
                default:
                    throw new NotImplementedException("Encryption mode not supported: " + EncryptionMode);
            }
        }

        private SymmetricAlgorithm CreateAlgorithm()
        {
            string sid = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;
            return Encryption.CreateCryptoAlgorithm(EncryptionMode == EncryptionMode.Pattern ? Encryption.GeneratePasswordHash(pattern, sid) : sid, "LCGoogleApps");
        }

        private void Migrate(int currentVersion)
        {
            switch (currentVersion)
            {
                case 0:
                    SaveSettings();
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "AccountData", encryptedData, RegistryValueKind.String);
                    Registry.CurrentUser.DeleteSubKey(@"Software\LCGoogleApps");
                    var startup = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run", "LCGoogleApps", String.Empty);
                    if (startup != String.Empty)
                    {
                        RunOnWindowsStartup();
                        Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true).DeleteValue("LCGoogleApps");
                    }
                    break;
            }
        }

        public void RunOnWindowsStartup()
        {
            var path = Path.Combine(Environment.CurrentDirectory, "Authentiqr.NET.exe");
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run", "Authentiqr.NET", path, RegistryValueKind.String);
        }
    }
}
