using Authentiqr.NET.Code;
using System;
using System.Security;
using System.Windows.Forms;

namespace Authentiqr.NET
{
    public partial class frmPassword : Form
    {
        private bool constructing = true;
        private Settings settings;

        public frmPassword(Settings settings, string prompt)
        {
            InitializeComponent();
            this.settings = settings;
            this.Text = prompt;
            this.StartPosition = FormStartPosition.Manual;
            this.Top = settings.PasswordWindowTop;
            this.Left = settings.PasswordWindowLeft;
            this.constructing = false;
        }

        public SecureString GetPassword()
        {
            var result = new SecureString();
            result.AppendChars(txtPassword.Text);
            return result;
        }

        private void frmPassword_Move(object sender, EventArgs e)
        {
            if (constructing == false)
            {
                settings.PasswordWindowTop = this.Top;
                settings.PasswordWindowLeft = this.Left;
                settings.SaveSettings();
            }
        }
    }
}
