using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Drawing.Drawing2D;
using System.Web;
using System.IO;

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

        public string Message
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

        private void frmAddAccount_Load(object sender, EventArgs e)
        {
            this.AllowDrop = true;
        }

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
                DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show(String.Format("Invalid password, please try again:\r\n\r\n{0}", Message));
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(String.Format("Are you sure you want to remove the following account?\r\n\r\n{0}", AccountName),
                                "Remove Account",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Exclamation) == DialogResult.Yes)
            {
                Key = "";
                AccountName = "";
                DialogResult = DialogResult.OK;
            }
        }

        private void txtKey_KeyUp(object sender, KeyEventArgs e)
        {
            SetKey(txtKey.Text);
        }

        private void frmAddAccount_DragDrop(object sender, DragEventArgs e)
        {
            var formats = e.Data.GetFormats();

            try
            {
                if (formats.Contains("DragImageBits"))
                {
                    var stream = (MemoryStream)e.Data.GetData("DragImageBits");
                    var bitmap = new Bitmap(stream);
                    ReadBitmap(bitmap);
                }
                else if (formats.Contains("FileDrop"))
                {
                    var filename = ((string[])e.Data.GetData("FileDrop"))[0];
                    var bitmap = new Bitmap(Bitmap.FromFile(filename));
                    ReadBitmap(bitmap);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not read QR Code", "LCGoogleApps", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void frmAddAccount_DragEnter(object sender, DragEventArgs e)
        {
            var formats = e.Data.GetFormats();

            if (formats.Contains("FileDrop"))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        #endregion User Events

        #region Methods

        public void ShowRemove(bool visible)
        {
            btnRemove.Visible = visible;
            btnRemove.Enabled = visible;
        }

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
            catch (Exception ex)
            {
                lblCode.Visible = false;
                tmrMain.Enabled = false;
                IsValid = false;
                Message = ex.Message;
                txtKey.ForeColor = Color.Red;
            }
        }

        public void RenderQRCode(string accountName, string key)
        {
            RenderQRCode(String.Format("otpauth://totp/{0}?secret={1}", accountName, key));
        }

        public void RenderQRCode(string otpauth)
        {
            var writer = new ZXing.BarcodeWriter
            {
                Format = ZXing.BarcodeFormat.QR_CODE
            };

            var newBitmap = writer.Write(otpauth);
            pbQRCode.Image = ResizeImage(newBitmap, new Size(300, 300));
        }

        private void ReadBitmap(Bitmap bitmap)
        {
            var reader = new ZXing.BarcodeReader();
            var decoded = reader.Decode(bitmap);

            RenderQRCode(decoded.Text);

            Regex otpInfo = new Regex(@"otpauth://totp/(.*)\?secret=([^&]+)(&.*)?");
            if (otpInfo.IsMatch(decoded.Text))
            {
                var match = otpInfo.Match(decoded.Text);
                txtAccountName.Text = HttpUtility.UrlDecode(match.Groups[1].Value);
                SetKey(match.Groups[2].Value);
            }
        }

        private Bitmap ResizeImage(Bitmap imgToResize, Size size, InterpolationMode mode = InterpolationMode.NearestNeighbor)
        {
            try
            {
                Bitmap b = new Bitmap(size.Width, size.Height);
                using (Graphics g = Graphics.FromImage((Image)b))
                {
                    g.InterpolationMode = mode;
                    g.DrawImage(imgToResize, 0, 0, size.Width, size.Height);
                }
                return b;
            }
            catch
            {
                return imgToResize;
            }
        }

        #endregion Methods
    }
}
