using Authentiqr.Core;
using Authentiqr.NET.Code;
using Authentiqr.NET.Code.EncryptionV3;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Authentiqr.NET
{
    public partial class frmMain : Form, IIconFinder
    {
        #region Properties

        private Settings settings;
        private Authenticator generator = new Authenticator();
        private List<ToolStripItem> timeoutMenuItems = new List<ToolStripItem>();

        #endregion Properties

        #region Constructor

        public frmMain()
        {
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.Manual;
            this.AutoScaleBaseSize = new Size(5, 13);
            this.FormBorderStyle = FormBorderStyle.None;
            this.Enabled = false;
            this.AccessibleRole = AccessibleRole.None;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = SizeGripStyle.Hide;
            this.ControlBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new Size(1, 7);
            this.Opacity = 0.0d;

            InitializeComponent();
        }

        #endregion Constructor

        #region .NET Events

        private void frmMain_Load(object sender, EventArgs e)
        {
            settings = new Settings();
            settings.Load();

            if (settings.StartupPrompt)
            {
                StartupPrompt();
            }

            if (settings.EncryptionMode == EncryptionMode.Basic)
            {
                mnuUnlockOrSetPassword.Text = "Set Password";
                settings.Unlock();
                AddAccounts();
            }
            else
            {
                mnuAddAccount.Enabled = false;
            }

            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLock:
                case SessionSwitchReason.SessionLogoff:
                case SessionSwitchReason.RemoteDisconnect:
                case SessionSwitchReason.ConsoleDisconnect:
                    // Lock the 2FA data when the machine is locked
                    Lock();
                    break;
            }
        }

        private void tmrMain_Tick(object sender, EventArgs e)
        {
            foreach (var item in timeoutMenuItems)
            {
                var account = item.Tag as string;

                if (settings.Accounts.ContainsKey(account))
                {
                    item.Text = generator.GenerateCode(settings.Accounts[account]);
                }
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

            using (frmAccount form = new frmAccount(settings, this))
            {
                form.ShowRemove(true);
                form.AccountName = oldAccountName;
                form.Key = settings.Accounts[oldAccountName].Use(p => p);
                DialogResult result = form.ShowDialog(this);

                if (result == DialogResult.OK)
                {
                    settings.Accounts.Remove(oldAccountName);

                    string accountName = form.AccountName;

                    if (String.IsNullOrEmpty(accountName) == false)
                    {
                        settings.Accounts[accountName] = new SecureString().AppendChars(form.Key);

                        accountMenuItem.Text = accountName;
                        timeoutMenuItem.Tag = accountName;
                    }
                    else
                    {
                        timeoutMenuItems.Remove(timeoutMenuItem);
                        var ix = contextMenu.Items.IndexOf(accountMenuItem);
                        contextMenu.Items.RemoveAt(ix + 2); // remove separator
                        contextMenu.Items.RemoveAt(ix + 1); // remove timeout password
                        contextMenu.Items.RemoveAt(ix); // remove account name
                    }

                    settings.SaveSettings();
                    settings.SaveAccounts();
                }
            }
        }

        private void mnuAddAccount_Click(object sender, EventArgs e)
        {
            using (frmAccount form = new frmAccount(settings, this))
            {
                DialogResult result = form.ShowDialog(this);

                if (result == DialogResult.OK)
                {
                    string password = Regex.Replace(form.Key, "\\s", "");
                    string accountName = form.AccountName;

                    settings.Accounts[accountName] = new SecureString().AppendChars(password);

                    settings.SaveSettings();
                    settings.SaveAccounts();

                    AddAccount(accountName, settings.Accounts[accountName]);
                }
            }
        }

        private void mnuExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void mnuUnlockOrSetPassword_Click(object sender, EventArgs e)
        {
            if (settings.Locked)
            {
                Unlock();
            }
            else
            {
                SetPassword();
            }
        }

        #endregion User Events

        #region Methods

        private void AddAccounts()
        {
            foreach (var account in settings.Accounts.OrderByDescending(f => f.Key.ToLower()))
            {
                AddAccount(account.Key, account.Value);
            }
        }

        private void AddAccount(string accountName, SecureString key)
        {
            ToolStripItem accountMenuItem = new ToolStripMenuItem(accountName, FindImage(accountName), mnuAccount_Click);
            ToolStripItem timeoutMenuItem = new ToolStripMenuItem(generator.GenerateCode(key), null, mnuTimeoutMenuItem_Click);
            ToolStripItem separator = new ToolStripSeparator();

            timeoutMenuItems.Add(timeoutMenuItem);

            accountMenuItem.Tag = timeoutMenuItem;
            timeoutMenuItem.Tag = accountName;

            contextMenu.Items.Insert(0, separator);
            contextMenu.Items.Insert(0, timeoutMenuItem);
            contextMenu.Items.Insert(0, accountMenuItem);

            tmrMain.Enabled = true;
        }

        public Image FindImage(string accountName)
        {
            var accountNameLower = accountName.ToLower();

            foreach (var key in imageList.Images.Keys)
            {
                if (accountNameLower.StartsWith(key))
                {
                    return imageList.Images[key];
                }
            }

            return null;
        }

        private void Lock()
        {
            for (int i = 0; i < settings.Accounts.Count * 3; i++)
            {
                contextMenu.Items.RemoveAt(0);
            }
            settings.Lock();
            timeoutMenuItems.Clear();
            tmrMain.Enabled = false;
            mnuUnlockOrSetPassword.Text = "Unlock";
            mnuAddAccount.Enabled = false;
        }

        private void Unlock()
        {
            try
            {
                if (settings.EncryptionMode == EncryptionMode.Pattern ||
                    settings.EncryptionMode == EncryptionMode.PatternAndPassword)
                {
                    var pattern = GetPattern();
                    if (pattern == null) return;
                    settings.SetPattern(pattern);
                }

                if (settings.EncryptionMode == EncryptionMode.Password ||
                    settings.EncryptionMode == EncryptionMode.PatternAndPassword)
                {
                    var password = GetPassword();
                    if (password == null) return;
                    settings.SetPassword(password);
                }

                // Perform unlock
                settings.Unlock();
                AddAccounts();
                mnuUnlockOrSetPassword.Text = "Set Password";
                mnuAddAccount.Enabled = true;

                if (settings.EncryptionUpgradeRequired)
                {
                    settings.SaveAccounts();
                }
            }
            catch (CryptographicException)
            {
                MessageBox.Show("Unable to decrypt", "Authentiqr.NET", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (ChecksumValidationException)
            {
                MessageBox.Show("Unable to decrypt due to invalid checksum", "Authentiqr.NET", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetPassword()
        {
            bool patternSet = false;
            bool passwordSet = false;

            settings.SetPattern(null);
            settings.SetPassword(null);

            if (MessageBox.Show("Use pattern lock?", "Set Password", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var pattern = GetPattern();
                if (pattern == null) return;
                settings.SetPattern(pattern);
                patternSet = true;
            }

            if (MessageBox.Show("Use password?", "Set Password", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var password = CreatePassword();
                if (password == null) return;
                settings.SetPassword(password);
                passwordSet = true;
            }

            if (!patternSet && !passwordSet && MessageBox.Show("Are you sure you want to use basic encryption only?", "Set Password", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
            {
                return;
            }

            settings.SaveAccounts();

            MessageBox.Show($"Password set to the following mode:\r\n\r\n- {settings.EncryptionMode.GetDescription()}", "Password Set", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private SecureString GetPattern()
        {
            using (frmPatternLock form = new frmPatternLock(settings))
            {
                return form.ShowDialog(this) == DialogResult.OK ? form.GetPattern() : null;
            }
        }

        private SecureString GetPassword(string prompt = "Enter Password")
        {
            using (frmPassword form = new frmPassword(settings, prompt))
            {
                return form.ShowDialog(this) == DialogResult.OK ? form.GetPassword() : null;
            }
        }

        private SecureString CreatePassword()
        {
            do
            {
                var password = GetPassword();

                if (password == null)
                {
                    return null;
                }

                var confirm = GetPassword("Confirm Password");

                if (confirm == null)
                {
                    return null;
                }
                else if (password.Use(pwd => confirm.Use(cnf => pwd == cnf)))
                {
                    return password;
                }
                else
                {
                    MessageBox.Show("Passwords do not match", "Set Password", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            while (true);
        }

        private void StartupPrompt()
        {
            var response = MessageBox.Show("Would you like to run Authentiqr.NET on startup?", "Authentiqr.NET", MessageBoxButtons.YesNo);

            if (response == DialogResult.Yes)
            {
                settings.RunOnWindowsStartup();
            }

            settings.StartupPrompt = false;
            settings.SaveSettings();
        }

        #endregion Methods
    }
}
