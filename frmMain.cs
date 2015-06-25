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
using System.Security;

namespace LCGoogleApps
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
            settings = new Settings();
            settings.LoadSettings();

            if (settings.PatternEnabled == false)
            {
                mnuUnlock.Text = "Lock Data";
                settings.LoadAccounts();
                InitAccounts();
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

                    settings.SaveAccounts();
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

                    settings.Accounts[accountName] = password;
                    settings.SaveAccounts();

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
            using (frmPatternLock form = new frmPatternLock(settings))
            {
                DialogResult result = form.ShowDialog(this);

                if (result == DialogResult.OK)
                {
                    if (settings.PatternEnabled)
                    {
                        try
                        {
                            // Perform unlock
                            settings.SetPattern(form.GetPattern());
                            settings.LoadAccounts();
                            InitAccounts();
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
                        mnuUnlock.Visible = false;

                        settings.PatternEnabled = true;
                        settings.SetPattern(form.GetPattern());
                        settings.SaveAccounts();
                    }
                }

                settings.SaveSettings();
            }
        }

        #endregion User Events

        #region Methods

        private void InitAccounts()
        {
            foreach (var account in settings.Accounts.OrderByDescending(f => f.Key.ToLower()))
            {
                InitAccount(account.Key, account.Value);
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

            contextMenu.Items.Insert(0, separator);
            contextMenu.Items.Insert(0, timeoutMenuItem);
            contextMenu.Items.Insert(0, accountMenuItem);

            tmrMain.Enabled = true;
        }

        #endregion Methods
    }
}
