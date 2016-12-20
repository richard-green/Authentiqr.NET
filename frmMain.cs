using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Authentiqr.NET.Code;

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

            using (frmAddAccount form = new frmAddAccount(settings))
            {
                form.ShowRemove(true);
                form.AccountName = oldAccountName;
                form.SetKey(oldPassword);
                DialogResult result = form.ShowDialog(this);

                if (result == DialogResult.OK)
                {
                    settings.Accounts.Remove(oldAccountName);

                    string password = form.Key;
                    string accountName = form.AccountName;

                    if (String.IsNullOrEmpty(accountName) == false &&
                        String.IsNullOrEmpty(password) == false)
                    {
                        settings.Accounts[accountName] = password;

                        accountMenuItem.Text = accountName;
                        timeoutMenuItem.Tag = password;
                    }
                    else
                    {
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
            using (frmAddAccount form = new frmAddAccount(settings))
            {
                DialogResult result = form.ShowDialog(this);

                if (result == DialogResult.OK)
                {
                    string password = Regex.Replace(form.Key, "\\s", "");
                    string accountName = form.AccountName;

                    settings.Accounts[accountName] = password;

                    settings.SaveSettings();
                    settings.SaveAccounts();

                    AddAccount(accountName, password);
                }
            }
        }

        private void mnuExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void mnuLockUnlock_Click(object sender, EventArgs e)
        {
            using (frmPatternLock form = new frmPatternLock(settings))
            {
                DialogResult result = form.ShowDialog(this);

                if (result == DialogResult.OK)
                {
                    if (settings.EncryptionMode == EncryptionMode.Pattern)
                    {
                        try
                        {
                            // Perform unlock
                            settings.SetPattern(form.GetPattern());
                            settings.Unlock();
                            AddAccounts();
                            mnuLockUnlock.Visible = false;
                            mnuAddAccount.Enabled = true;
                        }
                        catch (CryptographicException)
                        {
                            MessageBox.Show("Invalid Pattern");
                        }
                    }
                    else
                    {
                        // Perform first lock
                        mnuLockUnlock.Visible = false;
                        settings.SetPattern(form.GetPattern());
                        settings.SaveAccounts();
                    }
                }

                settings.SaveSettings();
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

        private void AddAccount(string accountName, string key)
        {
            ToolStripItem accountMenuItem = new ToolStripMenuItem(accountName, FindImage(accountName), mnuAccount_Click);
            ToolStripItem timeoutMenuItem = new ToolStripMenuItem(Generator.GenerateTimeoutCode(key), null, mnuTimeoutMenuItem_Click);
            ToolStripItem separator = new ToolStripSeparator();

            TimeoutMenuItems.Add(timeoutMenuItem);

            accountMenuItem.Tag = timeoutMenuItem;
            timeoutMenuItem.Tag = key;

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

            return null;
        }

        #endregion Methods
    }
}
