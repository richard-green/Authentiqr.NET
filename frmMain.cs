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

		protected Dictionary<string, string> Accounts = new Dictionary<string,string>();

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
			var passwordSection = Config.AppSettings.Settings["encPass"];

			if (passwordSection != null)
			{
				// Single Account

				string password = DecryptData(passwordSection.Value);

				Accounts.Add("Default Account", password);
				InitAccount("Default Account", password);

				Config.AppSettings.Settings.Remove("encPass");
				UpdateConfig();
			}
			else
			{
				// Multiple Accounts

				var accountsSection = Config.AppSettings.Settings["encAccounts"];

				if (accountsSection != null)
				{
					Accounts = ParseAccountsData(DecryptData(accountsSection.Value));

					foreach (var account in Accounts)
					{
						InitAccount(account.Key, account.Value);
					}
				}
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
				form.AccountName = oldAccountName;
				form.SetKey(oldPassword);
				DialogResult result = form.ShowDialog(this);

				if (result == DialogResult.OK)
				{
					Accounts.Remove(oldAccountName);

					string password = Regex.Replace(form.Key, "\\s", "");
					string accountName = form.AccountName;

					Accounts[accountName] = password;
					UpdateConfig();

					accountMenuItem.Text = accountName;
					timeoutMenuItem.Tag = password;
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
					UpdateConfig();

					InitAccount(accountName, password);
				}
			}
		}

		private void mnuExit_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		#endregion User Events

		#region Methods

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

		private void UpdateConfig()
		{
			string accountsData = EncryptData(GenerateAccountsData(Accounts));

			Configuration configuration = Config;

			if (configuration.AppSettings.Settings["encAccounts"] == null)
			{
				configuration.AppSettings.Settings.Add("encAccounts", accountsData);
			}
			else
			{
				configuration.AppSettings.Settings["encAccounts"].Value = accountsData;
			}

			// Save the configuration file.
			configuration.Save(ConfigurationSaveMode.Modified, true);
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
			return Encryption.CreateCryptoAlgorithm(sid, "LCGoogleApps");
		}
		
		#endregion Methods
	}
}
