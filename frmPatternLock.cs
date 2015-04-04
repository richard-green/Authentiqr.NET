using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace LCGoogleApps
{
    public partial class frmPatternLock : Form
    {
        bool mouseDown = false;
        List<int> pattern = new List<int>();
        private Settings settings;
        private bool constructing = true;

        public frmPatternLock(Settings settings)
        {
            InitializeComponent();
            this.settings = settings;
            this.StartPosition = FormStartPosition.Manual;
            this.Top = settings.PatternWindowTop;
            this.Left = settings.PatternWindowLeft;
            constructing = false;
        }

        public string GetPattern()
        {
            return String.Join("", pattern.Select(i => i.ToString()).ToArray());
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDown = true;
            pattern.Clear();
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                for (int i = 0; i < 9; i++)
                {
                    int x = i % 3;
                    int y = i / 3;
                    int px = 10 + x * 75;
                    int py = 10 + y * 75;

                    if (px < e.X && e.X < px + 50 &&
                        py < e.Y && e.Y < py + 50)
                    {
                        if (pattern.Contains(i) == false)
                        {
                            pattern.Add(i);
                            pictureBox1.Refresh();
                        }
                    }
                }
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.Black, e.ClipRectangle);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            for (int i = 0; i < 9; i++)
            {
                bool selected = pattern.Contains(i);
                int x = i % 3;
                int y = i / 3;
                int px = 10 + x * 75;
                int py = 10 + y * 75;
                e.Graphics.DrawEllipse(selected ? new Pen(Brushes.White, 3.5f) : new Pen(Brushes.Silver, 2.5f), new Rectangle(px, py, 50, 50));
            }
        }

        private void frmPatternLock_Move(object sender, EventArgs e)
        {
            if (constructing == false)
            {
                settings.PatternWindowTop = this.Top;
                settings.PatternWindowLeft = this.Left;
            }
        }
    }
}
