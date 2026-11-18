using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AIMClassic
{
    public class BuddyListForm : XpForm
    {
        private PictureBox _pbMe;
        private Label _lblMe;
        private ComboBox _cboStatus;
        private Panel _pnlAwayBanner;
        private Label _lblAwayBanner, _lblAimMailBadge;
        private Panel _userCard, _aimToday;
        private TextBox _txtSearch, _txtMood;
        private ListBox _lstBuddies;
        private ToolStripButton _tbIm;
        private AimBannerStrip _bannerStrip;
        private NotifyIcon _trayIcon;
        private Icon _trayIconHandle;
        private Timer _simTimer;
        private Timer _ollamaBootTimer;
        private bool _allowClose = false;

        private class TreeRow
        {
            public bool IsGroup;
            public BuddyGroup Group;
            public Buddy Buddy;
            public bool IsOfflineGroup;
            public int OnlineCount;
            public int TotalCount;
        }

        private readonly List<TreeRow> _rows = new List<TreeRow>();
        private bool _offlineCollapsed = false;

        public BuddyListForm()
        {
            Size = new Size(340, 650);
            MinimumSize = new Size(320, 440);
            StartPosition = FormStartPosition.Manual;
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(Math.Max(10, wa.Right - Width - 18), Math.Max(12, wa.Top + 20));
            MinimizeOnClose = true;

            BuildUi();
            SetupTrayIcon();
            RefreshAll();

            Store.Changed += OnStoreChanged;

            _simTimer = new Timer { Interval = 42000 };
            _simTimer.Tick += delegate { SimulateGentleActivity(); };
            _simTimer.Start();

            if (Store.State.Settings.AiEnabled && !AiEngine.HasAnyCloudApiKey(Store.State.Settings) && !LocalAiManager.PromptShownThisSession)
            {
                _ollamaBootTimer = new Timer { Interval = 1600 };
                _ollamaBootTimer.Tick += delegate
                {
                    if (_ollamaBootTimer != null)
                    {
                        _ollamaBootTimer.Stop();
                        _ollamaBootTimer.Dispose();
                        _ollamaBootTimer = null;
                    }
                    if (IsDisposed || !Visible || LocalAiManager.PromptShownThisSession) return;
                    System.Threading.ThreadPool.QueueUserWorkItem(delegate
                    {
                        bool ready = LocalAiManager.EnsureOrAutoCheckWithoutKeys();
                        if (!IsDisposed)
                        {
                            try
                            {
                                BeginInvoke((Action)delegate
                                {
                                    if (IsDisposed || !Visible) return;
                                    if (ready)
                                    {
                                        LocalAiManager.PromptShownThisSession = true;
                                        RefreshAll();
                                    }
                                    else if (!LocalAiManager.PromptShownThisSession && !AiEngine.HasAnyCloudApiKey(Store.State.Settings))
                                    {
                                        LocalAiManager.PromptShownThisSession = true;
                                        using (OllamaSetupForm ol = new OllamaSetupForm(true))
                                        {
                                            ol.ShowDialog(this);
                                        }
                                        RefreshAll();
                                    }
                                });
                            }
                            catch { }
                        }
                    });
                };
                _ollamaBootTimer.Start();
            }
        }

        public void CloseReal()
        {
            _allowClose = true;
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            Store.Changed -= OnStoreChanged;
            if (_simTimer != null) _simTimer.Dispose();
            if (_ollamaBootTimer != null) _ollamaBootTimer.Dispose();
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
            if (_trayIconHandle != null) _trayIconHandle.Dispose();
            base.OnFormClosing(e);
        }

        private void OnStoreChanged(string topic)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => OnStoreChanged(topic)));
                return;
            }
            RefreshAll();
        }

        private void BuildUi()
        {
            XpToolRenderer renderer = new XpToolRenderer();

            // 1. Menu Bar
            MenuStrip menu = new MenuStrip { Dock = DockStyle.Top, Renderer = renderer, Font = XpTheme.UiFont };
            ToolStripMenuItem mFile = new ToolStripMenuItem(I18n.T("File", "Файл"));
            mFile.DropDownItems.Add(I18n.T("My Profile...", "Мой профиль..."), null, delegate { new ProfileForm(null, true).Show(this); });
            mFile.DropDownItems.Add(I18n.T("AIM Mail Inbox...", "Почта AIM (Входящие)..."), null, delegate { AimApp.OpenMailInbox(this); });
            mFile.DropDownItems.Add(I18n.T("AIM Music Player...", "Музыкальный плеер AIM..."), null, delegate { AimApp.OpenMusicPlayer(this); });
            mFile.DropDownItems.Add(I18n.T("AIM Mobile App (Android & iPhone)...", "Мобильное приложение (Android и iPhone)..."), null, delegate { AimApp.OpenMobileBridge(this); });
            mFile.DropDownItems.Add(I18n.T("Away Message Editor...", "Редактор Away..."), null, delegate { new AwayEditorForm().Show(this); });
            mFile.DropDownItems.Add(I18n.T("Change Buddy Icon (Avatars)...", "Сменить аватар..."), null, delegate { new AvatarGalleryForm().ShowDialog(this); });
            mFile.DropDownItems.Add(new ToolStripSeparator());
            mFile.DropDownItems.Add(I18n.T("Send a File...", "Отправить файл..."), null, delegate { new FileTransferForm(SelectedBuddySN()).Show(this); });
            mFile.DropDownItems.Add(I18n.T("Chat History Viewer...", "История сообщений..."), null, delegate { new HistoryForm(SelectedBuddySN()).Show(this); });
            mFile.DropDownItems.Add(new ToolStripSeparator());
            mFile.DropDownItems.Add(I18n.T("Account Manager & Registration...", "Аккаунты и регистрация..."), null, delegate { new AccountManagerForm("list").ShowDialog(this); });
            mFile.DropDownItems.Add(I18n.T("Sign Off", "Выйти из аккаунта"), null, delegate { AimApp.SignOff(); });
            mFile.DropDownItems.Add(I18n.T("Exit AIM", "Закрыть AIM"), null, delegate { ExitApplicationGracefully(); });

            ToolStripMenuItem mEdit = new ToolStripMenuItem(I18n.T("Edit", "Правка"));
            mEdit.DropDownItems.Add(I18n.T("Preferences / Settings...", "Настройки..."), null, delegate { new SettingsForm("general").ShowDialog(this); });
            mEdit.DropDownItems.Add(I18n.T("API Keys for Bots (AI)...", "API Ключи для ботов..."), null, delegate { new ApiKeysForm().ShowDialog(this); });
            mEdit.DropDownItems.Add(I18n.T("Download Local AI Brain (Ollama)...", "Скачать локальный мозг Ollama..."), null, delegate { new OllamaSetupForm(false).ShowDialog(this); });
            mEdit.DropDownItems.Add(I18n.T("Language & Translator...", "Язык и перевод..."), null, delegate { new SettingsForm("language").ShowDialog(this); });
            mEdit.DropDownItems.Add(I18n.T("AIM Workshop (Themes & Aero)...", "Мастерская AIM (Темы и Aero)..."), null, delegate { new WorkshopForm().ShowDialog(this); });
            mEdit.DropDownItems.Add(I18n.T("Theme Engine...", "Темы оформления..."), null, delegate { new SettingsForm("theme").ShowDialog(this); });
            mEdit.DropDownItems.Add(new ToolStripSeparator());
            mEdit.DropDownItems.Add(I18n.T("Toggle Mute Sounds", "Вкл/выкл звук"), null, delegate
            {
                Store.State.Settings.SoundMuted = !Store.State.Settings.SoundMuted;
                Store.Notify("settings");
            });

            ToolStripMenuItem mPeople = new ToolStripMenuItem(I18n.T("People", "Контакты"));
            mPeople.DropDownItems.Add(I18n.T("Send Instant Message", "Написать сообщение"), null, delegate { OpenSelectedIm(); });
            mPeople.DropDownItems.Add(I18n.T("Get Buddy Info / Profile", "Профиль контакта"), null, delegate
            {
                string sn = SelectedBuddySN();
                if (!string.IsNullOrEmpty(sn)) new ProfileForm(sn, false).Show(this);
            });
            mPeople.DropDownItems.Add(new ToolStripSeparator());
            mPeople.DropDownItems.Add(I18n.T("Add Real Friend (Online / Code)...", "Добавить реального друга..."), null, delegate { new ContactManagerForm("real", null).ShowDialog(this); });
            mPeople.DropDownItems.Add(I18n.T("Add AI Bot Buddy...", "Добавить ИИ-бота..."), null, delegate { new ContactManagerForm("bot", null).ShowDialog(this); });
            mPeople.DropDownItems.Add(I18n.T("Manage Contacts & Groups...", "Управление группами..."), null, delegate { new ContactManagerForm("manage", SelectedBuddySN()).ShowDialog(this); });
            mPeople.DropDownItems.Add(I18n.T("Browse Global Public Groups...", "Публичные группы (Онлайн)..."), null, delegate { AimApp.OpenPublicGroupsHub(this); });
            mPeople.DropDownItems.Add(I18n.T("Join Group Chat Room...", "Групповой чат..."), null, delegate { AimApp.OpenGroupChat(null); });

            ToolStripMenuItem mApi = new ToolStripMenuItem(I18n.T("API Keys", "API Ключи"));
            mApi.Click += delegate { new ApiKeysForm().ShowDialog(this); };

            ToolStripMenuItem mHelp = new ToolStripMenuItem(I18n.T("Help", "Помощь"));
            mHelp.DropDownItems.Add(I18n.T("AIM Support Center...", "Служба поддержки AIM..."), null, delegate { new SupportForm().ShowDialog(this); });
            mHelp.DropDownItems.Add(I18n.T("AIM Update Center...", "Центр обновлений AIM..."), null, delegate { new UpdateCenterForm().ShowDialog(this); });
            mHelp.DropDownItems.Add(I18n.T("Mobile Phone App (QR & APK)...", "Приложение для телефона (QR и APK)..."), null, delegate { AimApp.OpenMobileBridge(this); });
            mHelp.DropDownItems.Add(I18n.T("AIM Theme Workshop...", "Мастерская тем AIM..."), null, delegate { new WorkshopForm().ShowDialog(this); });
            mHelp.DropDownItems.Add(new ToolStripSeparator());
            mHelp.DropDownItems.Add(I18n.T("AIM Help Topics...", "Справка AIM..."), null, delegate { new HelpAboutForm("help").Show(this); });
            mHelp.DropDownItems.Add(I18n.T("Simulate Live Events...", "Симуляция событий..."), null, delegate { new HelpAboutForm("about").Show(this); });
            mHelp.DropDownItems.Add(new ToolStripSeparator());
            mHelp.DropDownItems.Add(I18n.T("About AOL Instant Messenger...", "О программе..."), null, delegate { new HelpAboutForm("about").Show(this); });

            menu.Items.AddRange(new ToolStripItem[] { mFile, mEdit, mPeople, mApi, mHelp });

            // 2. User Header Card (#8 Status + Mood Note / Music + #16 Avatar)
            _userCard = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = XpTheme.Current.SubBarTop, Padding = new Padding(6) };
            _pbMe = new PictureBox { Location = new Point(7, 14), Size = new Size(42, 42), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, Cursor = Cursors.Hand };
            _pbMe.Click += delegate { new AvatarGalleryForm().ShowDialog(this); };

            _lblMe = new Label { Location = new Point(56, 5), Size = new Size(206, 17), Font = XpTheme.UiBold, ForeColor = XpTheme.Current.AimNavy, AutoEllipsis = true };
            XpButton btnMiniPhone = new XpButton
            {
                Text = I18n.T("Phone", "Моб."),
                Location = new Point(266, 2),
                Size = new Size(54, 19),
                Font = XpTheme.SmallFont
            };
            btnMiniPhone.Click += delegate { AimApp.OpenMobileBridge(this); };
            _cboStatus = new ComboBox { Location = new Point(56, 23), Width = 264, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboStatus.Items.AddRange(new object[] { "● Online", "✎ Away", "⊘ Busy (Do Not Disturb)", "◌ Invisible" });
            _cboStatus.SelectedIndexChanged += OnStatusComboChanged;

            _txtMood = new TextBox
            {
                Location = new Point(56, 48),
                Width = 206,
                BorderStyle = BorderStyle.FixedSingle,
                Font = XpTheme.SmallFont,
                Text = Store.State.CurrentUser.CustomStatusNote ?? "Chillin on WinXP :-)"
            };
            _txtMood.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    SaveMoodNote();
                }
            };
            _txtMood.Leave += delegate { SaveMoodNote(); };

            XpButton btnMiniMusic = new XpButton
            {
                Text = "♪ Play",
                Location = new Point(266, 47),
                Size = new Size(54, 21),
                Font = XpTheme.SmallFont
            };
            btnMiniMusic.Click += delegate { AimApp.OpenMusicPlayer(this); };

            _userCard.Controls.Add(_pbMe);
            _userCard.Controls.Add(_lblMe);
            _userCard.Controls.Add(btnMiniPhone);
            _userCard.Controls.Add(_cboStatus);
            _userCard.Controls.Add(_txtMood);
            _userCard.Controls.Add(btnMiniMusic);

            // 3. #14 Classic Toolbar
            ToolStrip tb = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                Renderer = renderer,
                ImageScalingSize = new Size(20, 20),
                AutoSize = false,
                Height = 44
            };
            _tbIm = MakeTbBtn("IM", "im", delegate { OpenSelectedIm(); });
            tb.Items.Add(_tbIm);
            tb.Items.Add(MakeTbBtn("Groups", "groups", delegate { AimApp.OpenPublicGroupsHub(this); }));
            tb.Items.Add(MakeTbBtn("Music", "music", delegate { AimApp.OpenMusicPlayer(this); }));
            tb.Items.Add(MakeTbBtn("Add", "add", delegate { new ContactManagerForm("real", null).ShowDialog(this); }));
            tb.Items.Add(new ToolStripSeparator());
            tb.Items.Add(MakeTbBtn("Away", "away", delegate { new AwayEditorForm().Show(this); }));
            tb.Items.Add(MakeTbBtn("Profile", "profile", delegate
            {
                string sn = SelectedBuddySN();
                if (!string.IsNullOrEmpty(sn)) new ProfileForm(sn, false).Show(this);
                else new ProfileForm(null, true).Show(this);
            }));
            tb.Items.Add(MakeTbBtn("Files", "files", delegate { new FileTransferForm(SelectedBuddySN()).Show(this); }));
            tb.Items.Add(MakeTbBtn("API", "apikey", delegate { new ApiKeysForm().ShowDialog(this); }));
            tb.Items.Add(MakeTbBtn("Prefs", "settings", delegate { new SettingsForm("general").ShowDialog(this); }));

            // 4. Away Banner
            _pnlAwayBanner = new Panel { Dock = DockStyle.Top, Height = 26, BackColor = Color.FromArgb(255, 246, 193), Visible = false };
            _lblAwayBanner = new Label { Location = new Point(6, 5), Size = new Size(146, 16), Font = XpTheme.SmallFont, ForeColor = Color.FromArgb(92, 67, 0), AutoEllipsis = true };
            XpButton btnEditAway = new XpButton { Text = "Edit", Location = new Point(156, 3), Size = new Size(44, 20), Font = XpTheme.SmallFont };
            XpButton btnBack = new XpButton { Text = "I'm Back", Location = new Point(204, 3), Size = new Size(56, 20), Font = XpTheme.SmallFont };
            btnEditAway.Click += delegate { new AwayEditorForm().Show(this); };
            btnBack.Click += delegate { SetStatus("online"); };
            _pnlAwayBanner.Controls.Add(_lblAwayBanner);
            _pnlAwayBanner.Controls.Add(btnEditAway);
            _pnlAwayBanner.Controls.Add(btnBack);

            // 5. Search bar
            Panel searchBar = new Panel { Dock = DockStyle.Top, Height = 27, BackColor = Color.FromArgb(237, 243, 251), Padding = new Padding(5, 3, 5, 3) };
            _txtSearch = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            _txtSearch.TextChanged += delegate { RefreshBuddyTree(); };
            Label lblHint = new Label { Text = "Filter:", Dock = DockStyle.Left, Width = 38, TextAlign = ContentAlignment.MiddleLeft, Font = XpTheme.SmallFont, ForeColor = Color.FromArgb(60, 80, 110) };
            searchBar.Controls.Add(_txtSearch);
            searchBar.Controls.Add(lblHint);

            // 6. Classic 2005 Promo Banner + AIM Today Footer
            _bannerStrip = new AimBannerStrip(false)
            {
                Dock = DockStyle.Bottom,
                Visible = Store.State.Settings.ShowBanners
            };

            _aimToday = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = XpTheme.Current.SubBarBot, Padding = new Padding(6, 4, 6, 4) };
            _lblAimMailBadge = new Label
            {
                Text = "AIM Mail (3)",
                BackColor = XpTheme.Current.AimHeaderTop,
                ForeColor = XpTheme.Current.AimGold,
                Font = XpTheme.SmallFont,
                Location = new Point(6, 6),
                Size = new Size(76, 16),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            _lblAimMailBadge.Click += delegate
            {
                SoundEngine.Play("youveGotMail");
                AimApp.OpenMailInbox(this);
            };
            LinkLabel lnkAddF = new LinkLabel { Text = I18n.T("+ Friend", "+ Друг"), Location = new Point(86, 6), AutoSize = true, LinkColor = XpTheme.Current.AimNavy, Font = XpTheme.SmallFont };
            lnkAddF.Click += delegate { new ContactManagerForm("real", null).ShowDialog(this); };
            LinkLabel lnkShop = new LinkLabel { Text = I18n.T("Workshop", "Темы"), Location = new Point(144, 6), AutoSize = true, LinkColor = XpTheme.Current.AimNavy, Font = XpTheme.SmallFont };
            lnkShop.Click += delegate { new WorkshopForm().ShowDialog(this); };
            LinkLabel lnkMus = new LinkLabel { Text = I18n.T("Music", "Плеер"), Location = new Point(206, 6), AutoSize = true, LinkColor = XpTheme.Current.AimNavy, Font = XpTheme.SmallFont };
            lnkMus.Click += delegate { AimApp.OpenMusicPlayer(this); };
            LinkLabel lnkLobby = new LinkLabel { Text = I18n.T("Public Chat", "Группы"), Location = new Point(254, 6), AutoSize = true, LinkColor = XpTheme.Current.AimNavy, Font = XpTheme.SmallFont };
            lnkLobby.Click += delegate { AimApp.OpenPublicGroupsHub(this); };
            _aimToday.Controls.Add(_lblAimMailBadge);
            _aimToday.Controls.Add(lnkAddF);
            _aimToday.Controls.Add(lnkShop);
            _aimToday.Controls.Add(lnkMus);
            _aimToday.Controls.Add(lnkLobby);

            // 7. Buddy Tree ListBox (OwnerDrawVariable)
            _lstBuddies = new ListBox
            {
                Dock = DockStyle.Fill,
                DrawMode = DrawMode.OwnerDrawVariable,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                IntegralHeight = false
            };
            _lstBuddies.MeasureItem += OnMeasureBuddyRow;
            _lstBuddies.DrawItem += OnDrawBuddyRow;
            _lstBuddies.SelectedIndexChanged += delegate { UpdateToolbarButtons(); };
            _lstBuddies.Click += OnBuddyClick;
            _lstBuddies.DoubleClick += delegate { OpenSelectedIm(); };
            _lstBuddies.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) OpenSelectedIm(); };
            _lstBuddies.MouseUp += OnBuddyMouseUp;

            Panel treePad = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4) };
            treePad.Controls.Add(_lstBuddies);

            // Dock order (Fill first, then Bottom outer->inner, then Top)
            ClientBody.Controls.Add(treePad);
            ClientBody.Controls.Add(_bannerStrip);
            ClientBody.Controls.Add(_aimToday);
            ClientBody.Controls.Add(searchBar);
            ClientBody.Controls.Add(_pnlAwayBanner);
            ClientBody.Controls.Add(tb);
            ClientBody.Controls.Add(_userCard);
            ClientBody.Controls.Add(menu);
        }

        private ToolStripButton MakeTbBtn(string text, string iconKey, EventHandler onClick)
        {
            ToolStripButton b = new ToolStripButton
            {
                Text = text,
                Image = RetroAssets.GetIconBitmap(iconKey, 20),
                TextImageRelation = TextImageRelation.ImageAboveText,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Font = XpTheme.SmallFont,
                AutoSize = false,
                Size = new Size(text.Length >= 7 ? 42 : (text.Length >= 6 ? 40 : (text.Length >= 5 ? 36 : 30)), 40)
            };
            b.Click += onClick;
            return b;
        }

        private void SetupTrayIcon()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Visible = true;
            _trayIcon.MouseClick += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (Visible) Hide();
                    else { Show(); WindowState = FormWindowState.Normal; BringToFront(); }
                }
            };

            ContextMenuStrip trayMenu = new ContextMenuStrip { Renderer = new XpToolRenderer() };
            trayMenu.Items.Add("Open Buddy List", null, delegate { Show(); WindowState = FormWindowState.Normal; BringToFront(); });
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Status: Online", null, delegate { SetStatus("online"); });
            trayMenu.Items.Add("Status: Away", null, delegate { SetStatus("away"); });
            trayMenu.Items.Add("Status: Busy", null, delegate { SetStatus("busy"); });
            trayMenu.Items.Add("Status: Invisible", null, delegate { SetStatus("invisible"); });
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Add Real Friend...", null, delegate { new ContactManagerForm("real", null).ShowDialog(this); });
            trayMenu.Items.Add("AIM Mail Inbox...", null, delegate { AimApp.OpenMailInbox(this); });
            trayMenu.Items.Add("AIM Music Player...", null, delegate { AimApp.OpenMusicPlayer(this); });
            trayMenu.Items.Add("AIM Mobile App (QR & Phone)...", null, delegate { AimApp.OpenMobileBridge(this); });
            trayMenu.Items.Add("AIM Workshop (Themes & Aero)...", null, delegate { new WorkshopForm().ShowDialog(this); });
            trayMenu.Items.Add("Global Public Groups...", null, delegate { AimApp.OpenPublicGroupsHub(this); });
            trayMenu.Items.Add("Join Group Chat Room...", null, delegate { AimApp.OpenGroupChat(null); });
            trayMenu.Items.Add("Away Message Editor...", null, delegate { new AwayEditorForm().Show(this); });
            trayMenu.Items.Add("API Keys (AI)...", null, delegate { new ApiKeysForm().ShowDialog(this); });
            trayMenu.Items.Add("AIM Update Center...", null, delegate { new UpdateCenterForm().ShowDialog(this); });
            trayMenu.Items.Add("Support Center...", null, delegate { new SupportForm().ShowDialog(this); });
            trayMenu.Items.Add("Preferences...", null, delegate { new SettingsForm("general").ShowDialog(this); });
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Sign Off", null, delegate { AimApp.SignOff(); });
            trayMenu.Items.Add("Exit AIM", null, delegate { ExitApplicationGracefully(); });
            _trayIcon.ContextMenuStrip = trayMenu;
        }

        private void ExitApplicationGracefully()
        {
            MobileBridgeServer.Stop();
            MusicEngine.Stop();
            SoundEngine.Play("goodbye", false, true);
            Application.Exit();
        }

        private void SaveMoodNote()
        {
            if (_txtMood == null || Store.State.CurrentUser == null) return;
            string note = _txtMood.Text.Trim();
            if (Store.State.CurrentUser.CustomStatusNote != note)
            {
                Store.State.CurrentUser.CustomStatusNote = note;
                Store.Save();
                NetworkEngine.BroadcastStatusChange();
            }
        }

        private void UpdateTrayVisuals()
        {
            Account acc = Store.GetActiveAccount();
            string st = Store.State.CurrentUser.Status;
            using (Bitmap bmp = RetroAssets.GetRunningManBitmap(16, st))
            {
                Icon next = RetroAssets.CreateIconFromBitmap(bmp);
                _trayIcon.Icon = next;
                if (_trayIconHandle != null) _trayIconHandle.Dispose();
                _trayIconHandle = next;

                if (TitleIconImage != null) TitleIconImage.Dispose();
                TitleIconImage = RetroAssets.GetRunningManBitmap(16, st);
            }
            Icon = RetroAssets.DefaultAppIcon;
            _trayIcon.Text = string.Format("AIM - {0} ({1})", acc.ScreenName, st.ToUpperInvariant());
        }

        public void SetStatus(string status)
        {
            string prev = Store.State.CurrentUser.Status;
            Store.State.CurrentUser.Status = status;
            if (status == "away")
            {
                SoundEngine.Play("buddyDing");
                AimApp.ShowToast("Status: Away", Store.State.CurrentUser.AwayMessageText, Store.GetActiveAccount().Avatar,
                    delegate { new AwayEditorForm().Show(this); });
            }
            else if (prev == "away" && status == "online" && Store.State.UnreadBuddies.Count > 0)
            {
                SoundEngine.Play("youveGotVoicemail");
            }
            else if (prev != "online" && status == "online")
            {
                SoundEngine.Play("connectedChime");
            }
            else
            {
                SoundEngine.Play("talkClick");
            }
            Store.Notify("status");
            NetworkEngine.BroadcastStatusChange();
        }

        private void OnStatusComboChanged(object sender, EventArgs e)
        {
            if (_cboStatus.Tag as string == "sync") return;
            string[] keys = new[] { "online", "away", "busy", "invisible" };
            int idx = Math.Max(0, _cboStatus.SelectedIndex);
            SetStatus(keys[idx]);
        }

        public void RefreshAll()
        {
            Account acc = Store.GetActiveAccount();
            string st = Store.State.CurrentUser.Status;
            Text = acc.ScreenName + "'s Buddy List";
            _lblMe.Text = acc.ScreenName;
            RetroAssets.SetPictureBoxAvatar(_pbMe, acc.Avatar);

            _cboStatus.Tag = "sync";
            _cboStatus.SelectedIndex = st == "away" ? 1 : (st == "busy" ? 2 : (st == "invisible" ? 3 : 0));
            _cboStatus.Tag = null;

            _pnlAwayBanner.Visible = (st == "away");
            if (_pnlAwayBanner.Visible) _lblAwayBanner.Text = "Away: " + Store.State.CurrentUser.AwayMessageTitle;

            UpdateTrayVisuals();
            if (_userCard != null) _userCard.BackColor = XpTheme.Current.SubBarTop;
            if (_lblMe != null) _lblMe.ForeColor = XpTheme.Current.AimNavy;
            if (_txtMood != null && !_txtMood.Focused && Store.State.CurrentUser != null)
                _txtMood.Text = Store.State.CurrentUser.CustomStatusNote ?? "";
            if (_aimToday != null) _aimToday.BackColor = XpTheme.Current.SubBarBot;
            if (_lblAimMailBadge != null)
            {
                int unreadMail = Store.State.MailItems != null ? Store.State.MailItems.Count(m => !m.Read) : 0;
                _lblAimMailBadge.Text = "AIM Mail (" + unreadMail + ")";
                _lblAimMailBadge.BackColor = XpTheme.Current.AimHeaderTop;
                _lblAimMailBadge.ForeColor = XpTheme.Current.AimGold;
            }
            if (_bannerStrip != null) _bannerStrip.Visible = Store.State.Settings.ShowBanners;
            RefreshBuddyTree();
            Invalidate();
        }

        public void RefreshBuddyTree()
        {
            string prevSn = SelectedBuddySN();
            string q = (_txtSearch.Text ?? "").Trim().ToLowerInvariant();
            _rows.Clear();
            _lstBuddies.BeginUpdate();
            _lstBuddies.Items.Clear();

            foreach (BuddyGroup grp in Store.State.Groups)
            {
                List<Buddy> members = Store.State.Buddies.Where(b =>
                    b.GroupId == grp.Id && b.Status != "offline" &&
                    (q == "" || b.ScreenName.ToLowerInvariant().Contains(q) || (!string.IsNullOrEmpty(b.Alias) && b.Alias.ToLowerInvariant().Contains(q)))).ToList();
                int totalInGrp = Store.State.Buddies.Count(b => b.GroupId == grp.Id);

                TreeRow gRow = new TreeRow { IsGroup = true, Group = grp, OnlineCount = members.Count, TotalCount = totalInGrp };
                _rows.Add(gRow);
                _lstBuddies.Items.Add(gRow);

                if (!grp.Collapsed || q != "")
                {
                    foreach (Buddy b in members)
                    {
                        TreeRow bRow = new TreeRow { IsGroup = false, Buddy = b };
                        _rows.Add(bRow);
                        _lstBuddies.Items.Add(bRow);
                    }
                }
            }

            List<Buddy> offline = Store.State.Buddies.Where(b =>
                b.Status == "offline" &&
                (q == "" || b.ScreenName.ToLowerInvariant().Contains(q) || (!string.IsNullOrEmpty(b.Alias) && b.Alias.ToLowerInvariant().Contains(q)))).ToList();

            TreeRow offRow = new TreeRow { IsGroup = true, IsOfflineGroup = true, OnlineCount = offline.Count, TotalCount = offline.Count };
            _rows.Add(offRow);
            _lstBuddies.Items.Add(offRow);
            if (!_offlineCollapsed || q != "")
            {
                foreach (Buddy b in offline)
                {
                    TreeRow bRow = new TreeRow { IsGroup = false, Buddy = b };
                    _rows.Add(bRow);
                    _lstBuddies.Items.Add(bRow);
                }
            }

            if (!string.IsNullOrEmpty(prevSn))
            {
                for (int i = 0; i < _rows.Count; i++)
                {
                    if (!_rows[i].IsGroup && string.Equals(_rows[i].Buddy.ScreenName, prevSn, StringComparison.OrdinalIgnoreCase))
                    {
                        _lstBuddies.SelectedIndex = i;
                        break;
                    }
                }
            }
            _lstBuddies.EndUpdate();
            UpdateToolbarButtons();
        }

        private string SelectedBuddySN()
        {
            int idx = _lstBuddies.SelectedIndex;
            if (idx >= 0 && idx < _rows.Count && !_rows[idx].IsGroup)
                return _rows[idx].Buddy.ScreenName;
            return null;
        }

        private void UpdateToolbarButtons()
        {
            if (_tbIm != null) _tbIm.Enabled = !string.IsNullOrEmpty(SelectedBuddySN());
        }

        private void OnMeasureBuddyRow(object sender, MeasureItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _rows.Count) { e.ItemHeight = 20; return; }
            TreeRow r = _rows[e.Index];
            if (r.IsGroup) { e.ItemHeight = 22; return; }
            bool hasSub = r.Buddy.Blocked || (r.Buddy.Status == "away" && !string.IsNullOrEmpty(r.Buddy.AwayText)) || r.Buddy.Status == "busy";
            e.ItemHeight = hasSub ? 32 : 22;
        }

        private void OnDrawBuddyRow(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _rows.Count) return;
            Graphics g = e.Graphics;
            TreeRow r = _rows[e.Index];
            Rectangle bounds = e.Bounds;

            if (r.IsGroup)
            {
                using (var br = new System.Drawing.Drawing2D.LinearGradientBrush(bounds, Color.FromArgb(237, 244, 252), Color.FromArgb(217, 232, 251), System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                {
                    g.FillRectangle(br, bounds);
                }
                using (Pen p = new Pen(Color.FromArgb(200, 220, 245))) g.DrawLine(p, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
                bool col = r.IsOfflineGroup ? _offlineCollapsed : r.Group.Collapsed;
                string label = r.IsOfflineGroup
                    ? string.Format("{0} Offline ({1})", col ? "▸" : "▾", r.TotalCount)
                    : string.Format("{0} {1} ({2}/{3})", col ? "▸" : "▾", r.Group.Name, r.OnlineCount, r.TotalCount);
                TextRenderer.DrawText(g, label, XpTheme.UiBold, new Rectangle(bounds.X + 4, bounds.Y + 3, bounds.Width - 8, 16),
                    r.IsOfflineGroup ? Color.FromArgb(90, 90, 90) : XpTheme.Current.AimNavy, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                return;
            }

            Buddy b = r.Buddy;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using (SolidBrush bg = new SolidBrush(selected ? Color.FromArgb(49, 106, 197) : Color.White))
            {
                g.FillRectangle(bg, bounds);
            }

            using (Bitmap dot = RetroAssets.GetStatusDotBitmap(b.Status, 13))
            using (Bitmap av = RetroAssets.GetAvatarThumbnail(b.Avatar, 16))
            {
                g.DrawImage(dot, bounds.X + 12, bounds.Y + 4, 13, 13);
                g.DrawImage(av, bounds.X + 29, bounds.Y + 3, 16, 16);
                g.DrawRectangle(Pens.LightSteelBlue, bounds.X + 29, bounds.Y + 3, 16, 16);
            }

            string name = string.IsNullOrEmpty(b.Alias) ? b.ScreenName : string.Format("{0} ({1})", b.Alias, b.ScreenName);
            if (Store.State.UnreadBuddies.ContainsKey(b.ScreenName)) name = "★ " + name;

            Color nameCol = selected ? Color.White : (b.Status == "offline" ? Color.Gray : Color.Black);
            Font fName = b.JustSignedOn ? XpTheme.UiBold : XpTheme.UiFont;
            int rightReserve = b.IdleMinutes > 0 && b.Status != "offline" ? 32 : 6;
            TextRenderer.DrawText(g, name, fName, new Rectangle(bounds.X + 50, bounds.Y + 3, bounds.Width - 50 - rightReserve, 15),
                nameCol, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            if (b.IdleMinutes > 0 && b.Status != "offline")
            {
                TextRenderer.DrawText(g, b.IdleMinutes + "m", XpTheme.SmallFont, new Rectangle(bounds.Right - 32, bounds.Y + 4, 28, 14),
                    selected ? Color.FromArgb(220, 232, 255) : Color.Gray, TextFormatFlags.Right);
            }

            string sub = b.Blocked ? "[Blocked]" : (b.Status == "away" ? b.AwayText : (b.Status == "busy" ? "Busy / Do Not Disturb" : ""));
            if (!string.IsNullOrEmpty(sub))
            {
                TextRenderer.DrawText(g, sub, XpTheme.SmallFont, new Rectangle(bounds.X + 50, bounds.Y + 17, bounds.Width - 56, 13),
                    selected ? Color.FromArgb(220, 232, 255) : Color.FromArgb(95, 108, 128), TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            }
        }

        private void OnBuddyClick(object sender, EventArgs e)
        {
            int idx = _lstBuddies.SelectedIndex;
            if (idx >= 0 && idx < _rows.Count && _rows[idx].IsGroup)
            {
                TreeRow r = _rows[idx];
                if (r.IsOfflineGroup) _offlineCollapsed = !_offlineCollapsed;
                else r.Group.Collapsed = !r.Group.Collapsed;
                Store.Save();
                if (_bannerStrip != null) _bannerStrip.Visible = Store.State.Settings.ShowBanners;
            RefreshBuddyTree();
            }
        }

        private void OpenSelectedIm()
        {
            string sn = SelectedBuddySN();
            if (!string.IsNullOrEmpty(sn)) AimApp.OpenPrivateChat(sn);
        }

        private void OnBuddyMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            int idx = _lstBuddies.IndexFromPoint(e.Location);
            if (idx < 0 || idx >= _rows.Count || _rows[idx].IsGroup) return;
            _lstBuddies.SelectedIndex = idx;
            Buddy b = _rows[idx].Buddy;

            ContextMenuStrip cm = new ContextMenuStrip { Renderer = new XpToolRenderer() };
            cm.Items.Add("Send Instant Message", null, delegate { AimApp.OpenPrivateChat(b.ScreenName); });
            cm.Items.Add("Get Info / Profile", null, delegate { new ProfileForm(b.ScreenName, false).Show(this); });
            cm.Items.Add("Send a File...", null, delegate { new FileTransferForm(b.ScreenName).Show(this); });
            cm.Items.Add("View Chat History", null, delegate { new HistoryForm(b.ScreenName).Show(this); });
            cm.Items.Add(new ToolStripSeparator());
            cm.Items.Add("Invite to Group Chat", null, delegate { AimApp.OpenGroupChat(b.ScreenName); });
            cm.Items.Add("Warn " + b.ScreenName, null, delegate
            {
                b.WarningLevel = Math.Min(100, b.WarningLevel + 5);
                Store.Notify("buddies");
                XpMessageBox.Show(this, string.Format("Warned {0}. Warning Level: {1}%", b.ScreenName, b.WarningLevel), "Warn Buddy", false);
            });
            cm.Items.Add(b.Blocked ? "Unblock " + b.ScreenName : "Block " + b.ScreenName, null, delegate
            {
                b.Blocked = !b.Blocked;
                Store.Notify("buddies");
            });
            cm.Items.Add(new ToolStripSeparator());
            cm.Items.Add("Edit / Move Buddy...", null, delegate { new ContactManagerForm("manage", b.ScreenName).ShowDialog(this); });
            cm.Items.Add("Remove Buddy", null, delegate
            {
                if (XpMessageBox.Show(this, "Remove \"" + b.ScreenName + "\" from your Buddy List?", "Remove Buddy", true) == DialogResult.Yes)
                {
                    Store.State.Buddies.Remove(b);
                    Store.Notify("buddies");
                }
            });
            cm.Show(_lstBuddies, e.Location);
        }

        private void SimulateGentleActivity()
        {
            if (!Store.State.LoggedIn) return;
            List<Buddy> offline = Store.State.Buddies.Where(b => !b.IsRealUser && b.Status == "offline").ToList();
            if (offline.Count > 0)
            {
                Buddy b = offline[new Random().Next(offline.Count)];
                b.Status = "online";
                b.JustSignedOn = true;
                SoundEngine.Play("doorOpen");
                AimApp.ShowToast(b.ScreenName + " signed on", "Click to send an Instant Message", b.Avatar, delegate { AimApp.OpenPrivateChat(b.ScreenName); });
                Store.Notify("buddies");
                Timer clearBold = new Timer { Interval = 6000 };
                clearBold.Tick += delegate { clearBold.Stop(); clearBold.Dispose(); b.JustSignedOn = false; RefreshBuddyTree(); };
                clearBold.Start();
            }
        }
    }
}
