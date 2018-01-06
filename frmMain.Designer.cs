namespace Authentiqr.NET
{
	partial class frmMain
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mnuAddAccount = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuUnlockOrSetPassword = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuExit = new System.Windows.Forms.ToolStripMenuItem();
            this.tmrMain = new System.Windows.Forms.Timer(this.components);
            this.imageList = new System.Windows.Forms.ImageList(this.components);
            this.contextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIcon
            // 
            this.notifyIcon.BalloonTipText = "Authentiqr.NET";
            this.notifyIcon.ContextMenuStrip = this.contextMenu;
            this.notifyIcon.Icon = global::Authentiqr.NET.Resources.Padlock;
            this.notifyIcon.Text = "Authentiqr.NET";
            this.notifyIcon.Visible = true;
            // 
            // contextMenu
            // 
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuAddAccount,
            this.toolStripMenuItem2,
            this.mnuUnlockOrSetPassword,
            this.mnuExit});
            this.contextMenu.Name = "contextMenuStrip1";
            this.contextMenu.Size = new System.Drawing.Size(145, 76);
            // 
            // mnuAddAccount
            // 
            this.mnuAddAccount.Name = "mnuAddAccount";
            this.mnuAddAccount.Size = new System.Drawing.Size(144, 22);
            this.mnuAddAccount.Text = "Add Account";
            this.mnuAddAccount.Click += new System.EventHandler(this.mnuAddAccount_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(141, 6);
            // 
            // mnuUnlockOrSetPassword
            // 
            this.mnuUnlockOrSetPassword.Name = "mnuUnlockOrSetPassword";
            this.mnuUnlockOrSetPassword.Size = new System.Drawing.Size(144, 22);
            this.mnuUnlockOrSetPassword.Text = "Unlock";
            this.mnuUnlockOrSetPassword.Click += new System.EventHandler(this.mnuUnlockOrSetPassword_Click);
            // 
            // mnuExit
            // 
            this.mnuExit.Name = "mnuExit";
            this.mnuExit.Size = new System.Drawing.Size(144, 22);
            this.mnuExit.Text = "Exit";
            this.mnuExit.Click += new System.EventHandler(this.mnuExit_Click);
            // 
            // tmrMain
            // 
            this.tmrMain.Enabled = true;
            this.tmrMain.Interval = 1000;
            this.tmrMain.Tick += new System.EventHandler(this.tmrMain_Tick);
            // 
            // imageList
            // 
            this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
            this.imageList.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList.Images.SetKeyName(0, "facebook");
            this.imageList.Images.SetKeyName(1, "google");
            this.imageList.Images.SetKeyName(2, "microsoft");
            this.imageList.Images.SetKeyName(3, "github");
            this.imageList.Images.SetKeyName(4, "dropbox");
            this.imageList.Images.SetKeyName(5, "uplay");
            this.imageList.Images.SetKeyName(6, "protonmail");
            this.imageList.Images.SetKeyName(7, "coinbase");
            this.imageList.Images.SetKeyName(8, "binance");
            this.imageList.Images.SetKeyName(9, "bitmex");
            this.imageList.Images.SetKeyName(10, "cryptonator");
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(212, 76);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = global::Authentiqr.NET.Resources.Padlock;
            this.Location = new System.Drawing.Point(-1000, 0);
            this.Name = "frmMain";
            this.ShowInTaskbar = false;
            this.Text = "Authentiqr.NET";
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.contextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.NotifyIcon notifyIcon;
		private System.Windows.Forms.ContextMenuStrip contextMenu;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem mnuExit;
		private System.Windows.Forms.Timer tmrMain;
		private System.Windows.Forms.ToolStripMenuItem mnuAddAccount;
        private System.Windows.Forms.ToolStripMenuItem mnuUnlockOrSetPassword;
        private System.Windows.Forms.ImageList imageList;
    }
}

