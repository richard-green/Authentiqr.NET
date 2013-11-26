using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Configuration;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace LCGoogleApps
{
    public partial class frmMain : Form
    {
        #region Properties

        private bool patternEnabled = false;
        private string pattern;

        private PasscodeGenerator generator;
        protected PasscodeGenerator Generator
        {
            get
            {
                if (generator == null)
                {
                    generator = new PasscodeGenerator();
                }

                return generator;
            }
        }

        private Configuration config;
        protected Configuration Config
        {
            get
            {
                if (config == null)
                {
                    config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                }

                return config;
            }
        }

        protected Dictionary<string, string> Accounts = new Dictionary<string, string>();

        private List<ToolStripItem> TimeoutMenuItems = new List<ToolStripItem>();

        #endregion Properties

        #region Constructor

        public frmMain()
        {
            this.MaximizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Enabled = false;
            this.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.ControlBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new System.Drawing.Size(1, 7);
            this.Opacity = 0.0d;

            InitializeComponent();
        }

        #endregion Constructor

        #region .NET Events

        private void frmMain_Load(object sender, EventArgs e)
        {
            patternEnabled = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "PatternEnabled", 0)) == 1;

            if (patternEnabled == false)
            {
                mnuUnlock.Text = "Lock Data";
                LoadAccounts();
            }
            else
            {
                mnuAddAccount.Enabled = false;
            }
        }

        private void tmrMain_Tick(object sender, EventArgs e)
        {
            foreach (var item in TimeoutMenuItems)
            {
                item.Text = Generator.GenerateTimeoutCode(item.Tag as string);
            }
        }

        #endregion .NET Events

        #region User Events

        private void mnuTimeoutMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripItem item = sender as ToolStripItem;
            Clipboard.SetText(item.Text);
        }

        private void mnuAccount_Click(object sender, EventArgs e)
        {
            ToolStripItem accountMenuItem = sender as ToolStripItem;
            string oldAccountName = accountMenuItem.Text;
            ToolStripItem timeoutMenuItem = accountMenuItem.Tag as ToolStripItem;
            string oldPassword = timeoutMenuItem.Tag as string;

            using (frmAddAccount form = new frmAddAccount())
            {
                form.ShowRemove(true);
                form.AccountName = oldAccountName;
                form.SetKey(oldPassword);
                DialogResult result = form.ShowDialog(this);

                if (result == DialogResult.OK)
                {
                    Accounts.Remove(oldAccountName);

                    string password = form.Key;
                    string accountName = form.AccountName;

                    if (String.IsNullOrEmpty(accountName) == false &&
                        String.IsNullOrEmpty(password) == false)
                    {
                        Accounts[accountName] = password;

                        accountMenuItem.Text = accountName;
                        timeoutMenuItem.Tag = password;
                    }
                    else
                    {
                        var ix = ContextMenu.Items.IndexOf(accountMenuItem);
                        ContextMenu.Items.RemoveAt(ix + 2); // remove separator
                        ContextMenu.Items.RemoveAt(ix + 1); // remove timeout password
                        ContextMenu.Items.RemoveAt(ix); // remove account name
                    }

                    SaveAccounts();
                }
            }
        }

        private void mnuAddAccount_Click(object sender, EventArgs e)
        {
            using (frmAddAccount form = new frmAddAccount())
            {
                DialogResult result = form.ShowDialog(this);

                if (result == DialogResult.OK)
                {
                    string password = Regex.Replace(form.Key, "\\s", "");
                    string accountName = form.AccountName;

                    Accounts[accountName] = password;
                    SaveAccounts();

                    InitAccount(accountName, password);
                }
            }
        }

        private void mnuExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void mnuUnlock_Click(object sender, EventArgs e)
        {
            using (frmPatternLock form = new frmPatternLock())
            {
                DialogResult result = form.ShowDialog(this);

                if (result == DialogResult.OK)
                {
                    if (patternEnabled)
                    {
                        // Perform unlock
                        pattern = form.GetPattern();

                        try
                        {
                            LoadAccounts();
                            mnuUnlock.Visible = false;
                            mnuAddAccount.Enabled = true;
                        }
                        catch (CryptographicException)
                        {
                            MessageBox.Show("Invalid Pattern");
                        }
                    }
                    else
                    {
                        // Perform lock
                        patternEnabled = true;
                        mnuUnlock.Visible = false;
                        pattern = form.GetPattern();

                        SaveAccounts();
                    }
                }
            }
        }

        #endregion User Events

        #region Methods

        private void LoadAccounts()
        {
            var passwordSection = Config.AppSettings.Settings["encPass"];
            var accountsSection = Config.AppSettings.Settings["encAccounts"];
            var registryValue = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "encAccounts", String.Empty);

            if (String.IsNullOrEmpty(registryValue) == false)
            {
                // Multiple Accounts in the user registry

                Accounts = ParseAccountsData(DecryptData(registryValue));

                foreach (var account in Accounts)
                {
                    InitAccount(account.Key, account.Value);
                }
            }
            else if (accountsSection != null)
            {
                // Multiple Accounts in config

                Accounts = ParseAccountsData(DecryptData(accountsSection.Value));

                foreach (var account in Accounts)
                {
                    InitAccount(account.Key, account.Value);
                }

                // Remove from config
                Config.AppSettings.Settings.Remove("encAccounts");
                Config.Save(ConfigurationSaveMode.Modified, true);

                // Save to registry
                SaveAccounts();
            }
            else if (passwordSection != null)
            {
                // Single Account in config

                string password = DecryptData(passwordSection.Value);

                Accounts.Add("Default Account", password);
                InitAccount("Default Account", password);

                // Remove from config
                Config.AppSettings.Settings.Remove("encPass");
                Config.Save(ConfigurationSaveMode.Modified, true);

                // Save to registry
                SaveAccounts();
            }
        }

        private void InitAccount(string accountName, string key)
        {
            ToolStripItem accountMenuItem = new ToolStripMenuItem(accountName, null, mnuAccount_Click);
            ToolStripItem timeoutMenuItem = new ToolStripMenuItem(Generator.GenerateTimeoutCode(key), null, mnuTimeoutMenuItem_Click);
            ToolStripItem separator = new ToolStripSeparator();

            TimeoutMenuItems.Add(timeoutMenuItem);

            accountMenuItem.Tag = timeoutMenuItem;
            timeoutMenuItem.Tag = key;

            ContextMenu.Items.Insert(0, separator);
            ContextMenu.Items.Insert(0, timeoutMenuItem);
            ContextMenu.Items.Insert(0, accountMenuItem);

            tmrMain.Enabled = true;
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

        private void SaveAccounts()
        {
            if (Accounts.Count > 0)
            {
                string accountsData = EncryptData(GenerateAccountsData(Accounts));
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "encAccounts", accountsData, RegistryValueKind.String);
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "PatternEnabled", patternEnabled ? 1 : 0, RegistryValueKind.DWord);
            }
            else
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "encAccounts", String.Empty, RegistryValueKind.String);
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\LCGoogleApps", "PatternEnabled", patternEnabled ? 1 : 0, RegistryValueKind.DWord);
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
            return Encryption.CreateCryptoAlgorithm(patternEnabled ? Encryption.GeneratePasswordHash(pattern, sid) : sid, "LCGoogleApps");
        }

        private SymmetricAlgorithm CreateOldAlgorithm()
        {
            string sid = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;
            return Encryption.CreateCryptoAlgorithm(patternEnabled ? pattern + sid : sid, "LCGoogleApps");
        }

        #endregion Methods
    }
}
