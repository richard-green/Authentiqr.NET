using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace LCGoogleApps
{
    public class Settings
    {
        public Dictionary<string, string> Accounts = new Dictionary<string, string>();
        public bool PatternEnabled = false;
        public int PatternWindowTop = 100;
        public int PatternWindowLeft = 100;

        private string pattern;

        public void LoadSettings()
        {
            PatternEnabled = ((int?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "PatternEnabled", 0)) == 1;
            PatternWindowTop = ((int?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "PatternWindowTop", PatternWindowTop)).GetValueOrDefault(100);
            PatternWindowLeft = ((int?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "PatternWindowLeft", PatternWindowLeft)).GetValueOrDefault(100);
        }

        public void SaveSettings()
        {
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "PatternEnabled", PatternEnabled ? 1 : 0, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "PatternWindowTop", PatternWindowTop, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "PatternWindowLeft", PatternWindowLeft, RegistryValueKind.DWord);
        }

        public void LoadAccounts()
        {
            var registryValue = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "encAccounts", String.Empty);

            if (String.IsNullOrEmpty(registryValue) == false)
            {
                // Multiple Accounts in the user registry

                Accounts = ParseAccountsData(DecryptData(registryValue));
            }
        }

        public void SaveAccounts()
        {
            if (Accounts.Count > 0)
            {
                string accountsData = EncryptData(GenerateAccountsData(Accounts));
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "encAccounts", accountsData, RegistryValueKind.String);
            }
            else
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "encAccounts", String.Empty, RegistryValueKind.String);
            }
        }

        private string GenerateAccountsData(Dictionary<string, string> accounts)
        {
            string[] accountData = new string[accounts.Count];
            int i = 0;

            foreach (var account in accounts)
            {
                accountData[i++] = String.Format("{0}@{1}",
                    Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(account.Key)),
                    Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(account.Value)));
            }

            return String.Join("*", accountData);
        }

        private Dictionary<string, string> ParseAccountsData(string data)
        {
            if (String.IsNullOrEmpty(data))
            {
                return new Dictionary<string, string>();
            }
            else
            {
                Dictionary<string, string> output = new Dictionary<string, string>();
                string[] accountData = data.Split('*');

                foreach (var account in accountData)
                {
                    string[] kvp = account.Split('@');
                    kvp[0] = ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(kvp[0]));
                    kvp[1] = ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(kvp[1]));
                    output[kvp[0]] = kvp[1];
                }

                return output;
            }
        }

        private string EncryptData(string data)
        {
            return Encryption.ToBase64(Encryption.Encrypt(data, CreateAlgorithm()));
        }

        private string DecryptData(string encData)
        {
            return Encryption.Decrypt(Encryption.FromBase64(encData), CreateAlgorithm());
        }

        private SymmetricAlgorithm CreateAlgorithm()
        {
            string sid = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;
            return Encryption.CreateCryptoAlgorithm(PatternEnabled ? Encryption.GeneratePasswordHash(pattern, sid) : sid, "LCGoogleApps");
        }

        public void SetPattern(string pattern)
        {
            this.PatternEnabled = true;
            this.pattern = pattern;
        }
    }
}
