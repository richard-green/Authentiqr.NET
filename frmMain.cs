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
			// Get password from App.config
			DecryptConfigSection("appSettings");
			var passwordSection = Config.AppSettings.Settings["password"];

			if (passwordSection != null)
			{
				string password = Config.AppSettings.Settings["password"].Value;
				SetPassword(password);
			}
			else
			{
				mnuPassCode.Visible = false;
				tmrMain.Enabled = false;
			}
		}

		private void tmrMain_Tick(object sender, EventArgs e)
		{
			mnuPassCode.Text = Generator.GenerateTimeoutCode();
		}

		#endregion .NET Events

		#region User Events

		private void mnuPassCode_Click(object sender, EventArgs e)
		{
			Clipboard.SetText(mnuPassCode.Text);
		}

		private void mnuSetKey_Click(object sender, EventArgs e)
		{
			InputBoxResult result = InputBox.Show(this, "Enter new key:");
			if (result.ReturnCode == System.Windows.Forms.DialogResult.OK)
			{
				string password = Regex.Replace(result.Text, "\\s", "");
				ChangePassword(password);
			}
		}

		private void mnuExit_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		#endregion User Events

		#region Methods

		private void EncryptConfigSection(string sectionKey)
		{
			ConfigurationSection section = Config.GetSection(sectionKey);
			if (section != null)
			{
				if (!section.SectionInformation.IsProtected)
				{
					if (!section.ElementInformation.IsLocked)
					{
						section.SectionInformation.ProtectSection("DataProtectionConfigurationProvider");
						section.SectionInformation.ForceSave = true;
						config.Save(ConfigurationSaveMode.Full);
					}
				}
			}
		}

		private void DecryptConfigSection(string sectionKey)
		{
			ConfigurationSection section = Config.GetSection(sectionKey);
			if (section != null)
			{
				if (section.SectionInformation.IsProtected)
				{
					if (section.ElementInformation.IsLocked)
					{
						section.SectionInformation.UnprotectSection();
						//section.SectionInformation.ForceSave = true;
						//config.Save(ConfigurationSaveMode.Full);
					}
				}
			}
		}

		private void UpdateConfig()
		{
			EncryptConfigSection("appSettings");

			// Save the configuration file.
			Config.Save(ConfigurationSaveMode.Modified, true);
		}

		private void SetPassword(string password)
		{
			try
			{
				Generator.SetPassword(password);
				mnuPassCode.Text = Generator.GenerateTimeoutCode();
				mnuPassCode.Visible = true;
				tmrMain.Enabled = true;
			}
			catch (Exception)
			{
				MessageBox.Show("Invalid key - please re-enter");
				mnuPassCode.Visible = false;
				tmrMain.Enabled = false;
				mnuSetKey.PerformClick();
			}
		}

		private void ChangePassword(string password)
		{
			if (Config.AppSettings.Settings["password"] == null)
			{
				Config.AppSettings.Settings.Add("password", password);
			}
			else
			{
				Config.AppSettings.Settings["password"].Value = password;
			}

			UpdateConfig();
			SetPassword(password);
		}
		
		#endregion Methods
	}
}
