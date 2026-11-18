using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AIMClassic
{
    public class AimBannerStrip : Control
    {
        private class BannerSlide
        {
            public string Tag;
            public string TitleEn;
            public string SubEn;
            public string TitleRu;
            public string SubRu;
            public Color C1;
            public Color C2;
            public Color Accent;
            public string ImageKey;
            public string ActionKey;
        }

        private static readonly BannerSlide[] _slides = new BannerSlide[]
        {
            new BannerSlide
            {
                Tag = "GLOBAL P2P",
                TitleEn = "Add Global Friends Anywhere!",
                SubEn = "Connect worldwide via Global Room or LAN / Invite Code ->",
                TitleRu = "Добавляй друзей по всему миру!",
                SubRu = "Глобальная комната (ntfy), LAN или инвайт-код ->",
                C1 = Color.FromArgb(12, 74, 152),
                C2 = Color.FromArgb(36, 152, 224),
                Accent = Color.FromArgb(255, 214, 24),
                ImageKey = "banner_globe.jpg",
                ActionKey = "real"
            },
            new BannerSlide
            {
                Tag = "FRUTIGER AERO",
                TitleEn = "NEW: Frutiger Aero Glossy Theme!",
                SubEn = "2007 Aqua glass windows & wallpaper in AIM Workshop ->",
                TitleRu = "НОВОЕ: Тема Фрутигер Аэро (Aqua Glass)!",
                SubRu = "Глянцевое стекло 2007 и обои в AIM Workshop ->",
                C1 = Color.FromArgb(8, 108, 158),
                C2 = Color.FromArgb(46, 198, 214),
                Accent = Color.FromArgb(174, 255, 198),
                ImageKey = "banner_aero.jpg",
                ActionKey = "frutiger"
            },
            new BannerSlide
            {
                Tag = "AOL® MUSIC",
                TitleEn = "AIM Music Lobby — 2000s Hits",
                SubEn = "Join #AIM_Lobby_2006 & chat with friends live ->",
                TitleRu = "AOL Music — Чат хитов 2000-х",
                SubRu = "Заходи в #AIM_Lobby_2006 и общайся вживую ->",
                C1 = Color.FromArgb(82, 22, 132),
                C2 = Color.FromArgb(168, 52, 196),
                Accent = Color.FromArgb(255, 204, 68),
                ImageKey = "banner_music.jpg",
                ActionKey = "lobby"
            },
            new BannerSlide
            {
                Tag = "WORKSHOP",
                TitleEn = "AIM Theme & Avatar Workshop",
                SubEn = "Browse XP skins, create custom themes & import icons ->",
                TitleRu = "Мастерская тем и аватарок AIM",
                SubRu = "Кастомные скины, конструктор тем и паки иконок ->",
                C1 = Color.FromArgb(24, 64, 136),
                C2 = Color.FromArgb(204, 102, 24),
                Accent = Color.FromArgb(255, 226, 98),
                ImageKey = "banner_workshop.jpg",
                ActionKey = "workshop"
            },
            new BannerSlide
            {
                Tag = "AIM® MAIL",
                TitleEn = "You've Got Mail! Open Inbox",
                SubEn = "Read retro AOL emails & message friends inside AIM ->",
                TitleRu = "Вам письмо! Открыть AOL Почту",
                SubRu = "Читайте письма, отвечайте и пишите друзьям ->",
                C1 = Color.FromArgb(10, 56, 136),
                C2 = Color.FromArgb(26, 118, 208),
                Accent = Color.FromArgb(255, 212, 42),
                ImageKey = "banner_mail.jpg",
                ActionKey = "mail"
            },
            new BannerSlide
            {
                Tag = "BOTS + API",
                TitleEn = "AI Bots, Updates & Diagnostics",
                SubEn = "Configure OpenAI / Gemini keys & Check Updates ->",
                TitleRu = "API ключи для ботов и обновления",
                SubRu = "OpenAI / Gemini боты, обновления и поддержка ->",
                C1 = Color.FromArgb(14, 98, 82),
                C2 = Color.FromArgb(30, 158, 132),
                Accent = Color.FromArgb(255, 236, 118),
                ImageKey = "banner_aero.jpg",
                ActionKey = "apikeys"
            }
        };

        private int _index = 0;
        private bool _hover = false;
        private readonly System.Windows.Forms.Timer _timer;
        private readonly bool _compact;

        public AimBannerStrip(bool compact)
        {
            _compact = compact;
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            Height = compact ? 38 : 54;

            _timer = new System.Windows.Forms.Timer { Interval = 10000 };
            _timer.Tick += delegate { _index = (_index + 1) % _slides.Length; Invalidate(); };
            _timer.Start();

            MouseEnter += delegate { _hover = true; Invalidate(); };
            MouseLeave += delegate { _hover = false; Invalidate(); };
            Click += OnBannerClick;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
            }
            base.Dispose(disposing);
        }

        private void OnBannerClick(object sender, EventArgs e)
        {
            Point pt = PointToClient(Cursor.Position);
            if (pt.X >= Width - 18 && pt.Y <= 16)
            {
                _index = (_index + 1) % _slides.Length;
                Invalidate();
                return;
            }
            BannerSlide slide = _slides[_index];
            Form owner = FindForm();
            if (slide.ActionKey == "real") new ContactManagerForm("real", null).ShowDialog(owner);
            else if (slide.ActionKey == "lobby") AimApp.OpenPublicGroupsHub(owner);
            else if (slide.ActionKey == "mail") AimApp.OpenMailInbox(owner);
            else if (slide.ActionKey == "frutiger" || slide.ActionKey == "workshop") new WorkshopForm().ShowDialog(owner);
            else if (slide.ActionKey == "apikeys") new ApiKeysForm().ShowDialog(owner);
            else if (slide.ActionKey == "updates") new UpdateCenterForm().ShowDialog(owner);
            else if (slide.ActionKey == "support") new SupportForm().ShowDialog(owner);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            BannerSlide sl = _slides[_index];

            Rectangle bg = new Rectangle(3, 2, Math.Max(40, Width - 6), Math.Max(20, Height - 4));
            using (LinearGradientBrush br = new LinearGradientBrush(bg, sl.C1, sl.C2, 12f))
                g.FillRectangle(br, bg);

            Bitmap photo = RetroAssets.GetEmbeddedBannerImage(sl.ImageKey);
            if (photo != null)
            {
                GraphicsState st = g.Save();
                g.SetClip(bg);
                float srcAspect = (float)photo.Width / Math.Max(1, photo.Height);
                int drawH = bg.Height;
                int drawW = Math.Max(bg.Width, (int)(drawH * srcAspect));
                int drawX = bg.Right - drawW;
                g.DrawImage(photo, new Rectangle(drawX, bg.Y, drawW, drawH));

                // Left contrast scrim so text stays crystal clear while photo artwork pops on right
                using (LinearGradientBrush scrim = new LinearGradientBrush(
                    bg,
                    Color.FromArgb(215, Math.Max((int)0, (int)sl.C1.R - 10), Math.Max((int)0, (int)sl.C1.G - 10), Math.Max((int)0, (int)sl.C1.B - 10)),
                    Color.FromArgb(24, sl.C2),
                    LinearGradientMode.Horizontal))
                {
                    ColorBlend cb = new ColorBlend(3);
                    cb.Positions = new float[] { 0f, 0.58f, 1f };
                    cb.Colors = new Color[]
                    {
                        Color.FromArgb(218, Math.Max((int)0, (int)sl.C1.R - 12), Math.Max((int)0, (int)sl.C1.G - 10), sl.C1.B),
                        Color.FromArgb(148, sl.C1),
                        Color.FromArgb(22, sl.C2)
                    };
                    scrim.InterpolationColors = cb;
                    g.FillRectangle(scrim, bg);
                }
                g.Restore(st);
            }

            // Retro glossy top shine overlay
            using (SolidBrush shine = new SolidBrush(Color.FromArgb(_hover ? 46 : 26, 255, 255, 255)))
                g.FillRectangle(shine, bg.X, bg.Y, bg.Width, bg.Height / 2);

            using (Pen border = new Pen(_hover ? sl.Accent : Color.FromArgb(14, 42, 92)))
                g.DrawRectangle(border, bg.X, bg.Y, bg.Width - 1, bg.Height - 1);

            // Tag pill on left
            int pillW = _compact ? 62 : 70;
            Rectangle pill = new Rectangle(bg.X + 5, bg.Y + 4, pillW, bg.Height - 8);
            using (SolidBrush pBr = new SolidBrush(sl.Accent))
                g.FillRectangle(pBr, pill);
            using (Pen pPill = new Pen(Color.FromArgb(120, 255, 255, 255)))
                g.DrawRectangle(pPill, pill.X, pill.Y, pill.Width - 1, pill.Height - 1);
            using (Font fTag = new Font("Tahoma", _compact ? 6.5f : 6.75f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, sl.Tag, fTag, pill, Color.FromArgb(16, 30, 62),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            }

            string title = I18n.IsRussian ? sl.TitleRu : sl.TitleEn;
            string sub = I18n.IsRussian ? sl.SubRu : sl.SubEn;
            int tx = pill.Right + 7;
            int tw = Math.Max(40, bg.Right - tx - 22);

            if (_compact)
            {
                TextRenderer.DrawText(g, title, XpTheme.UiBold, new Rectangle(tx, bg.Y + 2, tw, bg.Height - 4),
                    Color.White, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            else
            {
                int ty = bg.Y + Math.Max(6, (bg.Height - 32) / 2);
                TextRenderer.DrawText(g, title, XpTheme.UiBold, new Rectangle(tx, ty, tw, 16),
                    Color.White, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, sub, XpTheme.SmallFont, new Rectangle(tx, ty + 17, tw, 15),
                    Color.FromArgb(236, 246, 255), TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }

            // Next slide chevron
            using (SolidBrush navBg = new SolidBrush(Color.FromArgb(115, 8, 22, 48)))
                g.FillRectangle(navBg, bg.Right - 17, bg.Y + 1, 16, bg.Height - 2);
            TextRenderer.DrawText(g, "›", XpTheme.UiBold, new Rectangle(bg.Right - 17, bg.Y, 16, bg.Height),
                sl.Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }
}
