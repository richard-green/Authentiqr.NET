using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace LCGoogleApps
{
	public partial class frmAddAccount : Form
	{
		#region Properties

		public string AccountName
		{
			get { return txtAccountName.Text; }
			set { txtAccountName.Text = value; }
		}

		public string Key
		{
			get { return txtKey.Text; }
			private set { txtKey.Text = value; }
		}

		public bool IsValid
		{
			get;
			set;
		}

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

		#endregion Properties

		#region Constructor

		public frmAddAccount()
		{
			InitializeComponent();
		}

		#endregion Constructor

		#region .NET Events

		private void tmrMain_Tick(object sender, EventArgs e)
		{
			if (IsValid)
			{
				lblCode.Text = Generator.GenerateTimeoutCode(Key);
			}
		}

		#endregion .NET Events

		#region User Events

		private void btnOK_Click(object sender, EventArgs e)
		{
			if (IsValid)
			{
				DialogResult = System.Windows.Forms.DialogResult.OK;
			}
			else
			{
				MessageBox.Show("Invalid password, please try again");
			}
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = System.Windows.Forms.DialogResult.Cancel;
		}

		private void txtKey_KeyUp(object sender, KeyEventArgs e)
		{
			SetKey(txtKey.Text);
		}

		#endregion User Events

		#region Methods

		public void SetKey(string key)
		{
			Key = Regex.Replace(key, "\\s", "");

			try
			{
				lblCode.Text = Generator.GenerateTimeoutCode(Key);
				lblCode.Visible = true;
				IsValid = true;
				tmrMain.Enabled = true;
				txtKey.ForeColor = Color.Black;
			}
			catch (Exception)
			{
				lblCode.Visible = false;
				tmrMain.Enabled = false;
				IsValid = false;
				txtKey.ForeColor = Color.Red;
			}
		}

		#endregion Methods
	}
}
