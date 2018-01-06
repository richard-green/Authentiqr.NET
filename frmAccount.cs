using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows.Forms;
using Authentiqr.NET.Code;

namespace Authentiqr.NET
{
    public partial class frmAccount : Form
    {
        #region Properties

        private Settings settings;
        private bool constructing = true;
        private PasscodeGenerator generator = new PasscodeGenerator();
        private IIconFinder iconFinder;

        public string AccountName
        {
            get
            {
                return txtAccountName.Text;
            }
            set
            {
                txtAccountName.Text = value;
                pbIcon.Image = iconFinder.FindImage(txtAccountName.Text);
            }
        }

        public string Key
        {
            get
            {
                return txtKey.Text;
            }
            set
            {
                txtKey.Text = Regex.Replace(value, "\\s", "");
                Message = "";

                try
                {
                    if (String.IsNullOrEmpty(txtKey.Text))
                    {
                        lblCode.Text = "------";
                        IsKeyValid = false;
                        Message = "Password is blank";
                        return;
                    }

                    lblCode.Text = generator.GenerateTimeoutCode(txtKey.Text);
                    IsKeyValid = true;
                    tmrMain.Enabled = true;
                    txtKey.ForeColor = Color.Black;
                    btnOK.Enabled = true;
                }
                catch (Exception ex)
                {
                    lblCode.Text = "------";
                    tmrMain.Enabled = false;
                    IsKeyValid = false;
                    Message = ex.Message;
                    txtKey.ForeColor = Color.Red;
                    btnOK.Enabled = false;
                }
                finally
                {
                    RenderQRCode();
                }
            }
        }

        public bool IsKeyValid { get; set; }

        public string Message { get; set; }

        #endregion Properties

        #region Constructor

        public frmAccount(Settings settings, IIconFinder iconFinder)
        {
            InitializeComponent();
            this.settings = settings;
            this.iconFinder = iconFinder;
            this.StartPosition = FormStartPosition.Manual;
            this.Top = settings.AccountWindowTop;
            this.Left = settings.AccountWindowLeft;
            lblCode.Text = "------";
            constructing = false;
        }

        #endregion Constructor

        #region .NET Events

        private void frmAddAccount_Load(object sender, EventArgs e)
        {
            this.AllowDrop = true;
        }

        private void tmrMain_Tick(object sender, EventArgs e)
        {
            if (IsKeyValid)
            {
                lblCode.Text = generator.GenerateTimeoutCode(Key);
            }
            else
            {
                lblCode.Text = "------";
            }
        }

        #endregion .NET Events

        #region User Events

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (IsKeyValid)
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
            ChooseIcon();
        }

        private void txtKey_KeyUp(object sender, KeyEventArgs e)
        {
            Key = txtKey.Text;
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
                MessageBox.Show("Could not read QR Code\r\n\r\n" + ex.Message + "\r\n\r\n" + ex.ToString(), "Authentiqr.NET", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if (IsKeyValid)
            {
                saveFileDialog.FileName = String.Format("{0}.png", AccountName);
                saveFileDialog.ShowDialog();
            }
        }

        private void saveFileDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            pbQRCode.Image.Save(saveFileDialog.FileName);
        }

        private void frmAddAccount_Move(object sender, EventArgs e)
        {
            if (constructing == false)
            {
                settings.AccountWindowTop = this.Top;
                settings.AccountWindowLeft = this.Left;
                settings.SaveSettings();
            }
        }

        #endregion User Events

        #region Methods

        public void ShowRemove(bool visible)
        {
            btnRemove.Visible = visible;
            btnRemove.Enabled = visible;
        }

        public void RenderQRCode()
        {
            if (IsKeyValid)
            {
                var writer = new ZXing.BarcodeWriter
                {
                    Format = ZXing.BarcodeFormat.QR_CODE
                };

                var otpauth = String.Format("otpauth://totp/{0}?secret={1}", AccountName, Key);
                var newBitmap = writer.Write(otpauth);
                pbQRCode.Image = ResizeImage(newBitmap, new Size(300, 300));
            }
            else
            {
                System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmAccount));
                pbQRCode.Image = (Image)resources.GetObject("pbQRCode.Image");
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
                    AccountName = HttpUtility.UrlDecode(account);
                    Key = secret;
                }
                else
                {
                    MessageBox.Show("The QR Code does not contain valid OAuth data", "Authentiqr.NET", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("The QR Code could not be read", "Authentiqr.NET", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void ChooseIcon()
        {
            pbIcon.Image = iconFinder.FindImage(AccountName);
        }

        #endregion Methods
    }
}
