using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows.Forms;
using LCGoogleApps.Code;

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
            else
            {
                lblCode.Text = "";
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

        private void txtAccountName_KeyUp(object sender, KeyEventArgs e)
        {
            RenderQRCode();
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
                if (formats.Contains("FileDrop"))
                {
                    var filename = ((string[])e.Data.GetData("FileDrop"))[0];

                    using (var image = Bitmap.FromFile(filename))
                    {
                        using (var bitmap = new Bitmap(image))
                        {
                            ReadBitmap(bitmap);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not read QR Code\r\n\r\n" + ex.Message + "\r\n\r\n" + ex.ToString(), "LCGoogleApps", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void pbQRCode_DoubleClick(object sender, EventArgs e)
        {
            saveFileDialog.FileName = String.Format("{0}.png", AccountName);
            saveFileDialog.ShowDialog();
        }

        private void saveFileDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            pbQRCode.Image.Save(saveFileDialog.FileName);
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
                if (String.IsNullOrEmpty(Key))
                {
                    IsValid = false;
                    return;
                }

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
            finally
            {
                RenderQRCode();
            }
        }

        public void RenderQRCode()
        {
            if (IsValid)
            {
                var writer = new ZXing.BarcodeWriter
                {
                    Format = ZXing.BarcodeFormat.QR_CODE
                };

                var otpauth = String.Format("otpauth://totp/{0}?secret={1}", AccountName, Key);
                var newBitmap = writer.Write(otpauth);
                pbQRCode.Image = ResizeImage(newBitmap, new Size(300, 300));
            }
        }

        private void ReadBitmap(Bitmap bitmap)
        {
            var reader = new ZXing.BarcodeReader();
            reader.Options.TryHarder = true;
            reader.AutoRotate = true;

            var decoded = reader.Decode(bitmap);

            if (decoded != null)
            {
                var text = decoded.Text;

                var uri = new Uri(text);

                if (uri.Scheme == "otpauth")
                {
                    var queryString = HttpUtility.ParseQueryString(uri.Query);
                    var secret = queryString["secret"];
                    var account = uri.LocalPath.StartsWith("/") ? uri.LocalPath.Substring(1) : uri.LocalPath;
                    txtAccountName.Text = HttpUtility.UrlDecode(account);
                    SetKey(secret);
                }
                else
                {
                    MessageBox.Show("The QR Code does not contain valid OAuth data", "LCGoogleApps", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("The QR Code could not be read", "LCGoogleApps", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
