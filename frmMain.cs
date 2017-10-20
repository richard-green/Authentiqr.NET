using Authentiqr.NET.Code;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Authentiqr.NET
{
    public partial class frmMain : Form
    {
        #region Properties

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

        private Settings settings;

        private List<ToolStripItem> TimeoutMenuItems = new List<ToolStripItem>();

        #endregion Properties

        #region Constructor

        public frmMain()
        {
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.Manual;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.FormBorderStyle = FormBorderStyle.None;
            this.Enabled = false;
            this.AccessibleRole = AccessibleRole.None;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = SizeGripStyle.Hide;
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
            settings = new Settings();
            settings.LoadSettings();

            if (settings.StartupPrompt)
            {
                StartupPrompt();
            }

            if (settings.EncryptionMode == EncryptionMode.Basic)
            {
                mnuLockUnlock.Text = "Lock Data";
                settings.Unlock();
                AddAccounts();
            }
            else
            {
                mnuAddAccount.Enabled = false;
            }

            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
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

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLock:
                case SessionSwitchReason.SessionLogoff:
                case SessionSwitchReason.RemoteDisconnect:
                case SessionSwitchReason.ConsoleDisconnect:
                    // Lock the 2FA data when the machine is locked
                    for (int i = 0; i < settings.Accounts.Count * 3; i++)
                    {
                        contextMenu.Items.RemoveAt(0);
                    }
                    settings.Lock();
                    TimeoutMenuItems.Clear();
                    tmrMain.Enabled = false;
                    mnuLockUnlock.Visible = true;
                    mnuAddAccount.Enabled = false;
                    break;
            }
        }

        private void tmrMain_Tick(object sender, EventArgs e)
        {
            foreach (var item in TimeoutMenuItems)
            {
                var account = item.Tag as string;

                if (settings.Accounts.ContainsKey(account))
                {
                    item.Text = Generator.GenerateTimeoutCode(settings.Accounts[account]);
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

            using (frmAccount form = new frmAccount(settings))
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
                        TimeoutMenuItems.Remove(timeoutMenuItem);
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
            using (frmAccount form = new frmAccount(settings))
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

        private void mnuLockUnlock_Click(object sender, EventArgs e)
        {
            if (settings.Locked)
            {
                try
                {
                    if (settings.EncryptionMode == EncryptionMode.Pattern ||
                        settings.EncryptionMode == EncryptionMode.PatternAndPassword)
                    {
                        settings.SetPattern(GetPattern());
                    }

                    if (settings.EncryptionMode == EncryptionMode.Password ||
                        settings.EncryptionMode == EncryptionMode.PatternAndPassword)
                    {
                        settings.SetPassword(GetPassword("Enter password"));
                    }

                    // Perform unlock
                    settings.Unlock();
                    AddAccounts();
                    mnuLockUnlock.Text = "Set Password";
                    mnuAddAccount.Enabled = true;
                }
                catch (CryptographicException)
                {
                    MessageBox.Show("Invalid Pattern");
                }
            }
            else
            {
                settings.SetPattern(null);
                settings.SetPassword(null);

                if (MessageBox.Show("Use pattern lock?", "Set Password", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    settings.SetPattern(GetPattern());
                }

                if (MessageBox.Show("Use password?", "Set Password", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    do
                    {
                        var pwd1 = GetPassword("Enter password");

                        if (pwd1.Length == 0)
                        {
                            break;
                        }

                        var pwd2 = GetPassword("Confirm password");

                        if (pwd1.Use(pwd1s => pwd2.Use(pwd2s => pwd1s == pwd2s)))
                        {
                            settings.SetPassword(pwd1);
                            break;
                        }
                        else
                        {
                            MessageBox.Show("Passwords do not match", "Set Password", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    while (true);
                }

                settings.SaveAccounts();
            }

            settings.SaveSettings();
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
            ToolStripItem timeoutMenuItem = new ToolStripMenuItem(Generator.GenerateTimeoutCode(key), null, mnuTimeoutMenuItem_Click);
            ToolStripItem separator = new ToolStripSeparator();

            TimeoutMenuItems.Add(timeoutMenuItem);

            accountMenuItem.Tag = timeoutMenuItem;
            timeoutMenuItem.Tag = accountName;

            contextMenu.Items.Insert(0, separator);
            contextMenu.Items.Insert(0, timeoutMenuItem);
            contextMenu.Items.Insert(0, accountMenuItem);

            tmrMain.Enabled = true;
        }

        private Image FindImage(string accountName)
        {
            var accountNameLower = accountName.ToLower();

            if (accountNameLower.StartsWith("facebook"))
            {
                return imageList.Images[0];
            }
            else if (accountNameLower.StartsWith("google"))
            {
                return imageList.Images[1];
            }
            else if (accountNameLower.StartsWith("microsoft"))
            {
                return imageList.Images[2];
            }
            else if (accountNameLower.StartsWith("github"))
            {
                return imageList.Images[3];
            }
            else if (accountNameLower.StartsWith("dropbox"))
            {
                return imageList.Images[4];
            }
            else if (accountNameLower.StartsWith("uplay"))
            {
                return imageList.Images[5];
            }
            else if (accountNameLower.StartsWith("protonmail"))
            {
                return imageList.Images[6];
            }

            return null;
        }

        private SecureString GetPattern()
        {
            using (frmPatternLock form = new frmPatternLock(settings))
            {
                return form.ShowDialog(this) == DialogResult.OK ? form.GetPattern() : null;
            }
        }

        private SecureString GetPassword(string prompt)
        {
            using (frmPassword form = new frmPassword(settings, prompt))
            {
                return form.ShowDialog(this) == DialogResult.OK ? form.GetPassword() : null;
            }
        }

        #endregion Methods
    }
}
