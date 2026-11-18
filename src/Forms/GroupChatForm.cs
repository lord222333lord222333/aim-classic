using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace AIMClassic
{
    public class GroupChatForm : XpForm
    {
        private ComboBox _cboRooms;
        private Label _lblTopic, _lblRoomBadge;
        private RichTextBox _rtbChat;
        private ListBox _lstPeople;
        private TextBox _txtInput;
        private ToolStripButton _btnAiBots;
        private ChatRoom _currentRoom;
        private System.Windows.Forms.Timer _syncTimer;
        private long _roomSinceUnix = 0;
        private Action<string, string, string, string> _roomMsgHandler;
        private bool _syncingCombo = false;

        public GroupChatForm()
        {
            Text = I18n.T("AIM Public Groups & Chat Rooms", "AIM Публичные группы и чат-комнаты");
            Size = new Size(644, 486);
            MinimumSize = new Size(520, 380);
            StartPosition = FormStartPosition.Manual;
            Location = new Point(92, 48);
            TitleIconImage = RetroAssets.GetIconBitmap("groups", 16);

            _currentRoom = Store.State.ChatRooms[0];
            BuildUi();

            _roomMsgHandler = delegate(string roomName, string from, string text, string mType)
            {
                if (IsDisposed) return;
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        if (IsDisposed) return;
                        HandleIncomingPublicEvent(roomName, from, text, mType);
                    });
                }
                catch { }
            };
            NetworkEngine.RoomMessageReceived += _roomMsgHandler;

            _syncTimer = new System.Windows.Forms.Timer { Interval = 3500 };
            _syncTimer.Tick += delegate
            {
                if (_currentRoom != null && _currentRoom.IsPublic)
                {
                    string rName = _currentRoom.Name;
                    ThreadPool.QueueUserWorkItem(delegate { NetworkEngine.PollPublicRoom(rName, ref _roomSinceUnix); });
                }
            };
            _syncTimer.Start();

            FormClosed += delegate
            {
                NetworkEngine.RoomMessageReceived -= _roomMsgHandler;
                if (_syncTimer != null) _syncTimer.Dispose();
            };

            Load += delegate
            {
                _rtbChat.BackColor = Color.FromArgb(255, 255, 254);
                _rtbChat.BackColor = Color.White;
                LoadRoom(_currentRoom);
            };
        }

        private void BuildUi()
        {
            Panel top = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = XpTheme.Current.SubBarTop };
            PictureBox pic = new PictureBox { Location = new Point(8, 14), Size = new Size(26, 26), Image = RetroAssets.GetIconBitmap("groups", 24), SizeMode = PictureBoxSizeMode.Zoom };
            Label lblR = new Label { Text = I18n.T("Room:", "Комната:"), Font = XpTheme.UiBold, ForeColor = XpTheme.Current.AimNavy, Location = new Point(40, 8), AutoSize = true };
            _lblRoomBadge = new Label { Location = new Point(108, 9), Size = new Size(96, 14), Font = new Font("Tahoma", 6.5f, FontStyle.Bold), BackColor = Color.FromArgb(16, 142, 62), ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter, Text = "PUBLIC GLOBAL" };
            _lblTopic = new Label { Location = new Point(40, 29), Size = new Size(262, 18), Font = XpTheme.SmallFont, ForeColor = Color.FromArgb(60, 86, 122), AutoEllipsis = true, UseMnemonic = false };

            Panel topRight = new Panel { Dock = DockStyle.Right, Width = 334 };
            _cboRooms = new ComboBox { Location = new Point(4, 15), Width = 136, DropDownStyle = ComboBoxStyle.DropDownList };
            RefreshRoomDropdown();
            _cboRooms.SelectedIndexChanged += delegate
            {
                if (_syncingCombo) return;
                if (_cboRooms.SelectedIndex >= 0 && _cboRooms.SelectedIndex < Store.State.ChatRooms.Count)
                {
                    LoadRoom(Store.State.ChatRooms[_cboRooms.SelectedIndex]);
                }
            };

            XpButton btnHub = new XpButton { Text = I18n.T("Public Hub...", "Все группы..."), IsPrimary = true, Location = new Point(144, 14), Size = new Size(76, 23), Font = XpTheme.SmallFont };
            XpButton btnCreate = new XpButton { Text = I18n.T("+ New", "+ Создать"), Location = new Point(224, 14), Size = new Size(56, 23), Font = XpTheme.SmallFont };
            XpButton btnInvite = new XpButton { Text = I18n.T("Invite", "Позвать"), Location = new Point(284, 14), Size = new Size(46, 23), Font = XpTheme.SmallFont };

            btnHub.Click += delegate
            {
                using (PublicGroupsForm hub = new PublicGroupsForm())
                {
                    if (hub.ShowDialog(this) == DialogResult.OK && hub.JoinedRoom != null)
                        SelectOrAddRoom(hub.JoinedRoom);
                }
            };
            btnCreate.Click += delegate { CreateNewRoomDialog(); };
            btnInvite.Click += delegate
            {
                ContextMenuStrip cm = new ContextMenuStrip { Renderer = new XpToolRenderer() };
                foreach (Buddy b in Store.State.Buddies)
                {
                    if (!_currentRoom.Participants.Contains(b.ScreenName))
                    {
                        string sn = b.ScreenName;
                        cm.Items.Add("Invite " + sn, null, delegate { InviteBuddy(sn); });
                    }
                }
                if (cm.Items.Count == 0) cm.Items.Add("All buddies are already in this room!").Enabled = false;
                cm.Show(btnInvite, new Point(0, btnInvite.Height));
            };
            topRight.Controls.AddRange(new Control[] { _cboRooms, btnHub, btnCreate, btnInvite });
            top.Controls.AddRange(new Control[] { pic, lblR, _lblRoomBadge, _lblTopic, topRight });

            // Input Panel
            Panel inputPanel = new Panel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(6, 3, 6, 5) };
            Panel sendCol = new Panel { Dock = DockStyle.Right, Width = 84, Padding = new Padding(6, 0, 0, 0) };
            XpButton btnSend = new XpButton { Text = I18n.T("Send", "Отпр."), IsPrimary = true, Location = new Point(6, 0), Size = new Size(76, 33) };
            XpButton btnClear = new XpButton { Text = I18n.T("Clear", "Очист."), Location = new Point(6, 36), Size = new Size(76, 20), Font = XpTheme.SmallFont };
            btnSend.Click += delegate { SendRoomMessage(); };
            btnClear.Click += delegate
            {
                SoundEngine.Play("chatLeave");
                _currentRoom.Messages.Clear();
                _currentRoom.Messages.Add(new ChatMessage { Sender = "SYSTEM", Text = "*** Transcript cleared.", Time = Store.NowTimeStr() });
                Store.Save();
                LoadRoom(_currentRoom);
            };
            sendCol.Controls.AddRange(new Control[] { btnSend, btnClear });

            _txtInput = new TextBox { Dock = DockStyle.Fill, Multiline = true, BorderStyle = BorderStyle.FixedSingle };
            _txtInput.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter && !e.Shift) { e.SuppressKeyPress = true; SendRoomMessage(); }
            };
            inputPanel.Controls.Add(_txtInput);
            inputPanel.Controls.Add(sendCol);

            // Toolbar (Fun, Music, Translate, AI Toggle)
            ToolStrip fmt = new ToolStrip { Dock = DockStyle.Bottom, GripStyle = ToolStripGripStyle.Hidden, Renderer = new XpToolRenderer(), AutoSize = false, Height = 26 };
            ToolStripButton emoBtn = new ToolStripButton("Smiley ▾", RetroAssets.GetEmoticonBitmap("smile"));
            emoBtn.Click += delegate { PrivateChatForm.ShowEmoticonMenu(fmt, _txtInput); };
            ToolStripButton waveBtn = new ToolStripButton("/me Wave");
            waveBtn.Click += delegate
            {
                string me = Store.GetActiveAccount().ScreenName;
                BroadcastAndAppendSystem(string.Format("* {0} waves hello to everyone in {1}! :-)", me, _currentRoom.Name));
            };
            ToolStripButton rollBtn = new ToolStripButton("/roll Dice");
            rollBtn.Click += delegate
            {
                int roll = new Random().Next(1, 101);
                string me = Store.GetActiveAccount().ScreenName;
                BroadcastAndAppendSystem(string.Format("🎲 {0} rolled {1} (1-100)!", me, roll));
            };
            ToolStripButton ballBtn = new ToolStripButton("/8ball");
            ballBtn.Click += delegate
            {
                string[] ans = new[] { "Yes, definitely! :-D", "Signs point to YES :-)", "Ask again later ;-)", "My 2006 crystal ball says MAYBE!", "No way :-P", "Most likely <3" };
                string me = Store.GetActiveAccount().ScreenName;
                BroadcastAndAppendSystem(string.Format("🎱 {0} shakes Magic 8-Ball -> {1}", me, ans[new Random().Next(ans.Length)]));
            };
            ToolStripButton npBtn = new ToolStripButton("♫ Now Playing", RetroAssets.GetIconBitmap("music", 16));
            npBtn.Click += delegate
            {
                MusicEngine.EnsureInitialized();
                MusicTrack tr = MusicEngine.CurrentTrack;
                string me = Store.GetActiveAccount().ScreenName;
                string txt = tr != null
                    ? string.Format("♫ {0} is listening to: {1} — {2}", me, tr.Title, tr.Artist)
                    : string.Format("♫ {0} opened AIM Music Player!", me);
                BroadcastAndAppendSystem(txt);
            };
            ToolStripDropDownButton transBtn = new ToolStripDropDownButton("Translate", RetroAssets.GetIconBitmap("translate", 16));
            transBtn.DropDownItems.Add("Translate Last Message -> EN", null, delegate { TranslateLastRoomMsg("en"); });
            transBtn.DropDownItems.Add("Translate Last Message -> RU", null, delegate { TranslateLastRoomMsg("ru"); });

            _btnAiBots = new ToolStripButton("AI Bots: ON") { Checked = true, CheckOnClick = true, ToolTipText = "Allow AI buddies in this room to reply" };
            _btnAiBots.CheckedChanged += delegate
            {
                _btnAiBots.Text = _btnAiBots.Checked ? "AI Bots: ON" : "AI Bots: OFF";
            };
            fmt.Items.AddRange(new ToolStripItem[] { emoBtn, waveBtn, rollBtn, ballBtn, npBtn, transBtn, new ToolStripSeparator(), _btnAiBots });

            // Main Split: Transcript + People Here
            Panel mainSplit = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 5, 6, 3) };
            Panel peoplePanel = new Panel { Dock = DockStyle.Right, Width = 170, Padding = new Padding(5, 0, 0, 0) };
            Label lblPpl = new Label { Dock = DockStyle.Top, Height = 22, Text = I18n.T("People Here", "Участники"), BackColor = XpTheme.Current.SubBarBot, ForeColor = XpTheme.Current.AimNavy, Font = XpTheme.UiBold, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0) };
            _lstPeople = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            _lstPeople.DoubleClick += delegate
            {
                string target = _lstPeople.SelectedItem as string;
                string me = Store.GetActiveAccount().ScreenName;
                if (!string.IsNullOrEmpty(target) && !string.Equals(target, me, StringComparison.OrdinalIgnoreCase))
                {
                    if (Store.GetBuddy(target) == null)
                        Store.AddRealFriend(target, "", 0, "online", "AIM Icon.gif", "Met in " + _currentRoom.Name, "grp_real");
                    AimApp.OpenPrivateChat(target);
                }
            };
            peoplePanel.Controls.Add(_lstPeople);
            peoplePanel.Controls.Add(lblPpl);

            _rtbChat = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, DetectUrls = true };
            mainSplit.Controls.Add(_rtbChat);
            mainSplit.Controls.Add(peoplePanel);

            ClientBody.Controls.Add(mainSplit);
            ClientBody.Controls.Add(fmt);
            ClientBody.Controls.Add(inputPanel);
            ClientBody.Controls.Add(top);
        }

        private void RefreshRoomDropdown()
        {
            _syncingCombo = true;
            _cboRooms.Items.Clear();
            int sel = 0;
            for (int i = 0; i < Store.State.ChatRooms.Count; i++)
            {
                ChatRoom r = Store.State.ChatRooms[i];
                _cboRooms.Items.Add(r.Name);
                if (_currentRoom != null && string.Equals(r.Name, _currentRoom.Name, StringComparison.OrdinalIgnoreCase))
                    sel = i;
            }
            if (_cboRooms.Items.Count > 0) _cboRooms.SelectedIndex = sel;
            _syncingCombo = false;
        }

        public void SelectOrAddRoom(ChatRoom room)
        {
            if (room == null) return;
            ChatRoom existing = Store.State.ChatRooms.FirstOrDefault(r => string.Equals(r.Name, room.Name, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                if (room.Participants == null) room.Participants = new List<string>();
                if (room.Messages == null) room.Messages = new List<ChatMessage>();
                if (room.Messages.Count == 0)
                    room.Messages.Add(new ChatMessage { Sender = "SYSTEM", Text = "*** Joined Public Room " + room.Name + " — " + room.Topic, Time = Store.NowTimeStr() });
                Store.State.ChatRooms.Add(room);
                Store.Save();
                existing = room;
            }
            _roomSinceUnix = 0;
            LoadRoom(existing);
            RefreshRoomDropdown();
        }

        public void InviteBuddy(string sn)
        {
            if (!_currentRoom.Participants.Contains(sn))
            {
                _currentRoom.Participants.Add(sn);
                AppendRoomMsg(new ChatMessage { Sender = "SYSTEM", Text = string.Format("*** {0} has entered {1}.", sn, _currentRoom.Name), Time = Store.NowTimeStr() });
                SoundEngine.Play("chatJoin");
                LoadRoom(_currentRoom);
            }
        }

        private void LoadRoom(ChatRoom room)
        {
            _currentRoom = room;
            _roomSinceUnix = 0;
            string me = Store.GetActiveAccount().ScreenName;
            if (room.Participants == null) room.Participants = new List<string>();
            if (room.Messages == null) room.Messages = new List<ChatMessage>();
            if (!room.Participants.Contains(me)) room.Participants.Insert(0, me);

            Text = room.Name + " — " + (room.IsPublic ? I18n.T("Public Group", "Публичная группа") : I18n.T("Chat Room", "Чат"));
            _lblTopic.Text = room.Topic ?? "";
            _lblRoomBadge.Text = room.IsPublic ? I18n.T("PUBLIC GLOBAL", "ПУБЛИЧНАЯ") : I18n.T("PRIVATE ROOM", "ЛОКАЛЬНАЯ");
            _lblRoomBadge.BackColor = room.IsPublic ? Color.FromArgb(14, 138, 62) : Color.FromArgb(88, 102, 128);

            _lstPeople.Items.Clear();
            foreach (string p in room.Participants) _lstPeople.Items.Add(p);

            _rtbChat.Clear();
            foreach (ChatMessage m in room.Messages)
            {
                RetroAssets.AppendChatMessageToRtb(_rtbChat, m, me, Store.State.Settings.ShowTimestamps, Store.State.Settings.EnableEmoticons);
            }

            if (room.IsPublic)
            {
                NetworkEngine.SendPublicRoomMessage(room.Name, me, "ROOM_JOIN");
                string rn = room.Name;
                ThreadPool.QueueUserWorkItem(delegate { NetworkEngine.PollPublicRoom(rn, ref _roomSinceUnix); });
            }
        }

        private void HandleIncomingPublicEvent(string roomName, string from, string text, string mType)
        {
            if (_currentRoom == null || !string.Equals(_currentRoom.Name, roomName, StringComparison.OrdinalIgnoreCase)) return;
            if (!string.IsNullOrEmpty(from) && !_currentRoom.Participants.Contains(from))
            {
                _currentRoom.Participants.Add(from);
                _lstPeople.Items.Add(from);
                SoundEngine.Play("chatJoin");
                if (mType == "ROOM_JOIN")
                {
                    AppendRoomMsg(new ChatMessage { Sender = "SYSTEM", Text = string.Format("*** {0} joined {1} [Global/LAN].", from, roomName), Time = Store.NowTimeStr() });
                    return;
                }
            }
            if (mType == "ROOM_JOIN") return;
            if (mType == "ROOM_SYS")
            {
                AppendRoomMsg(new ChatMessage { Sender = "SYSTEM", Text = text, Time = Store.NowTimeStr() });
                return;
            }
            SoundEngine.Play("imReceive");
            AppendRoomMsg(new ChatMessage { Sender = from, Text = text, Time = Store.NowTimeStr(), Color = "#0055AA" });
        }

        private void AppendRoomMsg(ChatMessage m)
        {
            _currentRoom.Messages.Add(m);
            Store.Save();
            RetroAssets.AppendChatMessageToRtb(_rtbChat, m, Store.GetActiveAccount().ScreenName, Store.State.Settings.ShowTimestamps, Store.State.Settings.EnableEmoticons);
        }

        private void BroadcastAndAppendSystem(string text)
        {
            AppendRoomMsg(new ChatMessage { Sender = "SYSTEM", Text = text, Time = Store.NowTimeStr() });
            if (_currentRoom != null && _currentRoom.IsPublic)
                NetworkEngine.SendPublicRoomMessage(_currentRoom.Name, text, "ROOM_SYS");
        }

        private void TranslateLastRoomMsg(string targetLang)
        {
            if (_currentRoom == null || _currentRoom.Messages.Count == 0) return;
            ChatMessage target = null;
            for (int i = _currentRoom.Messages.Count - 1; i >= 0; i--)
            {
                if (_currentRoom.Messages[i].Sender != "SYSTEM" && !string.IsNullOrWhiteSpace(_currentRoom.Messages[i].Text))
                {
                    target = _currentRoom.Messages[i];
                    break;
                }
            }
            if (target == null) return;
            string orig = target.Text;
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
                            AppendRoomMsg(new ChatMessage { Sender = "SYSTEM", Text = string.Format("[Translate -> {0}] {1}", targetLang.ToUpperInvariant(), tr), Time = Store.NowTimeStr() });
                        });
                    }
                    catch { }
                }
            });
        }

        private void SendRoomMessage()
        {
            string text = _txtInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            _txtInput.Clear();

            string me = Store.GetActiveAccount().ScreenName;
            string low = text.ToLowerInvariant();
            if (low == "/roll")
            {
                SoundEngine.Play("imSend");
                BroadcastAndAppendSystem(string.Format("🎲 {0} rolled {1} (1-100)!", me, new Random().Next(1, 101)));
                return;
            }
            if (low == "/8ball")
            {
                string[] ans = new[] { "Yes, definitely! :-D", "Outlook good :-)", "Ask again later ;-)", "Cannot predict now :-/", "Absolutely not :-P" };
                SoundEngine.Play("imSend");
                BroadcastAndAppendSystem(string.Format("🎱 {0} shakes Magic 8-Ball -> {1}", me, ans[new Random().Next(ans.Length)]));
                return;
            }

            SoundEngine.Play("imSend");
            AppendRoomMsg(new ChatMessage { Sender = me, Text = text, Color = "#990000" });

            if (_currentRoom != null && _currentRoom.IsPublic)
            {
                NetworkEngine.SendPublicRoomMessage(_currentRoom.Name, text, "ROOM_MSG");
            }

            if (_btnAiBots != null && !_btnAiBots.Checked) return;

            List<string> botPeers = _currentRoom.Participants.Where(p =>
                !string.Equals(p, me, StringComparison.OrdinalIgnoreCase) &&
                (Store.GetBuddy(p) == null || !Store.GetBuddy(p).IsRealUser)).ToList();

            if (botPeers.Count > 0)
            {
                string responder = botPeers[new Random().Next(botPeers.Count)];
                Buddy b = Store.GetBuddy(responder) ?? new Buddy { ScreenName = responder, Persona = "friendly", Interests = "Chatting" };
                ThreadPool.QueueUserWorkItem(delegate
                {
                    Thread.Sleep(1100);
                    string reply = AiEngine.GenerateReply(b, text, me);
                    if (!IsDisposed)
                    {
                        try
                        {
                            BeginInvoke((Action)delegate
                            {
                                if (IsDisposed) return;
                                SoundEngine.Play("imReceive");
                                AppendRoomMsg(new ChatMessage { Sender = responder, Text = reply, Time = Store.NowTimeStr(), Color = "#0000AA" });
                            });
                        }
                        catch { }
                    }
                });
            }
        }

        private void CreateNewRoomDialog()
        {
            using (XpForm dlg = new XpForm())
            {
                dlg.Text = I18n.T("Create Public / Private AIM Group", "Создать публичную или обычную группу");
                dlg.Size = new Size(376, 232);
                dlg.ShowMaximizeButton = false;
                dlg.ShowMinimizeButton = false;
                dlg.AllowResize = false;

                Label l1 = new Label { Text = I18n.T("Room Name:", "Название:"), Location = new Point(12, 16), AutoSize = true, Font = XpTheme.UiBold };
                TextBox t1 = new TextBox { Location = new Point(108, 13), Width = 242, Text = "#Global_Hangout_2007", BorderStyle = BorderStyle.FixedSingle };
                Label l2 = new Label { Text = I18n.T("Category:", "Категория:"), Location = new Point(12, 45), AutoSize = true };
                ComboBox cboCat = new ComboBox { Location = new Point(108, 42), Width = 242, DropDownStyle = ComboBoxStyle.DropDownList };
                cboCat.Items.AddRange(new object[] { "Global", "General", "Themes & Design", "Gaming", "Music", "Tech & AI", "Languages" });
                cboCat.SelectedIndex = 0;
                Label l3 = new Label { Text = I18n.T("Topic:", "Тема:"), Location = new Point(12, 75), AutoSize = true };
                TextBox t2 = new TextBox { Location = new Point(108, 72), Width = 242, Text = "Open chat for everyone who downloaded AIM :-)", BorderStyle = BorderStyle.FixedSingle };
                CheckBox chkPub = new CheckBox { Text = I18n.T("Publish to Global Public Groups Directory", "Опубликовать в глобальный каталог групп"), Location = new Point(12, 106), AutoSize = true, Checked = true, Font = XpTheme.UiBold, UseMnemonic = false };

                Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = XpTheme.Current.FaceDark };
                XpButton ok = new XpButton { Text = I18n.T("Create & Join", "Создать и войти"), IsPrimary = true, Location = new Point(156, 6), Size = new Size(116, 24) };
                XpButton cancel = new XpButton { Text = I18n.T("Cancel", "Отмена"), Location = new Point(278, 6), Size = new Size(76, 24) };
                ok.Click += delegate
                {
                    string raw = t1.Text.Trim();
                    if (string.IsNullOrEmpty(raw)) return;
                    string name = raw.StartsWith("#") ? raw : "#" + raw;
                    string me = Store.GetActiveAccount().ScreenName;
                    ChatRoom nr = new ChatRoom
                    {
                        Id = "room_" + DateTime.Now.Ticks,
                        Name = name,
                        Topic = t2.Text.Trim(),
                        Category = Convert.ToString(cboCat.SelectedItem),
                        CreatedBy = me,
                        IsPublic = chkPub.Checked,
                        Participants = new List<string> { me, "AIM_AI_Helper" },
                        Messages = new List<ChatMessage> { new ChatMessage { Sender = "SYSTEM", Text = "*** Created " + (chkPub.Checked ? "Public Global" : "Private") + " Group " + name, Time = Store.NowTimeStr() } }
                    };
                    Store.State.ChatRooms.Add(nr);
                    Store.Save();
                    if (nr.IsPublic) NetworkEngine.PublishPublicRoom(nr);
                    LoadRoom(nr);
                    RefreshRoomDropdown();
                    dlg.Close();
                };
                cancel.Click += delegate { dlg.Close(); };
                foot.Controls.AddRange(new Control[] { ok, cancel });
                dlg.ClientBody.Controls.AddRange(new Control[] { l1, t1, l2, cboCat, l3, t2, chkPub, foot });
                dlg.ShowDialog(this);
            }
        }
    }

    /* ======================================================================
       Global Public Groups Browser & Directory Hub
       ====================================================================== */
    public class PublicGroupsForm : XpForm
    {
        private TextBox _txtSearch;
        private ComboBox _cboCat;
        private ListView _lvRooms;
        private Label _lblStatus;
        public ChatRoom JoinedRoom { get; private set; }

        public PublicGroupsForm()
        {
            Text = I18n.T("AIM Public Groups Directory — Join & Create Rooms", "Публичные группы AIM — Обзор и создание");
            Size = new Size(632, 468);
            ShowMaximizeButton = false;
            MinimumSize = new Size(540, 390);
            TitleIconImage = RetroAssets.GetIconBitmap("groups", 16);

            XpBanner banner = new XpBanner
            {
                BannerTitle = I18n.T("Global Public Groups Directory", "Каталог публичных групп AIM"),
                BannerSubtitle = I18n.T("Discover public rooms created by AIM users worldwide or publish your own!", "Находите открытые группы пользователей по всему миру или создавайте свои!"),
                BannerIcon = RetroAssets.GetIconBitmap("groups", 26)
            };
            ClientBody.Controls.Add(banner);

            Panel filterBar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = XpTheme.Current.SubBarTop };
            filterBar.Controls.Add(new Label { Text = I18n.T("Search:", "Поиск:"), Location = new Point(10, 12), AutoSize = true, Font = XpTheme.UiBold });
            _txtSearch = new TextBox { Location = new Point(60, 9), Width = 196, BorderStyle = BorderStyle.FixedSingle };
            _txtSearch.TextChanged += delegate { PopulateRooms(); };

            filterBar.Controls.Add(new Label { Text = I18n.T("Category:", "Тема:"), Location = new Point(268, 12), AutoSize = true, Font = XpTheme.UiBold });
            _cboCat = new ComboBox { Location = new Point(332, 9), Width = 152, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboCat.Items.AddRange(new object[] { "All Categories", "Global", "General", "Themes & Design", "Gaming", "Music", "Tech & AI", "Languages" });
            _cboCat.SelectedIndex = 0;
            _cboCat.SelectedIndexChanged += delegate { PopulateRooms(); };

            XpButton btnRefresh = new XpButton { Text = I18n.T("Sync Global", "Обновить"), Location = new Point(494, 8), Size = new Size(110, 24), Font = XpTheme.SmallFont };
            btnRefresh.Click += delegate
            {
                NetworkEngine.RefreshPublicRoomsGlobal();
                _lblStatus.Text = I18n.T("Synced with Global Public Groups directory...", "Синхронизация с глобальным каталогом...");
                PopulateRooms();
            };
            filterBar.Controls.AddRange(new Control[] { _txtSearch, _cboCat, btnRefresh });

            _lvRooms = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false,
                BackColor = Color.White,
                Font = XpTheme.UiFont
            };
            _lvRooms.Columns.Add(I18n.T("Group Name", "Группа"), 154);
            _lvRooms.Columns.Add(I18n.T("Category", "Категория"), 102);
            _lvRooms.Columns.Add(I18n.T("Creator", "Создатель"), 96);
            _lvRooms.Columns.Add(I18n.T("Topic / Description", "Описание"), 238);
            _lvRooms.DoubleClick += delegate { JoinSelected(); };

            Panel wrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 6, 8, 4) };
            wrap.Controls.Add(_lvRooms);

            _lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                BackColor = Color.FromArgb(255, 255, 225),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8, 5, 8, 4),
                Font = XpTheme.SmallFont,
                UseMnemonic = false,
                Text = I18n.T("Double-click any public group to join, or click '+ Create Public Group'!", "Дважды кликните по группе для входа или создайте новую публичную группу!")
            };

            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 38, Width = ClientBody.ClientSize.Width, BackColor = XpTheme.Current.FaceDark };
            XpButton btnCreate = new XpButton { Text = I18n.T("+ Create Public Group...", "+ Создать публичную группу..."), Location = new Point(10, 7), Size = new Size(188, 24) };
            XpButton btnJoin = new XpButton { Text = I18n.T("Join Public Group", "Войти в группу"), IsPrimary = true, Location = new Point(Math.Max(204, ClientBody.ClientSize.Width - 250), 7), Size = new Size(146, 24), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            XpButton btnClose = new XpButton { Text = I18n.T("Close", "Закрыть"), Location = new Point(Math.Max(356, ClientBody.ClientSize.Width - 98), 7), Size = new Size(88, 24), Anchor = AnchorStyles.Top | AnchorStyles.Right };

            btnCreate.Click += delegate { OpenQuickCreatePublic(); };
            btnJoin.Click += delegate { JoinSelected(); };
            btnClose.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            foot.Controls.AddRange(new Control[] { btnCreate, btnJoin, btnClose });

            ClientBody.Controls.Add(wrap);
            ClientBody.Controls.Add(_lblStatus);
            ClientBody.Controls.Add(filterBar);
            ClientBody.Controls.Add(foot);
            banner.SendToBack();
            wrap.BringToFront();

            Action onPubUpdate = delegate
            {
                if (!IsDisposed)
                {
                    try { BeginInvoke((Action)PopulateRooms); } catch { }
                }
            };
            NetworkEngine.PublicRoomsUpdated += onPubUpdate;
            FormClosed += delegate { NetworkEngine.PublicRoomsUpdated -= onPubUpdate; };

            NetworkEngine.RefreshPublicRoomsGlobal();
            PopulateRooms();
        }

        private void PopulateRooms()
        {
            Dictionary<string, ChatRoom> merged = new Dictionary<string, ChatRoom>(StringComparer.OrdinalIgnoreCase);
            foreach (ChatRoom r in Store.State.ChatRooms)
            {
                if (r.IsPublic || (r.Name ?? "").StartsWith("#")) merged[r.Name] = r;
            }
            lock (NetworkEngine.DiscoveredPublicRooms)
            {
                foreach (ChatRoom dr in NetworkEngine.DiscoveredPublicRooms.Values)
                {
                    if (!merged.ContainsKey(dr.Name)) merged[dr.Name] = dr;
                }
            }

            string q = (_txtSearch.Text ?? "").Trim().ToLowerInvariant();
            string cat = _cboCat.SelectedIndex > 0 ? Convert.ToString(_cboCat.SelectedItem) : "";

            _lvRooms.BeginUpdate();
            _lvRooms.Items.Clear();
            foreach (ChatRoom r in merged.Values)
            {
                string rCat = r.Category ?? "General";
                if (!string.IsNullOrEmpty(cat) && !string.Equals(rCat, cat, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.IsNullOrEmpty(q))
                {
                    bool hit = (r.Name ?? "").ToLowerInvariant().Contains(q) ||
                               (r.Topic ?? "").ToLowerInvariant().Contains(q) ||
                               (r.CreatedBy ?? "").ToLowerInvariant().Contains(q);
                    if (!hit) continue;
                }
                ListViewItem lvi = new ListViewItem(r.Name);
                lvi.SubItems.Add(rCat);
                lvi.SubItems.Add(r.CreatedBy ?? "AIM Team");
                lvi.SubItems.Add(r.Topic ?? "");
                lvi.Tag = r;
                if (r.Name == "#Global_AIM_Plaza" || r.Name == "#FrutigerAero_Hub")
                {
                    lvi.Font = XpTheme.UiBold;
                    lvi.ForeColor = XpTheme.Current.AimNavy;
                }
                _lvRooms.Items.Add(lvi);
            }
            if (_lvRooms.Items.Count > 0) _lvRooms.Items[0].Selected = true;
            _lvRooms.EndUpdate();
        }

        private void JoinSelected()
        {
            if (_lvRooms.SelectedItems.Count == 0) return;
            JoinedRoom = _lvRooms.SelectedItems[0].Tag as ChatRoom;
            if (JoinedRoom == null) return;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OpenQuickCreatePublic()
        {
            using (XpForm dlg = new XpForm())
            {
                dlg.Text = I18n.T("Publish New Public Group", "Опубликовать новую публичную группу");
                dlg.Size = new Size(376, 196);
                dlg.ShowMaximizeButton = false;
                dlg.ShowMinimizeButton = false;
                dlg.AllowResize = false;

                Label l1 = new Label { Text = I18n.T("Group #Name:", "Название #...:"), Location = new Point(12, 16), AutoSize = true, Font = XpTheme.UiBold };
                TextBox t1 = new TextBox { Location = new Point(112, 13), Width = 238, Text = "#My_Public_Group", BorderStyle = BorderStyle.FixedSingle };
                Label l2 = new Label { Text = I18n.T("Category:", "Категория:"), Location = new Point(12, 46), AutoSize = true };
                ComboBox cbo = new ComboBox { Location = new Point(112, 43), Width = 238, DropDownStyle = ComboBoxStyle.DropDownList };
                cbo.Items.AddRange(new object[] { "Global", "General", "Themes & Design", "Gaming", "Music", "Tech & AI", "Languages" });
                cbo.SelectedIndex = 0;
                Label l3 = new Label { Text = I18n.T("Topic:", "Описание:"), Location = new Point(12, 76), AutoSize = true };
                TextBox t2 = new TextBox { Location = new Point(112, 73), Width = 238, Text = "Welcome everyone! Say hello :-)", BorderStyle = BorderStyle.FixedSingle };

                Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = XpTheme.Current.FaceDark };
                XpButton ok = new XpButton { Text = I18n.T("Publish & Join", "Создать и войти"), IsPrimary = true, Location = new Point(156, 6), Size = new Size(116, 24) };
                XpButton cancel = new XpButton { Text = I18n.T("Cancel", "Отмена"), Location = new Point(278, 6), Size = new Size(76, 24) };
                ok.Click += delegate
                {
                    string raw = t1.Text.Trim();
                    if (raw.Length < 2) return;
                    string name = raw.StartsWith("#") ? raw : "#" + raw;
                    string me = Store.GetActiveAccount().ScreenName;
                    ChatRoom nr = new ChatRoom
                    {
                        Id = "room_" + DateTime.Now.Ticks,
                        Name = name,
                        Topic = t2.Text.Trim(),
                        Category = Convert.ToString(cbo.SelectedItem),
                        CreatedBy = me,
                        IsPublic = true,
                        Participants = new List<string> { me, "AIM_AI_Helper" },
                        Messages = new List<ChatMessage> { new ChatMessage { Sender = "SYSTEM", Text = "*** Created & published Global Public Group " + name, Time = Store.NowTimeStr() } }
                    };
                    Store.State.ChatRooms.Add(nr);
                    Store.Save();
                    NetworkEngine.PublishPublicRoom(nr);
                    JoinedRoom = nr;
                    dlg.Close();
                    DialogResult = DialogResult.OK;
                    Close();
                };
                cancel.Click += delegate { dlg.Close(); };
                foot.Controls.AddRange(new Control[] { ok, cancel });
                dlg.ClientBody.Controls.AddRange(new Control[] { l1, t1, l2, cbo, l3, t2, foot });
                dlg.ShowDialog(this);
            }
        }
    }
}
