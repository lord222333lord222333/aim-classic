using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AIMClassic
{
    public class EmoticonDef
    {
        public string Code;
        public string[] Aliases;
        public string Label;
        public string Face;
    }

    public static class RetroAssets
    {
        private static readonly Dictionary<string, byte[]> _avatarBytes = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<string> _avatarNames = new List<string>();
        private static readonly Dictionary<string, string> _emoticonDibHex = new Dictionary<string, string>();
        private static readonly Dictionary<string, Bitmap> _emoticonBmps = new Dictionary<string, Bitmap>();
        private static readonly Dictionary<PictureBox, MemoryStream> _pbStreams = new Dictionary<PictureBox, MemoryStream>();
        private static readonly Dictionary<string, Bitmap> _xpSprites = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Bitmap> _bannerImages = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        public static readonly Icon DefaultAppIcon;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        public static readonly EmoticonDef[] Emoticons = new EmoticonDef[]
        {
            new EmoticonDef { Code = ":-)", Aliases = new[] { ":)" }, Label = "Smile", Face = "smile" },
            new EmoticonDef { Code = ":-(", Aliases = new[] { ":(" }, Label = "Sad", Face = "sad" },
            new EmoticonDef { Code = ";-)", Aliases = new[] { ";)" }, Label = "Wink", Face = "wink" },
            new EmoticonDef { Code = ":-D", Aliases = new[] { ":D" }, Label = "Big Grin", Face = "grin" },
            new EmoticonDef { Code = ":-P", Aliases = new[] { ":P", ":-p", ":p" }, Label = "Tongue", Face = "tongue" },
            new EmoticonDef { Code = ":-O", Aliases = new[] { ":O", ":-o", ":o" }, Label = "Surprised", Face = "surprised" },
            new EmoticonDef { Code = ":'(", Aliases = new[] { ":'-(" }, Label = "Crying", Face = "cry" },
            new EmoticonDef { Code = "B-)", Aliases = new[] { "8-)", "8)", "B)" }, Label = "Cool", Face = "cool" },
            new EmoticonDef { Code = ">:(", Aliases = new[] { ">:-(" }, Label = "Angry", Face = "angry" },
            new EmoticonDef { Code = ":-*", Aliases = new[] { ":*" }, Label = "Kiss", Face = "kiss" },
            new EmoticonDef { Code = "O:)", Aliases = new[] { "O:-)", "0:)" }, Label = "Angel", Face = "angel" },
            new EmoticonDef { Code = ">:)", Aliases = new[] { ">:-)" }, Label = "Devil", Face = "devil" },
            new EmoticonDef { Code = "<3", Aliases = new string[0], Label = "Heart", Face = "heart" },
            new EmoticonDef { Code = ":-/", Aliases = new[] { ":/" }, Label = "Confused", Face = "confused" },
            new EmoticonDef { Code = ":-$", Aliases = new[] { ":$" }, Label = "Blush", Face = "blush" },
            new EmoticonDef { Code = "|-)", Aliases = new[] { "I-)" }, Label = "Sleepy", Face = "sleepy" }
        };

        static RetroAssets()
        {
            DefaultAppIcon = LoadDefaultAppIcon();
            LoadEmbeddedAvatars();
            LoadXpSprites();
            BuildEmoticons();
        }

        private static void LoadXpSprites()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            const string prefix = "AIMClassic.XpSprites.";
            foreach (string resName in asm.GetManifestResourceNames())
            {
                if (resName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string key = resName.Substring(prefix.Length);
                    using (Stream s = asm.GetManifestResourceStream(resName))
                    {
                        if (s != null)
                        {
                            using (Image img = Image.FromStream(s))
                            {
                                _xpSprites[key] = new Bitmap(img);
                            }
                        }
                    }
                }
            }
            string diskDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "xp");
            if (Directory.Exists(diskDir))
            {
                foreach (string f in Directory.GetFiles(diskDir, "*.png"))
                {
                    string key = Path.GetFileName(f);
                    if (!_xpSprites.ContainsKey(key))
                    {
                        try { using (Image img = Image.FromFile(f)) _xpSprites[key] = new Bitmap(img); } catch { }
                    }
                }
            }
        }

        public static Bitmap GetXpCaptionSprite(string fileName)
        {
            Bitmap bmp;
            return _xpSprites.TryGetValue(fileName, out bmp) ? bmp : null;
        }

        public static Bitmap GetEmbeddedBannerImage(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            Bitmap cached;
            if (_bannerImages.TryGetValue(fileName, out cached)) return cached;

            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                string resName = "AIMClassic.Banners." + fileName;
                using (Stream s = asm.GetManifestResourceStream(resName))
                {
                    if (s != null)
                    {
                        using (Image img = Image.FromStream(s))
                        {
                            cached = new Bitmap(img);
                            _bannerImages[fileName] = cached;
                            return cached;
                        }
                    }
                }
            }
            catch { }

            try
            {
                string diskPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "banners", fileName);
                if (File.Exists(diskPath))
                {
                    using (Image img = Image.FromFile(diskPath))
                    {
                        cached = new Bitmap(img);
                        _bannerImages[fileName] = cached;
                        return cached;
                    }
                }
            }
            catch { }

            return null;
        }

        private static Icon LoadDefaultAppIcon()
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream s = asm.GetManifestResourceStream("AIMClassic.AppIcon.ico"))
                {
                    if (s != null) return new Icon(s);
                }
            }
            catch { }
            using (Bitmap b = GetAimBadgeLogoBitmap(32))
            {
                return CreateIconFromBitmap(b);
            }
        }

        public static Bitmap GetAimBadgeLogoBitmap(int size)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                float sc = size / 32f;
                g.ScaleTransform(sc, sc);

                using (GraphicsPath box = new GraphicsPath())
                {
                    int r = 6;
                    box.AddArc(1, 1, r, r, 180, 90);
                    box.AddArc(30 - r, 1, r, r, 270, 90);
                    box.AddArc(30 - r, 30 - r, r, r, 0, 90);
                    box.AddArc(1, 30 - r, r, r, 90, 90);
                    box.CloseFigure();

                    using (LinearGradientBrush bg = new LinearGradientBrush(new Rectangle(1, 1, 30, 30),
                        Color.FromArgb(42, 130, 240), Color.FromArgb(8, 52, 146), LinearGradientMode.Vertical))
                    {
                        g.FillPath(bg, box);
                    }
                    using (Pen edge = new Pen(Color.White, 1.6f))
                    {
                        g.DrawPath(edge, box);
                    }
                }

                using (Bitmap rm = GetRunningManBitmap(24, "online"))
                {
                    g.DrawImage(rm, 4, 4, 24, 24);
                }
            }
            return bmp;
        }

        private static void LoadEmbeddedAvatars()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            const string prefix = "AIMClassic.Avatars.";
            foreach (string resName in asm.GetManifestResourceNames().OrderBy(n => n))
            {
                if (resName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string shortName = resName.Substring(prefix.Length);
                    using (Stream s = asm.GetManifestResourceStream(resName))
                    {
                        if (s != null)
                        {
                            byte[] data = new byte[s.Length];
                            int read = 0;
                            while (read < data.Length)
                            {
                                int r = s.Read(data, read, data.Length - read);
                                if (r <= 0) break;
                                read += r;
                            }
                            _avatarBytes[shortName] = data;
                            _avatarNames.Add(shortName);
                        }
                    }
                }
            }

            // Also check disk fallback next to EXE
            string diskDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "avatars");
            if (Directory.Exists(diskDir))
            {
                foreach (string f in Directory.GetFiles(diskDir, "*.gif").OrderBy(x => x))
                {
                    string name = Path.GetFileName(f);
                    if (!_avatarBytes.ContainsKey(name))
                    {
                        _avatarBytes[name] = File.ReadAllBytes(f);
                        _avatarNames.Add(name);
                    }
                }
            }
        }

        public static List<string> GetAvatarNames()
        {
            return new List<string>(_avatarNames);
        }

        public static void SetPictureBoxAvatar(PictureBox pb, string avatarKey)
        {
            if (pb == null) return;
            Image oldImg = pb.Image;
            pb.Image = null;
            if (oldImg != null) oldImg.Dispose();

            MemoryStream oldMs;
            if (_pbStreams.TryGetValue(pb, out oldMs))
            {
                oldMs.Dispose();
                _pbStreams.Remove(pb);
            }

            byte[] bytes = ResolveAvatarBytes(avatarKey);
            if (bytes != null)
            {
                MemoryStream ms = new MemoryStream(bytes);
                try
                {
                    pb.Image = Image.FromStream(ms);
                    _pbStreams[pb] = ms;
                    pb.Disposed -= OnPbDisposed;
                    pb.Disposed += OnPbDisposed;
                    return;
                }
                catch
                {
                    ms.Dispose();
                }
            }

            pb.Image = GetRunningManBitmap(Math.Max(32, pb.Width), "online");
        }

        private static void OnPbDisposed(object sender, EventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null) return;
            if (pb.Image != null) { pb.Image.Dispose(); pb.Image = null; }
            MemoryStream ms;
            if (_pbStreams.TryGetValue(pb, out ms))
            {
                ms.Dispose();
                _pbStreams.Remove(pb);
            }
        }

        private static byte[] ResolveAvatarBytes(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                if (_avatarBytes.ContainsKey(key)) return _avatarBytes[key];
                if (File.Exists(key))
                {
                    try { return File.ReadAllBytes(key); } catch { }
                }
            }
            if (_avatarNames.Count > 0)
            {
                string fallback = _avatarBytes.ContainsKey("AIM Icon.gif") ? "AIM Icon.gif" : _avatarNames[0];
                return _avatarBytes[fallback];
            }
            return null;
        }

        public static Bitmap GetAvatarThumbnail(string key, int size)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.Clear(Color.White);
                byte[] bytes = ResolveAvatarBytes(key);
                if (bytes != null)
                {
                    using (MemoryStream ms = new MemoryStream(bytes))
                    using (Image src = Image.FromStream(ms))
                    {
                        g.DrawImage(src, 0, 0, size, size);
                    }
                }
                else
                {
                    using (Bitmap rm = GetRunningManBitmap(size, "online"))
                    {
                        g.DrawImage(rm, 0, 0, size, size);
                    }
                }
            }
            return bmp;
        }

        /* ==================================================================
           AIM Running Man & Status Icons
           ================================================================== */
        public static Bitmap GetRunningManBitmap(int size, string status)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                float sc = size / 24f;
                g.ScaleTransform(sc, sc);

                int alpha = status == "invisible" ? 130 : (status == "offline" ? 160 : 255);
                Color bodyCol = status == "offline" ? Color.FromArgb(alpha, 175, 180, 190) : Color.FromArgb(alpha, 255, 204, 0);
                Color edgeCol = status == "offline" ? Color.FromArgb(90, 95, 105) : Color.FromArgb(77, 51, 0);

                using (Brush br = new SolidBrush(bodyCol))
                using (Pen pen = new Pen(edgeCol, 1.1f))
                {
                    g.FillEllipse(br, 10.5f, 2.1f, 5.4f, 5.4f);
                    g.DrawEllipse(pen, 10.5f, 2.1f, 5.4f, 5.4f);

                    PointF[] fig = new PointF[]
                    {
                        new PointF(7.5f, 10.2f), new PointF(12.3f, 8.1f), new PointF(17.3f, 9.7f),
                        new PointF(16.0f, 11.9f), new PointF(13.1f, 11.1f), new PointF(12.1f, 14.5f),
                        new PointF(15.9f, 18.6f), new PointF(14.1f, 20.6f), new PointF(10.3f, 16.5f),
                        new PointF(7.4f, 20.8f), new PointF(5.2f, 19.3f), new PointF(8.7f, 13.7f),
                        new PointF(9.6f, 10.7f), new PointF(6.5f, 12.0f)
                    };
                    g.FillPolygon(br, fig);
                    g.DrawPolygon(pen, fig);
                }

                if (status == "away")
                {
                    using (Brush note = new SolidBrush(Color.FromArgb(255, 243, 128)))
                    using (Pen pNote = new Pen(Color.FromArgb(122, 91, 0), 1.2f))
                    {
                        g.FillRectangle(note, 13f, 13f, 10f, 10f);
                        g.DrawRectangle(pNote, 13f, 13f, 10f, 10f);
                        g.DrawLine(pNote, 15f, 16.5f, 21f, 16.5f);
                        g.DrawLine(pNote, 15f, 19.5f, 19.5f, 19.5f);
                    }
                }
                else if (status == "busy")
                {
                    using (Brush bRed = new SolidBrush(Color.FromArgb(216, 36, 24)))
                    using (Pen pWh = new Pen(Color.White, 1.2f))
                    {
                        g.FillEllipse(bRed, 13f, 13f, 10f, 10f);
                        g.DrawEllipse(pWh, 13f, 13f, 10f, 10f);
                        g.FillRectangle(Brushes.White, 15f, 17f, 6f, 2f);
                    }
                }
                else if (status == "invisible")
                {
                    using (Brush bInv = new SolidBrush(Color.FromArgb(184, 200, 220)))
                    using (Pen pInv = new Pen(Color.FromArgb(45, 74, 110), 1.2f))
                    {
                        g.FillEllipse(bInv, 13f, 13f, 9.5f, 9.5f);
                        g.DrawEllipse(pInv, 13f, 13f, 9.5f, 9.5f);
                    }
                }
            }
            return bmp;
        }

        public static Icon CreateIconFromBitmap(Bitmap bmp)
        {
            IntPtr hIcon = bmp.GetHicon();
            try
            {
                using (Icon tmp = Icon.FromHandle(hIcon))
                {
                    return (Icon)tmp.Clone();
                }
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }

        public static Bitmap GetStatusDotBitmap(string status, int size)
        {
            if (status == "online") return GetRunningManBitmap(size, "online");
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                float sc = size / 16f;
                g.ScaleTransform(sc, sc);
                if (status == "away")
                {
                    using (Brush b = new SolidBrush(Color.FromArgb(255, 241, 118)))
                    using (Pen p = new Pen(Color.FromArgb(140, 109, 0), 1.2f))
                    {
                        g.FillRectangle(b, 2, 2, 12, 12);
                        g.DrawRectangle(p, 2, 2, 12, 12);
                        g.DrawLine(p, 4.5f, 6, 11.5f, 6);
                        g.DrawLine(p, 4.5f, 9, 9.5f, 9);
                    }
                }
                else if (status == "busy")
                {
                    using (Brush b = new SolidBrush(Color.FromArgb(217, 38, 28)))
                    using (Pen p = new Pen(Color.FromArgb(123, 14, 8), 1.2f))
                    {
                        g.FillEllipse(b, 2, 2, 12, 12);
                        g.DrawEllipse(p, 2, 2, 12, 12);
                        g.FillRectangle(Brushes.White, 4.5f, 7, 7, 2);
                    }
                }
                else if (status == "invisible")
                {
                    using (Brush b = new SolidBrush(Color.FromArgb(220, 230, 242)))
                    using (Pen p = new Pen(Color.FromArgb(83, 110, 143), 1.2f) { DashStyle = DashStyle.Dot })
                    {
                        g.FillEllipse(b, 2.5f, 2.5f, 11, 11);
                        g.DrawEllipse(p, 2.5f, 2.5f, 11, 11);
                    }
                }
                else
                {
                    using (Brush b = new SolidBrush(Color.FromArgb(196, 199, 206)))
                    using (Pen p = new Pen(Color.FromArgb(110, 115, 125), 1.2f))
                    {
                        g.FillEllipse(b, 3, 3, 10, 10);
                        g.DrawEllipse(p, 3, 3, 10, 10);
                    }
                }
            }
            return bmp;
        }

        private static GraphicsPath CreateRoundedRectPath(float x, float y, float w, float h, float r)
        {
            GraphicsPath path = new GraphicsPath();
            float d = r * 2f;
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + w - d, y, d, d, 270, 90);
            path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            path.AddArc(x, y + h - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static Bitmap GetIconBitmap(string name, int size)
        {
            if (name == "aim") return GetRunningManBitmap(size, "online");
            int renderSz = Math.Max(48, size * 2);
            using (Bitmap hi = new Bitmap(renderSz, renderSz, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(hi))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.Clear(Color.Transparent);
                    float sc = renderSz / 24f;
                    g.ScaleTransform(sc, sc);

                    if (name == "im")
                    {
                        using (GraphicsPath sh = CreateRoundedRectPath(2.8f, 4.2f, 18.2f, 13.6f, 3.6f))
                        using (SolidBrush brSh = new SolidBrush(Color.FromArgb(48, 0, 30, 80)))
                            g.FillPath(brSh, sh);

                        using (GraphicsPath bubble = CreateRoundedRectPath(2.0f, 3.2f, 18.2f, 13.6f, 3.6f))
                        using (LinearGradientBrush br = new LinearGradientBrush(new RectangleF(2, 3, 18.2f, 14),
                            Color.FromArgb(148, 212, 255), Color.FromArgb(24, 108, 222), LinearGradientMode.Vertical))
                        using (Pen p = new Pen(Color.FromArgb(12, 60, 142), 1.25f))
                        {
                            g.FillPath(br, bubble);
                            PointF[] tail = new[] { new PointF(5.5f, 16.0f), new PointF(4.4f, 20.6f), new PointF(10.0f, 16.2f) };
                            using (SolidBrush tailBr = new SolidBrush(Color.FromArgb(28, 112, 224)))
                                g.FillPolygon(tailBr, tail);
                            g.DrawLines(p, new[] { new PointF(5.5f, 16.5f), new PointF(4.4f, 20.6f), new PointF(10.0f, 16.5f) });
                            g.DrawPath(p, bubble);
                        }
                        using (GraphicsPath gloss = CreateRoundedRectPath(3.2f, 4.1f, 15.8f, 4.8f, 2.2f))
                        using (SolidBrush brGloss = new SolidBrush(Color.FromArgb(95, 255, 255, 255)))
                            g.FillPath(brGloss, gloss);

                        using (Pen pLine = new Pen(Color.White, 1.45f))
                        {
                            pLine.StartCap = LineCap.Round;
                            pLine.EndCap = LineCap.Round;
                            g.DrawLine(pLine, 5.5f, 8.6f, 15.8f, 8.6f);
                            g.DrawLine(pLine, 5.5f, 12.0f, 12.6f, 12.0f);
                        }

                        // Classic 2005 gold sparkle / AIM badge accent
                        using (LinearGradientBrush brGold = new LinearGradientBrush(new RectangleF(15, 1, 7.5f, 7.5f),
                            Color.FromArgb(255, 236, 98), Color.FromArgb(228, 148, 0), LinearGradientMode.Vertical))
                        using (Pen pGold = new Pen(Color.FromArgb(126, 76, 0), 1.0f))
                        {
                            g.FillEllipse(brGold, 15.2f, 1.2f, 7.2f, 7.2f);
                            g.DrawEllipse(pGold, 15.2f, 1.2f, 7.2f, 7.2f);
                            g.FillEllipse(Brushes.White, 17.0f, 2.5f, 2.0f, 2.0f);
                        }
                    }
                    else if (name == "chat")
                    {
                        using (GraphicsPath bBack = CreateRoundedRectPath(7.6f, 2.4f, 14.2f, 11.4f, 3.0f))
                        using (LinearGradientBrush brBack = new LinearGradientBrush(new RectangleF(7.6f, 2.4f, 14.2f, 11.4f),
                            Color.FromArgb(255, 232, 108), Color.FromArgb(232, 148, 12), LinearGradientMode.Vertical))
                        using (Pen pBack = new Pen(Color.FromArgb(136, 80, 0), 1.2f))
                        {
                            g.FillPath(brBack, bBack);
                            PointF[] t1 = new[] { new PointF(16.0f, 13.2f), new PointF(19.6f, 16.8f), new PointF(19.0f, 12.8f) };
                            using (SolidBrush sb1 = new SolidBrush(Color.FromArgb(232, 148, 12))) g.FillPolygon(sb1, t1);
                            g.DrawLines(pBack, new[] { new PointF(16.0f, 13.6f), new PointF(19.6f, 16.8f), new PointF(19.0f, 13.2f) });
                            g.DrawPath(pBack, bBack);
                        }
                        using (GraphicsPath bFront = CreateRoundedRectPath(2.0f, 7.2f, 14.8f, 11.6f, 3.0f))
                        using (LinearGradientBrush brFront = new LinearGradientBrush(new RectangleF(2.0f, 7.2f, 14.8f, 11.6f),
                            Color.FromArgb(148, 212, 255), Color.FromArgb(24, 108, 222), LinearGradientMode.Vertical))
                        using (Pen pFront = new Pen(Color.FromArgb(12, 60, 142), 1.2f))
                        {
                            g.FillPath(brFront, bFront);
                            PointF[] t2 = new[] { new PointF(5.0f, 18.2f), new PointF(4.0f, 22.0f), new PointF(8.6f, 18.4f) };
                            using (SolidBrush sb2 = new SolidBrush(Color.FromArgb(24, 108, 222))) g.FillPolygon(sb2, t2);
                            g.DrawLines(pFront, new[] { new PointF(5.0f, 18.6f), new PointF(4.0f, 22.0f), new PointF(8.6f, 18.6f) });
                            g.DrawPath(pFront, bFront);
                        }
                        using (Pen pW = new Pen(Color.White, 1.25f))
                        {
                            pW.StartCap = LineCap.Round; pW.EndCap = LineCap.Round;
                            g.DrawLine(pW, 5.2f, 11.4f, 13.2f, 11.4f);
                            g.DrawLine(pW, 5.2f, 14.4f, 10.6f, 14.4f);
                        }
                    }
                    else if (name == "add")
                    {
                        using (LinearGradientBrush brBody = new LinearGradientBrush(new RectangleF(2, 11, 13, 11),
                            Color.FromArgb(132, 198, 255), Color.FromArgb(20, 92, 198), LinearGradientMode.Vertical))
                        using (Pen pBody = new Pen(Color.FromArgb(14, 58, 138), 1.2f))
                        {
                            g.FillPie(brBody, 2.2f, 11.2f, 12.6f, 18.0f, 180, 180);
                            g.DrawArc(pBody, 2.2f, 11.2f, 12.6f, 18.0f, 180, 180);
                            g.DrawLine(pBody, 2.2f, 20.2f, 14.8f, 20.2f);
                        }
                        using (LinearGradientBrush brHead = new LinearGradientBrush(new RectangleF(4.5f, 3, 8, 8),
                            Color.FromArgb(255, 236, 178), Color.FromArgb(234, 164, 62), LinearGradientMode.Vertical))
                        using (Pen pHead = new Pen(Color.FromArgb(138, 84, 14), 1.15f))
                        {
                            g.FillEllipse(brHead, 4.5f, 3.0f, 8.0f, 8.0f);
                            g.DrawEllipse(pHead, 4.5f, 3.0f, 8.0f, 8.0f);
                        }
                        // Glossy 2005 XP green + badge
                        PointF[] plusPoly = new[]
                        {
                            new PointF(15.6f, 4.2f), new PointF(18.8f, 4.2f), new PointF(18.8f, 7.4f),
                            new PointF(22.0f, 7.4f), new PointF(22.0f, 10.6f), new PointF(18.8f, 10.6f),
                            new PointF(18.8f, 13.8f), new PointF(15.6f, 13.8f), new PointF(15.6f, 10.6f),
                            new PointF(12.4f, 10.6f), new PointF(12.4f, 7.4f), new PointF(15.6f, 7.4f)
                        };
                        using (LinearGradientBrush brPlus = new LinearGradientBrush(new RectangleF(12, 4, 10, 10),
                            Color.FromArgb(128, 234, 92), Color.FromArgb(22, 136, 26), LinearGradientMode.Vertical))
                        using (Pen pPlus = new Pen(Color.FromArgb(12, 78, 16), 1.15f))
                        {
                            g.FillPolygon(brPlus, plusPoly);
                            g.DrawPolygon(pPlus, plusPoly);
                        }
                    }
                    else if (name == "away")
                    {
                        // Classic 2005 sticky note with folded corner + XP pencil
                        using (SolidBrush sh = new SolidBrush(Color.FromArgb(48, 0, 0, 0)))
                            g.FillRectangle(sh, 3.8f, 3.6f, 15.2f, 17.6f);

                        PointF[] note = new[]
                        {
                            new PointF(2.8f, 2.6f), new PointF(18.2f, 2.6f),
                            new PointF(18.2f, 16.4f), new PointF(14.4f, 20.4f),
                            new PointF(2.8f, 20.4f)
                        };
                        using (LinearGradientBrush brNote = new LinearGradientBrush(new RectangleF(2.8f, 2.6f, 15.4f, 17.8f),
                            Color.FromArgb(255, 253, 172), Color.FromArgb(245, 208, 62), LinearGradientMode.Vertical))
                        using (Pen pNote = new Pen(Color.FromArgb(156, 114, 10), 1.2f))
                        {
                            g.FillPolygon(brNote, note);
                            g.DrawPolygon(pNote, note);
                            PointF[] fold = new[] { new PointF(14.4f, 16.4f), new PointF(18.2f, 16.4f), new PointF(14.4f, 20.4f) };
                            using (SolidBrush brFold = new SolidBrush(Color.FromArgb(220, 172, 32))) g.FillPolygon(brFold, fold);
                            g.DrawPolygon(pNote, fold);
                        }
                        using (Pen pTxt = new Pen(Color.FromArgb(154, 116, 20), 1.2f))
                        {
                            g.DrawLine(pTxt, 5.4f, 6.8f, 15.0f, 6.8f);
                            g.DrawLine(pTxt, 5.4f, 10.2f, 13.6f, 10.2f);
                            g.DrawLine(pTxt, 5.4f, 13.6f, 11.4f, 13.6f);
                        }
                        // Angled yellow XP pencil
                        using (Pen pBody = new Pen(Color.FromArgb(255, 184, 28), 3.0f))
                        using (Pen pOut = new Pen(Color.FromArgb(118, 72, 0), 1.0f))
                        using (Pen pEraser = new Pen(Color.FromArgb(230, 100, 110), 3.0f))
                        {
                            g.DrawLine(pBody, 12.8f, 14.8f, 19.6f, 8.0f);
                            g.DrawLine(pEraser, 19.6f, 8.0f, 21.2f, 6.4f);
                            g.FillPolygon(new SolidBrush(Color.FromArgb(60, 60, 60)),
                                new[] { new PointF(11.4f, 16.2f), new PointF(12.2f, 13.8f), new PointF(13.8f, 15.4f) });
                            g.DrawLine(pOut, 12.0f, 13.9f, 20.4f, 5.5f);
                            g.DrawLine(pOut, 13.9f, 15.8f, 22.1f, 7.6f);
                        }
                    }
                    else if (name == "profile")
                    {
                        // Classic XP Get-Info User Card with 'i' badge
                        using (SolidBrush sh = new SolidBrush(Color.FromArgb(46, 0, 30, 80)))
                            using (GraphicsPath shp = CreateRoundedRectPath(2.8f, 4.6f, 18.8f, 14.6f, 2.0f)) g.FillPath(sh, shp);

                        using (GraphicsPath card = CreateRoundedRectPath(2.0f, 3.6f, 18.8f, 14.6f, 2.0f))
                        using (LinearGradientBrush brCard = new LinearGradientBrush(new RectangleF(2, 3.6f, 18.8f, 14.6f),
                            Color.White, Color.FromArgb(214, 232, 252), LinearGradientMode.Vertical))
                        using (Pen pCard = new Pen(Color.FromArgb(24, 84, 174), 1.2f))
                        {
                            g.FillPath(brCard, card);
                            using (LinearGradientBrush brHdr = new LinearGradientBrush(new RectangleF(2, 3.6f, 18.8f, 3.6f),
                                Color.FromArgb(52, 132, 236), Color.FromArgb(14, 72, 168), LinearGradientMode.Vertical))
                                g.FillRectangle(brHdr, 2.4f, 4.0f, 18.0f, 3.4f);
                            g.DrawPath(pCard, card);
                        }
                        using (SolidBrush brUser = new SolidBrush(Color.FromArgb(46, 120, 214)))
                        {
                            g.FillEllipse(brUser, 4.4f, 9.0f, 4.4f, 4.4f);
                            g.FillPie(brUser, 3.8f, 13.6f, 5.6f, 5.4f, 180, 180);
                        }
                        using (Pen pLine = new Pen(Color.FromArgb(72, 118, 178), 1.2f))
                        {
                            g.DrawLine(pLine, 10.8f, 10.2f, 18.2f, 10.2f);
                            g.DrawLine(pLine, 10.8f, 13.2f, 17.0f, 13.2f);
                        }
                        // Glossy XP Info "i" sphere
                        using (LinearGradientBrush brInfo = new LinearGradientBrush(new RectangleF(14.6f, 13.0f, 8.2f, 8.2f),
                            Color.FromArgb(116, 194, 255), Color.FromArgb(14, 74, 182), LinearGradientMode.Vertical))
                        using (Pen pInfo = new Pen(Color.FromArgb(10, 52, 132), 1.05f))
                        {
                            g.FillEllipse(brInfo, 14.6f, 13.0f, 8.2f, 8.2f);
                            g.DrawEllipse(pInfo, 14.6f, 13.0f, 8.2f, 8.2f);
                            g.FillRectangle(Brushes.White, 18.0f, 14.6f, 1.5f, 1.4f);
                            g.FillRectangle(Brushes.White, 18.0f, 16.6f, 1.5f, 3.2f);
                        }
                    }
                    else if (name == "files" || name == "folder")
                    {
                        // Authentic XP 3D gold manila folder + paper + transfer arrow
                        PointF[] tab = new[]
                        {
                            new PointF(2.2f, 6.2f), new PointF(2.2f, 4.0f), new PointF(8.6f, 4.0f),
                            new PointF(10.6f, 6.2f), new PointF(19.8f, 6.2f), new PointF(19.8f, 18.8f),
                            new PointF(2.2f, 18.8f)
                        };
                        using (LinearGradientBrush brBack = new LinearGradientBrush(new RectangleF(2, 4, 18, 15),
                            Color.FromArgb(245, 200, 74), Color.FromArgb(196, 138, 12), LinearGradientMode.Vertical))
                        using (Pen pFold = new Pen(Color.FromArgb(144, 96, 0), 1.15f))
                        {
                            g.FillPolygon(brBack, tab);
                            g.DrawPolygon(pFold, tab);
                            g.FillRectangle(Brushes.White, 4.6f, 5.8f, 13.2f, 5.2f);
                            g.DrawRectangle(Pens.LightGray, 4.6f, 5.8f, 13.2f, 5.2f);

                            PointF[] front = new[]
                            {
                                new PointF(2.2f, 18.8f), new PointF(3.6f, 8.6f),
                                new PointF(21.4f, 8.6f), new PointF(19.8f, 18.8f)
                            };
                            using (LinearGradientBrush brFront = new LinearGradientBrush(new RectangleF(2, 8.6f, 19.4f, 10.2f),
                                Color.FromArgb(255, 236, 132), Color.FromArgb(234, 166, 22), LinearGradientMode.Vertical))
                                g.FillPolygon(brFront, front);
                            g.DrawPolygon(pFold, front);
                        }
                        if (name == "files")
                        {
                            PointF[] arr = new[]
                            {
                                new PointF(16.0f, 6.8f), new PointF(21.4f, 12.2f), new PointF(18.0f, 12.2f),
                                new PointF(18.0f, 19.8f), new PointF(14.0f, 19.8f), new PointF(14.0f, 12.2f),
                                new PointF(10.6f, 12.2f)
                            };
                            using (LinearGradientBrush brArr = new LinearGradientBrush(new RectangleF(10.6f, 6.8f, 11, 13),
                                Color.FromArgb(118, 198, 255), Color.FromArgb(10, 76, 180), LinearGradientMode.Vertical))
                            using (Pen pArr = new Pen(Color.FromArgb(8, 48, 126), 1.1f))
                            {
                                g.FillPolygon(brArr, arr);
                                g.DrawPolygon(pArr, arr);
                            }
                        }
                    }
                    else if (name == "history")
                    {
                        // XP 2005 Luna clock with metallic bezel + orange rewind arc arrow
                        using (LinearGradientBrush brRim = new LinearGradientBrush(new RectangleF(2.5f, 2.2f, 19, 19),
                            Color.FromArgb(204, 228, 255), Color.FromArgb(46, 104, 192), LinearGradientMode.Vertical))
                        using (Pen pRim = new Pen(Color.FromArgb(16, 62, 138), 1.2f))
                        {
                            g.FillEllipse(brRim, 2.5f, 2.2f, 19.0f, 19.0f);
                            g.DrawEllipse(pRim, 2.5f, 2.2f, 19.0f, 19.0f);
                        }
                        using (LinearGradientBrush brDial = new LinearGradientBrush(new RectangleF(5, 4.7f, 14, 14),
                            Color.White, Color.FromArgb(224, 236, 250), LinearGradientMode.Vertical))
                        using (Pen pInner = new Pen(Color.FromArgb(92, 132, 192), 1.0f))
                        {
                            g.FillEllipse(brDial, 5.0f, 4.7f, 14.0f, 14.0f);
                            g.DrawEllipse(pInner, 5.0f, 4.7f, 14.0f, 14.0f);
                        }
                        using (Pen pTick = new Pen(Color.FromArgb(64, 96, 144), 1.2f))
                        {
                            g.DrawLine(pTick, 12.0f, 5.6f, 12.0f, 7.0f);
                            g.DrawLine(pTick, 17.8f, 11.7f, 16.4f, 11.7f);
                        }
                        using (Pen pHand = new Pen(Color.FromArgb(24, 48, 92), 1.55f))
                        {
                            pHand.StartCap = LineCap.Round; pHand.EndCap = LineCap.Round;
                            g.DrawLine(pHand, 12.0f, 11.7f, 12.0f, 7.6f);
                            g.DrawLine(pHand, 12.0f, 11.7f, 15.6f, 13.6f);
                        }
                        using (Pen pArc = new Pen(Color.FromArgb(220, 68, 12), 2.1f))
                        {
                            g.DrawArc(pArc, 2.0f, 2.0f, 19.6f, 19.6f, 132, 72);
                        }
                        using (SolidBrush brTip = new SolidBrush(Color.FromArgb(220, 68, 12)))
                            g.FillPolygon(brTip, new[] { new PointF(2.2f, 14.8f), new PointF(5.2f, 17.6f), new PointF(1.2f, 18.6f) });
                    }
                    else if (name == "settings")
                    {
                        // Authentic AIM 5.9 Setup Cogwheel + Diagonal Metallic XP Wrench
                        GraphicsState stGear = g.Save();
                        g.TranslateTransform(9.8f, 10.6f);
                        using (GraphicsPath gearPath = new GraphicsPath())
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                double a1 = (i * 45.0 - 11.0) * Math.PI / 180.0;
                                double a2 = (i * 45.0 + 11.0) * Math.PI / 180.0;
                                double a3 = ((i + 1) * 45.0 - 11.0) * Math.PI / 180.0;
                                float rOut = 7.8f, rIn = 5.4f;
                                if (i == 0) gearPath.StartFigure();
                                gearPath.AddLine((float)(Math.Cos(a1) * rOut), (float)(Math.Sin(a1) * rOut),
                                                 (float)(Math.Cos(a2) * rOut), (float)(Math.Sin(a2) * rOut));
                                gearPath.AddLine((float)(Math.Cos(a2) * rOut), (float)(Math.Sin(a2) * rOut),
                                                 (float)(Math.Cos(a2 + 0.16) * rIn), (float)(Math.Sin(a2 + 0.16) * rIn));
                                gearPath.AddLine((float)(Math.Cos(a2 + 0.16) * rIn), (float)(Math.Sin(a2 + 0.16) * rIn),
                                                 (float)(Math.Cos(a3 - 0.16) * rIn), (float)(Math.Sin(a3 - 0.16) * rIn));
                            }
                            gearPath.CloseFigure();
                            using (LinearGradientBrush brGear = new LinearGradientBrush(new RectangleF(-8, -8, 16, 16),
                                Color.FromArgb(240, 246, 255), Color.FromArgb(124, 150, 188), LinearGradientMode.Vertical))
                            using (Pen pGear = new Pen(Color.FromArgb(52, 78, 114), 1.15f))
                            {
                                g.FillPath(brGear, gearPath);
                                g.DrawPath(pGear, gearPath);
                                g.FillEllipse(Brushes.White, -2.5f, -2.5f, 5.0f, 5.0f);
                                g.DrawEllipse(pGear, -2.5f, -2.5f, 5.0f, 5.0f);
                            }
                        }
                        g.Restore(stGear);

                        // Overlapping Diagonal Silver XP Wrench
                        GraphicsState stW = g.Save();
                        g.TranslateTransform(14.6f, 14.6f);
                        g.RotateTransform(45f);
                        using (LinearGradientBrush brW = new LinearGradientBrush(new RectangleF(-3, -8, 6, 17),
                            Color.FromArgb(255, 255, 255), Color.FromArgb(148, 172, 204), LinearGradientMode.Horizontal))
                        using (Pen pW = new Pen(Color.FromArgb(46, 72, 110), 1.15f))
                        {
                            PointF[] wrench = new[]
                            {
                                new PointF(-2.2f, -3.5f), new PointF(-3.4f, -6.4f), new PointF(-1.4f, -8.2f),
                                new PointF(-1.4f, -5.2f), new PointF(1.4f, -5.2f), new PointF(1.4f, -8.2f),
                                new PointF(3.4f, -6.4f), new PointF(2.2f, -3.5f), new PointF(1.8f, 6.8f),
                                new PointF(0.0f, 8.2f), new PointF(-1.8f, 6.8f)
                            };
                            g.FillPolygon(brW, wrench);
                            g.DrawPolygon(pW, wrench);
                            using (SolidBrush brGrip = new SolidBrush(Color.FromArgb(58, 126, 218)))
                                g.FillRectangle(brGrip, -1.0f, 0.2f, 2.0f, 5.2f);
                        }
                        g.Restore(stW);
                    }
                    else if (name == "accounts")
                    {
                        using (LinearGradientBrush brB1 = new LinearGradientBrush(new RectangleF(10, 11, 11, 10),
                            Color.FromArgb(164, 212, 255), Color.FromArgb(54, 126, 216), LinearGradientMode.Vertical))
                        using (Pen pB = new Pen(Color.FromArgb(14, 58, 138), 1.1f))
                        {
                            g.FillPie(brB1, 10.5f, 11.5f, 10.6f, 14.0f, 180, 180);
                            g.DrawArc(pB, 10.5f, 11.5f, 10.6f, 14.0f, 180, 180);
                            g.FillEllipse(new SolidBrush(Color.FromArgb(255, 212, 92)), 12.6f, 4.8f, 6.2f, 6.2f);
                            g.DrawEllipse(new Pen(Color.FromArgb(138, 84, 14), 1.0f), 12.6f, 4.8f, 6.2f, 6.2f);
                        }
                        using (LinearGradientBrush brB2 = new LinearGradientBrush(new RectangleF(2, 11.5f, 12, 10),
                            Color.FromArgb(128, 196, 255), Color.FromArgb(22, 94, 202), LinearGradientMode.Vertical))
                        using (Pen pB = new Pen(Color.FromArgb(14, 58, 138), 1.15f))
                        {
                            g.FillPie(brB2, 2.2f, 12.0f, 12.0f, 16.0f, 180, 180);
                            g.DrawArc(pB, 2.2f, 12.0f, 12.0f, 16.0f, 180, 180);
                            g.FillEllipse(new SolidBrush(Color.FromArgb(255, 224, 118)), 4.6f, 3.6f, 7.2f, 7.2f);
                            g.DrawEllipse(new Pen(Color.FromArgb(138, 84, 14), 1.1f), 4.6f, 3.6f, 7.2f, 7.2f);
                        }
                    }
                    else if (name == "help")
                    {
                        using (LinearGradientBrush br = new LinearGradientBrush(new RectangleF(2, 2, 20, 20),
                            Color.FromArgb(118, 194, 255), Color.FromArgb(14, 72, 178), LinearGradientMode.Vertical))
                        using (Pen p = new Pen(Color.FromArgb(10, 48, 124), 1.25f))
                        using (Font f = new Font("Tahoma", 11.5f, FontStyle.Bold))
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            g.FillEllipse(br, 2.2f, 2.2f, 19.6f, 19.6f);
                            g.DrawEllipse(p, 2.2f, 2.2f, 19.6f, 19.6f);
                            g.DrawString("?", f, Brushes.White, new RectangleF(2, 2.5f, 20, 20), sf);
                        }
                    }
                    else if (name == "apikey")
                    {
                        // Classic 2005 45-degree glossy XP golden security key
                        GraphicsState stK = g.Save();
                        g.TranslateTransform(12.0f, 12.0f);
                        g.RotateTransform(-42f);
                        using (LinearGradientBrush brKey = new LinearGradientBrush(new RectangleF(-10, -5, 20, 10),
                            Color.FromArgb(255, 242, 134), Color.FromArgb(212, 138, 0), LinearGradientMode.Vertical))
                        using (Pen pKey = new Pen(Color.FromArgb(118, 68, 0), 1.15f))
                        {
                            g.FillRectangle(brKey, -1.5f, -1.6f, 10.6f, 3.2f);
                            g.DrawRectangle(pKey, -1.5f, -1.6f, 10.6f, 3.2f);
                            g.FillRectangle(brKey, 4.4f, 1.6f, 2.2f, 3.2f);
                            g.DrawRectangle(pKey, 4.4f, 1.6f, 2.2f, 3.2f);
                            g.FillRectangle(brKey, 7.2f, 1.6f, 2.0f, 2.2f);
                            g.DrawRectangle(pKey, 7.2f, 1.6f, 2.0f, 2.2f);

                            g.FillEllipse(brKey, -9.8f, -4.6f, 9.2f, 9.2f);
                            g.DrawEllipse(pKey, -9.8f, -4.6f, 9.2f, 9.2f);
                            g.FillEllipse(Brushes.White, -7.4f, -1.8f, 3.2f, 3.2f);
                            g.DrawEllipse(pKey, -7.4f, -1.8f, 3.2f, 3.2f);
                        }
                        g.Restore(stK);
                    }
                    else if (name == "update")
                    {
                        using (LinearGradientBrush brSh = new LinearGradientBrush(new RectangleF(2.5f, 2, 19, 20),
                            Color.FromArgb(124, 224, 96), Color.FromArgb(20, 134, 34), LinearGradientMode.Vertical))
                        using (Pen pSh = new Pen(Color.FromArgb(14, 82, 20), 1.2f))
                        {
                            g.FillEllipse(brSh, 2.5f, 2.2f, 19.0f, 19.0f);
                            g.DrawEllipse(pSh, 2.5f, 2.2f, 19.0f, 19.0f);
                        }
                        PointF[] dn = new[]
                        {
                            new PointF(12.0f, 18.2f), new PointF(6.5f, 12.2f), new PointF(10.0f, 12.2f),
                            new PointF(10.0f, 6.0f), new PointF(14.0f, 6.0f), new PointF(14.0f, 12.2f),
                            new PointF(17.5f, 12.2f)
                        };
                        g.FillPolygon(Brushes.White, dn);
                    }
                    else if (name == "support")
                    {
                        using (Pen pRing = new Pen(Color.FromArgb(214, 54, 36), 4.0f))
                        using (Pen pOut = new Pen(Color.FromArgb(124, 24, 16), 1.0f))
                        {
                            g.DrawEllipse(pRing, 4.2f, 4.2f, 15.6f, 15.6f);
                            g.DrawEllipse(pOut, 2.2f, 2.2f, 19.6f, 19.6f);
                            g.DrawEllipse(pOut, 6.2f, 6.2f, 11.6f, 11.6f);
                            using (Pen pW = new Pen(Color.White, 3.2f))
                            {
                                g.DrawLine(pW, 12.0f, 2.2f, 12.0f, 6.2f);
                                g.DrawLine(pW, 12.0f, 17.8f, 12.0f, 21.8f);
                                g.DrawLine(pW, 2.2f, 12.0f, 6.2f, 12.0f);
                                g.DrawLine(pW, 17.8f, 12.0f, 21.8f, 12.0f);
                            }
                        }
                    }
                    else if (name == "translate")
                    {
                        using (LinearGradientBrush brGl = new LinearGradientBrush(new RectangleF(2.5f, 2.2f, 18.5f, 18.5f),
                            Color.FromArgb(128, 200, 255), Color.FromArgb(18, 92, 198), LinearGradientMode.Vertical))
                        using (Pen pGl = new Pen(Color.FromArgb(12, 52, 134), 1.15f))
                        using (Pen pGrid = new Pen(Color.FromArgb(185, 255, 255, 255), 1.0f))
                        {
                            g.FillEllipse(brGl, 2.5f, 2.5f, 18.5f, 18.5f);
                            g.DrawEllipse(pGrid, 7.5f, 2.6f, 8.5f, 18.2f);
                            g.DrawLine(pGrid, 2.8f, 11.8f, 20.8f, 11.8f);
                            g.DrawEllipse(pGl, 2.5f, 2.5f, 18.5f, 18.5f);
                        }
                        using (Font ft = new Font("Tahoma", 7.5f, FontStyle.Bold))
                        using (StringFormat sft = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            g.DrawString("A", ft, Brushes.Yellow, new RectangleF(3, 3, 18, 18), sft);
                        }
                    }
                    else if (name == "mail")
                    {
                        using (LinearGradientBrush brEnv = new LinearGradientBrush(new RectangleF(2f, 5f, 20f, 14f),
                            Color.FromArgb(255, 250, 212), Color.FromArgb(245, 204, 102), LinearGradientMode.Vertical))
                        using (Pen pEnv = new Pen(Color.FromArgb(156, 108, 18), 1.25f))
                        using (Pen pFlap = new Pen(Color.FromArgb(180, 126, 26), 1.2f))
                        {
                            g.FillRectangle(brEnv, 2.2f, 5.2f, 19.6f, 13.6f);
                            g.DrawRectangle(pEnv, 2.2f, 5.2f, 19.6f, 13.6f);
                            g.DrawLine(pFlap, 2.6f, 5.8f, 12.0f, 12.8f);
                            g.DrawLine(pFlap, 21.4f, 5.8f, 12.0f, 12.8f);
                        }
                        using (SolidBrush brSeal = new SolidBrush(Color.FromArgb(0, 78, 178)))
                        {
                            g.FillEllipse(brSeal, 14.8f, 3.0f, 6.8f, 6.8f);
                            g.DrawEllipse(Pens.White, 14.8f, 3.0f, 6.8f, 6.8f);
                        }
                    }
                    else if (name == "workshop")
                    {
                        using (LinearGradientBrush brPal = new LinearGradientBrush(new RectangleF(2f, 3f, 19f, 17f),
                            Color.FromArgb(252, 240, 210), Color.FromArgb(224, 188, 132), LinearGradientMode.Vertical))
                        using (Pen pPal = new Pen(Color.FromArgb(134, 96, 52), 1.2f))
                        {
                            g.FillEllipse(brPal, 2.2f, 3.5f, 18.6f, 16.5f);
                            g.DrawEllipse(pPal, 2.2f, 3.5f, 18.6f, 16.5f);
                        }
                        using (SolidBrush brHole = new SolidBrush(Color.FromArgb(246, 244, 236)))
                            g.FillEllipse(brHole, 6.0f, 12.8f, 4.0f, 3.4f);
                        using (SolidBrush rB = new SolidBrush(Color.FromArgb(228, 56, 48))) g.FillEllipse(rB, 6.0f, 6.2f, 3.6f, 3.6f);
                        using (SolidBrush bB = new SolidBrush(Color.FromArgb(24, 152, 232))) g.FillEllipse(bB, 11.0f, 5.4f, 3.6f, 3.6f);
                        using (SolidBrush gB = new SolidBrush(Color.FromArgb(52, 184, 72))) g.FillEllipse(gB, 15.2f, 8.2f, 3.6f, 3.6f);
                        using (SolidBrush yB = new SolidBrush(Color.FromArgb(246, 198, 22))) g.FillEllipse(yB, 13.8f, 13.4f, 3.6f, 3.6f);
                        using (Pen pBrush = new Pen(Color.FromArgb(30, 108, 198), 2.2f))
                            g.DrawLine(pBrush, 19.5f, 4.2f, 11.2f, 14.5f);
                    }
                    else if (name == "music")
                    {
                        // Glossy aqua disc + XP musical eighth-note
                        using (LinearGradientBrush brDisc = new LinearGradientBrush(new RectangleF(2.5f, 3f, 18f, 18f),
                            Color.FromArgb(148, 232, 255), Color.FromArgb(14, 122, 206), LinearGradientMode.ForwardDiagonal))
                        using (Pen pDisc = new Pen(Color.FromArgb(10, 74, 142), 1.15f))
                        {
                            g.FillEllipse(brDisc, 2.5f, 3.0f, 18.0f, 18.0f);
                            g.DrawEllipse(pDisc, 2.5f, 3.0f, 18.0f, 18.0f);
                        }
                        using (SolidBrush brHole = new SolidBrush(Color.White))
                            g.FillEllipse(brHole, 9.2f, 9.8f, 4.6f, 4.6f);
                        using (SolidBrush brNote = new SolidBrush(Color.FromArgb(255, 214, 24)))
                        using (Pen pNote = new Pen(Color.FromArgb(110, 62, 0), 1.0f))
                        using (Pen pStem = new Pen(Color.FromArgb(255, 224, 54), 2.1f))
                        {
                            g.DrawLine(pStem, 16.5f, 4.5f, 16.5f, 16.5f);
                            g.DrawLine(pStem, 16.5f, 4.5f, 20.5f, 6.2f);
                            g.FillEllipse(brNote, 11.5f, 14.5f, 6.2f, 4.8f);
                            g.DrawEllipse(pNote, 11.5f, 14.5f, 6.2f, 4.8f);
                        }
                    }
                    else if (name == "groups")
                    {
                        using (LinearGradientBrush brGl = new LinearGradientBrush(new RectangleF(2f, 2f, 20f, 20f),
                            Color.FromArgb(104, 222, 168), Color.FromArgb(16, 134, 182), LinearGradientMode.Vertical))
                        using (Pen pGl = new Pen(Color.FromArgb(12, 74, 118), 1.15f))
                        {
                            g.FillEllipse(brGl, 3.0f, 3.0f, 18.0f, 18.0f);
                            g.DrawEllipse(pGl, 3.0f, 3.0f, 18.0f, 18.0f);
                        }
                        using (SolidBrush bBack = new SolidBrush(Color.FromArgb(255, 212, 42)))
                        using (SolidBrush bFront = new SolidBrush(Color.White))
                        using (Pen pOut = new Pen(Color.FromArgb(14, 52, 108), 1.0f))
                        {
                            g.FillEllipse(bBack, 11.5f, 7.2f, 4.8f, 4.8f);
                            g.DrawEllipse(pOut, 11.5f, 7.2f, 4.8f, 4.8f);
                            g.FillEllipse(bFront, 7.2f, 8.6f, 5.4f, 5.4f);
                            g.DrawEllipse(pOut, 7.2f, 8.6f, 5.4f, 5.4f);
                            g.FillPie(bFront, 5.2f, 14.0f, 9.6f, 6.4f, 180, 180);
                            g.DrawArc(pOut, 5.2f, 14.0f, 9.6f, 6.4f, 180, 180);
                        }
                    }
                    else if (name == "phone")
                    {
                        using (LinearGradientBrush brPh = new LinearGradientBrush(new RectangleF(5.5f, 2f, 13f, 20f),
                            Color.FromArgb(236, 244, 255), Color.FromArgb(148, 178, 214), LinearGradientMode.Vertical))
                        using (Pen pPh = new Pen(Color.FromArgb(32, 62, 112), 1.3f))
                        {
                            g.FillRectangle(brPh, 6.0f, 2.5f, 12.0f, 19.0f);
                            g.DrawRectangle(pPh, 6.0f, 2.5f, 12.0f, 19.0f);
                        }
                        using (LinearGradientBrush brSc = new LinearGradientBrush(new RectangleF(7.5f, 4.5f, 9f, 12f),
                            Color.FromArgb(88, 210, 255), Color.FromArgb(14, 108, 194), LinearGradientMode.Vertical))
                        {
                            g.FillRectangle(brSc, 7.6f, 4.6f, 8.8f, 12.2f);
                        }
                        using (SolidBrush brDot = new SolidBrush(Color.FromArgb(36, 68, 118)))
                            g.FillEllipse(brDot, 11.0f, 18.0f, 2.0f, 2.0f);
                    }
                }

                Bitmap finalBmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                using (Graphics gf = Graphics.FromImage(finalBmp))
                {
                    gf.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    gf.SmoothingMode = SmoothingMode.HighQuality;
                    gf.DrawImage(hi, 0, 0, size, size);
                }
                return finalBmp;
            }
        }

        /* ==================================================================
           #12 Classic 2000s Emoticons + RichTextBox DIB-RTF Renderer
           ================================================================== */
        private static void BuildEmoticons()
        {
            foreach (EmoticonDef em in Emoticons)
            {
                Bitmap bmp = DrawEmoticonBitmap(em.Face, 16, Color.White);
                _emoticonBmps[em.Face] = bmp;

                using (MemoryStream ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Bmp);
                    byte[] raw = ms.ToArray();
                    byte[] dib = new byte[raw.Length - 14];
                    Array.Copy(raw, 14, dib, 0, dib.Length);
                    StringBuilder hex = new StringBuilder(dib.Length * 2);
                    foreach (byte bt in dib) hex.Append(bt.ToString("x2"));
                    _emoticonDibHex[em.Face] = hex.ToString();
                }
            }
        }

        public static Bitmap GetEmoticonBitmap(string face)
        {
            Bitmap b;
            return _emoticonBmps.TryGetValue(face, out b) ? b : _emoticonBmps["smile"];
        }

        private static Bitmap DrawEmoticonBitmap(string face, int size, Color bg)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(bg);
                float sc = size / 18f;
                g.ScaleTransform(sc, sc);

                if (face == "heart")
                {
                    using (Brush b = new SolidBrush(Color.FromArgb(238, 34, 51)))
                    using (Pen p = new Pen(Color.FromArgb(122, 0, 16), 1.2f))
                    {
                        g.FillEllipse(b, 2.5f, 3f, 6.8f, 6.8f);
                        g.FillEllipse(b, 8.7f, 3f, 6.8f, 6.8f);
                        PointF[] tri = new PointF[] { new PointF(2.7f, 7.2f), new PointF(15.3f, 7.2f), new PointF(9f, 15.5f) };
                        g.FillPolygon(b, tri);
                        g.DrawArc(p, 2.5f, 3f, 6.8f, 6.8f, 150, 200);
                        g.DrawArc(p, 8.7f, 3f, 6.8f, 6.8f, 190, 200);
                        g.DrawLine(p, 2.8f, 7.8f, 9f, 15.5f);
                        g.DrawLine(p, 15.2f, 7.8f, 9f, 15.5f);
                    }
                    return bmp;
                }

                Color cTop = Color.FromArgb(255, 248, 184);
                Color cBot = face == "angry" ? Color.FromArgb(255, 130, 80) : (face == "devil" ? Color.FromArgb(245, 82, 82) : Color.FromArgb(255, 205, 32));
                Color cEdge = face == "devil" ? Color.FromArgb(122, 0, 0) : Color.FromArgb(128, 88, 0);

                using (LinearGradientBrush br = new LinearGradientBrush(new RectangleF(1.8f, 1.8f, 14.4f, 14.4f), cTop, cBot, LinearGradientMode.ForwardDiagonal))
                using (Pen p = new Pen(cEdge, 1.1f))
                {
                    g.FillEllipse(br, 1.8f, 1.8f, 14.4f, 14.4f);
                    g.DrawEllipse(p, 1.8f, 1.8f, 14.4f, 14.4f);
                }

                using (Pen blk = new Pen(Color.FromArgb(17, 17, 17), 1.3f))
                {
                    // Eyes
                    if (face == "wink")
                    {
                        g.DrawArc(blk, 5f, 5.8f, 3.2f, 2.6f, 190, 160);
                        g.FillEllipse(Brushes.Black, 10.6f, 6f, 2.2f, 2.2f);
                    }
                    else if (face == "cool")
                    {
                        g.FillRectangle(Brushes.Black, 3.8f, 5.5f, 4.5f, 3f);
                        g.FillRectangle(Brushes.Black, 9.7f, 5.5f, 4.5f, 3f);
                        g.DrawLine(blk, 8.3f, 6.8f, 9.7f, 6.8f);
                    }
                    else if (face == "angry")
                    {
                        g.DrawLine(blk, 4.8f, 5.2f, 7.8f, 6.6f);
                        g.DrawLine(blk, 13.2f, 5.2f, 10.2f, 6.6f);
                        g.FillEllipse(Brushes.Black, 5.5f, 6.8f, 2f, 2f);
                        g.FillEllipse(Brushes.Black, 10.5f, 6.8f, 2f, 2f);
                    }
                    else if (face == "sleepy")
                    {
                        g.DrawLine(blk, 4.8f, 7.2f, 7.8f, 7.2f);
                        g.DrawLine(blk, 10.2f, 7.2f, 13.2f, 7.2f);
                    }
                    else
                    {
                        g.FillEllipse(Brushes.Black, 5.4f, 6f, 2.2f, 2.2f);
                        g.FillEllipse(Brushes.Black, 10.4f, 6f, 2.2f, 2.2f);
                    }

                    // Mouth / Extras
                    if (face == "sad" || face == "cry" || face == "angry")
                    {
                        g.DrawArc(blk, 5.5f, 10.8f, 7f, 4f, 195, 150);
                        if (face == "cry") g.FillEllipse(Brushes.DeepSkyBlue, 5.6f, 8.8f, 2f, 3f);
                    }
                    else if (face == "grin")
                    {
                        g.FillPie(Brushes.White, 5f, 8.5f, 8f, 6f, 0, 180);
                        g.DrawPie(blk, 5f, 8.5f, 8f, 6f, 0, 180);
                    }
                    else if (face == "tongue")
                    {
                        g.DrawLine(blk, 5.5f, 11f, 12.5f, 11f);
                        g.FillPie(Brushes.DeepPink, 7.4f, 9.6f, 3.2f, 4.6f, 0, 180);
                    }
                    else if (face == "surprised")
                    {
                        g.FillEllipse(Brushes.Maroon, 7.2f, 10f, 3.6f, 4.2f);
                    }
                    else if (face == "confused")
                    {
                        g.DrawLine(blk, 5.8f, 12.5f, 12.2f, 10.8f);
                    }
                    else if (face == "sleepy")
                    {
                        g.FillEllipse(Brushes.Maroon, 7.8f, 11f, 2.4f, 2.4f);
                    }
                    else
                    {
                        g.DrawArc(blk, 5.4f, 8.4f, 7.2f, 4.8f, 15, 150);
                    }

                    if (face == "angel")
                    {
                        using (Pen halo = new Pen(Color.DodgerBlue, 1.3f))
                        {
                            g.DrawEllipse(halo, 4.5f, 0.9f, 9f, 2.3f);
                        }
                    }
                    else if (face == "devil")
                    {
                        g.FillPolygon(Brushes.DarkRed, new[] { new PointF(4.2f, 4.5f), new PointF(2.8f, 1.2f), new PointF(6.2f, 3.2f) });
                        g.FillPolygon(Brushes.DarkRed, new[] { new PointF(13.8f, 4.5f), new PointF(15.2f, 1.2f), new PointF(11.8f, 3.2f) });
                    }
                    else if (face == "blush" || face == "kiss")
                    {
                        using (Brush blush = new SolidBrush(Color.FromArgb(180, 255, 110, 130)))
                        {
                            g.FillEllipse(blush, 3.8f, 9.2f, 2.6f, 2f);
                            g.FillEllipse(blush, 11.6f, 9.2f, 2.6f, 2f);
                        }
                    }
                }
            }
            return bmp;
        }

        public static void AppendChatMessageToRtb(RichTextBox rtb, ChatMessage msg, string mySN, bool showTime, bool enableEmoticons)
        {
            if (rtb == null || msg == null) return;
            StringBuilder rtf = new StringBuilder();
            Color msgCol = ParseHexColor(msg.Color, Color.Black);

            // Color table: cf1=PeerBlue, cf2=MeRed, cf3=SysSlate, cf4=TimeGray, cf5=MsgColor
            rtf.Append(@"{\rtf1\ansi\ansicpg1251\deff0\uc1");
            rtf.Append(@"{\fonttbl{\f0\fnil\fcharset204 Tahoma;}{\f1\fnil\fcharset204 ");
            rtf.Append(EscapeRtfAscii(string.IsNullOrEmpty(msg.Font) ? "Tahoma" : msg.Font));
            rtf.Append(";}}");
            rtf.Append(@"{\colortbl;\red0\green0\blue220;\red204\green0\blue0;\red77\green106\blue142;\red110\green110\blue110;");
            rtf.AppendFormat(@"\red{0}\green{1}\blue{2};}}", msgCol.R, msgCol.G, msgCol.B);

            rtf.Append(@"\pard\sa45\fs19\f0 ");

            if (msg.Sender == "SYSTEM")
            {
                rtf.Append(@"\cf3\i ");
                AppendTextWithEmoticonsRtf(rtf, msg.Text ?? "", enableEmoticons);
                rtf.Append(@"\i0\par}");
            }
            else
            {
                bool isMe = string.Equals(msg.Sender, mySN, StringComparison.OrdinalIgnoreCase);
                rtf.Append(isMe ? @"\cf2\b " : @"\cf1\b ");
                AppendRtfEscaped(rtf, msg.Sender ?? "");
                rtf.Append(@"\b0 ");
                if (showTime && !string.IsNullOrEmpty(msg.Time))
                {
                    rtf.Append(@"\cf4\fs16 (");
                    AppendRtfEscaped(rtf, msg.Time);
                    rtf.Append(@")\fs19 ");
                }
                rtf.Append(@"\cf0 : \f1\cf5 ");
                if (msg.Bold) rtf.Append(@"\b ");
                if (msg.Italic) rtf.Append(@"\i ");
                if (msg.Underline) rtf.Append(@"\ul ");

                AppendTextWithEmoticonsRtf(rtf, msg.Text ?? "", enableEmoticons);

                if (msg.Underline) rtf.Append(@"\ulnone ");
                if (msg.Italic) rtf.Append(@"\i0 ");
                if (msg.Bold) rtf.Append(@"\b0 ");
                rtf.Append(@"\par}");
            }

            rtb.Select(rtb.TextLength, 0);
            rtb.SelectedRtf = rtf.ToString();
            rtb.SelectionStart = rtb.TextLength;
            rtb.ScrollToCaret();
        }

        private static void AppendTextWithEmoticonsRtf(StringBuilder rtf, string text, bool enableEmoticons)
        {
            if (!enableEmoticons)
            {
                AppendRtfEscaped(rtf, text);
                return;
            }

            // Build token list longest-first while avoiding :/ inside http:// or https://
            List<KeyValuePair<string, string>> tokens = new List<KeyValuePair<string, string>>();
            foreach (EmoticonDef em in Emoticons)
            {
                tokens.Add(new KeyValuePair<string, string>(em.Code, em.Face));
                foreach (string al in em.Aliases)
                {
                    tokens.Add(new KeyValuePair<string, string>(al, em.Face));
                }
            }
            tokens.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length));

            // Protect URLs first
            MatchCollection urls = Regex.Matches(text, @"https?://[^\s]+", RegexOptions.IgnoreCase);
            int idx = 0;
            while (idx < text.Length)
            {
                // Skip emoticon matching inside URL spans
                bool insideUrl = false;
                foreach (Match um in urls)
                {
                    if (idx >= um.Index && idx < um.Index + um.Length)
                    {
                        AppendRtfEscaped(rtf, um.Value);
                        idx = um.Index + um.Length;
                        insideUrl = true;
                        break;
                    }
                }
                if (insideUrl) continue;

                string matchedFace = null;
                int matchLen = 0;
                foreach (KeyValuePair<string, string> tk in tokens)
                {
                    if (idx + tk.Key.Length <= text.Length && string.Compare(text, idx, tk.Key, 0, tk.Key.Length, StringComparison.Ordinal) == 0)
                    {
                        matchedFace = tk.Value;
                        matchLen = tk.Key.Length;
                        break;
                    }
                }

                if (matchedFace != null)
                {
                    string hex;
                    if (_emoticonDibHex.TryGetValue(matchedFace, out hex))
                    {
                        rtf.Append(@"{\pict\dibitmap0\picw16\pich16\picwgoal240\pichgoal240 ");
                        rtf.Append(hex);
                        rtf.Append("}");
                    }
                    else
                    {
                        AppendRtfEscaped(rtf, text.Substring(idx, matchLen));
                    }
                    idx += matchLen;
                }
                else
                {
                    AppendRtfChar(rtf, text[idx]);
                    idx++;
                }
            }
        }

        private static string EscapeRtfAscii(string s)
        {
            return (s ?? "").Replace(@"\", @"\\").Replace("{", @"\{").Replace("}", @"\}");
        }

        private static void AppendRtfEscaped(StringBuilder rtf, string s)
        {
            foreach (char c in s) AppendRtfChar(rtf, c);
        }

        private static void AppendRtfChar(StringBuilder rtf, char c)
        {
            if (c == '\\' || c == '{' || c == '}')
            {
                rtf.Append('\\').Append(c);
            }
            else if (c == '\r')
            {
                // ignore \r before \n
            }
            else if (c == '\n')
            {
                rtf.Append(@"\line ");
            }
            else if (c > 127)
            {
                short u = unchecked((short)c);
                rtf.AppendFormat(@"\u{0}?", u);
            }
            else
            {
                rtf.Append(c);
            }
        }

        private static Color ParseHexColor(string hex, Color fallback)
        {
            try
            {
                if (!string.IsNullOrEmpty(hex)) return ColorTranslator.FromHtml(hex);
            }
            catch { }
            return fallback;
        }
    }
}
