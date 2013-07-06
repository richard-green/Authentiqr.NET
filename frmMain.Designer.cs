namespace LCGoogleApps
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
			this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
			this.ContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.mnuAddAccount = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.mnuExit = new System.Windows.Forms.ToolStripMenuItem();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.tmrMain = new System.Windows.Forms.Timer(this.components);
			this.ContextMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// notifyIcon1
			// 
			this.notifyIcon1.ContextMenuStrip = this.ContextMenu;
			this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
			this.notifyIcon1.Text = "LCGoogleApps";
			this.notifyIcon1.Visible = true;
			// 
			// ContextMenu
			// 
			this.ContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuAddAccount,
            this.toolStripMenuItem2,
            this.mnuExit});
			this.ContextMenu.Name = "contextMenuStrip1";
			this.ContextMenu.Size = new System.Drawing.Size(153, 76);
			// 
			// mnuAddAccount
			// 
			this.mnuAddAccount.Name = "mnuAddAccount";
			this.mnuAddAccount.Size = new System.Drawing.Size(152, 22);
			this.mnuAddAccount.Text = "Add Account";
			this.mnuAddAccount.Click += new System.EventHandler(this.mnuAddAccount_Click);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(149, 6);
			// 
			// mnuExit
			// 
			this.mnuExit.Name = "mnuExit";
			this.mnuExit.Size = new System.Drawing.Size(152, 22);
			this.mnuExit.Text = "Exit";
			this.mnuExit.Click += new System.EventHandler(this.mnuExit_Click);
			// 
			// imageList1
			// 
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
			this.imageList1.Images.SetKeyName(0, "shell32_246.ico");
			this.imageList1.Images.SetKeyName(1, "bullet_go.png");
			this.imageList1.Images.SetKeyName(2, "bullet_go.png");
			this.imageList1.Images.SetKeyName(3, "shell32_246.ico");
			this.imageList1.Images.SetKeyName(4, "Arrow Right.ico");
			// 
			// tmrMain
			// 
			this.tmrMain.Enabled = true;
			this.tmrMain.Interval = 1000;
			this.tmrMain.Tick += new System.EventHandler(this.tmrMain_Tick);
			// 
			// frmMain
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 262);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Location = new System.Drawing.Point(-1000, 0);
			this.Name = "frmMain";
			this.ShowInTaskbar = false;
			this.Text = "Google Apps for London & Colonial";
			this.Load += new System.EventHandler(this.frmMain_Load);
			this.ContextMenu.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.NotifyIcon notifyIcon1;
		private System.Windows.Forms.ContextMenuStrip ContextMenu;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem mnuExit;
		private System.Windows.Forms.ImageList imageList1;
		private System.Windows.Forms.Timer tmrMain;
		private System.Windows.Forms.ToolStripMenuItem mnuAddAccount;
	}
}

