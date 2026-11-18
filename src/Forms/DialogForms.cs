using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace AIMClassic
{
    /* ======================================================================
       #5 User Profile Form (Edit My Profile & View Buddy Profile)
       ====================================================================== */
    public class ProfileForm : XpForm
    {
        public ProfileForm(string buddySN, bool editMe)
        {
            Account me = Store.GetActiveAccount();
            bool isMe = editMe || string.IsNullOrEmpty(buddySN) || string.Equals(buddySN, me.ScreenName, StringComparison.OrdinalIgnoreCase);
            Buddy b = !isMe ? Store.GetBuddy(buddySN) : null;

            Text = isMe ? "My Profile — " + me.ScreenName : "Buddy Info — " + (b != null ? b.ScreenName : buddySN);
            Size = new Size(430, 425);
            ShowMaximizeButton = false;
            AllowResize = false;
            TitleIconImage = RetroAssets.GetIconBitmap("profile", 16);

            XpBanner banner = new XpBanner
            {
                BannerTitle = isMe ? me.ScreenName : (b != null ? b.ScreenName : buddySN),
                BannerSubtitle = isMe ? "Edit your AIM Member Profile & Quote" : string.Format("Status: {0}  |  Warn: {1}%", b != null ? b.Status.ToUpper() : "ONLINE", b != null ? b.WarningLevel : 0),
                BannerIcon = RetroAssets.GetAvatarThumbnail(isMe ? me.Avatar : (b != null ? b.Avatar : "AIM Icon.gif"), 26)
            };
            ClientBody.Controls.Add(banner);

            if (isMe)
            {
                Panel p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
                int y = 12;
                p.Controls.Add(new Label { Text = "Display Name:", Location = new Point(12, y + 3), AutoSize = true });
                TextBox txtDisp = new TextBox { Location = new Point(110, y), Width = 290, Text = me.DisplayName, BorderStyle = BorderStyle.FixedSingle };
                y += 26;
                p.Controls.Add(new Label { Text = "Location:", Location = new Point(12, y + 3), AutoSize = true });
                TextBox txtLoc = new TextBox { Location = new Point(110, y), Width = 290, Text = me.Location, BorderStyle = BorderStyle.FixedSingle };
                y += 26;
                p.Controls.Add(new Label { Text = "Interests:", Location = new Point(12, y + 3), AutoSize = true });
                TextBox txtInt = new TextBox { Location = new Point(110, y), Width = 290, Text = me.Interests, BorderStyle = BorderStyle.FixedSingle };
                y += 26;
                p.Controls.Add(new Label { Text = "About Me / Bio:", Location = new Point(12, y + 3), AutoSize = true });
                y += 20;
                TextBox txtBio = new TextBox { Location = new Point(12, y), Size = new Size(388, 120), Multiline = true, ScrollBars = ScrollBars.Vertical, Text = me.Bio, BorderStyle = BorderStyle.FixedSingle };
                y += 128;
                CheckBox chkDir = new CheckBox { Text = "Show me in AIM Directory & Chat Invites", Location = new Point(12, y), Checked = me.AvailableForChat, AutoSize = true };

                p.Controls.AddRange(new Control[] { txtDisp, txtLoc, txtInt, txtBio, chkDir });

                Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 38, BackColor = XpTheme.Current.FaceDark };
                XpButton btnAv = new XpButton { Text = "Change Icon...", Location = new Point(12, 7), Size = new Size(98, 24) };
                XpButton btnSave = new XpButton { Text = "Save Profile", IsPrimary = true, Location = new Point(226, 7), Size = new Size(92, 24) };
                XpButton btnCan = new XpButton { Text = "Cancel", Location = new Point(324, 7), Size = new Size(80, 24) };
                btnAv.Click += delegate { new AvatarGalleryForm().ShowDialog(this); };
                btnSave.Click += delegate
                {
                    me.DisplayName = string.IsNullOrWhiteSpace(txtDisp.Text) ? me.ScreenName : txtDisp.Text.Trim();
                    me.Location = txtLoc.Text.Trim();
                    me.Interests = txtInt.Text.Trim();
                    me.Bio = txtBio.Text;
                    me.AvailableForChat = chkDir.Checked;
                    Store.Notify("profile");
                    Close();
                };
                btnCan.Click += delegate { Close(); };
                foot.Controls.AddRange(new Control[] { btnAv, btnSave, btnCan });

                ClientBody.Controls.Add(p);
                p.BringToFront();
                ClientBody.Controls.Add(foot);
            }
            else if (b != null)
            {
                Panel p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
                PictureBox pb = new PictureBox { Location = new Point(12, 10), Size = new Size(48, 48), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
                RetroAssets.SetPictureBoxAvatar(pb, b.Avatar);
                Label lblInfo = new Label
                {
                    Location = new Point(70, 10),
                    Size = new Size(330, 82),
                    Text = string.Format("Screen Name: {0}\nLocation: {1}\nMember Since: {2}\nIdle: {3} min\nInterests: {4}",
                        b.ScreenName, b.Location ?? "Unknown", b.MemberSince ?? "2005", b.IdleMinutes, b.Interests ?? "-")
                };
                TextBox txtBio = new TextBox
                {
                    Location = new Point(12, 104),
                    Size = new Size(388, 175),
                    Multiline = true,
                    ReadOnly = true,
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    Text = (string.IsNullOrEmpty(b.AwayText) ? "" : "Away: " + b.AwayText + "\r\n\r\n") + (b.Bio ?? "")
                };
                p.Controls.AddRange(new Control[] { pb, lblInfo, txtBio });

                Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 38, BackColor = XpTheme.Current.FaceDark };
                XpButton btnIm = new XpButton { Text = "Send IM", IsPrimary = true, Location = new Point(116, 7), Size = new Size(80, 24) };
                XpButton btnFile = new XpButton { Text = "Send File", Location = new Point(202, 7), Size = new Size(80, 24) };
                XpButton btnClose = new XpButton { Text = "Close", Location = new Point(324, 7), Size = new Size(80, 24) };
                btnIm.Click += delegate { AimApp.OpenPrivateChat(b.ScreenName); Close(); };
                btnFile.Click += delegate { new FileTransferForm(b.ScreenName).Show(); Close(); };
                btnClose.Click += delegate { Close(); };
                foot.Controls.AddRange(new Control[] { btnIm, btnFile, btnClose });

                ClientBody.Controls.Add(p);
                p.BringToFront();
                ClientBody.Controls.Add(foot);
            }
        }
    }

    /* ======================================================================
       #6 Away Message Editor
       ====================================================================== */
    public class AwayEditorForm : XpForm
    {
        private ListBox _lst;
        private TextBox _txtTitle, _txtBody;
        private CheckBox _chkAuto;

        public AwayEditorForm()
        {
            Text = "Away Message Editor — AIM";
            Size = new Size(465, 395);
            ShowMaximizeButton = false;
            AllowResize = false;
            TitleIconImage = RetroAssets.GetIconBitmap("away", 16);

            XpBanner banner = new XpBanner
            {
                BannerTitle = "Away Message Editor",
                BannerSubtitle = "Use %t for current time and %n for sender Screen Name",
                BannerIcon = RetroAssets.GetIconBitmap("away", 26)
            };
            ClientBody.Controls.Add(banner);

            Panel body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            _lst = new ListBox { Location = new Point(10, 10), Size = new Size(148, 224), BorderStyle = BorderStyle.FixedSingle };
            _lst.SelectedIndexChanged += delegate { LoadSelected(); };

            XpButton btnNew = new XpButton { Text = "+ New", Location = new Point(10, 240), Size = new Size(71, 23), Font = XpTheme.SmallFont };
            XpButton btnDel = new XpButton { Text = "Delete", Location = new Point(87, 240), Size = new Size(71, 23), Font = XpTheme.SmallFont };

            body.Controls.Add(new Label { Text = "Message Title:", Location = new Point(168, 10), AutoSize = true, Font = XpTheme.UiBold });
            _txtTitle = new TextBox { Location = new Point(168, 28), Width = 270, BorderStyle = BorderStyle.FixedSingle };

            body.Controls.Add(new Label { Text = "Away Message Text:", Location = new Point(168, 56), AutoSize = true, Font = XpTheme.UiBold });
            XpButton btnEmo = new XpButton { Text = "Smiley", Location = new Point(372, 52), Size = new Size(66, 20), Font = XpTheme.SmallFont };
            _txtBody = new TextBox { Location = new Point(168, 76), Size = new Size(270, 152), Multiline = true, BorderStyle = BorderStyle.FixedSingle };
            btnEmo.Click += delegate { PrivateChatForm.ShowEmoticonMenu(btnEmo, _txtBody); };

            _chkAuto = new CheckBox { Text = "Auto-reply once to incoming IMs while Away", Location = new Point(168, 236), AutoSize = true };

            btnNew.Click += delegate
            {
                AwayPreset p = new AwayPreset { Id = "away_" + DateTime.Now.Ticks, Title = "Custom Away", Text = "~*~ Away (%t) — leave a message %n! ~*~", AutoReply = true };
                Store.State.AwayPresets.Add(p);
                Store.Save();
                RefreshList(Store.State.AwayPresets.Count - 1);
            };
            btnDel.Click += delegate
            {
                if (Store.State.AwayPresets.Count <= 1 || _lst.SelectedIndex < 0) return;
                Store.State.AwayPresets.RemoveAt(_lst.SelectedIndex);
                Store.Save();
                RefreshList(0);
            };

            body.Controls.AddRange(new Control[] { _lst, btnNew, btnDel, _txtTitle, btnEmo, _txtBody, _chkAuto });
            ClientBody.Controls.Add(body);
            body.BringToFront();

            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 38, BackColor = XpTheme.Current.FaceDark };
            XpButton btnSave = new XpButton { Text = "Save Preset", Location = new Point(155, 7), Size = new Size(88, 24) };
            XpButton btnSet = new XpButton { Text = "Set Away Now", IsPrimary = true, Location = new Point(249, 7), Size = new Size(106, 24) };
            XpButton btnClose = new XpButton { Text = "Close", Location = new Point(361, 7), Size = new Size(80, 24) };
            btnSave.Click += delegate { SaveCurrent(); Store.Save(); RefreshList(_lst.SelectedIndex); };
            btnSet.Click += delegate
            {
                SaveCurrent();
                Store.State.CurrentUser.AwayMessageTitle = _txtTitle.Text.Trim();
                Store.State.CurrentUser.AwayMessageText = _txtBody.Text;
                if (AimApp.BuddyWin != null && !AimApp.BuddyWin.IsDisposed) AimApp.BuddyWin.SetStatus("away");
                Close();
            };
            btnClose.Click += delegate { Close(); };
            foot.Controls.AddRange(new Control[] { btnSave, btnSet, btnClose });
            ClientBody.Controls.Add(foot);

            RefreshList(0);
        }

        private void RefreshList(int selIdx)
        {
            _lst.Items.Clear();
            foreach (AwayPreset p in Store.State.AwayPresets) _lst.Items.Add(p.Title);
            if (selIdx >= 0 && selIdx < _lst.Items.Count) _lst.SelectedIndex = selIdx;
        }

        private void LoadSelected()
        {
            int idx = _lst.SelectedIndex;
            if (idx < 0 || idx >= Store.State.AwayPresets.Count) return;
            AwayPreset p = Store.State.AwayPresets[idx];
            _txtTitle.Text = p.Title;
            _txtBody.Text = p.Text;
            _chkAuto.Checked = p.AutoReply;
        }

        private void SaveCurrent()
        {
            int idx = _lst.SelectedIndex;
            if (idx < 0 || idx >= Store.State.AwayPresets.Count) return;
            AwayPreset p = Store.State.AwayPresets[idx];
            p.Title = string.IsNullOrWhiteSpace(_txtTitle.Text) ? "Away" : _txtTitle.Text.Trim();
            p.Text = _txtBody.Text;
            p.AutoReply = _chkAuto.Checked;
        }
    }

    /* ======================================================================
       #10 File Transfer Window
       ====================================================================== */
    public class FileTransferForm : XpForm
    {
        private ComboBox _cboBuddy, _cboPreset;
        private TextBox _txtLocalFile;
        private XpProgressBar _prog;
        private Label _lblActive, _lblStats;
        private ListBox _lstQueue;
        private Timer _tickTimer;
        private FileTransferItem _activeFt;

        public FileTransferForm(string preselectBuddy)
        {
            Text = "AIM Direct File Transfer";
            Size = new Size(468, 428);
            ShowMaximizeButton = false;
            AllowResize = false;
            TitleIconImage = RetroAssets.GetIconBitmap("files", 16);

            XpBanner banner = new XpBanner
            {
                BannerTitle = "AIM Direct File Transfer",
                BannerSubtitle = "Send real local files or classic 2006 MP3/ZIP packages",
                BannerIcon = RetroAssets.GetIconBitmap("files", 26)
            };
            ClientBody.Controls.Add(banner);

            Panel p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            p.Controls.Add(new Label { Text = "Recipient:", Location = new Point(12, 13), AutoSize = true });
            _cboBuddy = new ComboBox { Location = new Point(92, 10), Width = 345, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (Buddy b in Store.State.Buddies)
            {
                if (!b.Blocked) _cboBuddy.Items.Add(b.ScreenName);
            }
            if (!string.IsNullOrEmpty(preselectBuddy) && _cboBuddy.Items.Contains(preselectBuddy)) _cboBuddy.SelectedItem = preselectBuddy;
            else if (_cboBuddy.Items.Count > 0) _cboBuddy.SelectedIndex = 0;

            p.Controls.Add(new Label { Text = "Preset File:", Location = new Point(12, 41), AutoSize = true });
            _cboPreset = new ComboBox { Location = new Point(92, 38), Width = 345, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboPreset.Items.AddRange(new object[]
            {
                "LinkinPark_Numb.mp3 (3.40 MB)",
                "prom_photos_2006.zip (4.60 MB)",
                "Winamp5_Luna_Skin.wsz (448 KB)",
                "homework_essay_final.doc (142 KB)"
            });
            _cboPreset.SelectedIndex = 0;

            p.Controls.Add(new Label { Text = "Local File:", Location = new Point(12, 69), AutoSize = true });
            _txtLocalFile = new TextBox { Location = new Point(92, 66), Width = 258, ReadOnly = true, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            XpButton btnBrowse = new XpButton { Text = "Browse...", Location = new Point(356, 65), Size = new Size(81, 22) };
            btnBrowse.Click += delegate
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    if (ofd.ShowDialog(this) == DialogResult.OK) _txtLocalFile.Text = ofd.FileName;
                }
            };

            XpButton btnSend = new XpButton { Text = "Send File Now", IsPrimary = true, Location = new Point(312, 95), Size = new Size(125, 25) };
            btnSend.Click += StartOutgoingTransfer;

            _lblActive = new Label { Location = new Point(12, 128), Size = new Size(425, 18), Font = XpTheme.UiBold, ForeColor = XpTheme.Current.AimNavy, Text = "Ready" };
            _prog = new XpProgressBar { Location = new Point(12, 149), Size = new Size(425, 16) };
            _lblStats = new Label { Location = new Point(12, 169), Size = new Size(425, 16), Font = XpTheme.SmallFont, Text = "Idle" };

            p.Controls.Add(new Label { Text = "Transfer Queue & History (Double-click offering to Accept):", Location = new Point(12, 194), AutoSize = true, Font = XpTheme.UiBold });
            _lstQueue = new ListBox { Location = new Point(12, 214), Size = new Size(425, 108), BorderStyle = BorderStyle.FixedSingle };
            _lstQueue.DoubleClick += delegate
            {
                int idx = _lstQueue.SelectedIndex;
                if (idx >= 0 && idx < Store.State.FileTransfers.Count)
                {
                    FileTransferItem item = Store.State.FileTransfers[idx];
                    if (item.Status == "offering") BeginTransfer(item);
                }
            };

            p.Controls.AddRange(new Control[] { _cboBuddy, _cboPreset, _txtLocalFile, btnBrowse, btnSend, _lblActive, _prog, _lblStats, _lstQueue });
            ClientBody.Controls.Add(p);
            p.BringToFront();

            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 38, BackColor = XpTheme.Current.FaceDark };
            XpButton btnSim = new XpButton { Text = "Simulate Incoming File", Location = new Point(12, 7), Size = new Size(148, 24) };
            XpButton btnClose = new XpButton { Text = "Close", Location = new Point(366, 7), Size = new Size(80, 24) };
            btnSim.Click += delegate { TriggerIncomingOffer(); };
            btnClose.Click += delegate { Close(); };
            foot.Controls.AddRange(new Control[] { btnSim, btnClose });
            ClientBody.Controls.Add(foot);

            _tickTimer = new Timer { Interval = 180 };
            _tickTimer.Tick += OnTickTransfer;
            FormClosed += delegate { _tickTimer.Dispose(); };

            RefreshQueue();
        }

        private void StartOutgoingTransfer(object sender, EventArgs e)
        {
            string buddy = Convert.ToString(_cboBuddy.SelectedItem);
            if (string.IsNullOrEmpty(buddy)) return;
            string fname;
            long size;
            if (!string.IsNullOrEmpty(_txtLocalFile.Text) && File.Exists(_txtLocalFile.Text))
            {
                FileInfo fi = new FileInfo(_txtLocalFile.Text);
                fname = fi.Name;
                size = Math.Max(65536, fi.Length);
            }
            else
            {
                long[] sizes = new long[] { 3565158, 4823449, 458752, 145408 };
                int idx = Math.Max(0, _cboPreset.SelectedIndex);
                fname = Convert.ToString(_cboPreset.SelectedItem).Split(' ')[0];
                size = sizes[idx];
            }

            FileTransferItem ft = new FileTransferItem
            {
                Id = "ft_" + DateTime.Now.Ticks,
                Direction = "outgoing",
                Buddy = buddy,
                FileName = fname,
                LocalSourcePath = _txtLocalFile.Text,
                SizeBytes = size,
                TransferredBytes = 0,
                SpeedKbps = 54.2,
                Status = "transferring",
                StartedAt = Store.NowTimeStr()
            };
            Store.State.FileTransfers.Insert(0, ft);
            BeginTransfer(ft);
        }

        public void TriggerIncomingOffer()
        {
            Buddy online = Store.State.Buddies.FirstOrDefault(b => b.Status == "online") ?? Store.State.Buddies[0];
            FileTransferItem offer = new FileTransferItem
            {
                Id = "ft_in_" + DateTime.Now.Ticks,
                Direction = "incoming",
                Buddy = online.ScreenName,
                FileName = "summer_mixtape_2006.mp3",
                SizeBytes = 2949120,
                TransferredBytes = 0,
                SpeedKbps = 49.8,
                Status = "offering",
                StartedAt = Store.NowTimeStr()
            };
            Store.State.FileTransfers.Insert(0, offer);
            Store.Save();
            SoundEngine.Play("ring");
            RefreshQueue();
            if (XpMessageBox.Show(this, string.Format("{0} wants to send you:\n\"{1}\" (2.81 MB)\n\nAccept file transfer?", online.ScreenName, offer.FileName), "Incoming File Offer", true) == DialogResult.Yes)
            {
                BeginTransfer(offer);
            }
            else
            {
                offer.Status = "cancelled";
                Store.Save();
                RefreshQueue();
            }
        }

        private void BeginTransfer(FileTransferItem ft)
        {
            _activeFt = ft;
            ft.Status = "transferring";
            _lblActive.Text = string.Format("{0} {1}: {2}", ft.Direction == "incoming" ? "Receiving from" : "Sending to", ft.Buddy, ft.FileName);
            _tickTimer.Start();
            RefreshQueue();
        }

        private void OnTickTransfer(object sender, EventArgs e)
        {
            if (_activeFt == null || _activeFt.Status != "transferring") { _tickTimer.Stop(); return; }
            long chunk = Math.Max(95000, _activeFt.SizeBytes / 16);
            _activeFt.TransferredBytes = Math.Min(_activeFt.SizeBytes, _activeFt.TransferredBytes + chunk);
            _activeFt.SpeedKbps = Math.Round(44.0 + new Random().NextDouble() * 24.0, 1);
            int pct = (int)Math.Round((_activeFt.TransferredBytes * 100.0) / Math.Max(1, _activeFt.SizeBytes));
            _prog.Value = pct;
            _lblStats.Text = string.Format("{0} KB / {1} KB  —  {2} KB/sec  ({3}%)",
                _activeFt.TransferredBytes / 1024, _activeFt.SizeBytes / 1024, _activeFt.SpeedKbps, pct);

            if (_activeFt.TransferredBytes >= _activeFt.SizeBytes)
            {
                _activeFt.Status = "completed";
                _tickTimer.Stop();
                Store.Save();
                SoundEngine.Play("fileDone");
                RefreshQueue();
                AimApp.ShowToast("File Transfer Complete", _activeFt.FileName, "AIM Icon.gif", null);
            }
        }

        private void RefreshQueue()
        {
            _lstQueue.Items.Clear();
            foreach (FileTransferItem ft in Store.State.FileTransfers)
            {
                _lstQueue.Items.Add(string.Format("{0} {1} ({2} KB) — {3} [{4}]",
                    ft.Direction == "incoming" ? "↓" : "↑", ft.FileName, Math.Max(1, ft.SizeBytes / 1024), ft.Buddy, ft.Status.ToUpper()));
            }
        }
    }

    /* ======================================================================
       #11 Chat History Viewer
       ====================================================================== */
    public class HistoryForm : XpForm
    {
        private ListBox _lstConvos;
        private TextBox _txtSearch;
        private RichTextBox _rtbLog;

        public HistoryForm(string initialSN)
        {
            Text = "AIM Chat History Viewer";
            Size = new Size(590, 445);
            TitleIconImage = RetroAssets.GetIconBitmap("history", 16);

            XpBanner banner = new XpBanner
            {
                BannerTitle = "Message History & Logs",
                BannerSubtitle = "Browse, search, and export archived AIM conversations",
                BannerIcon = RetroAssets.GetIconBitmap("history", 26)
            };
            ClientBody.Controls.Add(banner);

            Panel searchBar = new Panel { Dock = DockStyle.Top, Height = 32, Padding = new Padding(8, 5, 8, 3) };
            searchBar.Controls.Add(new Label { Text = "Search:", Location = new Point(8, 9), AutoSize = true });
            _txtSearch = new TextBox { Location = new Point(56, 6), Width = 320, BorderStyle = BorderStyle.FixedSingle };
            _txtSearch.TextChanged += delegate { RenderSelectedLog(); };
            XpButton btnExp = new XpButton { Text = "Export .TXT", Location = new Point(386, 5), Size = new Size(88, 22), Font = XpTheme.SmallFont };
            XpButton btnClr = new XpButton { Text = "Clear Log", Location = new Point(480, 5), Size = new Size(84, 22), Font = XpTheme.SmallFont };
            btnExp.Click += ExportSelectedLog;
            btnClr.Click += ClearSelectedLog;
            searchBar.Controls.AddRange(new Control[] { _txtSearch, btnExp, btnClr });

            Panel main = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 6) };
            _lstConvos = new ListBox { Dock = DockStyle.Left, Width = 165, BorderStyle = BorderStyle.FixedSingle };
            _lstConvos.SelectedIndexChanged += delegate { RenderSelectedLog(); };
            Panel padRtb = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 0, 0) };
            _rtbLog = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, DetectUrls = true };
            padRtb.Controls.Add(_rtbLog);
            main.Controls.Add(padRtb);
            main.Controls.Add(_lstConvos);

            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = XpTheme.Current.FaceDark };
            XpButton btnIm = new XpButton { Text = "Open IM Window", Location = new Point(368, 6), Size = new Size(116, 24) };
            XpButton btnClose = new XpButton { Text = "Close", Location = new Point(490, 6), Size = new Size(80, 24) };
            btnIm.Click += delegate { string sn = _lstConvos.SelectedItem as string; if (!string.IsNullOrEmpty(sn)) AimApp.OpenPrivateChat(sn); };
            btnClose.Click += delegate { Close(); };
            foot.Controls.AddRange(new Control[] { btnIm, btnClose });

            ClientBody.Controls.Add(main);
            ClientBody.Controls.Add(searchBar);
            ClientBody.Controls.Add(foot);
            main.BringToFront();

            LoadConvos(initialSN);
            Load += delegate { _rtbLog.BackColor = Color.FromArgb(255, 255, 254); _rtbLog.BackColor = Color.White; RenderSelectedLog(); };
        }

        private void LoadConvos(string selectSN)
        {
            _lstConvos.Items.Clear();
            foreach (string k in Store.State.ChatHistory.Keys) _lstConvos.Items.Add(k);
            if (!string.IsNullOrEmpty(selectSN) && _lstConvos.Items.Contains(selectSN)) _lstConvos.SelectedItem = selectSN;
            else if (_lstConvos.Items.Count > 0) _lstConvos.SelectedIndex = 0;
        }

        private void RenderSelectedLog()
        {
            _rtbLog.Clear();
            string sn = _lstConvos.SelectedItem as string;
            if (string.IsNullOrEmpty(sn)) return;
            string q = (_txtSearch.Text ?? "").Trim().ToLowerInvariant();
            string me = Store.GetActiveAccount().ScreenName;
            List<ChatMessage> msgs;
            if (!Store.State.ChatHistory.TryGetValue(sn, out msgs)) return;

            foreach (ChatMessage m in msgs)
            {
                if (q != "" && !(m.Text ?? "").ToLowerInvariant().Contains(q) && !(m.Sender ?? "").ToLowerInvariant().Contains(q)) continue;
                RetroAssets.AppendChatMessageToRtb(_rtbLog, m, me, true, Store.State.Settings.EnableEmoticons);
            }
        }

        private void ExportSelectedLog(object sender, EventArgs e)
        {
            string sn = _lstConvos.SelectedItem as string;
            if (string.IsNullOrEmpty(sn) || !Store.State.ChatHistory.ContainsKey(sn)) return;
            using (SaveFileDialog sfd = new SaveFileDialog { FileName = "AIM_Log_" + sn + ".txt", Filter = "Text Files (*.txt)|*.txt" })
            {
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    File.WriteAllLines(sfd.FileName, Store.State.ChatHistory[sn].Select(m => string.Format("[{0} {1}] {2}: {3}", m.Date, m.Time, m.Sender, m.Text)));
                    XpMessageBox.Show(this, "Exported log to:\n" + sfd.FileName, "Export Complete", false);
                }
            }
        }

        private void ClearSelectedLog(object sender, EventArgs e)
        {
            string sn = _lstConvos.SelectedItem as string;
            if (string.IsNullOrEmpty(sn)) return;
            if (XpMessageBox.Show(this, "Delete all logged messages with \"" + sn + "\"?", "Clear History", true) == DialogResult.Yes)
            {
                Store.State.ChatHistory.Remove(sn);
                Store.Notify("history");
                LoadConvos(null);
            }
        }
    }

    /* ======================================================================
       #16 Avatar Gallery (42 Real Classic AIM Animated GIFs + Custom Upload)
       ====================================================================== */
    public class AvatarGalleryForm : XpForm
    {
        private string _chosen;
        private PictureBox _pbPreview;
        private Label _lblName;

        public AvatarGalleryForm()
        {
            Account me = Store.GetActiveAccount();
            _chosen = me.Avatar;

            Text = "Buddy Icons — Classic AIM Avatar Gallery";
            Size = new Size(495, 445);
            ShowMaximizeButton = false;
            AllowResize = false;
            TitleIconImage = RetroAssets.GetIconBitmap("profile", 16);

            XpBanner banner = new XpBanner
            {
                BannerTitle = "Classic 2000s AIM Buddy Icons",
                BannerSubtitle = "Real vintage animated & pixel GIF avatars + custom image upload",
                BannerIcon = RetroAssets.GetIconBitmap("profile", 26)
            };
            ClientBody.Controls.Add(banner);

            Panel topPreview = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = XpTheme.Current.SubBarTop };
            _pbPreview = new PictureBox { Location = new Point(12, 8), Size = new Size(48, 48), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            _lblName = new Label { Location = new Point(70, 22), Size = new Size(250, 20), Font = XpTheme.UiBold, ForeColor = XpTheme.Current.AimNavy };
            XpButton btnUpload = new XpButton { Text = "Upload Custom...", Location = new Point(342, 19), Size = new Size(124, 24) };
            btnUpload.Click += delegate
            {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Images|*.gif;*.png;*.jpg;*.jpeg;*.bmp" })
                {
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        _chosen = ofd.FileName;
                        UpdatePreview();
                    }
                }
            };
            topPreview.Controls.AddRange(new Control[] { _pbPreview, _lblName, btnUpload });

            FlowLayoutPanel grid = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White, Padding = new Padding(8) };
            foreach (string avName in RetroAssets.GetAvatarNames())
            {
                string cap = avName;
                Panel card = new Panel { Size = new Size(98, 78), Margin = new Padding(4), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(248, 250, 253), Cursor = Cursors.Hand };
                PictureBox pb = new PictureBox { Location = new Point(25, 6), Size = new Size(48, 48), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };
                RetroAssets.SetPictureBoxAvatar(pb, cap);
                Label lbl = new Label { Location = new Point(2, 57), Size = new Size(92, 16), Text = Path.GetFileNameWithoutExtension(cap), Font = XpTheme.SmallFont, TextAlign = ContentAlignment.MiddleCenter, AutoEllipsis = true };
                EventHandler pick = delegate { _chosen = cap; UpdatePreview(); };
                card.Click += pick; pb.Click += pick; lbl.Click += pick;
                card.Controls.Add(pb); card.Controls.Add(lbl);
                grid.Controls.Add(card);
            }

            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 38, BackColor = XpTheme.Current.FaceDark };
            XpButton btnApply = new XpButton { Text = "Set Buddy Icon", IsPrimary = true, Location = new Point(268, 7), Size = new Size(110, 24) };
            XpButton btnCancel = new XpButton { Text = "Cancel", Location = new Point(384, 7), Size = new Size(80, 24) };
            btnApply.Click += delegate
            {
                me.Avatar = _chosen;
                SoundEngine.Play("youveGotPictures");
                Store.Notify("profile");
                Close();
            };
            btnCancel.Click += delegate { Close(); };
            foot.Controls.AddRange(new Control[] { btnApply, btnCancel });

            ClientBody.Controls.Add(grid);
            ClientBody.Controls.Add(topPreview);
            ClientBody.Controls.Add(foot);
            grid.BringToFront();

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            RetroAssets.SetPictureBoxAvatar(_pbPreview, _chosen);
            _lblName.Text = "Selected: " + Path.GetFileName(_chosen ?? "AIM Icon.gif");
        }
    }

    /* ======================================================================
       #13 Settings Panel + #17 Theme Engine + AI Companion Configuration
       ====================================================================== */
    public class SettingsForm : XpForm
    {
        private CheckBox _ts, _emo, _typ, _pop, _showBanners, _mute, _sDoorOn, _sDoorOff, _sImRx, _sImTx, _sFile, _sAlert, _sWelcome, _sGoodbye, _sMail, _sRing, _sChat, _muteBusy, _backdrop, _chkLanDisc, _chkGlobalCloud, _chkAutoTrans, _chkAutoUpd;
        private ComboBox _font, _theme, _cboLang, _cboTransTarget;
        private TextBox _host, _port, _p2pPort, _relayHost, _relayPort, _txtGlobalRoom;
        private readonly List<string> _themeKeysList = new List<string>();
        private ApiKeysPanel _apiKeysPanel;

        public SettingsForm(string initialTab)
        {
            Text = I18n.T("AIM Preferences, Network & Theme Engine", "Настройки, сеть и темы AIM");
            Size = new Size(602, 492);
            ShowMaximizeButton = false;
            AllowResize = false;
            TitleIconImage = RetroAssets.GetIconBitmap("settings", 16);

            AppSettings s = Store.State.Settings;
            TabControl tabs = new TabControl { Dock = DockStyle.Fill };

            TabPage tGen = new TabPage(I18n.T("IM & Chat", "Чат"));
            TabPage tSnd = new TabPage(I18n.T("Sounds", "Звуки"));
            TabPage tConn = new TabPage(I18n.T("Connection", "Сеть P2P"));
            TabPage tAi = new TabPage(I18n.T("API Keys", "API Ключи"));
            TabPage tLang = new TabPage(I18n.T("Language", "Язык"));
            TabPage tUpd = new TabPage(I18n.T("Updates", "Обновления"));
            TabPage tTheme = new TabPage(I18n.T("Theme", "Тема XP"));
            foreach (TabPage tp in new[] { tGen, tSnd, tConn, tAi, tLang, tUpd, tTheme }) tp.BackColor = XpTheme.Current.Face;
            tabs.TabPages.AddRange(new[] { tGen, tSnd, tConn, tAi, tLang, tUpd, tTheme });

            // General
            _ts = new CheckBox { Text = "Show timestamps on messages", Location = new Point(16, 18), Checked = s.ShowTimestamps, AutoSize = true };
            _emo = new CheckBox { Text = "Display classic 2000s graphic emoticons", Location = new Point(16, 44), Checked = s.EnableEmoticons, AutoSize = true };
            _typ = new CheckBox { Text = "Show \"is typing...\" status indicator", Location = new Point(16, 70), Checked = s.ShowTypingIndicator, AutoSize = true };
            _pop = new CheckBox { Text = "Automatically open IM window on incoming message", Location = new Point(16, 96), Checked = s.AutoPopupIM, AutoSize = true };
            _muteBusy = new CheckBox { Text = "Suppress sounds & popups when status is Busy", Location = new Point(16, 122), Checked = s.MuteWhenBusy, AutoSize = true };
            _showBanners = new CheckBox { Text = "Show classic 2005-2008 AIM promo banners in Buddy List & IM", Location = new Point(16, 148), Checked = s.ShowBanners, AutoSize = true };
            tGen.Controls.Add(new Label { Text = "Default Font:", Location = new Point(16, 186), AutoSize = true });
            _font = new ComboBox { Location = new Point(108, 182), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            _font.Items.AddRange(new object[] { "Tahoma", "Comic Sans MS", "Arial", "Times New Roman", "Courier New" });
            _font.SelectedItem = s.DefaultFont ?? "Tahoma";
            tGen.Controls.AddRange(new Control[] { _ts, _emo, _typ, _pop, _muteBusy, _showBanners, _font });

            // Sounds
            tSnd.AutoScroll = true;
            _mute = new CheckBox { Text = "Mute all AIM sound notifications", Location = new Point(16, 12), Checked = s.SoundMuted, AutoSize = true, Font = XpTheme.UiBold };
            _sWelcome = AddSoundRow(tSnd, 40, "Sign On — \"Welcome!\"", s.SoundWelcome, "welcome");
            _sGoodbye = AddSoundRow(tSnd, 66, "Sign Off — \"Goodbye!\"", s.SoundGoodbye, "goodbye");
            _sMail = AddSoundRow(tSnd, 92, "AIM Mail — \"You've got mail!\"", s.SoundMail, "youveGotMail");
            _sDoorOn = AddSoundRow(tSnd, 118, "Buddy Signs On (Door Open)", s.SoundDoorOpen, "doorOpen");
            _sDoorOff = AddSoundRow(tSnd, 144, "Buddy Signs Off (Door Close)", s.SoundDoorClose, "doorClose");
            _sImRx = AddSoundRow(tSnd, 170, "IM Received (Bubble Pop)", s.SoundImReceive, "imReceive");
            _sImTx = AddSoundRow(tSnd, 196, "Instant Message Sent", s.SoundImSend, "imSend");
            _sRing = AddSoundRow(tSnd, 222, "Audio Ring / Incoming File", s.SoundRing, "ring");
            _sFile = AddSoundRow(tSnd, 248, "File Transfer — \"File's done!\"", s.SoundFileDone, "fileDone");
            _sAlert = AddSoundRow(tSnd, 274, "Buddy Alert / Warning", s.SoundAlert, "buddyAlert");
            _sChat = AddSoundRow(tSnd, 300, "Group Chat Join / Leave", s.SoundChat, "chatJoin");
            AddSoundTestOnlyRow(tSnd, 326, "New IM Window Chime", "imNew");
            AddSoundTestOnlyRow(tSnd, 352, "Group Chat Leave Swoosh", "chatLeave");
            AddSoundTestOnlyRow(tSnd, 378, "Status / UI Click", "talkClick");
            AddSoundTestOnlyRow(tSnd, 404, "Pictures — \"You've got pictures!\"", "youveGotPictures");
            AddSoundTestOnlyRow(tSnd, 430, "Voicemail — \"You've got voicemail!\"", "youveGotVoicemail");
            AddSoundTestOnlyRow(tSnd, 456, "AIM 4-Note Xylophone Chime", "buddyDing");
            AddSoundTestOnlyRow(tSnd, 482, "Connected Arpeggio Chime", "connectedChime");
            AddSoundTestOnlyRow(tSnd, 508, "56k Dial-Up Modem Handshake", "dialupModem");
            tSnd.Controls.Add(_mute);

            // Connection & Real Friends P2P / Relay
            tConn.Controls.Add(new Label { Text = "OSCAR Server:", Location = new Point(16, 18), AutoSize = true });
            _host = new TextBox { Location = new Point(128, 15), Width = 220, Text = s.ServerHost, BorderStyle = BorderStyle.FixedSingle };
            tConn.Controls.Add(new Label { Text = "Port:", Location = new Point(360, 18), AutoSize = true });
            _port = new TextBox { Location = new Point(400, 15), Width = 70, Text = s.ServerPort, BorderStyle = BorderStyle.FixedSingle };

            GroupBox grpP2p = new GroupBox { Text = "Real Friends P2P, Global Cloud && AIM Relay Server", Location = new Point(14, 46), Size = new Size(544, 294), Font = XpTheme.UiBold };
            grpP2p.Controls.Add(new Label { Text = "Local P2P TCP Port:", Location = new Point(14, 24), AutoSize = true, Font = XpTheme.UiFont });
            _p2pPort = new TextBox { Location = new Point(148, 21), Width = 76, Text = s.P2pListenPort.ToString(), BorderStyle = BorderStyle.FixedSingle, Font = XpTheme.UiFont };
            _chkLanDisc = new CheckBox { Text = "Auto-discover real AIM users on LAN / VPN (UDP 5191)", Location = new Point(14, 50), AutoSize = true, Checked = s.AutoDiscoverLan, Font = XpTheme.UiFont };

            _chkGlobalCloud = new CheckBox
            {
                Text = "Enable Global Internet Friends Cloud (ntfy.sh — add friends worldwide without IP)",
                Location = new Point(14, 76),
                AutoSize = true,
                Checked = s.GlobalCloudEnabled,
                Font = XpTheme.UiBold,
                ForeColor = Color.FromArgb(0, 96, 72)
            };
            grpP2p.Controls.Add(new Label { Text = "Global Room Code:", Location = new Point(14, 104), AutoSize = true, Font = XpTheme.UiFont });
            _txtGlobalRoom = new TextBox { Location = new Point(148, 101), Width = 226, Text = s.GlobalCloudRoom ?? "AIM-Global-2006", BorderStyle = BorderStyle.FixedSingle, Font = XpTheme.UiFont };

            grpP2p.Controls.Add(new Label { Text = "Shared Relay Host:", Location = new Point(14, 134), AutoSize = true, Font = XpTheme.UiFont });
            _relayHost = new TextBox { Location = new Point(148, 131), Width = 226, Text = s.RelayServerHost ?? "", BorderStyle = BorderStyle.FixedSingle, Font = XpTheme.UiFont };
            grpP2p.Controls.Add(new Label { Text = "Relay Port:", Location = new Point(384, 134), AutoSize = true, Font = XpTheme.UiFont });
            _relayPort = new TextBox { Location = new Point(458, 131), Width = 68, Text = s.RelayServerPort.ToString(), BorderStyle = BorderStyle.FixedSingle, Font = XpTheme.UiFont };

            Label lblInvite = new Label
            {
                Text = "My Invite Code: " + NetworkEngine.GetMyInviteCode(),
                Location = new Point(14, 164),
                Size = new Size(512, 18),
                Font = XpTheme.UiBold,
                ForeColor = XpTheme.Current.AimNavy
            };

            Label lblRelayState = new Label
            {
                Location = new Point(14, 228),
                Size = new Size(512, 54),
                Font = XpTheme.SmallFont,
                ForeColor = Color.FromArgb(65, 80, 105),
                Text = NetworkEngine.IsRelayHosting
                    ? string.Format("Built-in AIM Relay Server is RUNNING on {0}:{1}! Friends can enter this Host & Port above.", NetworkEngine.LocalIpAddress, NetworkEngine.ActiveRelayHostPort)
                    : "Global Cloud lets friends add each other anywhere on Earth by Screen Name using the same Room Code (no port forwarding needed)."
            };

            XpButton btnHostRelay = new XpButton
            {
                Text = NetworkEngine.IsRelayHosting ? "Stop AIM Relay Server" : "Host Built-In AIM Relay Server (Port 5190)",
                IsPrimary = true,
                Location = new Point(14, 192),
                Size = new Size(276, 26),
                Font = XpTheme.SmallFont
            };
            btnHostRelay.Click += delegate
            {
                if (NetworkEngine.IsRelayHosting)
                {
                    NetworkEngine.StopRelayServer();
                    btnHostRelay.Text = "Host Built-In AIM Relay Server (Port 5190)";
                    lblRelayState.Text = "Relay Server stopped.";
                }
                else
                {
                    int rp;
                    if (!int.TryParse(_relayPort.Text.Trim(), out rp) || rp <= 0) rp = 5190;
                    string err;
                    if (NetworkEngine.StartRelayServer(rp, out err))
                    {
                        if (string.IsNullOrWhiteSpace(_relayHost.Text)) _relayHost.Text = "127.0.0.1";
                        btnHostRelay.Text = "Stop AIM Relay Server";
                        lblRelayState.Text = string.Format("RUNNING! Friends can connect to {0}:{1} (add to Relay Host above).", NetworkEngine.LocalIpAddress, rp);
                        SoundEngine.Play("connectedChime");
                    }
                    else
                    {
                        XpMessageBox.Show(this, "Could not start Relay Server: " + err, "Relay Server", false);
                    }
                }
            };

            grpP2p.Controls.AddRange(new Control[] { _p2pPort, _chkLanDisc, _chkGlobalCloud, _txtGlobalRoom, _relayHost, _relayPort, lblInvite, btnHostRelay, lblRelayState });
            tConn.Controls.AddRange(new Control[] { _host, _port, grpP2p });

            // API Keys tab
            _apiKeysPanel = new ApiKeysPanel { Dock = DockStyle.Fill, BackColor = XpTheme.Current.Face };
            tAi.Controls.Add(_apiKeysPanel);

            // Language & Chat Translator tab
            tLang.Controls.Add(new Label { Text = "AIM Interface Language:", Location = new Point(16, 22), AutoSize = true, Font = XpTheme.UiBold });
            _cboLang = new ComboBox { Location = new Point(190, 19), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboLang.Items.AddRange(new object[] { "English (Default 2005 AIM)", "Русский (Russian)" });
            _cboLang.SelectedIndex = string.Equals(s.Language, "ru", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            _chkAutoTrans = new CheckBox
            {
                Text = "Automatically translate incoming chat messages",
                Location = new Point(16, 62),
                Checked = s.AutoTranslateChat,
                AutoSize = true,
                Font = XpTheme.UiBold
            };
            tLang.Controls.Add(new Label { Text = "Auto-Translate Target:", Location = new Point(16, 98), AutoSize = true });
            _cboTransTarget = new ComboBox { Location = new Point(190, 95), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboTransTarget.Items.AddRange(new object[] { "English (en)", "Russian / Русский (ru)" });
            _cboTransTarget.SelectedIndex = string.Equals(s.TranslateTargetLang, "ru", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            Label lblTransNote = new Label
            {
                Location = new Point(16, 136),
                Size = new Size(520, 64),
                Font = XpTheme.SmallFont,
                ForeColor = Color.FromArgb(60, 80, 110),
                Text = "Chat Translator works via the 'Translate' button inside any IM window:\r\n" +
                       "1) Uses your active AI API Key when configured;\r\n" +
                       "2) Falls back to Online Translator when online, or Offline EN<->RU Dictionary when offline."
            };
            tLang.Controls.AddRange(new Control[] { _cboLang, _chkAutoTrans, _cboTransTarget, lblTransNote });

            // Updates & Support Center quick hub tab
            _chkAutoUpd = new CheckBox { Text = "Automatically check for AIM updates on Sign On", Location = new Point(16, 20), Checked = s.AutoCheckUpdates, AutoSize = true, Font = XpTheme.UiBold };
            XpButton btnOpenUpd = new XpButton { Text = "Open AIM Update Center...", IsPrimary = true, Location = new Point(16, 56), Size = new Size(230, 28), Image = RetroAssets.GetIconBitmap("update", 16), TextImageRelation = TextImageRelation.ImageBeforeText };
            btnOpenUpd.Click += delegate { new UpdateCenterForm().ShowDialog(this); };
            XpButton btnOpenSup = new XpButton { Text = "Open Support & Diagnostics Center...", Location = new Point(16, 94), Size = new Size(230, 28), Image = RetroAssets.GetIconBitmap("support", 16), TextImageRelation = TextImageRelation.ImageBeforeText };
            btnOpenSup.Click += delegate { new SupportForm().ShowDialog(this); };
            Label lblVerFoot = new Label
            {
                Text = "Installed Build: AIM Classic v" + UpdateCenterForm.CURRENT_VERSION + "\r\nData File: " + Store.DataFilePath,
                Location = new Point(16, 140),
                Size = new Size(520, 40),
                Font = XpTheme.SmallFont,
                ForeColor = Color.FromArgb(70, 85, 105)
            };
            tUpd.Controls.AddRange(new Control[] { _chkAutoUpd, btnOpenUpd, btnOpenSup, lblVerFoot });

            // Theme Engine
            tTheme.Controls.Add(new Label { Text = "Active Theme:", Location = new Point(16, 22), AutoSize = true, Font = XpTheme.UiBold });
            _theme = new ComboBox { Location = new Point(120, 19), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            _themeKeysList.Clear();
            string[] builtInKeys = new[] { "LunaBlue", "FrutigerAero", "Aim6Ice", "XpSilver", "XpRoyale", "XpOlive", "Y2KNeon" };
            string[] builtInLabels = new[]
            {
                "LunaBlue — Classic Windows XP Blue (2005)",
                "FrutigerAero — 2007 Glossy Aqua Glass + Wallpaper",
                "Aim6Ice — AIM 6.0 Triton Ice Blue",
                "XpSilver — Windows XP Silver + AIM Blue",
                "XpRoyale — Windows XP Media Center Energy Blue",
                "XpOlive — Windows XP Homestead Olive Green",
                "Y2KNeon — Cyber Purple / Cyan Gloss"
            };
            int selThemeIdx = 0;
            for (int ti = 0; ti < builtInKeys.Length; ti++)
            {
                _themeKeysList.Add(builtInKeys[ti]);
                _theme.Items.Add(builtInLabels[ti]);
                if (string.Equals(s.Theme, builtInKeys[ti], StringComparison.OrdinalIgnoreCase)) selThemeIdx = _themeKeysList.Count - 1;
            }
            if (Store.State.CustomThemes != null)
            {
                foreach (CustomThemeDef ct in Store.State.CustomThemes)
                {
                    string key = "Custom:" + ct.Id;
                    _themeKeysList.Add(key);
                    _theme.Items.Add("[Workshop] " + ct.Name + " (by " + (ct.Author ?? "User") + ")");
                    if (string.Equals(s.Theme, key, StringComparison.OrdinalIgnoreCase)) selThemeIdx = _themeKeysList.Count - 1;
                }
            }
            _theme.SelectedIndex = selThemeIdx;

            _backdrop = new CheckBox { Text = "Show classic 1024×768 Desktop Backdrop window (XP Bliss / Frutiger Aero)", Location = new Point(16, 58), Checked = s.ShowXpBackdrop, AutoSize = true };
            XpButton btnOpenWorkshop = new XpButton
            {
                Text = I18n.T("Open AIM Workshop (Browse Skins, Create Themes & Icons)...", "Открыть AIM Мастерскую (Темы, Конструктор и Аватары)..."),
                IsPrimary = true,
                Location = new Point(16, 98),
                Size = new Size(424, 30),
                Image = RetroAssets.GetIconBitmap("workshop", 16),
                TextImageRelation = TextImageRelation.ImageBeforeText
            };
            btnOpenWorkshop.Click += delegate
            {
                new WorkshopForm().ShowDialog(this);
                Close();
            };
            tTheme.Controls.AddRange(new Control[] { _theme, _backdrop, btnOpenWorkshop });

            if (initialTab == "connection") tabs.SelectedTab = tConn;
            else if (initialTab == "ai" || initialTab == "apikeys") tabs.SelectedTab = tAi;
            else if (initialTab == "language") tabs.SelectedTab = tLang;
            else if (initialTab == "updates" || initialTab == "support") tabs.SelectedTab = tUpd;
            else if (initialTab == "theme") tabs.SelectedTab = tTheme;

            Panel pad = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            pad.Controls.Add(tabs);
            ClientBody.Controls.Add(pad);
            pad.BringToFront();

            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 38, BackColor = XpTheme.Current.FaceDark };
            XpButton btnReset = new XpButton { Text = "Reset Data", Location = new Point(10, 7), Size = new Size(88, 24) };
            XpButton btnOk = new XpButton { Text = "OK", IsPrimary = true, Location = new Point(338, 7), Size = new Size(78, 24) };
            XpButton btnCancel = new XpButton { Text = "Cancel", Location = new Point(422, 7), Size = new Size(78, 24) };
            XpButton btnApply = new XpButton { Text = "Apply", Location = new Point(506, 7), Size = new Size(78, 24) };

            Action save = delegate
            {
                string prevLang = s.Language ?? "en";
                s.ShowTimestamps = _ts.Checked;
                s.EnableEmoticons = _emo.Checked;
                s.ShowTypingIndicator = _typ.Checked;
                s.AutoPopupIM = _pop.Checked;
                s.MuteWhenBusy = _muteBusy.Checked;
                s.ShowBanners = _showBanners.Checked;
                s.DefaultFont = Convert.ToString(_font.SelectedItem);
                s.SoundMuted = _mute.Checked;
                s.SoundDoorOpen = _sDoorOn.Checked;
                s.SoundDoorClose = _sDoorOff.Checked;
                s.SoundImReceive = _sImRx.Checked;
                s.SoundImSend = _sImTx.Checked;
                s.SoundFileDone = _sFile.Checked;
                s.SoundAlert = _sAlert.Checked;
                s.SoundWelcome = _sWelcome.Checked;
                s.SoundGoodbye = _sGoodbye.Checked;
                s.SoundMail = _sMail.Checked;
                s.SoundRing = _sRing.Checked;
                s.SoundChat = _sChat.Checked;
                s.ServerHost = _host.Text.Trim();
                s.ServerPort = _port.Text.Trim();
                int parsedP2p;
                if (int.TryParse(_p2pPort.Text.Trim(), out parsedP2p) && parsedP2p > 0) s.P2pListenPort = parsedP2p;
                s.AutoDiscoverLan = _chkLanDisc.Checked;
                s.GlobalCloudEnabled = _chkGlobalCloud.Checked;
                string gRoom = _txtGlobalRoom.Text.Trim();
                s.GlobalCloudRoom = string.IsNullOrEmpty(gRoom) ? "AIM-Global-2006" : gRoom;
                s.RelayServerHost = _relayHost.Text.Trim();
                int parsedRp;
                if (int.TryParse(_relayPort.Text.Trim(), out parsedRp) && parsedRp > 0) s.RelayServerPort = parsedRp;
                _apiKeysPanel.ApplyCurrentChanges();
                s.Language = _cboLang.SelectedIndex == 1 ? "ru" : "en";
                s.AutoTranslateChat = _chkAutoTrans.Checked;
                s.TranslateTargetLang = _cboTransTarget.SelectedIndex == 1 ? "ru" : "en";
                s.AutoCheckUpdates = _chkAutoUpd.Checked;
                if (_theme.SelectedIndex >= 0 && _theme.SelectedIndex < _themeKeysList.Count)
                    s.Theme = _themeKeysList[_theme.SelectedIndex];
                s.ShowXpBackdrop = _backdrop.Checked;
                Store.Notify("settings");
                if (s.GlobalCloudEnabled) NetworkEngine.SyncGlobalCloudNow();
                AimApp.ApplyThemeToAllForms();
                if (!string.Equals(prevLang, s.Language, StringComparison.OrdinalIgnoreCase) && AimApp.BuddyWin != null && !AimApp.BuddyWin.IsDisposed)
                {
                    AimApp.BuddyWin.CloseReal();
                    AimApp.OpenBuddyList();
                }
            };

            btnApply.Click += delegate { save(); };
            btnOk.Click += delegate { save(); Close(); };
            btnCancel.Click += delegate { Close(); };
            btnReset.Click += delegate
            {
                if (XpMessageBox.Show(this, "Reset all accounts, buddies, and chat logs to defaults?", "Reset AIM", true) == DialogResult.Yes)
                {
                    Store.ResetToFactory();
                    AimApp.SignOff();
                    Close();
                }
            };
            foot.Controls.AddRange(new Control[] { btnReset, btnOk, btnCancel, btnApply });
            ClientBody.Controls.Add(foot);
        }

        private CheckBox AddSoundRow(TabPage page, int y, string label, bool checkedVal, string evKey)
        {
            CheckBox chk = new CheckBox { Text = label, Location = new Point(16, y + 2), Checked = checkedVal, AutoSize = true };
            XpButton test = new XpButton { Text = "▶ Test", Location = new Point(358, y), Size = new Size(76, 21), Font = XpTheme.SmallFont };
            test.Click += delegate { SoundEngine.Play(evKey, true); };
            page.Controls.Add(chk);
            page.Controls.Add(test);
            return chk;
        }

        private void AddSoundTestOnlyRow(TabPage page, int y, string label, string evKey)
        {
            Label lbl = new Label { Text = "  • " + label, Location = new Point(16, y + 3), AutoSize = true, ForeColor = Color.FromArgb(65, 80, 105) };
            XpButton test = new XpButton { Text = "▶ Test", Location = new Point(358, y), Size = new Size(76, 21), Font = XpTheme.SmallFont };
            test.Click += delegate { SoundEngine.Play(evKey, true); };
            page.Controls.Add(lbl);
            page.Controls.Add(test);
        }
    }

    /* ======================================================================
       #20 Help & About Window + Live Event Simulator
       ====================================================================== */
    public class HelpAboutForm : XpForm
    {
        public HelpAboutForm(string initialTab)
        {
            Text = "Help & About AOL Instant Messenger";
            Size = new Size(478, 405);
            ShowMaximizeButton = false;
            AllowResize = false;
            TitleIconImage = RetroAssets.GetIconBitmap("help", 16);

            TabControl tabs = new TabControl { Dock = DockStyle.Fill };
            TabPage tabAbout = new TabPage("About AIM");
            TabPage tabHelp = new TabPage("Help Topics");
            tabAbout.BackColor = Color.FromArgb(252, 252, 254);
            tabHelp.BackColor = Color.FromArgb(252, 252, 254);
            tabs.TabPages.AddRange(new[] { tabAbout, tabHelp });

            XpBanner splash = new XpBanner
            {
                Height = 64,
                BannerTitle = "AOL® Instant Messenger™ v5.9.3861",
                BannerSubtitle = "Windows XP Classic Edition (2005–2008) • Desktop .EXE Build",
                BannerIcon = RetroAssets.GetAimBadgeLogoBitmap(32)
            };
            tabAbout.Controls.Add(splash);

            Label desc = new Label
            {
                Location = new Point(14, 78),
                Size = new Size(422, 82),
                Text = "Native Windows desktop edition of Classic AIM featuring:\r\n" +
                       "• Account Registration, Login & Multi-Account Manager\r\n" +
                       "• Add Friends by Nickname + AI Bilingual Companion Chat (RU/EN)\r\n" +
                       "• 42 Authentic Classic 2000s Animated GIF Buddy Icons + Emoticons\r\n" +
                       "• System Tray NotifyIcon, Sound Synthesizer, File Transfers & Chat History"
            };

            GroupBox grpSim = new GroupBox { Text = "Test Live 2006 Events", Location = new Point(14, 170), Size = new Size(422, 92) };
            XpButton bSign = new XpButton { Text = "Simulate Buddy Sign-On", Location = new Point(12, 26), Size = new Size(134, 25), Font = XpTheme.SmallFont };
            XpButton bIm = new XpButton { Text = "Simulate Incoming IM", Location = new Point(152, 26), Size = new Size(130, 25), Font = XpTheme.SmallFont };
            XpButton bFile = new XpButton { Text = "Simulate File Offer", Location = new Point(288, 26), Size = new Size(122, 25), Font = XpTheme.SmallFont };
            bSign.Click += delegate
            {
                Buddy b = Store.State.Buddies.FirstOrDefault(x => x.Status == "offline") ?? Store.State.Buddies[0];
                b.Status = "online"; b.JustSignedOn = true;
                SoundEngine.Play("doorOpen");
                Store.Notify("buddies");
                AimApp.ShowToast(b.ScreenName + " signed on", "Click to send an Instant Message", b.Avatar, delegate { AimApp.OpenPrivateChat(b.ScreenName); });
            };
            bIm.Click += delegate
            {
                Buddy b = Store.State.Buddies.FirstOrDefault(x => x.Status == "online") ?? Store.State.Buddies[0];
                AimApp.ReceiveIncomingIM(b.ScreenName, "Hey! How's Windows XP treatin' u in 2006? :-D");
            };
            bFile.Click += delegate { new FileTransferForm(null).Show(); };
            XpButton bSup = new XpButton { Text = "Support Center...", Location = new Point(12, 57), Size = new Size(134, 24), Font = XpTheme.SmallFont };
            XpButton bUpd = new XpButton { Text = "Update Center...", Location = new Point(152, 57), Size = new Size(130, 24), Font = XpTheme.SmallFont };
            bSup.Click += delegate { new SupportForm().ShowDialog(this); };
            bUpd.Click += delegate { new UpdateCenterForm().ShowDialog(this); };
            grpSim.Controls.AddRange(new Control[] { bSign, bIm, bFile, bSup, bUpd });
            tabAbout.Controls.AddRange(new Control[] { desc, grpSim });

            // Help tab
            TextBox helpTxt = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.White,
                ScrollBars = ScrollBars.Vertical,
                Text = "CLASSIC AIM HELP TOPICS\r\n" +
                       "========================\r\n\r\n" +
                       "1. Account Registration:\r\n" +
                       "Click 'Register / New ID' on the Sign On screen or File -> Account Manager.\r\n\r\n" +
                       "2. Adding Real Friends (No Bots!):\r\n" +
                       "Click 'Add' -> 'Real People (No Bots)' tab to discover friends on LAN/VPN, use a Shared AIM Relay Server, or paste a Nick@IP:Port Invite Code.\r\n\r\n" +
                       "3. AI Bots & API Keys:\r\n" +
                       "Click 'API Keys' on the menu or toolbar to connect OpenAI / Groq / Gemini / OpenRouter / Ollama, or chat offline out of the box.\r\n\r\n" +
                       "4. Translation, Update Center & Support:\r\n" +
                       "Use 'Translate' in IM chat windows (EN <-> RU), switch UI language in Edit -> Language, and open Help -> AIM Update Center or Support Center.\r\n\r\n" +
                       "5. Emoticon Shortcuts:\r\n" +
                       ":-)  :-(  ;-)  :-D  :-P  :-O  :'(  B-)  >:(  :-*  O:)  >:)  <3  :-/  :-$  |-)"
            };
            tabHelp.Controls.Add(helpTxt);
            if (initialTab == "help") tabs.SelectedTab = tabHelp;

            Panel pad = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            pad.Controls.Add(tabs);
            ClientBody.Controls.Add(pad);
            pad.BringToFront();

            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = XpTheme.Current.FaceDark };
            XpButton btnOk = new XpButton { Text = "OK", IsPrimary = true, Location = new Point(380, 6) };
            btnOk.Click += delegate { Close(); };
            foot.Controls.Add(btnOk);
            ClientBody.Controls.Add(foot);
        }
    }
}
