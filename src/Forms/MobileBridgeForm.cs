using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace AIMClassic
{
    /* ======================================================================
       Zero-Admin Embedded HTTP Server for Android & Apple iPhone PWA
       ====================================================================== */
    public static class MobileBridgeServer
    {
        private static TcpListener _listener;
        private static Thread _thread;
        private static volatile bool _running = false;
        private static readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        public const int DEFAULT_PORT = 5199;
        public static bool IsRunning { get { return _running; } }
        public static int ActivePort { get; private set; }
        public static string LastError { get; private set; }

        public static List<string> GetLocalIpv4Addresses()
        {
            List<string> ips = new List<string>();
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ua.Address))
                        {
                            string s = ua.Address.ToString();
                            if (!ips.Contains(s))
                            {
                                if (s.StartsWith("192.168.") || s.StartsWith("10."))
                                    ips.Insert(0, s);
                                else
                                    ips.Add(s);
                            }
                        }
                    }
                }
            }
            catch { }
            if (ips.Count == 0 && !string.IsNullOrEmpty(NetworkEngine.LocalIpAddress))
                ips.Add(NetworkEngine.LocalIpAddress);
            if (!ips.Contains("127.0.0.1")) ips.Add("127.0.0.1");
            return ips;
        }

        public static bool Start(int port, out string error)
        {
            error = "";
            if (_running) return true;
            try
            {
                TcpListener tl = new TcpListener(IPAddress.Any, port);
                tl.Start();
                _listener = tl;
                _running = true;
                ActivePort = port;
                LastError = "";
                _thread = new Thread(AcceptLoop) { IsBackground = true };
                _thread.Start();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                LastError = error;
                return false;
            }
        }

        public static void Stop()
        {
            _running = false;
            try { if (_listener != null) _listener.Stop(); } catch { }
            _listener = null;
        }

        private static void AcceptLoop()
        {
            while (_running && _listener != null)
            {
                try
                {
                    TcpClient cli = _listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(delegate { HandleHttpClient(cli); });
                }
                catch
                {
                    if (!_running) break;
                    Thread.Sleep(120);
                }
            }
        }

        private static void HandleHttpClient(TcpClient cli)
        {
            using (cli)
            {
                try
                {
                    cli.ReceiveTimeout = 5000;
                    cli.SendTimeout = 8000;
                    using (NetworkStream ns = cli.GetStream())
                    using (StreamReader sr = new StreamReader(ns, Encoding.ASCII))
                    {
                        string reqLine = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(reqLine)) return;
                        // Drain request headers
                        string h;
                        while (!string.IsNullOrEmpty(h = sr.ReadLine())) { }

                        string[] parts = reqLine.Split(' ');
                        string rawPath = parts.Length >= 2 ? parts[1] : "/";
                        int qIdx = rawPath.IndexOf('?');
                        if (qIdx >= 0) rawPath = rawPath.Substring(0, qIdx);
                        if (rawPath == "/" || rawPath == "") rawPath = "/index.html";

                        if (string.Equals(rawPath, "/api/bootstrap", StringComparison.OrdinalIgnoreCase))
                        {
                            Account me = Store.GetActiveAccount();
                            string payload = _json.Serialize(new
                            {
                                room = Store.State.Settings.GlobalCloudRoom ?? "AIM-Global-2006",
                                lang = Store.State.Settings.Language ?? "en",
                                pcNick = me != null ? me.ScreenName : "AIMUser"
                            });
                            WriteResponse(ns, "200 OK", "application/json; charset=utf-8", Encoding.UTF8.GetBytes(payload));
                            return;
                        }

                        string relDot = rawPath.TrimStart('/').Replace('/', '.');
                        byte[] data = LoadPwaAsset(relDot, rawPath);
                        if (data == null)
                        {
                            byte[] nf = Encoding.UTF8.GetBytes("404 Not Found");
                            WriteResponse(ns, "404 Not Found", "text/plain; charset=utf-8", nf);
                            return;
                        }
                        WriteResponse(ns, "200 OK", GuessMime(rawPath), data);
                    }
                }
                catch { }
            }
        }

        private static byte[] LoadPwaAsset(string relDot, string rawPath)
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                string resName = "AIMClassic.MobilePwa." + relDot;
                using (Stream s = asm.GetManifestResourceStream(resName))
                {
                    if (s != null)
                    {
                        byte[] buf = new byte[s.Length];
                        int read = 0;
                        while (read < buf.Length)
                        {
                            int n = s.Read(buf, read, buf.Length - read);
                            if (n <= 0) break;
                            read += n;
                        }
                        return buf;
                    }
                }
            }
            catch { }

            try
            {
                string safeRel = rawPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                if (safeRel.Contains("..")) return null;
                string disk = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mobile_pwa", safeRel);
                if (File.Exists(disk)) return File.ReadAllBytes(disk);
            }
            catch { }
            return null;
        }

        private static string GuessMime(string path)
        {
            string ext = (Path.GetExtension(path) ?? "").ToLowerInvariant();
            if (ext == ".html") return "text/html; charset=utf-8";
            if (ext == ".css") return "text/css; charset=utf-8";
            if (ext == ".js") return "application/javascript; charset=utf-8";
            if (ext == ".webmanifest" || ext == ".json") return "application/manifest+json; charset=utf-8";
            if (ext == ".png") return "image/png";
            if (ext == ".gif") return "image/gif";
            if (ext == ".jpg" || ext == ".jpeg") return "image/jpeg";
            if (ext == ".svg") return "image/svg+xml";
            if (ext == ".wav") return "audio/wav";
            if (ext == ".mp3") return "audio/mpeg";
            return "application/octet-stream";
        }

        private static void WriteResponse(NetworkStream ns, string status, string contentType, byte[] body)
        {
            string hdr = string.Format(
                "HTTP/1.1 {0}\r\nContent-Type: {1}\r\nContent-Length: {2}\r\nCache-Control: no-cache\r\nAccess-Control-Allow-Origin: *\r\nConnection: close\r\n\r\n",
                status, contentType, body.Length);
            byte[] hb = Encoding.ASCII.GetBytes(hdr);
            ns.Write(hb, 0, hb.Length);
            ns.Write(body, 0, body.Length);
            ns.Flush();
        }
    }

    /* ======================================================================
       ISO 18004 QR Code Matrix Generator (Byte Mode, Single-Block Ver 1-4 L)
       ====================================================================== */
    public static class SimpleQrEncoder
    {
        private static readonly byte[] _gfExp = new byte[512];
        private static readonly byte[] _gfLog = new byte[256];

        static SimpleQrEncoder()
        {
            int x = 1;
            for (int i = 0; i < 255; i++)
            {
                _gfExp[i] = (byte)x;
                _gfLog[x] = (byte)i;
                x <<= 1;
                if ((x & 0x100) != 0) x ^= 0x11d;
            }
            for (int i = 255; i < 512; i++) _gfExp[i] = _gfExp[i - 255];
        }

        private static byte GfMul(byte a, byte b)
        {
            if (a == 0 || b == 0) return 0;
            return _gfExp[_gfLog[a] + _gfLog[b]];
        }

        private static byte[] ComputeRsEc(byte[] data, int ecCount)
        {
            byte[] gen = new byte[] { 1 };
            for (int i = 0; i < ecCount; i++)
            {
                byte[] next = new byte[gen.Length + 1];
                byte root = _gfExp[i];
                for (int j = 0; j < gen.Length; j++)
                {
                    next[j] ^= gen[j];
                    next[j + 1] ^= GfMul(gen[j], root);
                }
                gen = next;
            }

            byte[] rem = new byte[data.Length + ecCount];
            Array.Copy(data, rem, data.Length);
            for (int i = 0; i < data.Length; i++)
            {
                byte factor = rem[i];
                if (factor != 0)
                {
                    for (int j = 0; j < gen.Length; j++)
                        rem[i + j] ^= GfMul(gen[j], factor);
                }
            }
            byte[] ec = new byte[ecCount];
            Array.Copy(rem, data.Length, ec, 0, ecCount);
            return ec;
        }

        public static bool[,] Encode(string text)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(text ?? "");
            // Pick smallest Version (1..4) Error Correction Level L with single RS block:
            // Ver 1: 19 data, 7 EC (cap 17B); Ver 2: 34 data, 10 EC (cap 32B);
            // Ver 3: 55 data, 15 EC (cap 53B); Ver 4: 80 data, 20 EC (cap 78B)
            int ver = utf8.Length <= 17 ? 1 : (utf8.Length <= 32 ? 2 : (utf8.Length <= 53 ? 3 : 4));
            int dataBytes = ver == 1 ? 19 : (ver == 2 ? 34 : (ver == 3 ? 55 : 80));
            int ecBytes = ver == 1 ? 7 : (ver == 2 ? 10 : (ver == 3 ? 15 : 20));
            int size = ver * 4 + 17;

            if (utf8.Length > dataBytes - 2)
            {
                byte[] truncated = new byte[dataBytes - 2];
                Array.Copy(utf8, truncated, truncated.Length);
                utf8 = truncated;
            }

            List<int> bits = new List<int>();
            AddBits(bits, 4, 4); // Byte mode indicator 0100
            AddBits(bits, utf8.Length, 8);
            foreach (byte b in utf8) AddBits(bits, b, 8);
            int maxBits = dataBytes * 8;
            for (int i = 0; i < 4 && bits.Count < maxBits; i++) bits.Add(0);
            while (bits.Count % 8 != 0 && bits.Count < maxBits) bits.Add(0);
            int padIdx = 0;
            while (bits.Count < maxBits)
            {
                AddBits(bits, (padIdx++ % 2 == 0) ? 0xEC : 0x11, 8);
            }

            byte[] cw = new byte[dataBytes];
            for (int i = 0; i < dataBytes; i++)
            {
                int val = 0;
                for (int b = 0; b < 8; b++) val = (val << 1) | bits[i * 8 + b];
                cw[i] = (byte)val;
            }
            byte[] ec = ComputeRsEc(cw, ecBytes);
            List<int> fullBits = new List<int>();
            foreach (byte b in cw) AddBits(fullBits, b, 8);
            foreach (byte b in ec) AddBits(fullBits, b, 8);

            bool[,] mod = new bool[size, size];
            bool[,] isFunc = new bool[size, size];

            PlaceFinder(mod, isFunc, 0, 0, size);
            PlaceFinder(mod, isFunc, size - 7, 0, size);
            PlaceFinder(mod, isFunc, 0, size - 7, size);
            if (ver >= 2) PlaceAlign(mod, isFunc, size - 7, size - 7);

            // Timing patterns
            for (int i = 8; i < size - 8; i++)
            {
                bool black = (i % 2 == 0);
                mod[6, i] = black; isFunc[6, i] = true;
                mod[i, 6] = black; isFunc[i, 6] = true;
            }
            // Dark module & reserve format info areas
            mod[size - 8, 8] = true; isFunc[size - 8, 8] = true;
            for (int i = 0; i < 9; i++)
            {
                if (i != 6) { isFunc[8, i] = true; isFunc[i, 8] = true; }
            }
            for (int i = 0; i < 8; i++)
            {
                isFunc[8, size - 1 - i] = true;
                if (i < 7) isFunc[size - 1 - i, 8] = true;
            }

            // Place data bits in zig-zag columns
            int bitPos = 0;
            bool up = true;
            for (int col = size - 1; col >= 1; col -= 2)
            {
                if (col == 6) col = 5;
                for (int rowStep = 0; rowStep < size; rowStep++)
                {
                    int r = up ? (size - 1 - rowStep) : rowStep;
                    for (int dc = 0; dc < 2; dc++)
                    {
                        int c = col - dc;
                        if (!isFunc[r, c])
                        {
                            bool bit = bitPos < fullBits.Count && fullBits[bitPos] == 1;
                            bitPos++;
                            mod[r, c] = bit;
                        }
                    }
                }
                up = !up;
            }

            // Pick best mask (0..7) minimizing N1+N2+N3 penalty
            int bestMask = 0;
            int bestPenalty = int.MaxValue;
            bool[,] bestGrid = null;
            for (int mask = 0; mask < 8; mask++)
            {
                bool[,] cand = new bool[size, size];
                for (int r = 0; r < size; r++)
                {
                    for (int c = 0; c < size; c++)
                    {
                        bool val = mod[r, c];
                        if (!isFunc[r, c] && MaskBit(mask, r, c)) val = !val;
                        cand[r, c] = val;
                    }
                }
                WriteFormatInfo(cand, size, 1, mask); // EC Level L = 01
                int pen = CalcPenalty(cand, size);
                if (pen < bestPenalty)
                {
                    bestPenalty = pen;
                    bestMask = mask;
                    bestGrid = cand;
                }
            }
            return bestGrid;
        }

        public static Bitmap RenderBitmap(string text, int scale)
        {
            bool[,] grid = Encode(text);
            int n = grid.GetLength(0);
            int quiet = 4;
            int px = (n + quiet * 2) * scale;
            Bitmap bmp = new Bitmap(px, px);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                for (int r = 0; r < n; r++)
                {
                    for (int c = 0; c < n; c++)
                    {
                        if (grid[r, c])
                            g.FillRectangle(Brushes.Black, (c + quiet) * scale, (r + quiet) * scale, scale, scale);
                    }
                }
            }
            return bmp;
        }

        private static void AddBits(List<int> list, int val, int count)
        {
            for (int i = count - 1; i >= 0; i--) list.Add((val >> i) & 1);
        }

        private static void PlaceFinder(bool[,] mod, bool[,] isFunc, int row, int col, int size)
        {
            for (int r = -1; r <= 7; r++)
            {
                for (int c = -1; c <= 7; c++)
                {
                    int rr = row + r, cc = col + c;
                    if (rr < 0 || rr >= size || cc < 0 || cc >= size) continue;
                    bool black = (r >= 0 && r <= 6 && c >= 0 && c <= 6) &&
                                 (r == 0 || r == 6 || c == 0 || c == 6 || (r >= 2 && r <= 4 && c >= 2 && c <= 4));
                    mod[rr, cc] = black;
                    isFunc[rr, cc] = true;
                }
            }
        }

        private static void PlaceAlign(bool[,] mod, bool[,] isFunc, int cr, int cc)
        {
            for (int r = -2; r <= 2; r++)
            {
                for (int c = -2; c <= 2; c++)
                {
                    bool black = Math.Abs(r) == 2 || Math.Abs(c) == 2 || (r == 0 && c == 0);
                    mod[cr + r, cc + c] = black;
                    isFunc[cr + r, cc + c] = true;
                }
            }
        }

        private static bool MaskBit(int mask, int r, int c)
        {
            switch (mask)
            {
                case 0: return (r + c) % 2 == 0;
                case 1: return r % 2 == 0;
                case 2: return c % 3 == 0;
                case 3: return (r + c) % 3 == 0;
                case 4: return ((r / 2) + (c / 3)) % 2 == 0;
                case 5: return ((r * c) % 2 + (r * c) % 3) == 0;
                case 6: return (((r * c) % 2 + (r * c) % 3) % 2) == 0;
                default: return (((r + c) % 2 + (r * c) % 3) % 2) == 0;
            }
        }

        private static void WriteFormatInfo(bool[,] grid, int size, int ecLevelBits, int mask)
        {
            int data = (ecLevelBits << 3) | mask;
            int rem = data;
            for (int i = 0; i < 10; i++) rem = (rem << 1) ^ (((rem >> 9) & 1) * 0x537);
            int fmt = ((data << 10) | rem) ^ 0x5412;

            for (int i = 0; i <= 5; i++)
            {
                bool b = ((fmt >> i) & 1) != 0;
                grid[8, i] = b;
                grid[size - 1 - i, 8] = b;
            }
            bool b6 = ((fmt >> 6) & 1) != 0;
            bool b7 = ((fmt >> 7) & 1) != 0;
            bool b8 = ((fmt >> 8) & 1) != 0;
            grid[8, 7] = b6; grid[size - 7, 8] = b6;
            grid[8, 8] = b7; grid[8, size - 8] = b7;
            grid[7, 8] = b8; grid[8, size - 7] = b8;

            for (int i = 9; i < 15; i++)
            {
                bool b = ((fmt >> i) & 1) != 0;
                grid[14 - i, 8] = b;
                grid[8, size - 15 + i] = b;
            }
        }

        private static int CalcPenalty(bool[,] g, int n)
        {
            int p = 0;
            for (int r = 0; r < n; r++)
            {
                int runR = 1, runC = 1;
                for (int c = 1; c < n; c++)
                {
                    if (g[r, c] == g[r, c - 1]) runR++;
                    else { if (runR >= 5) p += runR - 2; runR = 1; }
                    if (g[c, r] == g[c - 1, r]) runC++;
                    else { if (runC >= 5) p += runC - 2; runC = 1; }
                }
                if (runR >= 5) p += runR - 2;
                if (runC >= 5) p += runC - 2;
            }
            return p;
        }
    }

    /* ======================================================================
       Mobile App Connect & QR Window (Android & Apple iPhone Installer Hub)
       ====================================================================== */
    public class MobileBridgeForm : XpForm
    {
        private ComboBox _cboIp;
        private TextBox _txtUrl, _txtNativeInfo;
        private PictureBox _pbQr;
        private XpButton _btnToggle, _btnOpenPreview;
        private Label _lblSrvStatus;

        public MobileBridgeForm()
        {
            Text = I18n.T("AIM Mobile App — Android & Apple iPhone", "Мобильное приложение AIM — Android и Apple iPhone");
            Size = new Size(592, 502);
            ShowMaximizeButton = false;
            AllowResize = false;
            TitleIconImage = RetroAssets.GetIconBitmap("phone", 16);

            XpBanner banner = new XpBanner
            {
                BannerTitle = I18n.T("AIM Mobile (Android & Apple iPhone)", "AIM для телефонов (Android и iPhone)"),
                BannerSubtitle = I18n.T("Scan QR on Wi-Fi to install Mobile App now + native Flutter project (mobile_native/)", "Сканируйте QR по Wi-Fi для установки прямо сейчас + нативный проект (.apk / .ipa)"),
                BannerIcon = RetroAssets.GetIconBitmap("phone", 26)
            };
            ClientBody.Controls.Add(banner);

            int y = 58;
            ClientBody.Controls.Add(new Label { Text = I18n.T("PC Wi-Fi IP:", "IP компьютера:"), Location = new Point(14, y + 3), AutoSize = true, Font = XpTheme.UiBold });
            _cboIp = new ComboBox { Location = new Point(116, y), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (string ip in MobileBridgeServer.GetLocalIpv4Addresses()) _cboIp.Items.Add(ip);
            if (_cboIp.Items.Count > 0) _cboIp.SelectedIndex = 0;
            _cboIp.SelectedIndexChanged += delegate { UpdateUrlAndQr(); };

            _btnToggle = new XpButton
            {
                Text = MobileBridgeServer.IsRunning ? I18n.T("Stop Mobile Server", "Остановить сервер") : I18n.T("Start Mobile Server (Port 5199)", "Запустить сервер (Порт 5199)"),
                IsPrimary = true,
                Location = new Point(344, y - 1),
                Size = new Size(218, 24)
            };
            _btnToggle.Click += OnToggleServer;
            y += 30;

            ClientBody.Controls.Add(new Label { Text = I18n.T("Phone Link:", "Ссылка:"), Location = new Point(14, y + 3), AutoSize = true, Font = XpTheme.UiBold });
            _txtUrl = new TextBox { Location = new Point(116, y), Width = 330, ReadOnly = true, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            XpButton btnCopy = new XpButton { Text = I18n.T("Copy URL", "Копия"), Location = new Point(452, y - 1), Size = new Size(110, 23), Font = XpTheme.SmallFont };
            btnCopy.Click += delegate { try { Clipboard.SetText(_txtUrl.Text); } catch { } };
            y += 32;

            // Left: Scannable QR Code box
            _pbQr = new PictureBox
            {
                Location = new Point(14, y),
                Size = new Size(216, 216),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            // Right: Clear instructions + native project info
            _txtNativeInfo = new TextBox
            {
                Location = new Point(242, y),
                Size = new Size(320, 178),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(252, 254, 255),
                BorderStyle = BorderStyle.FixedSingle,
                Font = XpTheme.UiFont,
                Text = I18n.T(
                    "HOW TO INSTALL ON YOUR PHONE NOW:\r\n" +
                    "1. Connect phone to same Wi-Fi & click 'Start Mobile Server'.\r\n" +
                    "2. Scan QR code (or open link) on phone.\r\n" +
                    "   • Android (Chrome): Menu (⋮) -> 'Install app'\r\n" +
                    "   • Apple iPhone (Safari): Share -> 'Add to Home Screen'\r\n\r\n" +
                    "NATIVE ANDROID (.APK) & APPLE (.IPA):\r\n" +
                    "• Full Flutter project created in: mobile_native\\\r\n" +
                    "• Push repo to GitHub -> Actions builds .apk & .ipa automatically!",
                    "КАК УСТАНОВИТЬ НА ТЕЛЕФОН СЕЙЧАС:\r\n" +
                    "1. Подключите телефон к тому же Wi-Fi и нажмите 'Запустить сервер'.\r\n" +
                    "2. Отсканируйте QR-код (или откройте ссылку) на телефоне:\r\n" +
                    "   • Android (Chrome): Меню (⋮) -> 'Установить приложение'\r\n" +
                    "   • Apple iPhone (Safari): Поделиться -> 'На экран Домой'\r\n\r\n" +
                    "НАТИВНЫЕ ANDROID (.APK) и APPLE (.IPA):\r\n" +
                    "• Готовый Flutter-проект в папке: mobile_native\\\r\n" +
                    "• Залейте в GitHub -> Actions сам соберёт .apk и .ipa!")
            };

            _btnOpenPreview = new XpButton
            {
                Text = I18n.T("Open Phone App Preview on PC", "Открыть предпросмотр в браузере"),
                Location = new Point(242, y + 186),
                Size = new Size(320, 28)
            };
            _btnOpenPreview.Click += delegate
            {
                if (!MobileBridgeServer.IsRunning)
                {
                    string err;
                    MobileBridgeServer.Start(MobileBridgeServer.DEFAULT_PORT, out err);
                    RefreshServerControls();
                }
                try { Process.Start("http://127.0.0.1:" + MobileBridgeServer.ActivePort); } catch { }
            };
            y += 226;

            _lblSrvStatus = new Label
            {
                Location = new Point(14, y),
                Size = new Size(548, 24),
                Font = XpTheme.UiBold,
                UseMnemonic = false
            };

            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 38, Width = ClientBody.ClientSize.Width, BackColor = XpTheme.Current.FaceDark };
            XpButton btnClose = new XpButton { Text = I18n.T("Close", "Закрыть"), Location = new Point(478, 7), Size = new Size(84, 24) };
            btnClose.Click += delegate { Close(); };
            foot.Controls.Add(btnClose);

            ClientBody.Controls.AddRange(new Control[]
            {
                _cboIp, _btnToggle, _txtUrl, btnCopy, _pbQr, _txtNativeInfo, _btnOpenPreview, _lblSrvStatus, foot
            });

            if (!MobileBridgeServer.IsRunning)
            {
                string err;
                MobileBridgeServer.Start(MobileBridgeServer.DEFAULT_PORT, out err);
            }
            UpdateUrlAndQr();
            RefreshServerControls();
        }

        private void OnToggleServer(object sender, EventArgs e)
        {
            if (MobileBridgeServer.IsRunning)
            {
                MobileBridgeServer.Stop();
            }
            else
            {
                string err;
                if (!MobileBridgeServer.Start(MobileBridgeServer.DEFAULT_PORT, out err))
                    XpMessageBox.Show(this, "Could not start Mobile Server: " + err, "Mobile Server", false);
                else
                    SoundEngine.Play("connectedChime");
            }
            RefreshServerControls();
        }

        private void RefreshServerControls()
        {
            bool up = MobileBridgeServer.IsRunning;
            _btnToggle.Text = up
                ? I18n.T("Stop Mobile Server", "Остановить сервер")
                : I18n.T("Start Mobile Server (Port 5199)", "Запустить сервер (Порт 5199)");
            _lblSrvStatus.ForeColor = up ? Color.FromArgb(14, 128, 48) : Color.DarkRed;
            _lblSrvStatus.Text = up
                ? string.Format(I18n.T("RUNNING! Scan QR code from Android / iPhone on Wi-Fi ({0}).", "СЕРВЕР РАБОТАЕТ! Сканируйте QR телефоном в сети Wi-Fi ({0})."), _txtUrl.Text)
                : I18n.T("Server stopped. Click 'Start Mobile Server' above.", "Сервер остановлен. Нажмите 'Запустить сервер' выше.");
        }

        private void UpdateUrlAndQr()
        {
            string ip = Convert.ToString(_cboIp.SelectedItem);
            if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";
            int port = MobileBridgeServer.ActivePort > 0 ? MobileBridgeServer.ActivePort : MobileBridgeServer.DEFAULT_PORT;
            string url = string.Format("http://{0}:{1}", ip, port);
            _txtUrl.Text = url;
            Image old = _pbQr.Image;
            _pbQr.Image = SimpleQrEncoder.RenderBitmap(url, 6);
            if (old != null) old.Dispose();
        }
    }
}
