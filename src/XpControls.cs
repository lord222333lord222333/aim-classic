using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AIMClassic
{
    public class XpForm : Form
    {
        private bool _isActive = true;
        private int _hoverCap = -1; // 0=min, 1=max, 2=close
        private int _downCap = -1;
        private bool _dragging = false;
        private Point _dragStartMouse;
        private Point _dragStartForm;
        private bool _resizing = false;
        private Point _resizeStartMouse;
        private Size _resizeStartSize;
        private bool _xpMaximized = false;
        private Rectangle _restoreBounds;

        public Panel ClientBody;
        public bool ShowMinimizeButton = true;
        public bool ShowMaximizeButton = true;
        public bool AllowResize = true;
        public bool MinimizeOnClose = false;
        public Image TitleIconImage = null;

        private const int TITLE_HEIGHT = 28;
        private const int BORDER_W = 3;

        public XpForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;
            Font = XpTheme.UiFont;
            StartPosition = FormStartPosition.CenterScreen;
            Padding = new Padding(BORDER_W, TITLE_HEIGHT, BORDER_W, BORDER_W);
            Icon = RetroAssets.DefaultAppIcon;
            TitleIconImage = RetroAssets.GetRunningManBitmap(16, "online");

            ClientBody = new Panel();
            ClientBody.Dock = DockStyle.Fill;
            ClientBody.BackColor = XpTheme.Current.Face;
            ClientBody.Font = XpTheme.UiFont;
            Controls.Add(ClientBody);

            Activated += delegate { _isActive = true; Invalidate(); };
            Deactivate += delegate { _isActive = false; _hoverCap = -1; _downCap = -1; Invalidate(); };
            Resize += delegate { UpdateWindowRegion(); Invalidate(); };
        }

        public void ApplyThemePalette()
        {
            BackColor = XpTheme.Current.Face;
            ClientBody.BackColor = XpTheme.Current.Face;
            Invalidate(true);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyThemePalette();
            UpdateWindowRegion();
        }

        private void UpdateWindowRegion()
        {
            if (_xpMaximized || Width < 20 || Height < 20)
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
            int xMax = xClose - btnSize - 2;
            int xMin = ShowMaximizeButton ? (xMax - btnSize - 2) : (xClose - btnSize - 2);
            return new Rectangle[]
            {
                ShowMinimizeButton ? new Rectangle(xMin, y, btnSize, btnSize) : Rectangle.Empty,
                ShowMaximizeButton ? new Rectangle(xMax, y, btnSize, btnSize) : Rectangle.Empty,
                new Rectangle(xClose, y, btnSize, btnSize)
            };
        }

        private int HitTestCaption(Point pt)
        {
            Rectangle[] rects = GetCaptionRects();
            for (int i = 0; i < rects.Length; i++)
            {
                if (!rects[i].IsEmpty && rects[i].Contains(pt)) return i;
            }
            return -1;
        }

        private bool HitTestResizeGrip(Point pt)
        {
            if (!AllowResize || _xpMaximized) return false;
            return pt.X >= Width - 14 && pt.Y >= Height - 14;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            ThemePalette pal = XpTheme.Current;

            Rectangle titleRect = new Rectangle(0, 0, Width, TITLE_HEIGHT);
            if (_isActive && pal.Name == "LunaBlue")
            {
                // Exact 8-stop Windows XP Luna Blue titlebar gradient from XP.css (_window.scss)
                using (LinearGradientBrush br = new LinearGradientBrush(titleRect, Color.FromArgb(9, 151, 255), Color.FromArgb(0, 61, 215), LinearGradientMode.Vertical))
                {
                    ColorBlend cb = new ColorBlend(8);
                    cb.Colors = new Color[]
                    {
                        Color.FromArgb(9, 151, 255),
                        Color.FromArgb(0, 83, 238),
                        Color.FromArgb(0, 80, 238),
                        Color.FromArgb(0, 102, 255),
                        Color.FromArgb(0, 102, 255),
                        Color.FromArgb(0, 91, 255),
                        Color.FromArgb(0, 61, 215),
                        Color.FromArgb(0, 61, 215)
                    };
                    cb.Positions = new float[] { 0f, 0.08f, 0.40f, 0.88f, 0.93f, 0.95f, 0.96f, 1.0f };
                    br.InterpolationColors = cb;
                    g.FillRectangle(br, titleRect);
                }
            }
            else
            {
                Color tTop = _isActive ? pal.TitleTop : pal.TitleInactiveTop;
                Color tBot = _isActive ? pal.TitleBot : pal.TitleInactiveBot;
                using (LinearGradientBrush br = new LinearGradientBrush(titleRect, tTop, tBot, LinearGradientMode.Vertical))
                {
                    if (_isActive)
                    {
                        ColorBlend cb = new ColorBlend(4);
                        cb.Colors = new Color[] { pal.TitleTop, pal.TitleMid, pal.TitleBot, pal.TitleBot };
                        cb.Positions = new float[] { 0f, 0.45f, 0.92f, 1f };
                        br.InterpolationColors = cb;
                    }
                    g.FillRectangle(br, titleRect);
                }
            }

            if (pal.IsGlass)
            {
                Rectangle gloss = new Rectangle(0, 1, Width, TITLE_HEIGHT / 2);
                using (LinearGradientBrush gBr = new LinearGradientBrush(gloss,
                    Color.FromArgb(_isActive ? 118 : 65, 255, 255, 255),
                    Color.FromArgb(24, 255, 255, 255),
                    LinearGradientMode.Vertical))
                {
                    g.FillRectangle(gBr, gloss);
                }
                using (Pen pSpec = new Pen(Color.FromArgb(_isActive ? 155 : 80, 255, 255, 255)))
                {
                    g.DrawLine(pSpec, 2, 1, Width - 3, 1);
                }
            }

            // Authentic 3-layer Windows XP Luna side & bottom borders
            if (_isActive && pal.Name == "LunaBlue")
            {
                using (Pen pL1 = new Pen(Color.FromArgb(8, 49, 217)))
                using (Pen pL2 = new Pen(Color.FromArgb(22, 106, 238)))
                using (Pen pL3 = new Pen(Color.FromArgb(8, 85, 221)))
                using (Pen pR1 = new Pen(Color.FromArgb(0, 19, 140)))
                using (Pen pR2 = new Pen(Color.FromArgb(0, 30, 160)))
                using (Pen pR3 = new Pen(Color.FromArgb(0, 59, 218)))
                {
                    g.DrawLine(pL1, 0, TITLE_HEIGHT, 0, Height - 1);
                    g.DrawLine(pL2, 1, TITLE_HEIGHT, 1, Height - 2);
                    g.DrawLine(pL3, 2, TITLE_HEIGHT, 2, Height - 3);

                    g.DrawLine(pR1, Width - 1, TITLE_HEIGHT, Width - 1, Height - 1);
                    g.DrawLine(pR2, Width - 2, TITLE_HEIGHT, Width - 2, Height - 2);
                    g.DrawLine(pR3, Width - 3, TITLE_HEIGHT, Width - 3, Height - 3);

                    g.DrawLine(pR1, 0, Height - 1, Width - 1, Height - 1);
                    g.DrawLine(pR2, 1, Height - 2, Width - 2, Height - 2);
                    g.DrawLine(pR3, 2, Height - 3, Width - 3, Height - 3);

                    // Top rounded highlight edge
                    g.DrawLine(pL1, 6, 0, Width - 7, 0);
                }
            }
            else
            {
                Color frame = _isActive ? pal.FrameBorder : pal.TitleInactiveBot;
                Color inner = _isActive ? pal.FrameInner : Color.FromArgb(118, 144, 210);
                using (Brush bFrame = new SolidBrush(frame))
                {
                    g.FillRectangle(bFrame, 0, TITLE_HEIGHT, BORDER_W, Height - TITLE_HEIGHT);
                    g.FillRectangle(bFrame, Width - BORDER_W, TITLE_HEIGHT, BORDER_W, Height - TITLE_HEIGHT);
                    g.FillRectangle(bFrame, 0, Height - BORDER_W, Width, BORDER_W);
                }
                using (Pen pInner = new Pen(inner))
                {
                    g.DrawRectangle(pInner, 0, 0, Width - 1, Height - 1);
                }
            }

            // Icon + Title text
            int textX = 8;
            if (TitleIconImage != null)
            {
                g.DrawImage(TitleIconImage, 7, 6, 16, 16);
                textX = 27;
            }
            else if (Icon != null)
            {
                g.DrawIcon(Icon, new Rectangle(7, 6, 16, 16));
                textX = 27;
            }

            Rectangle[] capRects = GetCaptionRects();
            int rightLimit = Width - 78;
            Rectangle textRect = new Rectangle(textX, 5, Math.Max(40, rightLimit - textX), 20);
            Color titleColor = _isActive
                ? (pal.Name == "XpSilver" ? pal.TitleText : Color.White)
                : Color.FromArgb(228, 236, 252);
            if (_isActive && pal.Name != "XpSilver")
            {
                TextRenderer.DrawText(g, Text, XpTheme.TitleFont, new Rectangle(textRect.X + 1, textRect.Y + 1, textRect.Width, textRect.Height),
                    Color.FromArgb(15, 16, 137), TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            }
            TextRenderer.DrawText(g, Text, XpTheme.TitleFont, textRect, titleColor,
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

            // Real Windows XP 21x21 PNG Caption Buttons
            DrawCaptionButton(g, capRects[0], 0);
            DrawCaptionButton(g, capRects[1], 1);
            DrawCaptionButton(g, capRects[2], 2);

            // Resize Grip
            if (AllowResize && !_xpMaximized)
            {
                using (Pen pGrip = new Pen(Color.FromArgb(145, 140, 125)))
                {
                    g.DrawLine(pGrip, Width - 11, Height - 4, Width - 4, Height - 11);
                    g.DrawLine(pGrip, Width - 8, Height - 4, Width - 4, Height - 8);
                }
            }
        }

        private void DrawCaptionButton(Graphics g, Rectangle r, int kind)
        {
            if (r.IsEmpty) return;
            bool hover = (_hoverCap == kind);
            bool down = (_downCap == kind);

            string baseName = kind == 0 ? "minimize" : (kind == 1 ? (_xpMaximized ? "restore" : "maximize") : "close");
            string suffix = down ? "-active.png" : (hover ? "-hover.png" : ".png");
            Bitmap sprite = RetroAssets.GetXpCaptionSprite(baseName + suffix);

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

            // Fallback
            Color c1 = kind == 2 ? Color.FromArgb(244, 144, 123) : Color.FromArgb(97, 165, 250);
            Color c2 = kind == 2 ? Color.FromArgb(183, 38, 13) : Color.FromArgb(14, 88, 212);
            using (LinearGradientBrush br = new LinearGradientBrush(r, c1, c2, LinearGradientMode.Vertical)) g.FillRectangle(br, r);
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
            if (_resizing)
            {
                Point cur = Cursor.Position;
                int nw = Math.Max(MinimumSize.Width > 0 ? MinimumSize.Width : 220, _resizeStartSize.Width + (cur.X - _resizeStartMouse.X));
                int nh = Math.Max(MinimumSize.Height > 0 ? MinimumSize.Height : 140, _resizeStartSize.Height + (cur.Y - _resizeStartMouse.Y));
                Size = new Size(nw, nh);
                return;
            }

            Cursor = HitTestResizeGrip(e.Location) ? Cursors.SizeNWSE : Cursors.Default;
            int hit = HitTestCaption(e.Location);
            if (hit != _hoverCap)
            {
                _hoverCap = hit;
                Invalidate(new Rectangle(0, 0, Width, TITLE_HEIGHT));
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverCap = -1;
            Cursor = Cursors.Default;
            Invalidate(new Rectangle(0, 0, Width, TITLE_HEIGHT));
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
            if (HitTestResizeGrip(e.Location))
            {
                _resizing = true;
                _resizeStartMouse = Cursor.Position;
                _resizeStartSize = Size;
                return;
            }
            if (e.Y <= TITLE_HEIGHT && !_xpMaximized)
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
            if (_resizing) { _resizing = false; return; }
            if (_downCap >= 0)
            {
                int act = _downCap;
                _downCap = -1;
                Invalidate(new Rectangle(0, 0, Width, TITLE_HEIGHT));
                if (HitTestCaption(e.Location) == act)
                {
                    if (act == 0) WindowState = FormWindowState.Minimized;
                    else if (act == 1) ToggleXpMaximize();
                    else if (act == 2)
                    {
                        if (MinimizeOnClose) Hide();
                        else Close();
                    }
                }
            }
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);
            Point pt = PointToClient(Cursor.Position);
            if (ShowMaximizeButton && pt.Y <= TITLE_HEIGHT && HitTestCaption(pt) < 0)
            {
                ToggleXpMaximize();
            }
        }

        private void ToggleXpMaximize()
        {
            if (!_xpMaximized)
            {
                _restoreBounds = Bounds;
                _xpMaximized = true;
                Bounds = Screen.FromControl(this).WorkingArea;
            }
            else
            {
                _xpMaximized = false;
                Bounds = _restoreBounds;
            }
            UpdateWindowRegion();
        }
    }

    /* ======================================================================
       Volumetric 3D Windows XP Button
       ====================================================================== */
    public class XpButton : Button
    {
        private bool _hover = false;
        private bool _pressed = false;
        public bool IsPrimary = false;
        public bool IsAimGold = false;

        public XpButton()
        {
            Font = XpTheme.UiFont;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Size = new Size(84, 24);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; _pressed = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs mevent) { base.OnMouseDown(mevent); if (mevent.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } }
        protected override void OnMouseUp(MouseEventArgs mevent) { base.OnMouseUp(mevent); _pressed = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            g.Clear(Parent != null ? Parent.BackColor : XpTheme.Current.Face);

            if (!Enabled)
            {
                using (SolidBrush br = new SolidBrush(Color.FromArgb(236, 233, 216))) g.FillRectangle(br, r);
                using (Pen p = new Pen(Color.FromArgb(180, 177, 165))) g.DrawRectangle(p, r);
                TextRenderer.DrawText(g, Text, Font, r, Color.FromArgb(141, 137, 124), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return;
            }

            Color top, mid, bot, border;
            ThemePalette palBtn = XpTheme.Current;
            if (IsAimGold)
            {
                top = _pressed ? Color.FromArgb(234, 166, 0) : Color.FromArgb(255, 243, 176);
                mid = Color.FromArgb(255, 204, 0);
                bot = _pressed ? Color.FromArgb(255, 243, 176) : Color.FromArgb(234, 166, 0);
                border = Color.FromArgb(142, 91, 0);
            }
            else if (palBtn.IsGlass)
            {
                top = _pressed ? palBtn.FaceDark : Color.FromArgb(252, 255, 255);
                mid = _pressed ? palBtn.SubBarBot : palBtn.SubBarTop;
                bot = _pressed ? palBtn.SubBarTop : palBtn.FaceDark;
                border = palBtn.FrameBorder;
            }
            else
            {
                top = _pressed ? Color.FromArgb(205, 202, 195) : Color.White;
                mid = _pressed ? Color.FromArgb(229, 229, 222) : Color.FromArgb(236, 235, 229);
                bot = _pressed ? Color.FromArgb(242, 242, 241) : Color.FromArgb(216, 208, 196);
                border = Color.FromArgb(0, 60, 116);
            }

            using (LinearGradientBrush br = new LinearGradientBrush(r, top, bot, LinearGradientMode.Vertical))
            {
                ColorBlend cb = new ColorBlend(3);
                cb.Colors = new Color[] { top, mid, bot };
                cb.Positions = new float[] { 0f, _pressed ? 0.55f : (palBtn.IsGlass ? 0.52f : 0.86f), 1f };
                br.InterpolationColors = cb;
                g.FillRectangle(br, 1, 1, r.Width - 1, r.Height - 1);
            }

            if (palBtn.IsGlass && !_pressed)
            {
                using (SolidBrush brShine = new SolidBrush(Color.FromArgb(95, 255, 255, 255)))
                {
                    g.FillRectangle(brShine, 2, 1, Math.Max(1, r.Width - 3), Math.Max(1, r.Height / 2));
                }
            }

            if (_hover && !_pressed)
            {
                using (Pen pOut = new Pen(Color.FromArgb(253, 216, 137)))
                using (Pen pIn = new Pen(Color.FromArgb(229, 160, 26)))
                {
                    g.DrawRectangle(pOut, 1, 1, r.Width - 2, r.Height - 2);
                    g.DrawRectangle(pIn, 2, 2, r.Width - 4, r.Height - 4);
                }
            }
            else if (IsPrimary && !IsAimGold && !_pressed)
            {
                using (Pen pPri1 = new Pen(Color.FromArgb(152, 184, 234)))
                using (Pen pPri2 = new Pen(Color.FromArgb(137, 173, 228)))
                {
                    g.DrawRectangle(pPri1, 1, 1, r.Width - 2, r.Height - 2);
                    g.DrawRectangle(pPri2, 2, 2, r.Width - 4, r.Height - 4);
                }
            }

            using (Pen pBorder = new Pen(border))
            {
                g.DrawRectangle(pBorder, r);
            }

            Rectangle txtRect = r;
            if (_pressed) txtRect.Offset(1, 1);

            if (Image != null)
            {
                int iy = (Height - Image.Height) / 2 + (_pressed ? 1 : 0);
                int ix = 8 + (_pressed ? 1 : 0);
                g.DrawImage(Image, ix, iy, Image.Width, Image.Height);
                txtRect = new Rectangle(ix + Image.Width + 4, txtRect.Y, Width - (ix + Image.Width + 8), Height);
            }

            Font drawFont = (IsPrimary || IsAimGold) ? XpTheme.UiBold : Font;
            Color textCol = IsAimGold ? Color.FromArgb(0, 40, 104) : Color.Black;
            TextRenderer.DrawText(g, Text, drawFont, txtRect, textCol, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            if (Focused)
            {
                ControlPaint.DrawFocusRectangle(g, new Rectangle(3, 3, Width - 6, Height - 6));
            }
        }
    }

    /* ======================================================================
       Classic XP Segmented Green Progress Bar
       ====================================================================== */
    public class XpProgressBar : Control
    {
        private int _value = 0;
        public int Value
        {
            get { return _value; }
            set { _value = Math.Max(0, Math.Min(100, value)); Invalidate(); }
        }

        public XpProgressBar()
        {
            DoubleBuffered = true;
            Height = 16;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);
            using (Pen p = new Pen(Color.FromArgb(104, 104, 104)))
            {
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            }
            int innerW = Math.Max(0, Width - 4);
            int fillW = (int)Math.Round(innerW * (_value / 100.0));
            for (int x = 2; x + 6 <= 2 + fillW; x += 9)
            {
                Rectangle blk = new Rectangle(x, 2, 7, Height - 4);
                using (LinearGradientBrush br = new LinearGradientBrush(blk, Color.FromArgb(118, 232, 118), Color.FromArgb(37, 163, 39), LinearGradientMode.Vertical))
                {
                    g.FillRectangle(br, blk);
                }
            }
        }
    }

    /* ======================================================================
       Classic AIM Blue Header Banner
       ====================================================================== */
    public class XpBanner : Panel
    {
        public string BannerTitle = "AIM";
        public string BannerSubtitle = "";
        public Image BannerIcon = null;

        public XpBanner()
        {
            Dock = DockStyle.Top;
            Height = 46;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ThemePalette pal = XpTheme.Current;
            Graphics g = e.Graphics;
            Rectangle r = new Rectangle(0, 0, Width, Height);
            using (LinearGradientBrush br = new LinearGradientBrush(r, pal.AimHeaderTop, pal.AimHeaderBot, LinearGradientMode.Vertical))
            {
                g.FillRectangle(br, r);
            }
            if (pal.IsGlass)
            {
                using (LinearGradientBrush gBr = new LinearGradientBrush(
                    new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height / 2)),
                    Color.FromArgb(88, 255, 255, 255), Color.FromArgb(18, 255, 255, 255), LinearGradientMode.Vertical))
                {
                    g.FillRectangle(gBr, 0, 0, Width, Height / 2);
                }
            }
            using (Pen gold = new Pen(pal.AimGold, 2))
            {
                g.DrawLine(gold, 0, Height - 1, Width, Height - 1);
            }
            int x = 10;
            if (BannerIcon != null)
            {
                g.DrawImage(BannerIcon, 10, 9, 26, 26);
                x = 44;
            }
            TextRenderer.DrawText(g, BannerTitle, XpTheme.BannerTitleFont, new Rectangle(x, 6, Width - x - 8, 19), Color.White, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, BannerSubtitle, XpTheme.SmallFont, new Rectangle(x, 25, Width - x - 8, 15), Color.FromArgb(210, 229, 255), TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }

    /* ======================================================================
       Windows XP MenuStrip & ToolStrip Renderer
       ====================================================================== */
    public class XpToolRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is MenuStrip)
            {
                using (SolidBrush br = new SolidBrush(XpTheme.Current.Face))
                {
                    e.Graphics.FillRectangle(br, e.AffectedBounds);
                }
            }
            else if (e.ToolStrip is ToolStripDropDown)
            {
                using (SolidBrush br = new SolidBrush(Color.White))
                {
                    e.Graphics.FillRectangle(br, e.AffectedBounds);
                }
            }
            else if (e.AffectedBounds.Width > 0 && e.AffectedBounds.Height > 0)
            {
                ThemePalette palStrip = XpTheme.Current;
                using (LinearGradientBrush br = new LinearGradientBrush(e.AffectedBounds, palStrip.SubBarTop, palStrip.SubBarBot, LinearGradientMode.Vertical))
                {
                    e.Graphics.FillRectangle(br, e.AffectedBounds);
                }
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            Rectangle r = new Rectangle(2, 1, e.Item.Width - 4, e.Item.Height - 2);
            if (e.Item.Selected || e.Item.Pressed)
            {
                using (SolidBrush br = new SolidBrush(Color.FromArgb(49, 106, 197)))
                {
                    e.Graphics.FillRectangle(br, r);
                }
                e.Item.ForeColor = Color.White;
            }
            else
            {
                e.Item.ForeColor = Color.Black;
            }
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            Rectangle r = new Rectangle(1, 1, e.Item.Width - 3, e.Item.Height - 3);
            if (e.Item.Selected || e.Item.Pressed)
            {
                using (LinearGradientBrush br = new LinearGradientBrush(r, Color.FromArgb(255, 244, 204), Color.FromArgb(255, 214, 115), LinearGradientMode.Vertical))
                {
                    e.Graphics.FillRectangle(br, r);
                }
                using (Pen p = new Pen(Color.FromArgb(49, 106, 197)))
                {
                    e.Graphics.DrawRectangle(p, r);
                }
            }
        }
    }

    /* ======================================================================
       XP Modal MessageBox Replacement
       ====================================================================== */
    public static class XpMessageBox
    {
        public static DialogResult Show(IWin32Window owner, string message, string title, bool confirm)
        {
            using (XpForm dlg = new XpForm())
            {
                dlg.Text = title;
                dlg.Size = new Size(360, 165);
                dlg.ShowMaximizeButton = false;
                dlg.ShowMinimizeButton = false;
                dlg.AllowResize = false;
                dlg.StartPosition = owner != null ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;
                dlg.TitleIconImage = RetroAssets.GetIconBitmap("help", 16);

                PictureBox pic = new PictureBox { Location = new Point(16, 20), Size = new Size(32, 32), SizeMode = PictureBoxSizeMode.Zoom };
                pic.Image = RetroAssets.GetIconBitmap(confirm ? "help" : "im", 32);

                Label lbl = new Label
                {
                    Location = new Point(58, 14),
                    Size = new Size(280, 68),
                    Text = message,
                    Font = XpTheme.UiFont
                };

                Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 38, BackColor = XpTheme.Current.FaceDark };
                DialogResult res = DialogResult.OK;

                if (confirm)
                {
                    XpButton yes = new XpButton { Text = "Yes", IsPrimary = true, Location = new Point(100, 7) };
                    XpButton no = new XpButton { Text = "No", Location = new Point(192, 7) };
                    yes.Click += delegate { res = DialogResult.Yes; dlg.Close(); };
                    no.Click += delegate { res = DialogResult.No; dlg.Close(); };
                    foot.Controls.Add(yes);
                    foot.Controls.Add(no);
                    dlg.AcceptButton = yes;
                    dlg.CancelButton = no;
                }
                else
                {
                    XpButton ok = new XpButton { Text = "OK", IsPrimary = true, Location = new Point(135, 7) };
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
