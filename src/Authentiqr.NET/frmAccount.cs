using Authentiqr.Core;
using Authentiqr.Core.Encode;
using Authentiqr.NET.Code;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows.Forms;
using ZXing;
using ZXing.Windows.Compatibility;

namespace Authentiqr.NET
{
    public partial class frmAccount : Form
    {
        #region Properties

        private readonly Settings settings;
        private readonly bool constructing = true;
        private readonly Authenticator generator = new();
        private readonly IIconFinder iconFinder;

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
                    if (string.IsNullOrEmpty(txtKey.Text))
                    {
                        lblCode.Text = "------";
                        IsKeyValid = false;
                        Message = "Password is blank";
                        return;
                    }

                    lblCode.Text = generator.GenerateCode(txtKey.Text);
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
                lblCode.Text = generator.GenerateCode(Key);
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
                MessageBox.Show(string.Format("Invalid password, please try again:\r\n\r\n{0}", Message));
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(string.Format("Are you sure you want to remove the following account?\r\n\r\n{0}", AccountName),
                                "Remove Account",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Exclamation) == DialogResult.Yes)
            {
                Key = "";
                AccountName = "";
                DialogResult = DialogResult.OK;
            }
        }

        private void txtAccountName_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Control && e.KeyCode == Keys.V) ||
                (e.Shift && e.KeyCode == Keys.Insert))
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        var text = Clipboard.GetText();

                        if (ParseOtpAuth(text))
                        {
                            e.SuppressKeyPress = true;
                        }
                    }
                    else if (Clipboard.ContainsImage())
                    {
                        var image = Clipboard.GetImage();

                        ReadBitmap(new Bitmap(image));

                        e.SuppressKeyPress = true;
                    }
                    else if (Clipboard.ContainsFileDropList())
                    {
                        var files = Clipboard.GetFileDropList();
                        var filename = files[0];

                        using var image = Image.FromFile(filename);
                        using var bitmap = new Bitmap(image);
                        ReadBitmap(bitmap);

                        e.SuppressKeyPress = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Failed to paste from clipboard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
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

                    using var image = Image.FromFile(filename);
                    using var bitmap = new Bitmap(image);
                    ReadBitmap(bitmap);
                }
                else if (formats.Contains("Text"))
                {
                    var text = (string)e.Data.GetData("Text");

                    if (!ParseOtpAuth(text) && !ReadAsImage(text))
                    {
                        MessageBox.Show("Could not read QR Code", "Authentiqr.NET", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            if (formats.Contains("FileDrop") ||
                formats.Contains("Text"))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void pbQRCode_DoubleClick(object sender, EventArgs e)
        {
            if (IsKeyValid)
            {
                saveFileDialog.FileName = string.Format("{0}.png", AccountName);
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

        private void lblCode_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(lblCode.Text);
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
                var writer = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE
                };

                var otpauth = string.Format("otpauth://totp/{0}?secret={1}", AccountName, Key);
                var bitmap = writer.Write(otpauth);

                pbQRCode.Image = ResizeImage(bitmap, new Size(300, 300));
            }
            else
            {
                var resources = new ComponentResourceManager(typeof(frmAccount));
                pbQRCode.Image = (Image)resources.GetObject("pbQRCode.Image");
            }
        }

        private void ReadBitmap(Bitmap bitmap)
        {
            var reader = new BarcodeReader();
            reader.Options.TryHarder = true;
            reader.AutoRotate = true;

            var decoded = reader.Decode(bitmap);

            if (decoded != null)
            {
                if (!ParseOtpAuth(decoded.Text))
                {
                    MessageBox.Show("The QR Code does not contain valid OAuth data", "Authentiqr.NET", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("The QR Code could not be read", "Authentiqr.NET", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ParseOtpAuth(string text)
        {
            if (Uri.TryCreate(text, UriKind.Absolute, out var uri) && uri.Scheme.Equals("otpauth", StringComparison.CurrentCultureIgnoreCase))
            {
                var queryString = HttpUtility.ParseQueryString(uri.Query);
                var secret = queryString["secret"];
                var issuer = queryString["issuer"];
                var account = uri.LocalPath.StartsWith("/") ? uri.LocalPath[1..] : uri.LocalPath;
                var accountName = HttpUtility.UrlDecode(account);

                if (accountName.Contains(':'))
                {
                    var accountDetailsCapture = new Regex(@"([^:]+):\s*([^\s]+)");
                    var accountDetails = accountDetailsCapture.Match(accountName);

                    if (accountDetails.Success)
                    {
                        accountName = $"{accountDetails.Groups[1].Value}: {accountDetails.Groups[2].Value}";
                    }
                }
                else if (!string.IsNullOrEmpty(issuer))
                {
                    accountName = $"{issuer} - {accountName}";
                }

                AccountName = accountName;
                Key = secret;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool ReadAsImage(string text)
        {
            try
            {
                // Text might be a base64 image
                var match = Regex.Match(text, @"^data\:(?<MediaType>.*)(;base64)?,(?<Data>.*)$");

                if (match.Success)
                {
                    var mediaType = match.Groups["MediaType"].Value;
                    var data = match.Groups["Data"].Value;
                    var imageBytes = Base64.Decode(data);
                    var image = Image.FromStream(new MemoryStream(imageBytes));
                    using var bitmap = new Bitmap(image);
                    ReadBitmap(bitmap);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static Bitmap ResizeImage(Bitmap imgToResize, Size size, InterpolationMode mode = InterpolationMode.NearestNeighbor)
        {
            try
            {
                var b = new Bitmap(size.Width, size.Height);
                using (Graphics g = Graphics.FromImage(b))
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
