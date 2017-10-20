using Authentiqr.NET.Code;
using System;
using System.Security;
using System.Windows.Forms;

namespace Authentiqr.NET
{
    public partial class frmPassword : Form
    {
        private Settings settings;

        public frmPassword(Settings settings, string prompt)
        {
            InitializeComponent();
            this.settings = settings;
            this.Text = prompt;
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        public SecureString GetPassword()
        {
            var result = new SecureString();
            result.AppendChars(textBox1.Text);
            return result;
        }
    }
}
