using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace AIMClassic
{
    public class PrivateChatForm : XpForm
    {
        private readonly string _buddySN;
        private PictureBox _pbBuddy;
        private Label _lblStatus;
        private Label _lblAwayNotice;
        private RichTextBox _rtbTranscript;
        private TextBox _txtInput;
        private Label _lblTyping;
        private LinkLabel _lnkMode;
        private XpButton _btnBlock;
        private AimBannerStrip _bannerStrip;

        private bool _bold = false;
        private bool _italic = false;
        private bool _underline = false;
        private string _fontName;
        private Color _textColor = Color.Black;
        private bool _awayAutoReplied = false;
        private DateTime _lastTypingSent = DateTime.MinValue;
        private System.Windows.Forms.Timer _typingClearTimer;

        public PrivateChatForm(string buddySN)
        {
            _buddySN = buddySN;
            _fontName = Store.State.Settings.DefaultFont ?? "Tahoma";
            try { _textColor = ColorTranslator.FromHtml(Store.State.Settings.DefaultColor ?? "#000000"); }
            catch { _textColor = Color.Black; }

            Text = buddySN + " — Instant Message";
            Size = new Size(512, 462);
            MinimumSize = new Size(445, 340);
            StartPosition = FormStartPosition.Manual;
            int offset = (AimApp.ImWindows.Count % 5) * 26;
            Location = new Point(150 + offset, 65 + offset);
            TitleIconImage = RetroAssets.GetIconBitmap("im", 16);

            BuildUi();
            Load += delegate { _rtbTranscript.BackColor = Color.FromArgb(255, 255, 254); _rtbTranscript.BackColor = Color.White; ReloadTranscript(); };

            Action<string, string, int> onTyping = delegate(string from, string h, int p)
            {
                if (!IsDisposed && string.Equals(from, _buddySN, StringComparison.OrdinalIgnoreCase))
                {
                    try { BeginInvoke((Action)ShowRemoteTypingIndicator); } catch { }
                }
            };
            NetworkEngine.TypingReceived += onTyping;
            FormClosed += delegate
            {
                NetworkEngine.TypingReceived -= onTyping;
                if (_typingClearTimer != null) _typingClearTimer.Dispose();
            };
        }

        private Buddy GetOrFallbackBuddy()
        {
            Buddy b = Store.GetBuddy(_buddySN);
            if (b != null) return b;
            return new Buddy { ScreenName = _buddySN, Status = "online", Avatar = "Smiley Face.gif", Persona = "friendly", Interests = "Chatting" };
        }

        private void BuildUi()
        {
            Buddy b = GetOrFallbackBuddy();

            // Top buddy card
            Panel topBar = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = XpTheme.Current.SubBarTop };
            _pbBuddy = new PictureBox { Location = new Point(8, 7), Size = new Size(42, 42), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, Cursor = Cursors.Hand };
            RetroAssets.SetPictureBoxAvatar(_pbBuddy, b.Avatar);
            _pbBuddy.Click += delegate { new ProfileForm(_buddySN, false).Show(this); };

            Label lblName = new Label { Text = _buddySN, Font = XpTheme.UiBold, ForeColor = XpTheme.Current.AimNavy, Location = new Point(58, 8), Size = new Size(216, 17), AutoEllipsis = true };
            _lblStatus = new Label { Font = XpTheme.SmallFont, ForeColor = Color.FromArgb(60, 86, 122), Location = new Point(58, 27), Size = new Size(216, 16) };

            XpButton btnProf = new XpButton { Text = "Profile", Location = new Point(282, 14), Size = new Size(52, 23), Font = XpTheme.SmallFont, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            XpButton btnFile = new XpButton { Text = "File", Location = new Point(337, 14), Size = new Size(44, 23), Font = XpTheme.SmallFont, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            XpButton btnHist = new XpButton { Text = "History", Location = new Point(384, 14), Size = new Size(54, 23), Font = XpTheme.SmallFont, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            XpButton btnWarn = new XpButton { Text = "Warn", Location = new Point(441, 14), Size = new Size(48, 23), Font = XpTheme.SmallFont, Anchor = AnchorStyles.Top | AnchorStyles.Right };

            btnProf.Click += delegate { new ProfileForm(_buddySN, false).Show(this); };
            btnFile.Click += delegate { new FileTransferForm(_buddySN).Show(this); };
            btnHist.Click += delegate { new HistoryForm(_buddySN).Show(this); };
            btnWarn.Click += delegate
            {
                Buddy rb = Store.GetBuddy(_buddySN);
                if (rb != null)
                {
                    rb.WarningLevel = Math.Min(100, rb.WarningLevel + 5);
                    Store.Notify("buddies");
                }
                SoundEngine.Play("alert");
                Store.AppendChatMessage(_buddySN, new ChatMessage
                {
                    Sender = "SYSTEM",
                    Text = string.Format("*** You warned {0}. Warning level is now {1}%.", _buddySN, rb != null ? rb.WarningLevel : 5)
                });
                UpdateTopStatus();
                ReloadTranscript();
            };

            topBar.Controls.AddRange(new Control[] { _pbBuddy, lblName, _lblStatus, btnProf, btnFile, btnHist, btnWarn });

            _lblAwayNotice = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                BackColor = Color.FromArgb(255, 249, 216),
                ForeColor = Color.FromArgb(90, 69, 0),
                Font = XpTheme.SmallFont,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 6, 0),
                Visible = false
            };

            // Status bar
            Panel statusBar = new Panel { Dock = DockStyle.Bottom, Height = 20, BackColor = XpTheme.Current.FaceDark };
            _lnkMode = new LinkLabel
            {
                Dock = DockStyle.Right,
                Width = 228,
                Text = b.IsRealUser ? "Real Person (P2P / Relay)" : (AiEngine.GetActiveAiModeLabel(b) + " ▾"),
                Font = XpTheme.SmallFont,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 6, 0),
                LinkColor = Color.FromArgb(0, 51, 180)
            };
            _lnkMode.Click += delegate
            {
                Buddy cur = GetOrFallbackBuddy();
                if (cur.IsRealUser)
                    new ContactManagerForm("manage", _buddySN).ShowDialog(this);
                else
                    ShowModelSwitcherMenu(cur);
            };
            _lblTyping = new Label
            {
                Dock = DockStyle.Fill,
                Text = b.IsRealUser ? "Connected (P2P / Relay Live)" : "Connected via OSCAR (5190)",
                Font = XpTheme.SmallFont,
                ForeColor = Color.FromArgb(68, 68, 68),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };
            statusBar.Controls.Add(_lblTyping);
            statusBar.Controls.Add(_lnkMode);

            // Classic 2005 Promo Banner strip in IM window
            _bannerStrip = new AimBannerStrip(true)
            {
                Dock = DockStyle.Bottom,
                Visible = Store.State.Settings.ShowBanners
            };

            // Input Panel (Dock.Right buttons + Dock.Fill textbox)
            Panel inputPanel = new Panel { Dock = DockStyle.Bottom, Height = 68, Padding = new Padding(6, 3, 6, 5) };
            Panel sendCol = new Panel { Dock = DockStyle.Right, Width = 84, Padding = new Padding(6, 0, 0, 0) };
            XpButton btnSend = new XpButton { Text = I18n.T("Send", "Отправить"), IsPrimary = true, Location = new Point(6, 0), Size = new Size(76, 35) };
            _btnBlock = new XpButton { Text = b.Blocked ? "Unblock" : "Block", Location = new Point(6, 38), Size = new Size(76, 21), Font = XpTheme.SmallFont };
            btnSend.Click += delegate { SendCurrentInput(); };
            _btnBlock.Click += delegate
            {
                Buddy rb = Store.GetBuddy(_buddySN);
                if (rb != null)
                {
                    rb.Blocked = !rb.Blocked;
                    Store.Notify("buddies");
                    _btnBlock.Text = rb.Blocked ? "Unblock" : "Block";
                    UpdateTopStatus();
                }
            };
            sendCol.Controls.Add(btnSend);
            sendCol.Controls.Add(_btnBlock);

            _txtInput = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Tahoma", 9f) };
            _txtInput.TextChanged += delegate
            {
                Buddy rb = GetOrFallbackBuddy();
                if (rb.IsRealUser && _txtInput.Text.Length > 0 && (DateTime.UtcNow - _lastTypingSent).TotalSeconds >= 2.5)
                {
                    _lastTypingSent = DateTime.UtcNow;
                    NetworkEngine.SendTypingNotification(rb);
                }
            };
            _txtInput.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    SendCurrentInput();
                }
            };
            inputPanel.Controls.Add(_txtInput);
            inputPanel.Controls.Add(sendCol);

            // Formatting & Translation Toolbar
            ToolStrip fmtBar = new ToolStrip { Dock = DockStyle.Bottom, GripStyle = ToolStripGripStyle.Hidden, Renderer = new XpToolRenderer(), AutoSize = false, Height = 26 };
            ToolStripButton bBtn = new ToolStripButton("B") { Font = new Font("Tahoma", 8.25f, FontStyle.Bold), CheckOnClick = true };
            ToolStripButton iBtn = new ToolStripButton("I") { Font = new Font("Tahoma", 8.25f, FontStyle.Italic), CheckOnClick = true };
            ToolStripButton uBtn = new ToolStripButton("U") { Font = new Font("Tahoma", 8.25f, FontStyle.Underline), CheckOnClick = true };
            bBtn.Click += delegate { _bold = bBtn.Checked; ApplyInputFontPreview(); };
            iBtn.Click += delegate { _italic = iBtn.Checked; ApplyInputFontPreview(); };
            uBtn.Click += delegate { _underline = uBtn.Checked; ApplyInputFontPreview(); };

            ToolStripComboBox cboFont = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 92 };
            cboFont.Items.AddRange(new object[] { "Tahoma", "Comic Sans MS", "Arial", "Times New Roman", "Courier New" });
            cboFont.SelectedItem = _fontName;
            if (cboFont.SelectedIndex < 0) cboFont.SelectedIndex = 0;
            cboFont.SelectedIndexChanged += delegate { _fontName = Convert.ToString(cboFont.SelectedItem); ApplyInputFontPreview(); };

            ToolStripButton colBtn = new ToolStripButton("Color") { ToolTipText = "Text Color" };
            colBtn.Click += delegate
            {
                using (ColorDialog cd = new ColorDialog { Color = _textColor })
                {
                    if (cd.ShowDialog(this) == DialogResult.OK) { _textColor = cd.Color; ApplyInputFontPreview(); }
                }
            };

            ToolStripButton emoBtn = new ToolStripButton("Smiley ▾", RetroAssets.GetEmoticonBitmap("smile")) { ToolTipText = "Insert Classic 2000s Emoticon" };
            emoBtn.Click += delegate { ShowEmoticonMenu(fmtBar, _txtInput); };

            ToolStripButton ringBtn = new ToolStripButton("Buzz!") { ToolTipText = "Send Audio Alert Ring & Shake Window" };
            ringBtn.Click += delegate
            {
                SoundEngine.Play("ring");
                BuzzShakeWindow();
                Buddy rb = GetOrFallbackBuddy();
                if (rb.IsRealUser) NetworkEngine.SendRingNotification(rb);
                Store.AppendChatMessage(_buddySN, new ChatMessage { Sender = "SYSTEM", Text = string.Format("*** {0} sent a Buzz / Audio Ring to {1}!", Store.GetActiveAccount().ScreenName, _buddySN) });
                ReloadTranscript();
            };

            ToolStripButton invBtn = new ToolStripButton("Chat+") { ToolTipText = "Invite to Group Chat" };
            invBtn.Click += delegate { AimApp.OpenGroupChat(_buddySN); };

            ToolStripDropDownButton transBtn = new ToolStripDropDownButton("Translate", RetroAssets.GetIconBitmap("translate", 16)) { ToolTipText = "Translate Chat Messages (EN <-> RU)" };
            transBtn.DropDownItems.Add("Translate Last Message to English", null, delegate { TranslateLastMessage("en"); });
            transBtn.DropDownItems.Add("Translate Last Message to Russian", null, delegate { TranslateLastMessage("ru"); });
            transBtn.DropDownItems.Add(new ToolStripSeparator());
            transBtn.DropDownItems.Add("Translate My Typed Draft (EN <-> RU)", null, delegate { TranslateDraft(); });
            transBtn.DropDownItems.Add(new ToolStripSeparator());
            ToolStripMenuItem mAuto = new ToolStripMenuItem("Auto-Translate Incoming Messages") { Checked = Store.State.Settings.AutoTranslateChat, CheckOnClick = true };
            mAuto.CheckedChanged += delegate
            {
                Store.State.Settings.AutoTranslateChat = mAuto.Checked;
                Store.Notify("settings");
            };
            transBtn.DropDownItems.Add(mAuto);

            fmtBar.Items.AddRange(new ToolStripItem[] { bBtn, iBtn, uBtn, cboFont, colBtn, new ToolStripSeparator(), emoBtn, ringBtn, invBtn, transBtn });

            // Transcript RichTextBox
            _rtbTranscript = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                DetectUrls = true
            };
            _rtbTranscript.LinkClicked += delegate(object s, LinkClickedEventArgs e)
            {
                try { System.Diagnostics.Process.Start(e.LinkText); } catch { }
            };
            Panel trPad = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 5, 6, 3) };
            trPad.Controls.Add(_rtbTranscript);

            ClientBody.Controls.Add(trPad);
            ClientBody.Controls.Add(fmtBar);
            ClientBody.Controls.Add(inputPanel);
            ClientBody.Controls.Add(_bannerStrip);
            ClientBody.Controls.Add(statusBar);
            ClientBody.Controls.Add(_lblAwayNotice);
            ClientBody.Controls.Add(topBar);

            UpdateTopStatus();
            ApplyInputFontPreview();
        }

        private void ShowModelSwitcherMenu(Buddy cur)
        {
            ContextMenuStrip cm = new ContextMenuStrip { Renderer = new XpToolRenderer() };
            string[][] quick = new[]
            {
                new[] { "Google Gemini 4 Argon (Work Flagship)", "gemini-4-argon" },
                new[] { "Google Gemini 4 Hydrogen (Fast Code)", "gemini-4-hydrogen" },
                new[] { "Google Gemini 4 Carbon", "gemini-4-carbon" },
                new[] { "OpenAI GPT-6 Astra (ChatGPT Flagship)", "gpt-6-astra" },
                new[] { "OpenAI GPT-6 Luna (Fast)", "gpt-6-luna" },
                new[] { "OpenAI GPT-4o", "gpt-4o" },
                new[] { "Anthropic Claude 5 Sonnet", "claude-5-sonnet" },
                new[] { "DeepSeek V4", "deepseek-v4" },
                new[] { "xAI Grok 4", "grok-4" },
                new[] { "Local Ollama Llama 3.3", "llama3.3" }
            };
            foreach (string[] entry in quick)
            {
                string label = entry[0];
                string modelId = entry[1];
                ToolStripMenuItem item = new ToolStripMenuItem(label)
                {
                    Checked = string.Equals(cur.AiModelOverride, modelId, StringComparison.OrdinalIgnoreCase)
                };
                item.Click += delegate { PinModelToBuddy(modelId); };
                cm.Items.Add(item);
            }
            cm.Items.Add(new ToolStripSeparator());
            cm.Items.Add(I18n.T("Use Global Active Profile Default", "Использовать глобальную модель"), null, delegate { PinModelToBuddy(""); });
            cm.Items.Add(I18n.T("Browse All 55+ Models Catalog...", "Каталог всех 55+ моделей..."), null, delegate
            {
                using (ModelCatalogForm cat = new ModelCatalogForm(cur.AiModelOverride ?? Store.State.Settings.AiModel))
                {
                    if (cat.ShowDialog(this) == DialogResult.OK && cat.SelectedModel != null)
                        PinModelToBuddy(cat.SelectedModel.Id);
                }
            });
            cm.Items.Add(I18n.T("Download Free Local Brain (Ollama)...", "Скачать локальный мозг Ollama (Без ключа)..."), null, delegate
            {
                using (OllamaSetupForm ol = new OllamaSetupForm(false))
                {
                    ol.ShowDialog(this);
                    if (ol.ActivatedSuccessfully)
                        PinModelToBuddy(Store.State.Settings.AiModel);
                }
            });
            cm.Items.Add(I18n.T("Configure API Keys & Profiles...", "Настройка API ключей и провайдеров..."), null, delegate
            {
                new ApiKeysForm().ShowDialog(this);
                _lnkMode.Text = AiEngine.GetActiveAiModeLabel(GetOrFallbackBuddy()) + " ▾";
            });
            cm.Show(_lnkMode, new Point(0, -24));
        }

        private void PinModelToBuddy(string modelId)
        {
            Buddy rb = Store.GetBuddy(_buddySN);
            if (rb != null)
            {
                rb.AiModelOverride = modelId ?? "";
                Store.Save();
            }
            Buddy updated = GetOrFallbackBuddy();
            _lnkMode.Text = AiEngine.GetActiveAiModeLabel(updated) + " ▾";
            _lblTyping.Text = string.IsNullOrEmpty(modelId)
                ? "Using Global API Profile"
                : ("AI Model -> " + modelId);
        }

        private void ShowRemoteTypingIndicator()
        {
            _lblTyping.Text = _buddySN + " is typing a message...";
            if (_typingClearTimer == null)
            {
                _typingClearTimer = new System.Windows.Forms.Timer { Interval = 3500 };
                _typingClearTimer.Tick += delegate
                {
                    _typingClearTimer.Stop();
                    if (!IsDisposed) _lblTyping.Text = "Connected (P2P / Relay Live)";
                };
            }
            _typingClearTimer.Stop();
            _typingClearTimer.Start();
        }

        private void TranslateLastMessage(string targetLang)
        {
            List<ChatMessage> logs;
            if (!Store.State.ChatHistory.TryGetValue(_buddySN, out logs) || logs.Count == 0) return;
            ChatMessage targetMsg = null;
            for (int i = logs.Count - 1; i >= 0; i--)
            {
                if (logs[i].Sender != "SYSTEM" && !string.IsNullOrWhiteSpace(logs[i].Text))
                {
                    targetMsg = logs[i];
                    break;
                }
            }
            if (targetMsg == null) return;
            string orig = targetMsg.Text;
            _lblTyping.Text = "Translating...";
            ThreadPool.QueueUserWorkItem(delegate
            {
                string method;
                string tr = I18n.TranslateMessage(orig, targetLang, out method);
                if (!IsDisposed)
                {
                    try
                    {
                        BeginInvoke((Action)delegate
                        {
                            if (IsDisposed) return;
                            _lblTyping.Text = "Translated (" + method + ")";
                            Store.AppendChatMessage(_buddySN, new ChatMessage
                            {
                                Sender = "SYSTEM",
                                Text = string.Format("[Translation -> {0}] {1}", targetLang.ToUpperInvariant(), tr)
                            });
                            ReloadTranscript();
                        });
                    }
                    catch { }
                }
            });
        }

        private void TranslateDraft()
        {
            string raw = _txtInput.Text.Trim();
            if (string.IsNullOrEmpty(raw)) return;
            bool hasCyrillic = System.Text.RegularExpressions.Regex.IsMatch(raw, @"[\u0400-\u04FF]");
            string targetLang = hasCyrillic ? "en" : "ru";
            _lblTyping.Text = "Translating draft...";
            ThreadPool.QueueUserWorkItem(delegate
            {
                string method;
                string tr = I18n.TranslateMessage(raw, targetLang, out method);
                if (!IsDisposed)
                {
                    try
                    {
                        BeginInvoke((Action)delegate
                        {
                            if (IsDisposed) return;
                            _txtInput.Text = tr;
                            _lblTyping.Text = "Draft translated -> " + targetLang.ToUpperInvariant() + " (" + method + ")";
                        });
                    }
                    catch { }
                }
            });
        }

        private void BuzzShakeWindow()
        {
            Point orig = Location;
            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer { Interval = 28 };
            int step = 0;
            int[] dx = new[] { -6, 6, -5, 5, -3, 3, -1, 0 };
            t.Tick += delegate
            {
                if (IsDisposed || step >= dx.Length)
                {
                    t.Stop(); t.Dispose();
                    if (!IsDisposed) Location = orig;
                    return;
                }
                Location = new Point(orig.X + dx[step], orig.Y + (step % 2 == 0 ? 2 : -2));
                step++;
            };
            t.Start();
        }

        public static void ShowEmoticonMenu(Control anchor, TextBox targetBox)
        {
            ContextMenuStrip pop = new ContextMenuStrip { Renderer = new XpToolRenderer(), LayoutStyle = ToolStripLayoutStyle.Table };
            TableLayoutSettings tbl = pop.LayoutSettings as TableLayoutSettings;
            if (tbl != null) tbl.ColumnCount = 4;

            foreach (EmoticonDef em in RetroAssets.Emoticons)
            {
                EmoticonDef cap = em;
                ToolStripMenuItem item = new ToolStripMenuItem(cap.Code, RetroAssets.GetEmoticonBitmap(cap.Face));
                item.ToolTipText = cap.Label;
                item.Click += delegate
                {
                    targetBox.AppendText((targetBox.Text.Length > 0 && !targetBox.Text.EndsWith(" ") ? " " : "") + cap.Code + " ");
                    targetBox.Focus();
                };
                pop.Items.Add(item);
            }
            pop.Show(anchor, new Point(0, -pop.PreferredSize.Height));
        }

        private void ApplyInputFontPreview()
        {
            FontStyle fs = FontStyle.Regular;
            if (_bold) fs |= FontStyle.Bold;
            if (_italic) fs |= FontStyle.Italic;
            if (_underline) fs |= FontStyle.Underline;
            _txtInput.Font = new Font(_fontName, 9f, fs);
            _txtInput.ForeColor = _textColor;
        }

        public void UpdateTopStatus()
        {
            Buddy b = GetOrFallbackBuddy();
            string kindTag = b.IsRealUser ? "REAL PERSON  |  " : "";
            _lblStatus.Text = string.Format("{0}{1}  |  Warn: {2}%{3}", kindTag, b.Status.ToUpperInvariant(), b.WarningLevel, b.Blocked ? "  |  BLOCKED" : "");
            if (b.Status == "away" || b.Status == "busy")
            {
                _lblAwayNotice.Text = string.Format("{0} Message: {1}", b.Status == "busy" ? "Busy" : "Away", b.AwayText ?? "Currently AFK.");
                _lblAwayNotice.Visible = true;
            }
            else
            {
                _lblAwayNotice.Visible = false;
            }
            if (_lnkMode != null)
                _lnkMode.Text = b.IsRealUser ? "Real Person (P2P / Relay)" : (AiEngine.GetActiveAiModeLabel(b) + " ▾");
            if (_bannerStrip != null) _bannerStrip.Visible = Store.State.Settings.ShowBanners;
        }

        public void ReloadTranscript()
        {
            _rtbTranscript.Clear();
            List<ChatMessage> logs;
            string mySN = Store.GetActiveAccount().ScreenName;
            bool showTs = Store.State.Settings.ShowTimestamps;
            bool showEmo = Store.State.Settings.EnableEmoticons;

            if (!Store.State.ChatHistory.TryGetValue(_buddySN, out logs) || logs.Count == 0)
            {
                Buddy b = GetOrFallbackBuddy();
                string intro = b.IsRealUser
                    ? "Say hi to " + _buddySN + "! :-) (Real Friend via P2P / Relay)"
                    : "Say hi to " + _buddySN + "! :-) (AI-enabled buddy)";
                RetroAssets.AppendChatMessageToRtb(_rtbTranscript, new ChatMessage { Sender = "SYSTEM", Text = intro }, mySN, false, showEmo);
                return;
            }
            foreach (ChatMessage m in logs)
            {
                RetroAssets.AppendChatMessageToRtb(_rtbTranscript, m, mySN, showTs, showEmo);
            }
        }

        private void SendCurrentInput()
        {
            string text = _txtInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            Buddy b = GetOrFallbackBuddy();
            if (b.Blocked)
            {
                XpMessageBox.Show(this, _buddySN + " is blocked. Unblock them first to send an IM.", "User Blocked", false);
                return;
            }

            _txtInput.Clear();
            SoundEngine.Play("imSend");
            string mySN = Store.GetActiveAccount().ScreenName;

            ChatMessage sent = new ChatMessage
            {
                Sender = mySN,
                Text = text,
                Font = _fontName,
                Color = ColorTranslator.ToHtml(_textColor),
                Bold = _bold,
                Italic = _italic,
                Underline = _underline
            };
            Store.AppendChatMessage(_buddySN, sent);
            RetroAssets.AppendChatMessageToRtb(_rtbTranscript, sent, mySN, Store.State.Settings.ShowTimestamps, Store.State.Settings.EnableEmoticons);

            // Real human buddy: route over live P2P / Relay Network and NEVER run AI bot!
            if (b.IsRealUser)
            {
                _lblTyping.Text = "Sending over P2P / Relay...";
                ThreadPool.QueueUserWorkItem(delegate
                {
                    string err;
                    bool ok = NetworkEngine.SendRealIm(b, sent, out err);
                    if (!IsDisposed)
                    {
                        try
                        {
                            BeginInvoke((Action)delegate
                            {
                                if (IsDisposed) return;
                                if (ok)
                                {
                                    _lblTyping.Text = "Delivered to " + b.ScreenName;
                                }
                                else
                                {
                                    _lblTyping.Text = "Friend unreachable (saved locally)";
                                    Store.AppendChatMessage(_buddySN, new ChatMessage
                                    {
                                        Sender = "SYSTEM",
                                        Text = "*** Could not reach " + _buddySN + " directly (" + err + "). Message saved to history."
                                    });
                                    ReloadTranscript();
                                }
                            });
                        }
                        catch { }
                    }
                });
                return;
            }

            if (b.Status == "offline")
            {
                Store.AppendChatMessage(_buddySN, new ChatMessage { Sender = "SYSTEM", Text = "*** " + _buddySN + " is offline. Message saved to history." });
                ReloadTranscript();
                return;
            }

            if ((b.Status == "away" || b.Status == "busy") && !_awayAutoReplied)
            {
                _awayAutoReplied = true;
                System.Windows.Forms.Timer t = new System.Windows.Forms.Timer { Interval = 650 };
                t.Tick += delegate
                {
                    t.Stop(); t.Dispose();
                    if (IsDisposed) return;
                    AimApp.ReceiveIncomingIM(_buddySN, "[Auto-Response] " + (b.AwayText ?? "I am currently Away."));
                };
                t.Start();
                return;
            }

            if (Store.State.Settings.ShowTypingIndicator)
            {
                _lblTyping.Text = _buddySN + " is typing a message...";
            }

            ThreadPool.QueueUserWorkItem(delegate
            {
                Thread.Sleep(900);
                string reply = AiEngine.GenerateReply(b, text, mySN);
                if (!IsDisposed)
                {
                    try
                    {
                        BeginInvoke((Action)delegate
                        {
                            if (IsDisposed) return;
                            _lblTyping.Text = "Connected via OSCAR (5190) • AI Chat Ready";
                            AimApp.ReceiveIncomingIM(_buddySN, reply);
                        });
                    }
                    catch { }
                }
            });
        }
    }
}
