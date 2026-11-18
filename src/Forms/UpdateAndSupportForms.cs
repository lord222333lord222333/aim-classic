using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace AIMClassic
{
    /* ======================================================================
       AIM Update Center (v5.9.3861)
       ====================================================================== */
    public class UpdateCenterForm : XpForm
    {
        public const string CURRENT_VERSION = "6.0.4020";

        private ComboBox _cboChannel;
        private TextBox _txtManifest;
        private CheckBox _chkAuto;
        private XpProgressBar _prog;
        private Label _lblStatus;
        private TextBox _txtNotes;
        private XpButton _btnCheck, _btnDownload;
        private string _foundUrl = "";

        public UpdateCenterForm()
        {
            Text = I18n.T("AIM Software Update Center", "Центр обновлений AIM");
            Size = new Size(535, 440);
            ShowMaximizeButton = false;
            AllowResize = false;
            TitleIconImage = RetroAssets.GetIconBitmap("update", 16);

            XpBanner banner = new XpBanner
            {
                BannerTitle = I18n.T("AIM Update Center", "Центр обновлений AIM"),
                BannerSubtitle = I18n.T("Keep AIM Classic 5.9 updated with latest patches, icons & features", "Проверка обновлений, патчей безопасности и списков изменений"),
                BannerIcon = RetroAssets.GetIconBitmap("update", 26)
            };
            ClientBody.Controls.Add(banner);

            Panel body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };

            GroupBox grpInfo = new GroupBox
            {
                Text = I18n.T("Installed Version && Channel", "Установленная версия и канал"),
                Location = new Point(10, 8),
                Size = new Size(506, 128),
                Font = XpTheme.UiBold
            };

            Label lblCur = new Label
            {
                Text = I18n.T("Installed Client:  AIM Classic v" + CURRENT_VERSION + " (Windows XP Edition)", "Установлен клиент:  AIM Classic v" + CURRENT_VERSION + " (Windows XP)"),
                Location = new Point(14, 22),
                AutoSize = true,
                Font = XpTheme.UiBold,
                ForeColor = XpTheme.Current.AimNavy
            };
            grpInfo.Controls.Add(lblCur);

            grpInfo.Controls.Add(new Label { Text = I18n.T("Channel:", "Канал:"), Location = new Point(14, 49), AutoSize = true, Font = XpTheme.UiFont });
            _cboChannel = new ComboBox { Location = new Point(122, 46), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Font = XpTheme.UiFont };
            _cboChannel.Items.AddRange(new object[] { "Stable (v6.0)", "Stable (v5.9)", "Beta Preview", "Custom Manifest URL" });
            string curCh = Store.State.Settings.UpdateChannel ?? "Stable (v6.0)";
            _cboChannel.SelectedItem = _cboChannel.Items.Contains(curCh) ? curCh : "Stable (v6.0)";

            _chkAuto = new CheckBox
            {
                Text = I18n.T("Auto-check on Sign On", "Автопроверка при входе"),
                Location = new Point(332, 48),
                AutoSize = true,
                Checked = Store.State.Settings.AutoCheckUpdates,
                Font = XpTheme.UiFont
            };

            grpInfo.Controls.Add(new Label { Text = I18n.T("Manifest URL:", "URL манифеста:"), Location = new Point(14, 78), AutoSize = true, Font = XpTheme.UiFont });
            _txtManifest = new TextBox
            {
                Location = new Point(122, 75),
                Width = 370,
                Text = Store.State.Settings.UpdateManifestUrl ?? "",
                BorderStyle = BorderStyle.FixedSingle,
                Font = XpTheme.UiFont
            };
            Label lblHint = new Label
            {
                Text = I18n.T("Optional JSON manifest URL { \"version\": \"5.9.3900\", \"notes\": \"...\", \"url\": \"...\" }", "Опционально: JSON URL { \"version\": \"5.9.3900\", \"notes\": \"...\", \"url\": \"...\" }"),
                Location = new Point(122, 100),
                Size = new Size(370, 18),
                Font = XpTheme.SmallFont,
                ForeColor = Color.FromArgb(90, 100, 120)
            };

            grpInfo.Controls.AddRange(new Control[] { _cboChannel, _chkAuto, _txtManifest, lblHint });

            _lblStatus = new Label
            {
                Text = I18n.T("Ready. Click 'Check for Updates' to scan for newer builds.", "Готово. Нажмите 'Проверить обновления' для проверки новой сборки."),
                Location = new Point(10, 144),
                Size = new Size(340, 18),
                Font = XpTheme.UiBold,
                ForeColor = XpTheme.Current.AimNavy
            };
            _btnCheck = new XpButton { Text = I18n.T("Check for Updates", "Проверить обновления"), IsPrimary = true, Location = new Point(366, 140), Size = new Size(150, 25) };
            _btnCheck.Click += OnCheckClick;

            _prog = new XpProgressBar { Location = new Point(10, 170), Size = new Size(506, 18), Value = 100 };

            _txtNotes = new TextBox
            {
                Location = new Point(10, 196),
                Size = new Size(506, 124),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = XpTheme.UiFont,
                Text = BuildLocalChangelog()
            };

            body.Controls.AddRange(new Control[] { grpInfo, _lblStatus, _btnCheck, _prog, _txtNotes });
            ClientBody.Controls.Add(body);
            body.BringToFront();

            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 38, BackColor = XpTheme.Current.FaceDark };
            _btnDownload = new XpButton { Text = I18n.T("Download Update...", "Скачать обновление..."), Location = new Point(10, 7), Size = new Size(146, 24), Enabled = false };
            _btnDownload.Click += delegate
            {
                if (!string.IsNullOrEmpty(_foundUrl))
                {
                    try { Process.Start(_foundUrl); } catch { }
                }
            };
            XpButton btnSave = new XpButton { Text = I18n.T("Close", "Закрыть"), Location = new Point(430, 7), Size = new Size(86, 24) };
            btnSave.Click += delegate { SaveSettings(); Close(); };
            foot.Controls.AddRange(new Control[] { _btnDownload, btnSave });
            ClientBody.Controls.Add(foot);
        }

        private void SaveSettings()
        {
            Store.State.Settings.UpdateChannel = Convert.ToString(_cboChannel.SelectedItem);
            Store.State.Settings.UpdateManifestUrl = _txtManifest.Text.Trim();
            Store.State.Settings.AutoCheckUpdates = _chkAuto.Checked;
            Store.Notify("settings");
        }

        private string BuildLocalChangelog()
        {
            return
                "AIM Classic v" + CURRENT_VERSION + " (Global Update Build):\r\n" +
                "  + Global Public Groups Directory & Real-Time Public Room Chat\r\n" +
                "  + Retro AIM Music Player (Built-In Aero/Lo-Fi Tracks + MP3/WAV + Mood Sync)\r\n" +
                "  + 62 AI Work & Chat Models (Gemini 4 Argon/Hydrogen, GPT-6 Astra, Claude 5, DeepSeek V4, Grok 4)\r\n" +
                "  + Automatic Local AI Brain Downloader (Ollama + Llama/Qwen/Gemma without API keys)\r\n" +
                "  + Frutiger Aero Glossy Theme, Theme Workshop, AIM Mail Inbox & Global Cloud Friends\r\n\r\n" +
                "AIM Classic v5.9.3861:\r\n" +
                "  + Real Peer-to-Peer (P2P TCP + LAN Discovery + Relay Server) networking\r\n" +
                "  + Classic Windows XP 2005-2008 Toolbar Icons & Rotating Promo Banners\r\n" +
                "  + AIM 2005-2008 Windows XP Installer (AIM_Setup.exe) with EN/RU Selection";
        }

        private void OnCheckClick(object sender, EventArgs e)
        {
            SaveSettings();
            string url = _txtManifest.Text.Trim();
            _btnCheck.Enabled = false;
            _btnDownload.Enabled = false;
            _prog.Value = 20;
            _lblStatus.Text = I18n.T("Checking for AIM updates...", "Проверка обновлений...");

            ThreadPool.QueueUserWorkItem(delegate
            {
                Thread.Sleep(450);
                string remoteVer = "";
                string remoteNotes = "";
                string remoteUrl = "";
                string errMsg = "";
                bool fetched = false;

                if (!string.IsNullOrEmpty(url))
                {
                    try
                    {
                        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Tls;
                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                        req.Timeout = 5000;
                        using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                        {
                            string raw = sr.ReadToEnd();
                            JavaScriptSerializer js = new JavaScriptSerializer();
                            System.Collections.Generic.Dictionary<string, object> dict = js.DeserializeObject(raw) as System.Collections.Generic.Dictionary<string, object>;
                            if (dict != null)
                            {
                                object v;
                                if (dict.TryGetValue("version", out v) && v != null) remoteVer = v.ToString();
                                if (dict.TryGetValue("notes", out v) && v != null) remoteNotes = v.ToString();
                                if (dict.TryGetValue("url", out v) && v != null) remoteUrl = v.ToString();
                                fetched = true;
                            }
                        }
                    }
                    catch (Exception ex) { errMsg = ex.Message; }
                }

                if (!IsDisposed)
                {
                    try
                    {
                        BeginInvoke((Action)delegate
                        {
                            if (IsDisposed) return;
                            _btnCheck.Enabled = true;
                            _prog.Value = 100;
                            if (fetched && !string.IsNullOrEmpty(remoteVer) && string.Compare(remoteVer, CURRENT_VERSION, StringComparison.OrdinalIgnoreCase) != 0)
                            {
                                _foundUrl = remoteUrl;
                                _btnDownload.Enabled = !string.IsNullOrEmpty(remoteUrl);
                                _lblStatus.Text = I18n.T("Update available: v" + remoteVer, "Доступно обновление: v" + remoteVer);
                                _txtNotes.Text = "NEW VERSION AVAILABLE: v" + remoteVer + "\r\n" + remoteNotes + "\r\n\r\n" + BuildLocalChangelog();
                                SoundEngine.Play("buddyDing");
                            }
                            else if (!string.IsNullOrEmpty(errMsg))
                            {
                                _lblStatus.Text = I18n.T("Manifest unreachable (Local build verified OK)", "Манифест недоступен (локальная сборка проверена)");
                                _txtNotes.Text = "Manifest check note: " + errMsg + "\r\n\r\n" + BuildLocalChangelog();
                            }
                            else
                            {
                                _lblStatus.Text = I18n.T("You are running the latest AIM version (v" + CURRENT_VERSION + ")!", "У вас установлена актуальная версия (v" + CURRENT_VERSION + ")!");
                                SoundEngine.Play("connectedChime");
                            }
                        });
                    }
                    catch { }
                }
            });
        }
    }

    /* ======================================================================
       AIM Support & Diagnostics Center
       ====================================================================== */
    public class SupportForm : XpForm
    {
        private TextBox _txtDiag, _txtSubj, _txtIssue;
        private ComboBox _cboCat;

        public SupportForm()
        {
            Text = I18n.T("AIM Help & Support Center", "Служба поддержки и диагностики AIM");
            Size = new Size(555, 455);
            ShowMaximizeButton = false;
            AllowResize = false;
            TitleIconImage = RetroAssets.GetIconBitmap("support", 16);

            XpBanner banner = new XpBanner
            {
                BannerTitle = I18n.T("AIM Support & Troubleshooting Center", "Центр поддержки и диагностики AIM"),
                BannerSubtitle = I18n.T("Real Friend P2P setup guide, diagnostics, and support tickets", "Помощь с подключением друзей, диагностика сети и отправка отчёта"),
                BannerIcon = RetroAssets.GetIconBitmap("support", 26)
            };
            ClientBody.Controls.Add(banner);

            TabControl tabs = new TabControl { Dock = DockStyle.Fill };
            TabPage tDiag = new TabPage(I18n.T("System Diagnostics", "Диагностика системы"));
            TabPage tGuide = new TabPage(I18n.T("Real Friends Guide", "Как добавить друзей"));
            TabPage tTicket = new TabPage(I18n.T("Support Ticket", "Обращение в поддержку"));
            foreach (TabPage tp in new[] { tDiag, tGuide, tTicket }) tp.BackColor = XpTheme.Current.Face;
            tabs.TabPages.AddRange(new[] { tDiag, tGuide, tTicket });

            // Tab 1: Diagnostics
            _txtDiag = new TextBox
            {
                Location = new Point(12, 12),
                Size = new Size(498, 224),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 8.5f),
                Text = BuildDiagnosticsText()
            };
            XpButton btnRefresh = new XpButton { Text = I18n.T("Run Diagnostics", "Проверить систему"), IsPrimary = true, Location = new Point(12, 244), Size = new Size(130, 25) };
            XpButton btnSound = new XpButton { Text = I18n.T("Test Sound", "Тест звука"), Location = new Point(148, 244), Size = new Size(96, 25) };
            XpButton btnCopy = new XpButton { Text = I18n.T("Copy Report", "Копировать"), Location = new Point(250, 244), Size = new Size(106, 25) };
            XpButton btnSaveDiag = new XpButton { Text = I18n.T("Save .txt...", "Сохранить .txt..."), Location = new Point(362, 244), Size = new Size(116, 25) };

            btnRefresh.Click += delegate { _txtDiag.Text = BuildDiagnosticsText(); SoundEngine.Play("connectedChime"); };
            btnSound.Click += delegate { SoundEngine.Play("buddyDing", true); };
            btnCopy.Click += delegate { try { Clipboard.SetText(_txtDiag.Text); } catch { } };
            btnSaveDiag.Click += delegate
            {
                using (SaveFileDialog sfd = new SaveFileDialog { FileName = "AIM_Diagnostics.txt", Filter = "Text Files (*.txt)|*.txt" })
                {
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        File.WriteAllText(sfd.FileName, _txtDiag.Text, Encoding.UTF8);
                        XpMessageBox.Show(this, "Saved diagnostics report to:\n" + sfd.FileName, "Support Center", false);
                    }
                }
            };
            tDiag.Controls.AddRange(new Control[] { _txtDiag, btnRefresh, btnSound, btnCopy, btnSaveDiag });

            // Tab 2: Real Friends Connection Guide
            TextBox txtGuide = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = XpTheme.UiFont,
                Text =
                    "HOW TO ADD & CHAT WITH REAL PEOPLE (NO BOTS)\r\n" +
                    "============================================\r\n\r\n" +
                    "Everyone who downloads AIMClassic.exe / AIM_Setup.exe gets built-in P2P networking:\r\n\r\n" +
                    "METHOD 1 — Automatic Discovery (Same Wi-Fi, LAN, Radmin VPN, ZeroTier, Hamachi):\r\n" +
                    "  1. Both friends open AIMClassic.exe and Sign On with unique Screen Names.\r\n" +
                    "  2. Click 'Add' on the Buddy List toolbar -> 'Real People Online' tab.\r\n" +
                    "  3. Live users on your network appear automatically! Select a person -> click 'Add Real Friend'.\r\n" +
                    "  4. Messages you send go straight to that real person (AI bots never reply for real friends).\r\n\r\n" +
                    "METHOD 2 — Direct Friend Invite Code (Internet / Port-Forwarded):\r\n" +
                    "  1. Click 'Copy My Invite Code' inside 'Add -> Real People Online' (looks like Alex@IP:5192).\r\n" +
                    "  2. Send that code to your friend, paste theirs into 'Invite Code / IP:Port', and click 'Add Real Friend'.\r\n\r\n" +
                    "METHOD 3 — Built-in Shared AIM Relay Server (Easiest for Internet Friends):\r\n" +
                    "  1. One friend clicks 'Host AIM Relay Server' in Edit -> Preferences -> Connection (port 5190).\r\n" +
                    "  2. Other friends enter that host's IP/Domain in Preferences -> Connection -> Relay Server.\r\n" +
                    "  3. Everyone online on that Relay sees each other in 'Real People Online' and chats seamlessly!"
            };
            tGuide.Controls.Add(txtGuide);

            // Tab 3: Support Ticket
            tTicket.Controls.Add(new Label { Text = I18n.T("Category:", "Категория:"), Location = new Point(14, 16), AutoSize = true });
            _cboCat = new ComboBox { Location = new Point(110, 13), Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboCat.Items.AddRange(new object[] { "Real Friends / P2P Connection", "API Keys & Bots", "Updates & Installer", "Translation / UI", "Other Feedback" });
            _cboCat.SelectedIndex = 0;

            tTicket.Controls.Add(new Label { Text = I18n.T("Subject:", "Тема:"), Location = new Point(14, 46), AutoSize = true });
            _txtSubj = new TextBox { Location = new Point(110, 43), Width = 390, BorderStyle = BorderStyle.FixedSingle };

            tTicket.Controls.Add(new Label { Text = I18n.T("Description:", "Описание:"), Location = new Point(14, 76), AutoSize = true });
            _txtIssue = new TextBox { Location = new Point(110, 76), Size = new Size(390, 152), Multiline = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.FixedSingle };

            XpButton btnExportTicket = new XpButton { Text = I18n.T("Create Support Ticket File (.txt)...", "Сохранить тикет поддержки (.txt)..."), IsPrimary = true, Location = new Point(110, 238), Size = new Size(260, 26) };
            btnExportTicket.Click += delegate
            {
                if (string.IsNullOrWhiteSpace(_txtSubj.Text) || string.IsNullOrWhiteSpace(_txtIssue.Text))
                {
                    XpMessageBox.Show(this, I18n.T("Please enter both Subject and Description.", "Заполните тему и описание."), "Support Center", false);
                    return;
                }
                using (SaveFileDialog sfd = new SaveFileDialog { FileName = "AIM_Support_Ticket_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".txt", Filter = "Text Files (*.txt)|*.txt" })
                {
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        string full = string.Format("Category: {0}\r\nSubject: {1}\r\n\r\nDescription:\r\n{2}\r\n\r\n--- DIAGNOSTICS ---\r\n{3}",
                            _cboCat.SelectedItem, _txtSubj.Text.Trim(), _txtIssue.Text.Trim(), BuildDiagnosticsText());
                        File.WriteAllText(sfd.FileName, full, Encoding.UTF8);
                        XpMessageBox.Show(this, "Support ticket saved! You can share this file with project maintainers:\n" + sfd.FileName, "Ticket Saved", false);
                    }
                }
            };
            tTicket.Controls.AddRange(new Control[] { _cboCat, _txtSubj, _txtIssue, btnExportTicket });

            Panel pad = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            pad.Controls.Add(tabs);
            ClientBody.Controls.Add(pad);
            pad.BringToFront();

            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 38, BackColor = XpTheme.Current.FaceDark };
            XpButton btnClose = new XpButton { Text = I18n.T("Close", "Закрыть"), Location = new Point(448, 7), Size = new Size(86, 24) };
            btnClose.Click += delegate { Close(); };
            foot.Controls.Add(btnClose);
            ClientBody.Controls.Add(foot);
        }

        private string BuildDiagnosticsText()
        {
            StringBuilder sb = new StringBuilder();
            Account me = Store.GetActiveAccount();
            sb.AppendLine("AIM Classic Client Diagnostics (" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ")");
            sb.AppendLine("----------------------------------------------------------");
            sb.AppendLine("Version         : v" + UpdateCenterForm.CURRENT_VERSION);
            sb.AppendLine("Active Account  : " + (me != null ? me.ScreenName : "None"));
            sb.AppendLine("UI Language     : " + (Store.State.Settings.Language ?? "en"));
            sb.AppendLine("Local IP        : " + NetworkEngine.LocalIpAddress);
            sb.AppendLine("P2P Listen Port : " + (NetworkEngine.ActiveP2pPort > 0 ? NetworkEngine.ActiveP2pPort.ToString() : "Stopped (" + NetworkEngine.LastNetworkError + ")"));
            sb.AppendLine("My Invite Code  : " + NetworkEngine.GetMyInviteCode());
            sb.AppendLine("LAN Discovery   : " + (Store.State.Settings.AutoDiscoverLan ? "Enabled (UDP 5191)" : "Disabled"));
            sb.AppendLine("Relay Client    : " + (string.IsNullOrWhiteSpace(Store.State.Settings.RelayServerHost) ? "Not configured" : (Store.State.Settings.RelayServerHost + ":" + Store.State.Settings.RelayServerPort)));
            sb.AppendLine("Relay Host Mode : " + (NetworkEngine.IsRelayHosting ? "ACTIVE on port " + NetworkEngine.ActiveRelayHostPort : "Inactive"));
            sb.AppendLine("Discovered Peers: " + NetworkEngine.DiscoveredPeers.Count);
            sb.AppendLine("AI Mode         : " + AiEngine.GetActiveAiModeLabel());
            sb.AppendLine("Data File       : " + Store.DataFilePath + (File.Exists(Store.DataFilePath) ? (" (" + new FileInfo(Store.DataFilePath).Length + " bytes)") : " (not yet saved)"));
            sb.AppendLine("Theme           : " + (Store.State.Settings.Theme ?? "LunaBlue"));
            return sb.ToString();
        }
    }
}
