using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Media;
using System.Reflection;

namespace AIMSetup
{
    public static class SetupAssets
    {
        private static readonly Assembly _asm = typeof(SetupAssets).Assembly;
        private static readonly Dictionary<string, Bitmap> _xpSprites = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        private static Icon _setupIcon;

        public static Icon SetupIcon
        {
            get
            {
                if (_setupIcon == null)
                {
                    using (Stream s = _asm.GetManifestResourceStream("AIMSetup.AppIcon.ico"))
                    {
                        if (s != null) _setupIcon = new Icon(s);
                    }
                    if (_setupIcon == null) _setupIcon = SystemIcons.Application;
                }
                return _setupIcon;
            }
        }

        public static byte[] ReadResourceBytes(string resName)
        {
            using (Stream s = _asm.GetManifestResourceStream(resName))
            {
                if (s == null) return null;
                using (MemoryStream ms = new MemoryStream())
                {
                    s.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        public static void ExtractCompressedPayload(string destPath, Action<int> onPercent)
        {
            using (Stream raw = _asm.GetManifestResourceStream("AIMSetup.Payload.exe.gz"))
            {
                if (raw == null)
                    throw new InvalidOperationException("Embedded AIMClassic.exe payload was not found inside setup.");

                long compLen = raw.Length;
                using (GZipStream gz = new GZipStream(raw, CompressionMode.Decompress))
                using (FileStream fs = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] buf = new byte[65536];
                    int read;
                    while ((read = gz.Read(buf, 0, buf.Length)) > 0)
                    {
                        fs.Write(buf, 0, read);
                        if (onPercent != null && compLen > 0)
                        {
                            int pct = (int)Math.Min(100, (raw.Position * 100L) / compLen);
                            onPercent(pct);
                        }
                    }
                }
            }
        }

        public static void PlayCompletionChime()
        {
            try
            {
                byte[] wav = ReadResourceBytes("AIMSetup.Chime.wav");
                if (wav != null && wav.Length > 44)
                {
                    using (MemoryStream ms = new MemoryStream(wav))
                    using (SoundPlayer sp = new SoundPlayer(ms))
                    {
                        sp.Play();
                    }
                }
            }
            catch { }
        }

        public static Bitmap GetXpCaptionSprite(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            Bitmap cached;
            if (_xpSprites.TryGetValue(fileName, out cached)) return cached;

            try
            {
                using (Stream s = _asm.GetManifestResourceStream("AIMSetup.XpSprites." + fileName))
                {
                    if (s != null)
                    {
                        using (Bitmap tmp = new Bitmap(s))
                        {
                            Bitmap copy = new Bitmap(tmp.Width, tmp.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                            using (Graphics g = Graphics.FromImage(copy)) g.DrawImage(tmp, 0, 0, tmp.Width, tmp.Height);
                            _xpSprites[fileName] = copy;
                            return copy;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public static Bitmap GetAimBadgeBitmap(int size)
        {
            return GetLogoBitmap(size);
        }

        public static Bitmap GetRunningManIcon(int size)
        {
            return GetLogoBitmap(size);
        }

        public static Bitmap GetLogoBitmap(int size)
        {
            try
            {
                int pick = size <= 16 ? 16 : (size <= 32 ? 32 : (size <= 48 ? 48 : 64));
                using (Stream s = _asm.GetManifestResourceStream("AIMSetup.ClientIcon.ico") ?? _asm.GetManifestResourceStream("AIMSetup.AppIcon.ico"))
                {
                    if (s != null)
                    {
                        using (Icon ic = new Icon(s, pick, pick))
                        using (Bitmap raw = ic.ToBitmap())
                        {
                            Bitmap outBmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                            using (Graphics g = Graphics.FromImage(outBmp))
                            {
                                g.SmoothingMode = SmoothingMode.HighQuality;
                                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                g.DrawImage(raw, 0, 0, size, size);
                            }
                            return outBmp;
                        }
                    }
                }
            }
            catch { }

            Bitmap fallback = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(fallback))
            {
                g.Clear(Color.Transparent);
                DrawRunningManSilhouette(g, 1f, 1f, size - 2f, Color.FromArgb(255, 210, 0), Color.FromArgb(120, 75, 0));
            }
            return fallback;
        }

        public static void DrawRunningManSilhouette(Graphics g, float x, float y, float size, Color fill, Color outline)
        {
            GraphicsState st = g.Save();
            g.TranslateTransform(x, y);
            float sc = size / 32f;
            g.ScaleTransform(sc, sc);

            using (SolidBrush br = new SolidBrush(fill))
            using (Pen p = new Pen(outline, 1.3f))
            {
                g.FillEllipse(br, 14.5f, 3.5f, 6.4f, 6.4f);
                g.DrawEllipse(p, 14.5f, 3.5f, 6.4f, 6.4f);

                PointF[] body = new PointF[]
                {
                    new PointF(9.5f, 13.8f),
                    new PointF(16.2f, 10.6f),
                    new PointF(23.8f, 12.8f),
                    new PointF(21.8f, 16.0f),
                    new PointF(17.4f, 14.9f),
                    new PointF(16.0f, 19.6f),
                    new PointF(21.8f, 25.2f),
                    new PointF(19.0f, 28.0f),
                    new PointF(13.8f, 22.3f),
                    new PointF(9.6f, 28.2f),
                    new PointF(6.8f, 25.8f),
                    new PointF(11.6f, 18.2f),
                    new PointF(12.8f, 14.2f),
                    new PointF(8.0f, 16.2f)
                };
                g.FillPolygon(br, body);
                g.DrawPolygon(p, body);
            }
            g.Restore(st);
        }

        public static Bitmap GetInstallerSidebarArt(int w, int h)
        {
            Bitmap bmp = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (LinearGradientBrush bg = new LinearGradientBrush(new Rectangle(0, 0, w, h),
                    Color.FromArgb(6, 42, 118), Color.FromArgb(24, 102, 214), 115f))
                {
                    g.FillRectangle(bg, 0, 0, w, h);
                }

                // Classic 2005 diagonal beam / AOL globe wave accents
                using (SolidBrush beam = new SolidBrush(Color.FromArgb(26, 255, 255, 255)))
                {
                    PointF[] tri = new PointF[]
                    {
                        new PointF(-20, h * 0.65f),
                        new PointF(w + 20, h * 0.28f),
                        new PointF(w + 20, h * 0.56f),
                        new PointF(-20, h * 0.92f)
                    };
                    g.FillPolygon(beam, tri);
                }
                using (Pen arcPen = new Pen(Color.FromArgb(40, 175, 215, 255), 1.5f))
                {
                    g.DrawEllipse(arcPen, -55, h - 150, 190, 190);
                    g.DrawEllipse(arcPen, -35, h - 130, 150, 150);
                }

                // Soft gold border strip on right edge of sidebar
                using (LinearGradientBrush goldEdge = new LinearGradientBrush(new Rectangle(w - 4, 0, 4, h),
                    Color.FromArgb(255, 212, 56), Color.FromArgb(214, 142, 0), LinearGradientMode.Vertical))
                {
                    g.FillRectangle(goldEdge, w - 3, 0, 3, h);
                }

                // New AIM logo emblem centered on sidebar
                using (Bitmap logo = GetLogoBitmap(64))
                {
                    g.DrawImage(logo, (w - 64) / 2, 26, 64, 64);
                }

                using (Font brandFont = new Font("Tahoma", 14.5f, FontStyle.Bold))
                using (Font subFont = new Font("Tahoma", 8.25f, FontStyle.Bold))
                using (Font tinyFont = new Font("Tahoma", 7.5f, FontStyle.Regular))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center })
                {
                    g.DrawString("AIM® 5.9", brandFont, Brushes.White, w / 2f, 102f, sf);
                    g.DrawString("Classic Edition", subFont, new SolidBrush(Color.FromArgb(255, 216, 64)), w / 2f, 126f, sf);
                    g.DrawString("AOL Instant Messenger\r\n2005–2008 Setup", tinyFont, new SolidBrush(Color.FromArgb(204, 226, 255)), w / 2f, h - 46f, sf);
                }
            }
            return bmp;
        }
    }
}
