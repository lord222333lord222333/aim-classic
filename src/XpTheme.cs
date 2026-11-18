using System;
using System.Drawing;

namespace AIMClassic
{
    public class ThemePalette
    {
        public string Name;
        public Color Face;
        public Color FaceDark;
        public Color TitleTop;
        public Color TitleMid;
        public Color TitleBot;
        public Color TitleInactiveTop;
        public Color TitleInactiveBot;
        public Color FrameBorder;
        public Color FrameInner;
        public Color TitleText;
        public Color AimHeaderTop;
        public Color AimHeaderBot;
        public Color SubBarTop;
        public Color SubBarBot;
        public Color AimNavy;
        public Color AimGold;
        public bool IsGlass;
    }

    public static class XpTheme
    {
        public static readonly Font UiFont = new Font("Tahoma", 8.25f, FontStyle.Regular);
        public static readonly Font UiBold = new Font("Tahoma", 8.25f, FontStyle.Bold);
        public static readonly Font TitleFont = new Font("Trebuchet MS", 9.75f, FontStyle.Bold);
        public static readonly Font SmallFont = new Font("Tahoma", 7.5f, FontStyle.Regular);
        public static readonly Font BannerTitleFont = new Font("Tahoma", 10.5f, FontStyle.Bold);

        public static ThemePalette Current
        {
            get { return GetPalette(Store.State.Settings.Theme); }
        }

        public static Color ParseHexColor(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex)) return fallback;
            try { return ColorTranslator.FromHtml(hex.Trim()); }
            catch { return fallback; }
        }

        private static Color Darken(Color c, int amount)
        {
            return Color.FromArgb(c.A, Math.Max(0, c.R - amount), Math.Max(0, c.G - amount), Math.Max(0, c.B - amount));
        }

        private static Color Lighten(Color c, int amount)
        {
            return Color.FromArgb(c.A, Math.Min(255, c.R + amount), Math.Min(255, c.G + amount), Math.Min(255, c.B + amount));
        }

        public static ThemePalette GetPalette(string name)
        {
            if (name == "FrutigerAero")
            {
                return new ThemePalette
                {
                    Name = "FrutigerAero",
                    Face = Color.FromArgb(228, 246, 253),
                    FaceDark = Color.FromArgb(190, 232, 248),
                    TitleTop = Color.FromArgb(124, 232, 255),
                    TitleMid = Color.FromArgb(24, 168, 224),
                    TitleBot = Color.FromArgb(5, 100, 164),
                    TitleInactiveTop = Color.FromArgb(182, 231, 249),
                    TitleInactiveBot = Color.FromArgb(117, 187, 216),
                    FrameBorder = Color.FromArgb(12, 138, 200),
                    FrameInner = Color.FromArgb(6, 88, 144),
                    TitleText = Color.White,
                    AimHeaderTop = Color.FromArgb(11, 136, 200),
                    AimHeaderBot = Color.FromArgb(4, 86, 146),
                    SubBarTop = Color.FromArgb(242, 252, 255),
                    SubBarBot = Color.FromArgb(191, 234, 249),
                    AimNavy = Color.FromArgb(4, 74, 126),
                    AimGold = Color.FromArgb(255, 220, 62),
                    IsGlass = true
                };
            }
            if (name == "XpRoyale")
            {
                return new ThemePalette
                {
                    Name = "XpRoyale",
                    Face = Color.FromArgb(236, 239, 246),
                    FaceDark = Color.FromArgb(208, 214, 228),
                    TitleTop = Color.FromArgb(88, 142, 242),
                    TitleMid = Color.FromArgb(34, 86, 196),
                    TitleBot = Color.FromArgb(12, 54, 146),
                    TitleInactiveTop = Color.FromArgb(166, 186, 226),
                    TitleInactiveBot = Color.FromArgb(128, 148, 198),
                    FrameBorder = Color.FromArgb(22, 74, 184),
                    FrameInner = Color.FromArgb(12, 50, 144),
                    TitleText = Color.White,
                    AimHeaderTop = Color.FromArgb(20, 68, 166),
                    AimHeaderBot = Color.FromArgb(12, 46, 124),
                    SubBarTop = Color.FromArgb(245, 248, 255),
                    SubBarBot = Color.FromArgb(212, 224, 246),
                    AimNavy = Color.FromArgb(12, 48, 126),
                    AimGold = Color.FromArgb(255, 208, 28),
                    IsGlass = true
                };
            }
            if (name == "XpOlive")
            {
                return new ThemePalette
                {
                    Name = "XpOlive",
                    Face = Color.FromArgb(238, 237, 222),
                    FaceDark = Color.FromArgb(215, 214, 192),
                    TitleTop = Color.FromArgb(174, 194, 126),
                    TitleMid = Color.FromArgb(141, 163, 92),
                    TitleBot = Color.FromArgb(101, 122, 56),
                    TitleInactiveTop = Color.FromArgb(206, 214, 182),
                    TitleInactiveBot = Color.FromArgb(168, 178, 138),
                    FrameBorder = Color.FromArgb(118, 138, 70),
                    FrameInner = Color.FromArgb(86, 104, 44),
                    TitleText = Color.White,
                    AimHeaderTop = Color.FromArgb(84, 110, 42),
                    AimHeaderBot = Color.FromArgb(62, 84, 28),
                    SubBarTop = Color.FromArgb(247, 247, 236),
                    SubBarBot = Color.FromArgb(223, 228, 196),
                    AimNavy = Color.FromArgb(66, 88, 28),
                    AimGold = Color.FromArgb(255, 208, 40)
                };
            }
            if (name == "Y2KNeon")
            {
                return new ThemePalette
                {
                    Name = "Y2KNeon",
                    Face = Color.FromArgb(228, 233, 246),
                    FaceDark = Color.FromArgb(196, 206, 228),
                    TitleTop = Color.FromArgb(148, 92, 255),
                    TitleMid = Color.FromArgb(86, 36, 198),
                    TitleBot = Color.FromArgb(36, 12, 112),
                    TitleInactiveTop = Color.FromArgb(172, 162, 216),
                    TitleInactiveBot = Color.FromArgb(128, 118, 178),
                    FrameBorder = Color.FromArgb(72, 28, 178),
                    FrameInner = Color.FromArgb(40, 14, 116),
                    TitleText = Color.White,
                    AimHeaderTop = Color.FromArgb(42, 18, 118),
                    AimHeaderBot = Color.FromArgb(24, 10, 76),
                    SubBarTop = Color.FromArgb(242, 244, 255),
                    SubBarBot = Color.FromArgb(212, 216, 248),
                    AimNavy = Color.FromArgb(36, 14, 98),
                    AimGold = Color.FromArgb(0, 236, 255),
                    IsGlass = true
                };
            }
            if (!string.IsNullOrEmpty(name) && name.StartsWith("Custom:", StringComparison.OrdinalIgnoreCase))
            {
                string id = name.Substring(7);
                CustomThemeDef ct = null;
                if (Store.State != null && Store.State.CustomThemes != null)
                {
                    foreach (CustomThemeDef def in Store.State.CustomThemes)
                    {
                        if (string.Equals(def.Id, id, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(def.Name, id, StringComparison.OrdinalIgnoreCase))
                        {
                            ct = def;
                            break;
                        }
                    }
                }
                if (ct != null)
                {
                    Color face = ParseHexColor(ct.FaceHex, Color.FromArgb(236, 233, 216));
                    Color faceDark = ParseHexColor(ct.FaceDarkHex, Darken(face, 24));
                    Color tTop = ParseHexColor(ct.TitleTopHex, Color.FromArgb(53, 147, 255));
                    Color tBot = ParseHexColor(ct.TitleBotHex, Color.FromArgb(0, 61, 184));
                    Color tMid = ParseHexColor(ct.TitleMidHex, Color.FromArgb((tTop.R + tBot.R) / 2, (tTop.G + tBot.G) / 2, (tTop.B + tBot.B) / 2));
                    Color gold = ParseHexColor(ct.GoldHex, Color.FromArgb(255, 204, 0));
                    return new ThemePalette
                    {
                        Name = name,
                        Face = face,
                        FaceDark = faceDark,
                        TitleTop = tTop,
                        TitleMid = tMid,
                        TitleBot = tBot,
                        TitleInactiveTop = Lighten(tTop, 65),
                        TitleInactiveBot = Lighten(tBot, 65),
                        FrameBorder = Darken(tBot, 8),
                        FrameInner = Darken(tBot, 24),
                        TitleText = ParseHexColor(ct.TitleTextHex, Color.White),
                        AimHeaderTop = ParseHexColor(ct.HeaderTopHex, Darken(tTop, 28)),
                        AimHeaderBot = ParseHexColor(ct.HeaderBotHex, Darken(tBot, 10)),
                        SubBarTop = ParseHexColor(ct.SubTopHex, Lighten(face, 10)),
                        SubBarBot = ParseHexColor(ct.SubBotHex, Darken(face, 14)),
                        AimNavy = ParseHexColor(ct.NavyHex, Darken(tBot, 16)),
                        AimGold = gold,
                        IsGlass = ct.IsGlass
                    };
                }
            }
            if (name == "Aim6Ice")
            {
                return new ThemePalette
                {
                    Name = "Aim6Ice",
                    Face = Color.FromArgb(235, 241, 248),
                    FaceDark = Color.FromArgb(203, 217, 235),
                    TitleTop = Color.FromArgb(79, 160, 243),
                    TitleMid = Color.FromArgb(54, 136, 223),
                    TitleBot = Color.FromArgb(20, 82, 158),
                    TitleInactiveTop = Color.FromArgb(157, 185, 235),
                    TitleInactiveBot = Color.FromArgb(117, 147, 217),
                    FrameBorder = Color.FromArgb(26, 98, 185),
                    FrameInner = Color.FromArgb(20, 74, 148),
                    TitleText = Color.White,
                    AimHeaderTop = Color.FromArgb(22, 76, 150),
                    AimHeaderBot = Color.FromArgb(30, 92, 178),
                    SubBarTop = Color.FromArgb(244, 249, 255),
                    SubBarBot = Color.FromArgb(207, 226, 248),
                    AimNavy = Color.FromArgb(0, 51, 102),
                    AimGold = Color.FromArgb(255, 204, 0)
                };
            }
            if (name == "XpSilver")
            {
                return new ThemePalette
                {
                    Name = "XpSilver",
                    Face = Color.FromArgb(230, 231, 236),
                    FaceDark = Color.FromArgb(200, 203, 212),
                    TitleTop = Color.FromArgb(230, 232, 240),
                    TitleMid = Color.FromArgb(190, 196, 210),
                    TitleBot = Color.FromArgb(123, 129, 150),
                    TitleInactiveTop = Color.FromArgb(225, 227, 234),
                    TitleInactiveBot = Color.FromArgb(194, 198, 210),
                    FrameBorder = Color.FromArgb(118, 123, 142),
                    FrameInner = Color.FromArgb(94, 99, 117),
                    TitleText = Color.FromArgb(20, 25, 40),
                    AimHeaderTop = Color.FromArgb(23, 65, 125),
                    AimHeaderBot = Color.FromArgb(25, 74, 141),
                    SubBarTop = Color.FromArgb(242, 245, 252),
                    SubBarBot = Color.FromArgb(210, 223, 242),
                    AimNavy = Color.FromArgb(0, 51, 102),
                    AimGold = Color.FromArgb(255, 204, 0)
                };
            }
            // Default: Classic Windows XP Luna Blue + AIM 5.9
            return new ThemePalette
            {
                Name = "LunaBlue",
                Face = Color.FromArgb(236, 233, 216),
                FaceDark = Color.FromArgb(214, 210, 194),
                TitleTop = Color.FromArgb(53, 147, 255),
                TitleMid = Color.FromArgb(18, 125, 255),
                TitleBot = Color.FromArgb(0, 61, 184),
                TitleInactiveTop = Color.FromArgb(157, 185, 235),
                TitleInactiveBot = Color.FromArgb(117, 147, 217),
                FrameBorder = Color.FromArgb(0, 85, 234),
                FrameInner = Color.FromArgb(8, 49, 217),
                TitleText = Color.White,
                AimHeaderTop = Color.FromArgb(10, 55, 131),
                AimHeaderBot = Color.FromArgb(19, 72, 157),
                SubBarTop = Color.FromArgb(238, 245, 253),
                SubBarBot = Color.FromArgb(210, 227, 249),
                AimNavy = Color.FromArgb(0, 51, 102),
                AimGold = Color.FromArgb(255, 204, 0)
            };
        }
    }
}
