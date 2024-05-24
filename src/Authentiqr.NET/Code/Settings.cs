using Authentiqr.Core;
using Authentiqr.Core.Encode;
using Authentiqr.NET.Code.EncryptionV3;
using Microsoft.Win32;
using System;
using System.IO;
using System.Security;

namespace Authentiqr.NET.Code
{
    public class Settings
    {
        public AccountData Accounts { get; private set; } = new AccountData();
        public int AccountWindowTop { get; set; } = 200;
        public int AccountWindowLeft { get; set; } = 200;
        public int PatternWindowTop { get; set; } = 100;
        public int PatternWindowLeft { get; set; } = 100;
        public int PasswordWindowTop { get; set; } = 100;
        public int PasswordWindowLeft { get; set; } = 100;
        public bool StartupPrompt { get; set; } = true;
        public int EncryptionVersion { get; private set; } = 1;
        public EncryptionMode EncryptionMode { get; private set; } = EncryptionMode.Basic;
        public bool Locked { get; private set; }

        private string encryptedData;
        private SecureString pattern;
        private SecureString password;
        private SecureString userId;
        private CryptoConfig cryptoConfig;

        private const int RecommendedChecksumSize = 20;
        private const int RecommendedIterations = 10000;
        private const int RecommendedSaltSize = 32;

        public void Load()
        {
            var settingsVersion = ((int?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "SettingsVersion", 0)).GetValueOrDefault(-1);
            userId = new SecureString().AppendChars(System.Security.Principal.WindowsIdentity.GetCurrent().User.Value);
            cryptoConfig = new CryptoConfig();

            switch (settingsVersion)
            {
                case 1:
                    AccountWindowTop = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "AccountWindowTop", AccountWindowTop));
                    AccountWindowLeft = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "AccountWindowLeft", AccountWindowLeft));
                    PatternWindowTop = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "PatternWindowTop", PatternWindowTop));
                    PatternWindowLeft = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "PatternWindowLeft", PatternWindowLeft));
                    PasswordWindowTop = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "PasswordWindowTop", PasswordWindowTop));
                    PasswordWindowLeft = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "PasswordWindowLeft", PasswordWindowLeft));
                    StartupPrompt = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "StartupPrompt", 1)) == 1;
                    EncryptionVersion = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "EncryptionVersion", 1));
                    cryptoConfig.ChecksumSize = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "ChecksumSize", RecommendedChecksumSize));
                    cryptoConfig.Iterations = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "Iterations", RecommendedIterations));
                    cryptoConfig.SaltSize = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "SaltSize", RecommendedSaltSize));
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

            Locked = !string.IsNullOrEmpty(encryptedData);
        }

        public void SaveSettings()
        {
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "SettingsVersion", 1, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "AccountWindowTop", AccountWindowTop, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "AccountWindowLeft", AccountWindowLeft, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "PatternWindowTop", PatternWindowTop, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "PatternWindowLeft", PatternWindowLeft, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "PasswordWindowTop", PasswordWindowTop, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "PasswordWindowLeft", PasswordWindowLeft, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "StartupPrompt", StartupPrompt ? 1 : 0, RegistryValueKind.DWord);
        }

        public void SaveAccounts()
        {
            encryptedData = Encrypt(Accounts.Data);

            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "EncryptionMode", EncryptionMode, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "EncryptionVersion", EncryptionVersion, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "AccountData", encryptedData, RegistryValueKind.String);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "ChecksumSize", cryptoConfig.ChecksumSize, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "Iterations", cryptoConfig.Iterations, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "SaltSize", cryptoConfig.SaltSize, RegistryValueKind.DWord);
        }

        public void Unlock()
        {
            Accounts = AccountData.Parse(Decrypt(encryptedData));
            Locked = false;
        }

        public void Lock()
        {
            Accounts = new AccountData();

            if (password != null)
            {
                password.Dispose();
                password = null;
            }

            if (pattern != null)
            {
                pattern.Dispose();
                pattern = null;
            }

            Locked = true;
        }

        public void SetPattern(SecureString pattern)
        {
            this.pattern = pattern;
        }

        public void SetPassword(SecureString password)
        {
            this.password = password;
        }

        private string Encrypt(string data)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;

            EncryptionVersion = 3;
            EncryptionMode = EncryptionMode.Basic;
            cryptoConfig.ChecksumSize = RecommendedChecksumSize;
            cryptoConfig.Iterations = RecommendedIterations;
            cryptoConfig.SaltSize = RecommendedSaltSize;

            if (pattern != null && pattern.Length > 0)
            {
                EncryptionMode = EncryptionMode.Pattern;
            }

            if (password != null && password.Length > 0)
            {
                EncryptionMode = EncryptionMode == EncryptionMode.Pattern ? EncryptionMode.PatternAndPassword : EncryptionMode.Password;
            }

            var aesEncryptor = new AesEncryptor(cryptoConfig);

            switch (EncryptionMode)
            {
                case EncryptionMode.Basic:
                    return Base64.Encode(aesEncryptor.Encrypt(data, userId));
                case EncryptionMode.Pattern:
                    return Base64.Encode(aesEncryptor.Encrypt(data, pattern.Concat(userId)));
                case EncryptionMode.Password:
                    return Base64.Encode(aesEncryptor.Encrypt(data, password));
                case EncryptionMode.PatternAndPassword:
                    return Base64.Encode(aesEncryptor.Encrypt(data, password.Concat(pattern)));
                default:
                    throw new NotImplementedException("Encryption mode not supported: " + EncryptionMode);
            }
        }

        private string Decrypt(string encData)
        {
            if (string.IsNullOrEmpty(encData)) return string.Empty;

            if (EncryptionVersion == 3)
            {
                var encBytes = Base64.Decode(encData);

                var aesDecryptor = new AesDecryptor(cryptoConfig);

                switch (EncryptionMode)
                {
                    case EncryptionMode.Basic:
                        return aesDecryptor.Decrypt(encBytes, userId);
                    case EncryptionMode.Pattern:
                        return aesDecryptor.Decrypt(encBytes, pattern.Concat(userId));
                    case EncryptionMode.Password:
                        return aesDecryptor.Decrypt(encBytes, password);
                    case EncryptionMode.PatternAndPassword:
                        return aesDecryptor.Decrypt(encBytes, password.Concat(pattern));
                    default:
                        throw new NotImplementedException("Encryption mode not supported: " + EncryptionMode);
                }
            }
            else
            {
                throw new NotImplementedException("Encryption version not supported: " + EncryptionMode);
            }
        }

        public bool EncryptionUpgradeRequired
        {
            get
            {
                return EncryptionVersion < 3 ||
                       cryptoConfig.ChecksumSize < RecommendedChecksumSize ||
                       cryptoConfig.Iterations < RecommendedIterations ||
                       cryptoConfig.SaltSize < RecommendedSaltSize;
            }
        }

        private void Migrate(int currentVersion)
        {
            switch (currentVersion)
            {
                case 0:
                    SaveSettings();
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Authentiqr.NET", "AccountData", encryptedData, RegistryValueKind.String);
                    Registry.CurrentUser.DeleteSubKey(@"Software\LCGoogleApps");
                    var startup = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run", "LCGoogleApps", string.Empty);
                    if (startup != string.Empty)
                    {
                        RunOnWindowsStartup();
                        Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true).DeleteValue("LCGoogleApps");
                    }
                    break;
            }
        }

        public static void RunOnWindowsStartup()
        {
            var path = Path.Combine(Environment.CurrentDirectory, "Authentiqr.NET.exe");
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run", "Authentiqr.NET", path, RegistryValueKind.String);
        }
    }
}
