using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AIMSetup
{
    public static class SetupTheme
    {
        public static readonly Font UiFont = new Font("Tahoma", 8.25f, FontStyle.Regular);
        public static readonly Font UiBold = new Font("Tahoma", 8.25f, FontStyle.Bold);
        public static readonly Font TitleFont = new Font("Trebuchet MS", 9.75f, FontStyle.Bold);
        public static readonly Font SmallFont = new Font("Tahoma", 7.5f, FontStyle.Regular);
        public static readonly Font WizardTitleFont = new Font("Verdana", 11.5f, FontStyle.Bold);

        public static readonly Color Face = Color.FromArgb(236, 233, 216);
        public static readonly Color FaceDark = Color.FromArgb(214, 210, 194);
        public static readonly Color FrameBorder = Color.FromArgb(0, 85, 234);
        public static readonly Color FrameInner = Color.FromArgb(8, 49, 217);
        public static readonly Color AimNavy = Color.FromArgb(0, 51, 102);
        public static readonly Color AimGold = Color.FromArgb(255, 204, 0);
    }

    public class XpSetupForm : Form
    {
        private bool _isActive = true;
        private int _hoverCap = -1; // 0=min, 1=close
        private int _downCap = -1;
        private bool _dragging = false;
        private Point _dragStartMouse;
        private Point _dragStartForm;

        public Panel ClientBody;
        public bool ShowMinimizeButton = true;
        public Image TitleIconImage = null;

        private const int TITLE_HEIGHT = 28;
        private const int BORDER_W = 3;

        public XpSetupForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;
            Font = SetupTheme.UiFont;
            BackColor = SetupTheme.Face;
            StartPosition = FormStartPosition.CenterScreen;
            Padding = new Padding(BORDER_W, TITLE_HEIGHT, BORDER_W, BORDER_W);
            Icon = SetupAssets.SetupIcon;
            TitleIconImage = SetupAssets.GetRunningManIcon(16);

            ClientBody = new Panel();
            ClientBody.Dock = DockStyle.Fill;
            ClientBody.BackColor = SetupTheme.Face;
            ClientBody.Font = SetupTheme.UiFont;
            Controls.Add(ClientBody);

            Activated += delegate { _isActive = true; Invalidate(); };
            Deactivate += delegate { _isActive = false; _hoverCap = -1; _downCap = -1; Invalidate(); };
            Resize += delegate { UpdateWindowRegion(); Invalidate(); };
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateWindowRegion();
        }

        private void UpdateWindowRegion()
        {
            if (Width < 20 || Height < 20)
            {
                Region = null;
                return;
            }
            using (GraphicsPath path = new GraphicsPath())
            {
                int r = 8;
                path.AddArc(0, 0, r * 2, r * 2, 180, 90);
                path.AddArc(Width - r * 2 - 1, 0, r * 2, r * 2, 270, 90);
                path.AddLine(Width - 1, r, Width - 1, Height - 1);
                path.AddLine(0, Height - 1, 0, r);
                path.CloseFigure();
                Region = new Region(path);
            }
        }

        private Rectangle[] GetCaptionRects()
        {
            int btnSize = 21;
            int y = 4;
            int xClose = Width - BORDER_W - btnSize - 3;
            int xMin = xClose - btnSize - 2;
            return new Rectangle[]
            {
                ShowMinimizeButton ? new Rectangle(xMin, y, btnSize, btnSize) : Rectangle.Empty,
                new Rectangle(xClose, y, btnSize, btnSize)
            };
        }

        private int HitTestCaption(Point p)
        {
            Rectangle[] rects = GetCaptionRects();
            for (int i = 0; i < rects.Length; i++)
            {
                if (!rects[i].IsEmpty && rects[i].Contains(p)) return i;
            }
            return -1;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Title bar XP Luna 8-stop gradient
            Rectangle titleRect = new Rectangle(0, 0, Width, TITLE_HEIGHT);
            if (_isActive)
            {
                using (LinearGradientBrush tb = new LinearGradientBrush(titleRect,
                    Color.FromArgb(9, 151, 255), Color.FromArgb(0, 61, 220), LinearGradientMode.Vertical))
                {
                    ColorBlend cb = new ColorBlend(8);
                    cb.Positions = new float[] { 0f, 0.14f, 0.25f, 0.42f, 0.85f, 0.90f, 0.96f, 1.0f };
                    cb.Colors = new Color[]
                    {
                        Color.FromArgb(0, 88, 238),
                        Color.FromArgb(53, 147, 255),
                        Color.FromArgb(40, 142, 255),
                        Color.FromArgb(18, 125, 255),
                        Color.FromArgb(3, 111, 254),
                        Color.FromArgb(0, 98, 239),
                        Color.FromArgb(0, 82, 214),
                        Color.FromArgb(0, 61, 220)
                    };
                    tb.InterpolationColors = cb;
                    g.FillRectangle(tb, titleRect);
                }
                using (Pen pSpec = new Pen(Color.FromArgb(120, 180, 225, 255)))
                {
                    g.DrawLine(pSpec, 6, 2, Width - 7, 2);
                }
            }
            else
            {
                using (LinearGradientBrush tb = new LinearGradientBrush(titleRect,
                    Color.FromArgb(118, 151, 231), Color.FromArgb(123, 154, 224), LinearGradientMode.Vertical))
                {
                    g.FillRectangle(tb, titleRect);
                }
            }

            // 3-layer XP Luna window frame borders
            Color outer = _isActive ? Color.FromArgb(0, 60, 165) : Color.FromArgb(117, 141, 203);
            Color mid = _isActive ? SetupTheme.FrameBorder : Color.FromArgb(123, 154, 224);
            Color inner = _isActive ? SetupTheme.FrameInner : Color.FromArgb(158, 183, 236);
            using (Pen p1 = new Pen(outer))
            using (Pen p2 = new Pen(mid))
            using (Pen p3 = new Pen(inner))
            {
                g.DrawRectangle(p1, 0, TITLE_HEIGHT - 1, Width - 1, Height - TITLE_HEIGHT);
                g.DrawRectangle(p2, 1, TITLE_HEIGHT - 1, Width - 3, Height - TITLE_HEIGHT - 1);
                g.DrawRectangle(p3, 2, TITLE_HEIGHT - 1, Width - 5, Height - TITLE_HEIGHT - 2);
            }

            // Title icon + text
            int textX = 8;
            if (TitleIconImage != null)
            {
                g.DrawImage(TitleIconImage, 7, 6, 16, 16);
                textX = 27;
            }
            int rightLimit = Width - 56;
            Rectangle textRect = new Rectangle(textX, 5, Math.Max(40, rightLimit - textX), 20);
            if (_isActive)
            {
                TextRenderer.DrawText(g, Text, SetupTheme.TitleFont,
                    new Rectangle(textRect.X + 1, textRect.Y + 1, textRect.Width, textRect.Height),
                    Color.FromArgb(15, 16, 137),
                    TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            }
            TextRenderer.DrawText(g, Text, SetupTheme.TitleFont, textRect,
                _isActive ? Color.White : Color.FromArgb(228, 236, 252),
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

            Rectangle[] capRects = GetCaptionRects();
            DrawCaptionButton(g, capRects[0], 0);
            DrawCaptionButton(g, capRects[1], 1);
        }

        private void DrawCaptionButton(Graphics g, Rectangle r, int kind)
        {
            if (r.IsEmpty) return;
            bool hover = (_hoverCap == kind);
            bool down = (_downCap == kind);
            string baseName = kind == 0 ? "minimize" : "close";
            string suffix = down ? "-active.png" : (hover ? "-hover.png" : ".png");
            Bitmap sprite = SetupAssets.GetXpCaptionSprite(baseName + suffix);
            if (sprite != null)
            {
                if (_isActive)
                {
                    g.DrawImage(sprite, r.X, r.Y, 21, 21);
                }
                else
                {
                    using (System.Drawing.Imaging.ImageAttributes ia = new System.Drawing.Imaging.ImageAttributes())
                    {
                        System.Drawing.Imaging.ColorMatrix cm = new System.Drawing.Imaging.ColorMatrix();
                        cm.Matrix33 = 0.65f;
                        ia.SetColorMatrix(cm);
                        g.DrawImage(sprite, new Rectangle(r.X, r.Y, 21, 21), 0, 0, 21, 21, GraphicsUnit.Pixel, ia);
                    }
                }
                return;
            }
            using (SolidBrush br = new SolidBrush(kind == 1 ? Color.FromArgb(215, 72, 48) : Color.FromArgb(30, 110, 228)))
                g.FillRectangle(br, r);
            g.DrawRectangle(Pens.White, r.X, r.Y, r.Width - 1, r.Height - 1);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                Point cur = Cursor.Position;
                Location = new Point(_dragStartForm.X + (cur.X - _dragStartMouse.X), _dragStartForm.Y + (cur.Y - _dragStartMouse.Y));
                return;
            }
            int hit = HitTestCaption(e.Location);
            if (hit != _hoverCap)
            {
                _hoverCap = hit;
                Invalidate(new Rectangle(0, 0, Width, TITLE_HEIGHT));
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            int hit = HitTestCaption(e.Location);
            if (hit >= 0)
            {
                _downCap = hit;
                Invalidate(new Rectangle(0, 0, Width, TITLE_HEIGHT));
                return;
            }
            if (e.Y <= TITLE_HEIGHT)
            {
                _dragging = true;
                _dragStartMouse = Cursor.Position;
                _dragStartForm = Location;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_dragging) { _dragging = false; return; }
            if (_downCap >= 0)
            {
                int hit = HitTestCaption(e.Location);
                int act = _downCap;
                _downCap = -1;
                Invalidate(new Rectangle(0, 0, Width, TITLE_HEIGHT));
                if (hit == act)
                {
                    if (act == 0) WindowState = FormWindowState.Minimized;
                    else if (act == 1) Close();
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverCap >= 0)
            {
                _hoverCap = -1;
                Invalidate(new Rectangle(0, 0, Width, TITLE_HEIGHT));
            }
        }
    }

    public class XpSetupButton : Button
    {
        public bool IsPrimary = false;
        private bool _hover = false;
        private bool _down = false;

        public XpSetupButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = SetupTheme.UiFont;
            Size = new Size(80, 23);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; _down = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs mevent) { base.OnMouseDown(mevent); if (mevent.Button == MouseButtons.Left) { _down = true; Invalidate(); } }
        protected override void OnMouseUp(MouseEventArgs mevent) { base.OnMouseUp(mevent); _down = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : SetupTheme.Face);

            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color top, bot, border;
            if (!Enabled)
            {
                top = Color.FromArgb(245, 244, 234);
                bot = Color.FromArgb(232, 230, 218);
                border = Color.FromArgb(201, 199, 186);
            }
            else if (_down)
            {
                top = Color.FromArgb(209, 204, 192);
                bot = Color.FromArgb(229, 227, 216);
                border = Color.FromArgb(0, 60, 116);
            }
            else if (_hover)
            {
                top = Color.FromArgb(255, 240, 207);
                bot = Color.FromArgb(248, 179, 48);
                border = Color.FromArgb(0, 60, 116);
            }
            else
            {
                top = Color.White;
                bot = Color.FromArgb(214, 208, 197);
                border = Color.FromArgb(0, 60, 116);
            }

            using (GraphicsPath gp = RoundRect(r, 3))
            {
                using (LinearGradientBrush br = new LinearGradientBrush(r, top, bot, LinearGradientMode.Vertical))
                {
                    g.FillPath(br, gp);
                }
                if (IsPrimary && Enabled && !_down && !_hover)
                {
                    using (Pen pGlow = new Pen(Color.FromArgb(145, 105, 175, 248), 2f))
                        g.DrawPath(pGlow, RoundRect(new Rectangle(1, 1, Width - 3, Height - 3), 2));
                }
                using (Pen pb = new Pen(border))
                {
                    g.DrawPath(pb, gp);
                }
            }

            TextRenderer.DrawText(g, Text, IsPrimary ? SetupTheme.UiBold : Font,
                new Rectangle(3, _down ? 1 : 0, Width - 6, Height),
                Enabled ? Color.Black : Color.Gray,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        private static GraphicsPath RoundRect(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    public class XpSetupProgressBar : Control
    {
        private int _value;
        public int Value
        {
            get { return _value; }
            set { _value = Math.Max(0, Math.Min(100, value)); Invalidate(); }
        }

        public XpSetupProgressBar()
        {
            DoubleBuffered = true;
            Size = new Size(300, 18);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);
            using (Pen outer = new Pen(Color.FromArgb(104, 104, 104)))
            using (Pen inner = new Pen(Color.FromArgb(202, 200, 187)))
            {
                g.DrawRectangle(outer, 0, 0, Width - 1, Height - 1);
                g.DrawRectangle(inner, 1, 1, Width - 3, Height - 3);
            }
            int maxW = (int)Math.Round(((Width - 6) * _value) / 100.0);
            int x = 3;
            while (x + 2 <= 3 + maxW)
            {
                int cw = Math.Min(8, 3 + maxW - x);
                if (cw <= 0) break;
                Rectangle seg = new Rectangle(x, 3, cw, Height - 6);
                using (LinearGradientBrush br = new LinearGradientBrush(seg,
                    Color.FromArgb(148, 240, 152), Color.FromArgb(40, 200, 52), LinearGradientMode.Vertical))
                {
                    g.FillRectangle(br, seg);
                }
                x += 10;
            }
        }
    }

    public static class XpSetupMsgBox
    {
        public static DialogResult Show(IWin32Window owner, string message, string title, bool confirm)
        {
            using (XpSetupForm dlg = new XpSetupForm())
            {
                dlg.Text = title;
                dlg.Size = new Size(375, 165);
                dlg.ShowMinimizeButton = false;
                dlg.StartPosition = owner != null ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;

                PictureBox pic = new PictureBox
                {
                    Location = new Point(16, 18),
                    Size = new Size(36, 36),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Image = SetupAssets.GetAimBadgeBitmap(36)
                };
                Label lbl = new Label
                {
                    Location = new Point(62, 16),
                    Size = new Size(292, 66),
                    Text = message,
                    Font = SetupTheme.UiFont
                };

                Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = SetupTheme.FaceDark };
                DialogResult res = DialogResult.OK;

                if (confirm)
                {
                    XpSetupButton yes = new XpSetupButton { Text = "Yes", IsPrimary = true, Location = new Point(110, 8) };
                    XpSetupButton no = new XpSetupButton { Text = "No", Location = new Point(200, 8) };
                    yes.Click += delegate { res = DialogResult.Yes; dlg.Close(); };
                    no.Click += delegate { res = DialogResult.No; dlg.Close(); };
                    foot.Controls.Add(yes);
                    foot.Controls.Add(no);
                    dlg.AcceptButton = yes;
                    dlg.CancelButton = no;
                }
                else
                {
                    XpSetupButton ok = new XpSetupButton { Text = "OK", IsPrimary = true, Location = new Point(146, 8) };
                    ok.Click += delegate { res = DialogResult.OK; dlg.Close(); };
                    foot.Controls.Add(ok);
                    dlg.AcceptButton = ok;
                    dlg.CancelButton = ok;
                }

                dlg.ClientBody.Controls.Add(pic);
                dlg.ClientBody.Controls.Add(lbl);
                dlg.ClientBody.Controls.Add(foot);
                dlg.ShowDialog(owner);
                return res;
            }
        }
    }
}
