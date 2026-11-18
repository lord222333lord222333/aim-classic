using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace AIMClassic
{
    public class ContactManagerForm : XpForm
    {
        private TabControl _tabs;
        private ListBox _lstOnlinePeers;
        private readonly List<DiscoveredPeer> _peerCache = new List<DiscoveredPeer>();
        private TextBox _realInput, _txtGlobalRoom;
        private Label _lblRealStatus, _lblCloudStatus;

        private TextBox _addSn, _addAlias;
        private ComboBox _addGroup, _addAvatar, _addPersona, _addStatus, _addAiModel;
        private PictureBox _pbAddAv;
        private Label _lblLookup;

        private ListBox _lstManage;
        private TextBox _edAlias, _edPeerEndpoint;
        private ComboBox _edGroup;
        private CheckBox _edBlocked;
        private Label _lblBuddyKind;

        private ListBox _lstGroups;
        private TextBox _txtNewGroup;

        public ContactManagerForm(string initialTab, string editSN)
        {
            Text = I18n.T("Contact Management — Add Real Friends & Buddies", "Управление контактами — реальные друзья и боты");
            Size = new Size(526, 496);
            ShowMaximizeButton = false;
            AllowResize = false;
            TitleIconImage = RetroAssets.GetIconBitmap("add", 16);

            XpBanner banner = new XpBanner
            {
                BannerTitle = I18n.T("Add Real People & Manage Buddies", "Добавление реальных людей и контактов"),
                BannerSubtitle = I18n.T("Find real AIM users on LAN / Relay Server, invite by code, or add AI bots", "Поиск людей по сети / реле-серверу, код приглашения или ИИ-боты"),
                BannerIcon = RetroAssets.GetIconBitmap("add", 26)
            };
            ClientBody.Controls.Add(banner);

            _tabs = new TabControl { Dock = DockStyle.Fill };
            TabPage tabReal = new TabPage(I18n.T("Real People (No Bots)", "Реальные люди (без ботов)"));
            TabPage tabAddBot = new TabPage(I18n.T("Add AI Bot", "Добавить ИИ-бота"));
            TabPage tabManage = new TabPage(I18n.T("Edit / Remove", "Редактировать"));
            TabPage tabGroups = new TabPage(I18n.T("Groups", "Группы"));
            foreach (TabPage tp in new[] { tabReal, tabAddBot, tabManage, tabGroups }) tp.BackColor = XpTheme.Current.Face;
            _tabs.TabPages.AddRange(new[] { tabReal, tabAddBot, tabManage, tabGroups });

            BuildRealTab(tabReal);
            BuildAddBotTab(tabAddBot);
            BuildManageTab(tabManage);
            BuildGroupsTab(tabGroups);

            Panel pad = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            pad.Controls.Add(_tabs);
            ClientBody.Controls.Add(pad);
            pad.BringToFront();

            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = XpTheme.Current.FaceDark };
            XpButton btnClose = new XpButton { Text = I18n.T("Close", "Закрыть"), Location = new Point(422, 6) };
            btnClose.Click += delegate { Close(); };
            foot.Controls.Add(btnClose);
            ClientBody.Controls.Add(foot);

            if (initialTab == "bot") _tabs.SelectedTab = tabAddBot;
            else if (initialTab == "manage") _tabs.SelectedTab = tabManage;
            else if (initialTab == "groups") _tabs.SelectedTab = tabGroups;
            else _tabs.SelectedTab = tabReal;

            RefreshManageList(editSN);
            RefreshGroupsList();
            RefreshDiscoveredPeers();

            Action onPeers = delegate
            {
                if (!IsDisposed)
                {
                    try { BeginInvoke((Action)RefreshDiscoveredPeers); } catch { }
                }
            };
            NetworkEngine.PeersUpdated += onPeers;
            FormClosed += delegate { NetworkEngine.PeersUpdated -= onPeers; };
        }

        private void BuildRealTab(TabPage page)
        {
            page.Controls.Add(new Label { Text = I18n.T("My Invite Code:", "Ваш код:"), Location = new Point(12, 12), AutoSize = true, Font = XpTheme.UiBold, ForeColor = XpTheme.Current.AimNavy });
            TextBox txtMyCode = new TextBox
            {
                Location = new Point(116, 9),
                Width = 258,
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = NetworkEngine.GetMyInviteCode()
            };
            XpButton btnCopy = new XpButton { Text = I18n.T("Copy", "Копия"), Location = new Point(380, 8), Size = new Size(94, 22), Font = XpTheme.SmallFont };
            btnCopy.Click += delegate
            {
                try { Clipboard.SetText(txtMyCode.Text); } catch { }
                _lblRealStatus.Text = I18n.T("Copied! Friends can add you by Global Nickname or Invite Code.", "Скопировано! Друзья могут добавить вас по глобальному нику или коду.");
            };

            page.Controls.Add(new Label { Text = I18n.T("Global Room:", "Глобал комната:"), Location = new Point(12, 38), AutoSize = true, Font = XpTheme.UiBold, ForeColor = Color.FromArgb(0, 102, 74) });
            _txtGlobalRoom = new TextBox
            {
                Location = new Point(116, 35),
                Width = 172,
                BorderStyle = BorderStyle.FixedSingle,
                Text = Store.State.Settings.GlobalCloudRoom ?? "AIM-Global-2006"
            };
            XpButton btnJoinCloud = new XpButton { Text = I18n.T("Sync Global", "Синхр. Мир"), Location = new Point(294, 34), Size = new Size(90, 22), Font = XpTheme.SmallFont, IsPrimary = true };
            btnJoinCloud.Click += delegate
            {
                Store.State.Settings.GlobalCloudEnabled = true;
                string r = _txtGlobalRoom.Text.Trim();
                Store.State.Settings.GlobalCloudRoom = string.IsNullOrEmpty(r) ? "AIM-Global-2006" : r;
                _txtGlobalRoom.Text = Store.State.Settings.GlobalCloudRoom;
                Store.Save();
                NetworkEngine.SyncGlobalCloudNow();
                _lblCloudStatus.Text = I18n.T("Syncing...", "Синхронизация...");
            };
            _lblCloudStatus = new Label
            {
                Location = new Point(390, 38),
                Size = new Size(94, 18),
                Font = XpTheme.SmallFont,
                ForeColor = Color.FromArgb(0, 112, 68),
                Text = NetworkEngine.GlobalCloudStatus ?? "Idle"
            };

            page.Controls.Add(new Label { Text = I18n.T("Online Real Users (Global Cloud / LAN / Relay):", "Реальные люди онлайн (Глобал / LAN / Реле):"), Location = new Point(12, 66), AutoSize = true, Font = XpTheme.UiBold });
            XpButton btnScan = new XpButton { Text = I18n.T("Refresh", "Обновить"), Location = new Point(380, 62), Size = new Size(94, 21), Font = XpTheme.SmallFont };
            btnScan.Click += delegate
            {
                NetworkEngine.BroadcastStatusChange();
                NetworkEngine.SyncGlobalCloudNow();
                RefreshDiscoveredPeers();
            };

            _lstOnlinePeers = new ListBox { Location = new Point(12, 88), Size = new Size(462, 118), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            _lstOnlinePeers.SelectedIndexChanged += delegate
            {
                int i = _lstOnlinePeers.SelectedIndex;
                if (i >= 0 && i < _peerCache.Count)
                {
                    DiscoveredPeer p = _peerCache[i];
                    if (!string.IsNullOrEmpty(p.Host) && p.Port > 0)
                        _realInput.Text = string.Format("{0}@{1}:{2}", p.ScreenName, p.Host, p.Port);
                    else
                        _realInput.Text = p.ScreenName;
                }
            };

            page.Controls.Add(new Label { Text = I18n.T("Add by Global Nickname or Invite Code (Nick or Nick@IP:Port):", "Глобальный ник или код друга (Nick или Nick@IP:Port):"), Location = new Point(12, 214), AutoSize = true, Font = XpTheme.UiBold });
            _realInput = new TextBox { Location = new Point(12, 234), Width = 362, BorderStyle = BorderStyle.FixedSingle };
            XpButton btnPing = new XpButton { Text = I18n.T("Ping", "Проверить"), Location = new Point(380, 233), Size = new Size(94, 22), Font = XpTheme.SmallFont };
            btnPing.Click += OnPingPeerClick;

            _lblRealStatus = new Label
            {
                Location = new Point(12, 262),
                Size = new Size(462, 30),
                Font = XpTheme.SmallFont,
                ForeColor = Color.FromArgb(0, 74, 154),
                Text = I18n.T("Select a user above or enter a friend's Global Nickname (same Global Room) or IP:Port.", "Выберите пользователя выше или введите глобальный ник друга (в той же комнате) или IP:Port.")
            };

            XpButton btnAddReal = new XpButton
            {
                Text = I18n.T("Add Real Friend (No AI Bot)", "Добавить реального друга"),
                IsPrimary = true,
                Location = new Point(12, 298),
                Size = new Size(224, 26)
            };
            btnAddReal.Click += OnAddRealFriendClick;

            XpButton btnRelay = new XpButton
            {
                Text = I18n.T("Global Cloud & Relay Settings...", "Глобал облако и настройки сети..."),
                Location = new Point(246, 298),
                Size = new Size(228, 26)
            };
            btnRelay.Click += delegate { new SettingsForm("connection").ShowDialog(this); };

            page.Controls.AddRange(new Control[] { txtMyCode, btnCopy, _txtGlobalRoom, btnJoinCloud, _lblCloudStatus, btnScan, _lstOnlinePeers, _realInput, btnPing, _lblRealStatus, btnAddReal, btnRelay });
        }

        private void RefreshDiscoveredPeers()
        {
            _peerCache.Clear();
            _lstOnlinePeers.Items.Clear();
            if (_lblCloudStatus != null)
                _lblCloudStatus.Text = NetworkEngine.GlobalCloudStatus ?? "Idle";
            lock (NetworkEngine.DiscoveredPeers)
            {
                foreach (DiscoveredPeer p in NetworkEngine.DiscoveredPeers.Values)
                {
                    _peerCache.Add(p);
                    string addr = (!string.IsNullOrEmpty(p.Host) && p.Port > 0) ? string.Format("{0}:{1}", p.Host, p.Port) : "Global Room";
                    _lstOnlinePeers.Items.Add(string.Format("[{0}] {1} ({2}) — {3}",
                        p.Source ?? "P2P", p.ScreenName, addr, (p.Status ?? "online").ToUpperInvariant()));
                }
            }
            if (_lstOnlinePeers.Items.Count == 0)
            {
                _lstOnlinePeers.Items.Add(I18n.T("No peers discovered yet — click 'Sync Global' or enter Nickname / Nick@IP:Port below.",
                    "Пользователи пока не найдены — нажмите 'Синхр. Мир' или введите ник / код ниже."));
            }
        }

        private void OnPingPeerClick(object sender, EventArgs e)
        {
            string sn, host;
            int port;
            if (!NetworkEngine.ParseInviteCode(_realInput.Text, out sn, out host, out port))
            {
                _lblRealStatus.Text = I18n.T("Enter a Global Nickname or Nick@IP:Port to check.", "Введите глобальный ник или Nick@IP:Port.");
                return;
            }
            if (string.IsNullOrEmpty(host))
            {
                DiscoveredPeer found = null;
                lock (NetworkEngine.DiscoveredPeers)
                {
                    if (!string.IsNullOrEmpty(sn)) NetworkEngine.DiscoveredPeers.TryGetValue(sn, out found);
                }
                if (found != null)
                {
                    _lblRealStatus.Text = string.Format("Global User FOUND: {0} ({1}) — Click 'Add Real Friend'!", found.ScreenName, found.Status);
                    _lblRealStatus.ForeColor = Color.ForestGreen;
                }
                else
                {
                    NetworkEngine.SyncGlobalCloudNow();
                    _lblRealStatus.Text = string.Format("Global Cloud Mode (Room: {0}) — You can add \"{1}\" by nickname anytime!",
                        Store.State.Settings.GlobalCloudRoom ?? "AIM-Global-2006", sn);
                    _lblRealStatus.ForeColor = Color.FromArgb(0, 96, 136);
                }
                return;
            }
            _lblRealStatus.Text = string.Format("Pinging {0}:{1}...", host, port);
            ThreadPool.QueueUserWorkItem(delegate
            {
                NetPacket ack;
                string err;
                bool ok = NetworkEngine.ProbePeer(host, port, out ack, out err);
                if (!IsDisposed)
                {
                    try
                    {
                        BeginInvoke((Action)delegate
                        {
                            if (IsDisposed) return;
                            if (ok)
                            {
                                _lblRealStatus.Text = string.Format("ONLINE! Connected to real user {0} ({1}:{2})", ack.From, host, port);
                                _lblRealStatus.ForeColor = Color.ForestGreen;
                                _realInput.Text = string.Format("{0}@{1}:{2}", ack.From, host, port);
                            }
                            else
                            {
                                _lblRealStatus.Text = "Unreachable: " + err;
                                _lblRealStatus.ForeColor = Color.DarkRed;
                            }
                        });
                    }
                    catch { }
                }
            });
        }

        private void OnAddRealFriendClick(object sender, EventArgs e)
        {
            string sn, host;
            int port;
            if (!NetworkEngine.ParseInviteCode(_realInput.Text, out sn, out host, out port))
            {
                XpMessageBox.Show(this, I18n.T("Enter a nickname or Invite Code (e.g. Alex@192.168.1.10:5192).", "Введите ник или код приглашения."), "Add Real Friend", false);
                return;
            }

            DiscoveredPeer known = null;
            lock (NetworkEngine.DiscoveredPeers)
            {
                if (!string.IsNullOrEmpty(sn)) NetworkEngine.DiscoveredPeers.TryGetValue(sn, out known);
                if (known == null && !string.IsNullOrEmpty(host))
                    known = NetworkEngine.DiscoveredPeers.Values.FirstOrDefault(x => string.Equals(x.Host, host, StringComparison.OrdinalIgnoreCase));
            }
            if (known != null)
            {
                if (string.IsNullOrEmpty(sn)) sn = known.ScreenName;
                if (string.IsNullOrEmpty(host)) { host = known.Host; port = known.Port; }
            }

            if (string.IsNullOrEmpty(sn) && !string.IsNullOrEmpty(host))
            {
                NetPacket ack;
                string err;
                if (NetworkEngine.ProbePeer(host, port, out ack, out err) && !string.IsNullOrEmpty(ack.From))
                    sn = ack.From;
                else
                    sn = "User_" + host;
            }

            if (string.IsNullOrEmpty(sn) || sn.Length < 2)
            {
                XpMessageBox.Show(this, "Could not resolve friend's Screen Name.", "Add Real Friend", false);
                return;
            }

            Buddy created = Store.AddRealFriend(
                sn,
                host ?? "",
                port,
                known != null ? known.Status : "online",
                known != null ? known.Avatar : "AIM Icon.gif",
                known != null ? known.Bio : "Real AIM friend (" + (string.IsNullOrEmpty(host) ? ("Global Cloud: " + (Store.State.Settings.GlobalCloudRoom ?? "AIM-Global-2006")) : (host + ":" + port)) + ")",
                "grp_real");

            if (created != null)
            {
                SoundEngine.Play("doorOpen");
                NetworkEngine.SendFriendRequest(created);
                RefreshManageList(created.ScreenName);
                XpMessageBox.Show(this, string.Format("Added real person \"{0}\" to 'Real Friends'!\nMessages sent to them go over Global Cloud / live P2P network (no AI bots).", created.ScreenName), "Real Friend Added", false);
            }
        }

        private void BuildAddBotTab(TabPage page)
        {
            int y = 14;
            page.Controls.Add(new Label { Text = "Bot Nickname *:", Location = new Point(14, y + 3), AutoSize = true, Font = XpTheme.UiBold });
            _addSn = new TextBox { Location = new Point(138, y), Width = 200, BorderStyle = BorderStyle.FixedSingle };
            _addSn.TextChanged += delegate { CheckLookupHint(); };
            XpButton btnFind = new XpButton { Text = "Check", Location = new Point(344, y - 1), Size = new Size(64, 22), Font = XpTheme.SmallFont };
            btnFind.Click += delegate { CheckLookupHint(); };
            y += 23;

            _lblLookup = new Label { Location = new Point(138, y), Size = new Size(320, 16), Font = XpTheme.SmallFont, ForeColor = Color.FromArgb(0, 82, 164), Text = "Type any custom AI buddy name (e.g. RetroBot_07)" };
            y += 22;

            page.Controls.Add(new Label { Text = "Display Alias:", Location = new Point(14, y + 3), AutoSize = true });
            _addAlias = new TextBox { Location = new Point(138, y), Width = 220, BorderStyle = BorderStyle.FixedSingle };
            y += 27;

            page.Controls.Add(new Label { Text = "Buddy Group:", Location = new Point(14, y + 3), AutoSize = true });
            _addGroup = new ComboBox { Location = new Point(138, y), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (BuddyGroup g in Store.State.Groups) _addGroup.Items.Add(g.Name);
            if (_addGroup.Items.Count > 0) _addGroup.SelectedIndex = 0;
            y += 27;

            page.Controls.Add(new Label { Text = "AI Personality:", Location = new Point(14, y + 3), AutoSize = true });
            _addPersona = new ComboBox { Location = new Point(138, y), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            _addPersona.Items.AddRange(new object[] { "work (Pro Work / Coding AI)", "assistant (Smart AI Helper)", "friendly (2006 Friend)", "gamer (CS 1.6 / WoW)", "geek (Tech / Matrix)", "russian_friend (RU/EN)" });
            _addPersona.SelectedIndex = 0;
            y += 27;

            page.Controls.Add(new Label { Text = "AI Model:", Location = new Point(14, y + 3), AutoSize = true });
            _addAiModel = new ComboBox { Location = new Point(138, y), Width = 220, DropDownStyle = ComboBoxStyle.DropDown };
            _addAiModel.Items.Add("(Use Global Active Profile)");
            foreach (AiModelPreset mp in AiEngine.ModelCatalog) _addAiModel.Items.Add(mp.Id);
            _addAiModel.SelectedIndex = 0;
            y += 27;

            page.Controls.Add(new Label { Text = "Initial Status:", Location = new Point(14, y + 3), AutoSize = true });
            _addStatus = new ComboBox { Location = new Point(138, y), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            _addStatus.Items.AddRange(new object[] { "online", "away", "offline" });
            _addStatus.SelectedIndex = 0;
            y += 27;

            page.Controls.Add(new Label { Text = "Buddy Icon GIF:", Location = new Point(14, y + 3), AutoSize = true });
            _addAvatar = new ComboBox { Location = new Point(138, y), Width = 165, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (string av in RetroAssets.GetAvatarNames()) _addAvatar.Items.Add(av);
            if (_addAvatar.Items.Count > 0) _addAvatar.SelectedIndex = 0;
            _pbAddAv = new PictureBox { Location = new Point(312, y - 4), Size = new Size(40, 40), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            _addAvatar.SelectedIndexChanged += delegate { RetroAssets.SetPictureBoxAvatar(_pbAddAv, Convert.ToString(_addAvatar.SelectedItem)); };
            if (_addAvatar.SelectedItem != null) RetroAssets.SetPictureBoxAvatar(_pbAddAv, Convert.ToString(_addAvatar.SelectedItem));

            XpButton btnAdd = new XpButton { Text = "Add AI Bot to Buddy List", IsPrimary = true, Location = new Point(280, 294), Size = new Size(184, 26) };
            btnAdd.Click += delegate
            {
                string nick = _addSn.Text.Trim();
                if (nick.Length < 2)
                {
                    XpMessageBox.Show(this, "Enter a valid nickname (at least 2 characters).", "Add AI Bot", false);
                    return;
                }
                if (Store.GetBuddy(nick) != null)
                {
                    XpMessageBox.Show(this, "\"" + nick + "\" is already on your Buddy List!", "Add AI Bot", false);
                    return;
                }
                string gid = Store.State.Groups[Math.Max(0, _addGroup.SelectedIndex)].Id;
                string personaKey = Convert.ToString(_addPersona.SelectedItem).Split(' ')[0];
                string st = Convert.ToString(_addStatus.SelectedItem);
                Buddy created = Store.AddFriendByNickname(nick, _addAlias.Text.Trim(), gid, Convert.ToString(_addAvatar.SelectedItem), personaKey, st);
                if (created != null)
                {
                    string modSel = _addAiModel.Text.Trim();
                    if (!string.IsNullOrEmpty(modSel) && !modSel.StartsWith("("))
                    {
                        created.AiModelOverride = modSel;
                        Store.Save();
                    }
                    if (st == "online") SoundEngine.Play("doorOpen");
                }
                XpMessageBox.Show(this, string.Format("Added AI buddy \"{0}\" to your Buddy List!", created.ScreenName), "Buddy Added", false);
                RefreshManageList(created.ScreenName);
                _addSn.Clear();
                _addAlias.Clear();
            };

            page.Controls.AddRange(new Control[] { _addSn, btnFind, _lblLookup, _addAlias, _addGroup, _addPersona, _addAiModel, _addStatus, _addAvatar, _pbAddAv, btnAdd });
        }

        private void CheckLookupHint()
        {
            string nick = _addSn.Text.Trim();
            if (nick.Length < 2)
            {
                _lblLookup.Text = "Type any custom AI buddy name (e.g. RetroBot_07)";
                _lblLookup.ForeColor = Color.FromArgb(0, 82, 164);
                return;
            }
            if (Store.GetBuddy(nick) != null)
            {
                _lblLookup.Text = "Already in your Buddy List!";
                _lblLookup.ForeColor = Color.DarkRed;
                return;
            }
            _lblLookup.Text = "Ready to add \"" + nick + "\" as an interactive AI buddy.";
            _lblLookup.ForeColor = Color.FromArgb(0, 82, 164);
        }

        private void BuildManageTab(TabPage page)
        {
            _lstManage = new ListBox { Location = new Point(10, 10), Size = new Size(176, 282), BorderStyle = BorderStyle.FixedSingle };
            _lstManage.SelectedIndexChanged += delegate { PopulateManage(); };

            _lblBuddyKind = new Label { Location = new Point(198, 12), Size = new Size(270, 18), Font = XpTheme.UiBold, ForeColor = XpTheme.Current.AimNavy, Text = "Type: -" };

            page.Controls.Add(new Label { Text = "Alias:", Location = new Point(198, 40), AutoSize = true });
            _edAlias = new TextBox { Location = new Point(268, 37), Width = 200, BorderStyle = BorderStyle.FixedSingle };

            page.Controls.Add(new Label { Text = "Group:", Location = new Point(198, 72), AutoSize = true });
            _edGroup = new ComboBox { Location = new Point(268, 69), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };

            page.Controls.Add(new Label { Text = "Peer IP:Port:", Location = new Point(198, 104), AutoSize = true });
            _edPeerEndpoint = new TextBox { Location = new Point(268, 101), Width = 200, BorderStyle = BorderStyle.FixedSingle };

            _edBlocked = new CheckBox { Text = "Block this user", Location = new Point(198, 138), AutoSize = true };

            XpButton btnSave = new XpButton { Text = "Save", IsPrimary = true, Location = new Point(198, 266), Size = new Size(88, 25) };
            XpButton btnDel = new XpButton { Text = "Remove Buddy", Location = new Point(294, 266), Size = new Size(124, 25) };

            btnSave.Click += delegate
            {
                Buddy b = SelectedManageBuddy();
                if (b == null) return;
                b.Alias = _edAlias.Text.Trim();
                if (_edGroup.SelectedIndex >= 0 && _edGroup.SelectedIndex < Store.State.Groups.Count)
                    b.GroupId = Store.State.Groups[_edGroup.SelectedIndex].Id;
                b.Blocked = _edBlocked.Checked;
                if (!string.IsNullOrWhiteSpace(_edPeerEndpoint.Text))
                {
                    string sn, host;
                    int port;
                    if (NetworkEngine.ParseInviteCode(_edPeerEndpoint.Text, out sn, out host, out port) && !string.IsNullOrEmpty(host))
                    {
                        b.IsRealUser = true;
                        b.PeerHost = host;
                        b.PeerPort = port;
                    }
                }
                Store.Notify("buddies");
                XpMessageBox.Show(this, "Updated " + b.ScreenName, "Contact Saved", false);
            };

            btnDel.Click += delegate
            {
                Buddy b = SelectedManageBuddy();
                if (b == null) return;
                if (XpMessageBox.Show(this, "Remove \"" + b.ScreenName + "\" from your Buddy List?", "Remove Buddy", true) == DialogResult.Yes)
                {
                    Store.State.Buddies.Remove(b);
                    Store.Notify("buddies");
                    RefreshManageList(null);
                }
            };

            page.Controls.AddRange(new Control[] { _lstManage, _lblBuddyKind, _edAlias, _edGroup, _edPeerEndpoint, _edBlocked, btnSave, btnDel });
        }

        private void BuildGroupsTab(TabPage page)
        {
            _txtNewGroup = new TextBox { Location = new Point(10, 12), Width = 350, BorderStyle = BorderStyle.FixedSingle };
            XpButton btnAddGrp = new XpButton { Text = "Add Group", IsPrimary = true, Location = new Point(368, 10), Size = new Size(98, 24) };
            _lstGroups = new ListBox { Location = new Point(10, 44), Size = new Size(456, 210), BorderStyle = BorderStyle.FixedSingle };
            XpButton btnDelGrp = new XpButton { Text = "Delete Selected Group", Location = new Point(10, 262), Size = new Size(165, 25) };

            btnAddGrp.Click += delegate
            {
                string n = _txtNewGroup.Text.Trim();
                if (string.IsNullOrEmpty(n)) return;
                Store.State.Groups.Add(new BuddyGroup { Id = "grp_" + DateTime.Now.Ticks, Name = n, Collapsed = false });
                _txtNewGroup.Clear();
                Store.Notify("buddies");
                RefreshGroupsList();
            };

            btnDelGrp.Click += delegate
            {
                int idx = _lstGroups.SelectedIndex;
                if (idx < 0 || Store.State.Groups.Count <= 1) return;
                BuddyGroup g = Store.State.Groups[idx];
                BuddyGroup fallback = Store.State.Groups.First(x => x.Id != g.Id);
                foreach (Buddy b in Store.State.Buddies)
                {
                    if (b.GroupId == g.Id) b.GroupId = fallback.Id;
                }
                Store.State.Groups.Remove(g);
                Store.Notify("buddies");
                RefreshGroupsList();
            };

            page.Controls.AddRange(new Control[] { _txtNewGroup, btnAddGrp, _lstGroups, btnDelGrp });
        }

        private void RefreshManageList(string selectSN)
        {
            _lstManage.Items.Clear();
            _edGroup.Items.Clear();
            foreach (BuddyGroup g in Store.State.Groups) _edGroup.Items.Add(g.Name);
            foreach (Buddy b in Store.State.Buddies) _lstManage.Items.Add(b.ScreenName);
            if (!string.IsNullOrEmpty(selectSN) && _lstManage.Items.Contains(selectSN))
                _lstManage.SelectedItem = selectSN;
            else if (_lstManage.Items.Count > 0)
                _lstManage.SelectedIndex = 0;
        }

        private Buddy SelectedManageBuddy()
        {
            string sn = _lstManage.SelectedItem as string;
            return Store.GetBuddy(sn);
        }

        private void PopulateManage()
        {
            Buddy b = SelectedManageBuddy();
            if (b == null) return;
            _lblBuddyKind.Text = b.IsRealUser ? "Type: REAL PERSON (P2P / Relay)" : "Type: AI Bot / Simulated Buddy";
            _edAlias.Text = b.Alias ?? "";
            int gIdx = Store.State.Groups.FindIndex(g => g.Id == b.GroupId);
            if (gIdx >= 0) _edGroup.SelectedIndex = gIdx;
            _edPeerEndpoint.Text = !string.IsNullOrEmpty(b.PeerHost) ? string.Format("{0}:{1}", b.PeerHost, b.PeerPort > 0 ? b.PeerPort : 5192) : "";
            _edBlocked.Checked = b.Blocked;
        }

        private void RefreshGroupsList()
        {
            _lstGroups.Items.Clear();
            _addGroup.Items.Clear();
            foreach (BuddyGroup g in Store.State.Groups)
            {
                int cnt = Store.State.Buddies.Count(b => b.GroupId == g.Id);
                _lstGroups.Items.Add(string.Format("{0} ({1} buddies)", g.Name, cnt));
                _addGroup.Items.Add(g.Name);
            }
            if (_addGroup.Items.Count > 0) _addGroup.SelectedIndex = 0;
        }
    }
}
