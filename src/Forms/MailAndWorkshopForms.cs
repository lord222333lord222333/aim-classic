using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace AIMClassic
{
    /* ======================================================================
       Classic AOL / AIM Mail (2006) Inbox & Compose Window
       ====================================================================== */
    public class AimMailForm : XpForm
    {
        private ListView _lvMail;
        private Label _lblFromVal, _lblToVal, _lblSubjVal, _lblDateVal, _lblInboxStatus;
        private TextBox _txtBody;
        private XpBanner _banner;
        private MailItem _selected;
        private readonly Action<string> _mailObserver;

        public AimMailForm()
        {
            Text = I18n.T("AIM Mail — AOL Mail Inbox (2006)", "AIM Mail — Входящие (AOL Почта)");
            Size = new Size(612, 476);
            MinimumSize = new Size(520, 400);
            TitleIconImage = RetroAssets.GetIconBitmap("mail", 16);

            _banner = new XpBanner
            {
                BannerTitle = I18n.T("AOL® / AIM® Mail Center", "Почтовый центр AOL® / AIM®"),
                BannerSubtitle = I18n.T("Read & send retro AOL Mail and Global Cloud messages", "Чтение и отправка писем AOL Mail и через глобальную сеть"),
                BannerIcon = RetroAssets.GetIconBitmap("mail", 26)
            };
            ClientBody.Controls.Add(_banner);

            Panel toolbar = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = XpTheme.Current.SubBarTop };
            XpButton btnWrite = new XpButton { Text = I18n.T("Write Mail", "Написать"), IsPrimary = true, Location = new Point(8, 6), Size = new Size(92, 24), TabIndex = 0 };
            XpButton btnReply = new XpButton { Text = I18n.T("Reply", "Ответить"), Location = new Point(106, 6), Size = new Size(76, 24) };
            XpButton btnToggleRead = new XpButton { Text = I18n.T("Mark Read/Unread", "Прочитано/Нет"), Location = new Point(188, 6), Size = new Size(122, 24), Font = XpTheme.SmallFont };
            XpButton btnDelete = new XpButton { Text = I18n.T("Delete", "Удалить"), Location = new Point(316, 6), Size = new Size(72, 24) };
            XpButton btnSync = new XpButton { Text = I18n.T("Check Mail", "Проверить"), Location = new Point(394, 6), Size = new Size(92, 24) };
            _lblInboxStatus = new Label { Location = new Point(494, 10), Size = new Size(106, 18), Font = XpTheme.SmallFont, ForeColor = XpTheme.Current.AimNavy, TextAlign = ContentAlignment.MiddleRight };

            btnWrite.Click += delegate { OpenCompose(null, null); };
            btnReply.Click += delegate
            {
                if (_selected == null) return;
                OpenCompose(_selected.From, _selected.Subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase) ? _selected.Subject : "Re: " + _selected.Subject);
            };
            btnToggleRead.Click += delegate
            {
                if (_selected == null) return;
                _selected.Read = !_selected.Read;
                Store.Save();
                Store.Notify("mail");
            };
            btnDelete.Click += delegate
            {
                if (_selected == null) return;
                Store.State.MailItems.Remove(_selected);
                _selected = null;
                Store.Save();
                Store.Notify("mail");
            };
            btnSync.Click += delegate
            {
                NetworkEngine.SyncGlobalCloudNow();
                SoundEngine.Play("buddyDing");
                RefreshMailList();
            };

            toolbar.Controls.AddRange(new Control[] { btnWrite, btnReply, btnToggleRead, btnDelete, btnSync, _lblInboxStatus });

            _lvMail = new ListView
            {
                Dock = DockStyle.Top,
                Height = 148,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false,
                BackColor = Color.White,
                Font = XpTheme.UiFont
            };
            _lvMail.Columns.Add(I18n.T("State", "Статус"), 72);
            _lvMail.Columns.Add(I18n.T("From", "От кого"), 154);
            _lvMail.Columns.Add(I18n.T("Subject", "Тема"), 238);
            _lvMail.Columns.Add(I18n.T("Date", "Дата"), 124);
            _lvMail.SelectedIndexChanged += OnSelectMail;
            _lvMail.DoubleClick += delegate
            {
                if (_selected != null && !_selected.Read)
                {
                    _selected.Read = true;
                    Store.Save();
                    Store.Notify("mail");
                }
            };

            Panel readWrap = new Panel { Dock = DockStyle.Fill, BackColor = XpTheme.Current.Face, Padding = new Padding(8, 6, 8, 6) };
            Panel hdr = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = XpTheme.Current.SubBarTop, BorderStyle = BorderStyle.FixedSingle };
            hdr.Controls.Add(new Label { Text = I18n.T("From:", "От:"), Location = new Point(8, 6), Size = new Size(56, 15), Font = XpTheme.UiBold, UseMnemonic = false });
            _lblFromVal = new Label { Location = new Point(66, 6), Size = new Size(310, 15), Font = XpTheme.UiFont, ForeColor = XpTheme.Current.AimNavy, UseMnemonic = false };
            hdr.Controls.Add(new Label { Text = I18n.T("Date:", "Дата:"), Location = new Point(382, 6), Size = new Size(46, 15), Font = XpTheme.UiBold, UseMnemonic = false });
            _lblDateVal = new Label { Location = new Point(430, 6), Size = new Size(150, 15), Font = XpTheme.SmallFont, UseMnemonic = false };
            hdr.Controls.Add(new Label { Text = I18n.T("To:", "Кому:"), Location = new Point(8, 24), Size = new Size(56, 15), Font = XpTheme.UiBold, UseMnemonic = false });
            _lblToVal = new Label { Location = new Point(66, 24), Size = new Size(510, 15), Font = XpTheme.UiFont, UseMnemonic = false };
            hdr.Controls.Add(new Label { Text = I18n.T("Subject:", "Тема:"), Location = new Point(8, 42), Size = new Size(56, 15), Font = XpTheme.UiBold, UseMnemonic = false });
            _lblSubjVal = new Label { Location = new Point(66, 42), Size = new Size(510, 16), Font = XpTheme.UiBold, UseMnemonic = false };
            hdr.Controls.AddRange(new Control[] { _lblFromVal, _lblDateVal, _lblToVal, _lblSubjVal });

            _txtBody = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = XpTheme.UiFont
            };
            readWrap.Controls.Add(_txtBody);
            readWrap.Controls.Add(hdr);
            _txtBody.BringToFront();

            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 36, Width = ClientBody.ClientSize.Width, BackColor = XpTheme.Current.FaceDark };
            XpButton btnClose = new XpButton { Text = I18n.T("Close", "Закрыть"), Location = new Point(Math.Max(420, ClientBody.ClientSize.Width - 94), 6), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnClose.Click += delegate { Close(); };
            foot.Controls.Add(btnClose);

            ClientBody.Controls.Add(readWrap);
            ClientBody.Controls.Add(_lvMail);
            ClientBody.Controls.Add(toolbar);
            ClientBody.Controls.Add(foot);
            readWrap.BringToFront();

            _mailObserver = delegate(string topic)
            {
                if (!IsDisposed)
                {
                    try { BeginInvoke((Action)RefreshMailList); } catch { }
                }
            };
            Store.Changed += _mailObserver;
            FormClosed += delegate { Store.Changed -= _mailObserver; };

            RefreshMailList();
        }

        private void RefreshMailList()
        {
            string prevId = _selected != null ? _selected.Id : null;
            _lvMail.BeginUpdate();
            _lvMail.Items.Clear();

            int unread = 0;
            List<MailItem> items = Store.State.MailItems ?? new List<MailItem>();
            foreach (MailItem m in items)
            {
                if (!m.Read) unread++;
                ListViewItem lvi = new ListViewItem(m.Read ? I18n.T("Read", "Прочт.") : I18n.T("UNREAD", "НОВОЕ"));
                lvi.SubItems.Add(m.From ?? "");
                lvi.SubItems.Add(m.Subject ?? "(No Subject)");
                lvi.SubItems.Add(m.Date ?? "");
                lvi.Tag = m;
                if (!m.Read)
                {
                    lvi.Font = XpTheme.UiBold;
                    lvi.ForeColor = XpTheme.Current.AimNavy;
                    lvi.BackColor = Color.FromArgb(242, 248, 255);
                }
                _lvMail.Items.Add(lvi);
                if (prevId != null && m.Id == prevId) lvi.Selected = true;
            }
            if (_lvMail.SelectedItems.Count == 0 && _lvMail.Items.Count > 0)
            {
                _lvMail.Items[0].Selected = true;
            }
            _lvMail.EndUpdate();

            _lblInboxStatus.Text = string.Format(I18n.T("{0} Unread / {1}", "{0} новых / {1}"), unread, items.Count);
            _banner.BannerSubtitle = unread > 0
                ? string.Format(I18n.T("You've got mail! {0} unread message(s) in Inbox", "Вам письмо! {0} непрочитанных сообщений"), unread)
                : I18n.T("All caught up — no unread messages", "Все письма прочитаны");
            _banner.Invalidate();
        }

        private void OnSelectMail(object sender, EventArgs e)
        {
            if (_lvMail.SelectedItems.Count == 0)
            {
                _selected = null;
                _lblFromVal.Text = "";
                _lblToVal.Text = "";
                _lblSubjVal.Text = "";
                _lblDateVal.Text = "";
                _txtBody.Text = "";
                return;
            }
            _selected = _lvMail.SelectedItems[0].Tag as MailItem;
            if (_selected == null) return;
            _lblFromVal.Text = _selected.From ?? "";
            _lblToVal.Text = _selected.To ?? "";
            _lblSubjVal.Text = _selected.Subject ?? "";
            _lblDateVal.Text = _selected.Date ?? "";
            _txtBody.Text = _selected.Body ?? "";
            _txtBody.SelectionStart = 0;
            _txtBody.SelectionLength = 0;
        }

        private void OpenCompose(string presetTo, string presetSubject)
        {
            using (XpForm dlg = new XpForm())
            {
                dlg.Text = I18n.T("Write AIM Mail", "Новое письмо AIM Mail");
                dlg.Size = new Size(456, 368);
                dlg.ShowMaximizeButton = false;
                dlg.ShowMinimizeButton = false;
                dlg.AllowResize = false;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.TitleIconImage = RetroAssets.GetIconBitmap("mail", 16);

                Label lTo = new Label { Text = I18n.T("To (ScreenName / email):", "Кому (ник или email):"), Location = new Point(14, 14), AutoSize = true, Font = XpTheme.UiBold };
                TextBox txtTo = new TextBox { Location = new Point(14, 34), Width = 414, BorderStyle = BorderStyle.FixedSingle, Text = presetTo ?? "" };
                Label lSubj = new Label { Text = I18n.T("Subject:", "Тема:"), Location = new Point(14, 64), AutoSize = true, Font = XpTheme.UiBold };
                TextBox txtSubj = new TextBox { Location = new Point(14, 84), Width = 414, BorderStyle = BorderStyle.FixedSingle, Text = presetSubject ?? "" };
                Label lBody = new Label { Text = I18n.T("Message:", "Сообщение:"), Location = new Point(14, 114), AutoSize = true, Font = XpTheme.UiBold };
                TextBox txtMsg = new TextBox { Location = new Point(14, 134), Size = new Size(414, 140), Multiline = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.FixedSingle };

                XpButton btnSend = new XpButton { Text = I18n.T("Send Mail", "Отправить"), IsPrimary = true, Location = new Point(242, 286), Size = new Size(92, 26) };
                XpButton btnCancel = new XpButton { Text = I18n.T("Cancel", "Отмена"), Location = new Point(340, 286), Size = new Size(88, 26) };

                btnCancel.Click += delegate { dlg.Close(); };
                btnSend.Click += delegate
                {
                    string toRaw = txtTo.Text.Trim();
                    if (string.IsNullOrEmpty(toRaw))
                    {
                        XpMessageBox.Show(dlg, I18n.T("Please enter a recipient Screen Name or address.", "Введите ник получателя или адрес."), "AIM Mail", false);
                        return;
                    }
                    string targetNick = toRaw.Contains("@") ? toRaw.Substring(0, toRaw.IndexOf('@')) : toRaw;
                    string subj = string.IsNullOrEmpty(txtSubj.Text.Trim()) ? "(No Subject)" : txtSubj.Text.Trim();
                    string body = txtMsg.Text;
                    Account me = Store.GetActiveAccount();
                    string myNick = me != null ? me.ScreenName : "AIMUser";

                    bool sentGlobal = false;
                    string err = "";
                    if (Store.State.Settings.GlobalCloudEnabled && !string.Equals(targetNick, myNick, StringComparison.OrdinalIgnoreCase))
                    {
                        sentGlobal = NetworkEngine.SendGlobalMail(targetNick, subj, body, out err);
                    }

                    // Also add to local Inbox if self-mail or archive Sent item in Inbox/Sent
                    if (string.Equals(targetNick, myNick, StringComparison.OrdinalIgnoreCase) || string.Equals(targetNick, "self", StringComparison.OrdinalIgnoreCase))
                    {
                        Store.State.MailItems.Insert(0, new MailItem
                        {
                            Id = Guid.NewGuid().ToString("N").Substring(0, 10),
                            From = myNick + "@aim.com",
                            To = myNick + "@aim.com",
                            Subject = subj,
                            Body = body,
                            Date = Store.NowDateStr() + " " + Store.NowTimeStr(),
                            Read = false
                        });
                    }
                    else
                    {
                        Store.State.MailItems.Insert(0, new MailItem
                        {
                            Id = Guid.NewGuid().ToString("N").Substring(0, 10),
                            From = myNick + "@aim.com (Sent)",
                            To = toRaw,
                            Subject = "[Sent] " + subj,
                            Body = body + (sentGlobal ? "\r\n\r\n[Delivered via Global Cloud Mail]" : "\r\n\r\n[Saved locally in Sent Archive]"),
                            Date = Store.NowDateStr() + " " + Store.NowTimeStr(),
                            Read = true
                        });
                    }
                    SoundEngine.Play("youveGotMail");
                    Store.Notify("mail");
                    dlg.Close();
                };

                dlg.ClientBody.Controls.AddRange(new Control[] { lTo, txtTo, lSubj, txtSubj, lBody, txtMsg, btnSend, btnCancel });
                dlg.ShowDialog(this);
            }
        }
    }

    /* ======================================================================
       AIM Workshop — Featured Theme Skins, Custom Creator & Avatar Packs
       ====================================================================== */
    public class WorkshopForm : XpForm
    {
        private class BuiltInSkinEntry
        {
            public string Key;
            public string Title;
            public string Badge;
            public string Desc;
        }

        private FlowLayoutPanel _skinGallery;
        private CheckBox _chkEnableBackdrop;
        private TabControl _tabs;

        // Studio creator controls
        private TextBox _txtStudioName, _txtStudioAuthor;
        private CheckBox _chkStudioGlass;
        private Panel _pnlPreviewBox, _swTop, _swBot, _swFace, _swAccent;
        private Color _cTop = Color.FromArgb(46, 206, 255);
        private Color _cBot = Color.FromArgb(8, 108, 178);
        private Color _cFace = Color.FromArgb(228, 246, 253);
        private Color _cAccent = Color.FromArgb(132, 255, 192);

        // Packs tab
        private FlowLayoutPanel _avatarGrid;
        private Label _lblPackStatus;

        private static readonly BuiltInSkinEntry[] _builtInSkins = new BuiltInSkinEntry[]
        {
            new BuiltInSkinEntry { Key = "FrutigerAero", Title = "Frutiger Aero (2007)", Badge = "FEATURED", Desc = "Glossy Aqua Glass + Sky Wallpaper" },
            new BuiltInSkinEntry { Key = "LunaBlue", Title = "XP Luna Blue (2005)", Badge = "CLASSIC", Desc = "Authentic Windows XP Sapphire" },
            new BuiltInSkinEntry { Key = "XpRoyale", Title = "XP Royale Energy", Badge = "2006 SKIN", Desc = "Media Center Glossy Royal Blue" },
            new BuiltInSkinEntry { Key = "Aim6Ice", Title = "AIM 6.0 Triton Ice", Badge = "AIM 6", Desc = "Cool Frost Slate-Blue Chrome" },
            new BuiltInSkinEntry { Key = "XpSilver", Title = "Windows XP Silver", Badge = "XP PRO", Desc = "Metallic Silver + Navy Accent" },
            new BuiltInSkinEntry { Key = "XpOlive", Title = "XP Homestead Olive", Badge = "RETRO XP", Desc = "Warm Olive Green + Cream Face" },
            new BuiltInSkinEntry { Key = "Y2KNeon", Title = "Y2K Cyber Neon", Badge = "WINAMP", Desc = "Glossy Violet Chrome + Cyan" }
        };

        public WorkshopForm()
        {
            Text = I18n.T("AIM Workshop — Themes, Frutiger Aero & Packs", "Мастерская AIM — Темы, Фрутигер Аэро и Паки");
            Size = new Size(654, 514);
            MinimumSize = new Size(620, 470);
            TitleIconImage = RetroAssets.GetIconBitmap("workshop", 16);

            XpBanner banner = new XpBanner
            {
                BannerTitle = I18n.T("AIM Workshop (Skins, Creator & Packs)", "AIM Workshop — Скины, Конструктор и Паки"),
                BannerSubtitle = I18n.T("Install Frutiger Aero or retro XP themes, design your own skin & share .aimtheme.json", "Устанавливайте Frutiger Aero и XP темы, создавайте свои скины и делитесь с друзьями"),
                BannerIcon = RetroAssets.GetIconBitmap("workshop", 26)
            };
            ClientBody.Controls.Add(banner);

            _tabs = new TabControl { Dock = DockStyle.Fill };
            TabPage tabSkins = new TabPage(I18n.T("Theme Skins Gallery", "Галерея тем"));
            TabPage tabStudio = new TabPage(I18n.T("Theme Creator Studio", "Конструктор тем"));
            TabPage tabPacks = new TabPage(I18n.T("Avatar & Sound Packs", "Аватары и звуки"));
            foreach (TabPage tp in new[] { tabSkins, tabStudio, tabPacks }) tp.BackColor = XpTheme.Current.Face;
            _tabs.TabPages.AddRange(new[] { tabSkins, tabStudio, tabPacks });

            BuildSkinsTab(tabSkins);
            BuildStudioTab(tabStudio);
            BuildPacksTab(tabPacks);

            Panel pad = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            pad.Controls.Add(_tabs);
            ClientBody.Controls.Add(pad);
            pad.BringToFront();

            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 36, Width = ClientBody.ClientSize.Width, BackColor = XpTheme.Current.FaceDark };
            XpButton btnClose = new XpButton { Text = I18n.T("Close", "Закрыть"), Location = new Point(Math.Max(460, ClientBody.ClientSize.Width - 94), 6), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnClose.Click += delegate { Close(); };
            foot.Controls.Add(btnClose);
            ClientBody.Controls.Add(foot);

            RefreshSkinGallery();
        }

        private void BuildSkinsTab(TabPage page)
        {
            Panel topBar = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = XpTheme.Current.SubBarTop };
            _chkEnableBackdrop = new CheckBox
            {
                Text = I18n.T("Also show Frutiger Aero / XP Wallpaper Backdrop Window", "Также показывать окно обоев (Frutiger Aero / XP Bliss)"),
                Location = new Point(10, 8),
                AutoSize = true,
                Checked = Store.State.Settings.ShowXpBackdrop,
                Font = XpTheme.UiBold,
                UseMnemonic = false
            };
            _chkEnableBackdrop.CheckedChanged += delegate
            {
                Store.State.Settings.ShowXpBackdrop = _chkEnableBackdrop.Checked;
                Store.Save();
                AimApp.ApplyThemeToAllForms();
            };
            topBar.Controls.Add(_chkEnableBackdrop);

            _skinGallery = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            page.Controls.Add(_skinGallery);
            page.Controls.Add(topBar);
            _skinGallery.BringToFront();
        }

        private void RefreshSkinGallery()
        {
            if (_skinGallery == null) return;
            _skinGallery.SuspendLayout();
            _skinGallery.Controls.Clear();

            string current = Store.State.Settings.Theme ?? "LunaBlue";
            foreach (BuiltInSkinEntry skin in _builtInSkins)
            {
                _skinGallery.Controls.Add(CreateThemeCard(skin.Key, skin.Title, skin.Badge, skin.Desc, XpTheme.GetPalette(skin.Key), string.Equals(current, skin.Key, StringComparison.OrdinalIgnoreCase), null));
            }
            if (Store.State.CustomThemes != null)
            {
                foreach (CustomThemeDef ct in Store.State.CustomThemes)
                {
                    string key = "Custom:" + ct.Id;
                    _skinGallery.Controls.Add(CreateThemeCard(key, ct.Name, "WORKSHOP", "by " + (ct.Author ?? "User"), XpTheme.GetPalette(key), string.Equals(current, key, StringComparison.OrdinalIgnoreCase), ct));
                }
            }
            _skinGallery.ResumeLayout();
        }

        private Control CreateThemeCard(string themeKey, string title, string badge, string desc, ThemePalette pal, bool isActive, CustomThemeDef customDef)
        {
            Panel card = new Panel
            {
                Size = new Size(278, 152),
                Margin = new Padding(6),
                BackColor = isActive ? Color.FromArgb(236, 249, 255) : Color.FromArgb(248, 250, 253),
                BorderStyle = BorderStyle.FixedSingle
            };

            Panel preview = new Panel { Location = new Point(8, 8), Size = new Size(260, 72), BorderStyle = BorderStyle.FixedSingle };
            preview.Paint += delegate(object s, PaintEventArgs e)
            {
                PaintMiniWindowPreview(e.Graphics, new Rectangle(0, 0, preview.Width, preview.Height), pal);
            };

            Label lblTitle = new Label
            {
                Text = title,
                Location = new Point(8, 84),
                Size = new Size(202, 16),
                Font = XpTheme.UiBold,
                ForeColor = XpTheme.Current.AimNavy,
                AutoEllipsis = true,
                UseMnemonic = false
            };
            Label lblBadge = new Label
            {
                Text = isActive ? "ACTIVE" : badge,
                Location = new Point(214, 84),
                Size = new Size(54, 15),
                BackColor = isActive ? Color.FromArgb(22, 154, 58) : pal.AimHeaderTop,
                ForeColor = isActive ? Color.White : pal.AimGold,
                Font = new Font("Tahoma", 6.25f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Label lblDesc = new Label
            {
                Text = desc,
                Location = new Point(8, 102),
                Size = new Size(260, 16),
                Font = XpTheme.SmallFont,
                ForeColor = Color.FromArgb(72, 86, 108),
                AutoEllipsis = true,
                UseMnemonic = false
            };

            XpButton btnApply = new XpButton
            {
                Text = isActive ? I18n.T("Applied", "Активна") : I18n.T("Install / Apply", "Применить"),
                IsPrimary = !isActive,
                IsAimGold = isActive,
                Location = new Point(8, 121),
                Size = new Size(customDef != null ? 204 : 260, 23),
                Font = XpTheme.SmallFont
            };
            btnApply.Click += delegate
            {
                ApplyThemeKey(themeKey, themeKey == "FrutigerAero");
            };

            card.Controls.AddRange(new Control[] { preview, lblTitle, lblBadge, lblDesc, btnApply });
            if (customDef != null)
            {
                XpButton btnDel = new XpButton { Text = "Del", Location = new Point(216, 121), Size = new Size(52, 23), Font = XpTheme.SmallFont };
                btnDel.Click += delegate
                {
                    Store.State.CustomThemes.Remove(customDef);
                    if (string.Equals(Store.State.Settings.Theme, themeKey, StringComparison.OrdinalIgnoreCase))
                        Store.State.Settings.Theme = "LunaBlue";
                    Store.Save();
                    AimApp.ApplyThemeToAllForms();
                    RefreshSkinGallery();
                };
                card.Controls.Add(btnDel);
            }
            return card;
        }

        private void ApplyThemeKey(string themeKey, bool enableWallpaperIfAero)
        {
            Store.State.Settings.Theme = themeKey;
            if (enableWallpaperIfAero && !Store.State.Settings.ShowXpBackdrop)
            {
                Store.State.Settings.ShowXpBackdrop = true;
                if (_chkEnableBackdrop != null) _chkEnableBackdrop.Checked = true;
            }
            Store.Save();
            Store.Notify("settings");
            AimApp.ApplyThemeToAllForms();
            SoundEngine.Play("connectedChime");
            RefreshSkinGallery();
        }

        private static void PaintMiniWindowPreview(Graphics g, Rectangle bounds, ThemePalette pal)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush bf = new SolidBrush(pal.Face))
                g.FillRectangle(bf, bounds);

            Rectangle titleR = new Rectangle(0, 0, bounds.Width, 18);
            using (LinearGradientBrush br = new LinearGradientBrush(titleR, pal.TitleTop, pal.TitleBot, LinearGradientMode.Vertical))
                g.FillRectangle(br, titleR);

            if (pal.IsGlass)
            {
                using (SolidBrush sh = new SolidBrush(Color.FromArgb(105, 255, 255, 255)))
                    g.FillRectangle(sh, 0, 0, bounds.Width, 9);
            }

            using (SolidBrush cls = new SolidBrush(Color.FromArgb(224, 72, 46)))
                g.FillRectangle(cls, bounds.Width - 15, 3, 12, 12);
            TextRenderer.DrawText(g, "AIM", new Font("Tahoma", 6.5f, FontStyle.Bold), new Point(5, 3), pal.TitleText, TextFormatFlags.NoPrefix);

            Rectangle hdr = new Rectangle(4, 21, bounds.Width - 8, 16);
            using (LinearGradientBrush brH = new LinearGradientBrush(hdr, pal.AimHeaderTop, pal.AimHeaderBot, LinearGradientMode.Vertical))
                g.FillRectangle(brH, hdr);
            using (Pen pG = new Pen(pal.AimGold, 1.5f))
                g.DrawLine(pG, 4, 37, bounds.Width - 4, 37);

            using (SolidBrush card = new SolidBrush(Color.White))
                g.FillRectangle(card, 6, 41, bounds.Width - 12, 22);
            using (Pen pCard = new Pen(pal.FrameBorder))
                g.DrawRectangle(pCard, 6, 41, bounds.Width - 12, 22);
            TextRenderer.DrawText(g, pal.Name, XpTheme.SmallFont, new Point(10, 45), pal.AimNavy, TextFormatFlags.NoPrefix);
        }

        private void BuildStudioTab(TabPage page)
        {
            int y = 14;
            page.Controls.Add(new Label { Text = I18n.T("Skin Name:", "Название темы:"), Location = new Point(16, y + 3), AutoSize = true, Font = XpTheme.UiBold });
            _txtStudioName = new TextBox { Location = new Point(136, y), Width = 200, Text = "My Aqua Aero", BorderStyle = BorderStyle.FixedSingle };
            y += 28;

            page.Controls.Add(new Label { Text = I18n.T("Author:", "Автор:"), Location = new Point(16, y + 3), AutoSize = true, Font = XpTheme.UiBold });
            Account me = Store.GetActiveAccount();
            _txtStudioAuthor = new TextBox { Location = new Point(136, y), Width = 200, Text = me != null ? me.ScreenName : "RetroUser", BorderStyle = BorderStyle.FixedSingle };
            y += 32;

            AddColorPickerRow(page, y, I18n.T("Title Top Color:", "Верх заголовка:"), _cTop, out _swTop, delegate(Color c) { _cTop = c; _pnlPreviewBox.Invalidate(); });
            y += 30;
            AddColorPickerRow(page, y, I18n.T("Title Bottom Color:", "Низ заголовка:"), _cBot, out _swBot, delegate(Color c) { _cBot = c; _pnlPreviewBox.Invalidate(); });
            y += 30;
            AddColorPickerRow(page, y, I18n.T("Window Face Color:", "Фон окна:"), _cFace, out _swFace, delegate(Color c) { _cFace = c; _pnlPreviewBox.Invalidate(); });
            y += 30;
            AddColorPickerRow(page, y, I18n.T("Accent / Gold:", "Акцент / Золото:"), _cAccent, out _swAccent, delegate(Color c) { _cAccent = c; _pnlPreviewBox.Invalidate(); });
            y += 32;

            _chkStudioGlass = new CheckBox
            {
                Text = I18n.T("Enable Glossy Frutiger Aero Glass Shine", "Включить глянцевый эффект стекла Frutiger Aero"),
                Location = new Point(16, y),
                AutoSize = true,
                Checked = true,
                Font = XpTheme.UiBold
            };
            _chkStudioGlass.CheckedChanged += delegate { _pnlPreviewBox.Invalidate(); };

            // Live right-hand preview pane
            Label lblPrev = new Label { Text = I18n.T("Live Window Preview:", "Предпросмотр:"), Location = new Point(366, 14), AutoSize = true, Font = XpTheme.UiBold, ForeColor = XpTheme.Current.AimNavy };
            _pnlPreviewBox = new Panel { Location = new Point(366, 36), Size = new Size(242, 160), BorderStyle = BorderStyle.FixedSingle };
            _pnlPreviewBox.Paint += delegate(object s, PaintEventArgs e)
            {
                PaintMiniWindowPreview(e.Graphics, new Rectangle(0, 0, _pnlPreviewBox.Width, _pnlPreviewBox.Height), BuildPreviewPalette());
            };

            XpButton btnSaveSkin = new XpButton { Text = I18n.T("Save & Apply Skin", "Сохранить и применить"), IsPrimary = true, Location = new Point(16, 250), Size = new Size(172, 28) };
            btnSaveSkin.Click += delegate
            {
                CustomThemeDef def = BuildCustomThemeDef();
                Store.State.CustomThemes.Add(def);
                Store.Save();
                string key = "Custom:" + def.Id;
                ApplyThemeKey(key, false);
                _tabs.SelectedIndex = 0;
            };

            XpButton btnExport = new XpButton { Text = I18n.T("Export .aimtheme.json...", "Экспорт .aimtheme.json..."), Location = new Point(196, 250), Size = new Size(164, 28) };
            btnExport.Click += delegate
            {
                using (SaveFileDialog sfd = new SaveFileDialog { Filter = "AIM Theme|*.aimtheme.json|JSON|*.json", FileName = (_txtStudioName.Text.Trim().Replace(" ", "_")) + ".aimtheme.json" })
                {
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        File.WriteAllText(sfd.FileName, new JavaScriptSerializer().Serialize(BuildCustomThemeDef()));
                        XpMessageBox.Show(this, I18n.T("Theme exported! Friends can import it in AIM Workshop.", "Тема экспортирована!"), "Workshop Export", false);
                    }
                }
            };

            XpButton btnImport = new XpButton { Text = I18n.T("Import .aimtheme.json...", "Импорт .aimtheme.json..."), Location = new Point(368, 250), Size = new Size(170, 28) };
            btnImport.Click += delegate
            {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "AIM Theme|*.aimtheme.json;*.json" })
                {
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        try
                        {
                            CustomThemeDef imported = new JavaScriptSerializer().Deserialize<CustomThemeDef>(File.ReadAllText(ofd.FileName));
                            if (imported == null || string.IsNullOrEmpty(imported.Name)) throw new Exception("Invalid theme JSON");
                            imported.Id = Guid.NewGuid().ToString("N").Substring(0, 8);
                            Store.State.CustomThemes.Add(imported);
                            Store.Save();
                            ApplyThemeKey("Custom:" + imported.Id, false);
                            _tabs.SelectedIndex = 0;
                        }
                        catch (Exception ex)
                        {
                            XpMessageBox.Show(this, "Import error: " + ex.Message, "Workshop Import", false);
                        }
                    }
                }
            };

            page.Controls.AddRange(new Control[] { _txtStudioName, _txtStudioAuthor, _chkStudioGlass, lblPrev, _pnlPreviewBox, btnSaveSkin, btnExport, btnImport });
        }

        private void AddColorPickerRow(TabPage page, int y, string label, Color initial, out Panel swatch, Action<Color> onPick)
        {
            page.Controls.Add(new Label { Text = label, Location = new Point(16, y + 4), AutoSize = true });
            Panel sw = new Panel { Location = new Point(136, y + 2), Size = new Size(44, 21), BorderStyle = BorderStyle.FixedSingle, BackColor = initial };
            XpButton btn = new XpButton { Text = I18n.T("Pick Color...", "Выбрать..."), Location = new Point(188, y + 1), Size = new Size(102, 23), Font = XpTheme.SmallFont };
            btn.Click += delegate
            {
                using (ColorDialog cd = new ColorDialog { Color = sw.BackColor, FullOpen = true })
                {
                    if (cd.ShowDialog(this) == DialogResult.OK)
                    {
                        sw.BackColor = cd.Color;
                        onPick(cd.Color);
                    }
                }
            };
            page.Controls.Add(sw);
            page.Controls.Add(btn);
            swatch = sw;
        }

        private CustomThemeDef BuildCustomThemeDef()
        {
            return new CustomThemeDef
            {
                Id = Guid.NewGuid().ToString("N").Substring(0, 8),
                Name = string.IsNullOrWhiteSpace(_txtStudioName.Text) ? "Custom Skin" : _txtStudioName.Text.Trim(),
                Author = string.IsNullOrWhiteSpace(_txtStudioAuthor.Text) ? "User" : _txtStudioAuthor.Text.Trim(),
                TitleTopHex = ColorTranslator.ToHtml(_cTop),
                TitleBotHex = ColorTranslator.ToHtml(_cBot),
                FaceHex = ColorTranslator.ToHtml(_cFace),
                GoldHex = ColorTranslator.ToHtml(_cAccent),
                IsGlass = _chkStudioGlass.Checked
            };
        }

        private ThemePalette BuildPreviewPalette()
        {
            return new ThemePalette
            {
                Name = _txtStudioName != null ? _txtStudioName.Text : "Preview",
                Face = _cFace,
                TitleTop = _cTop,
                TitleMid = _cTop,
                TitleBot = _cBot,
                FrameBorder = _cBot,
                TitleText = Color.White,
                AimHeaderTop = _cTop,
                AimHeaderBot = _cBot,
                AimNavy = _cBot,
                AimGold = _cAccent,
                IsGlass = _chkStudioGlass != null && _chkStudioGlass.Checked
            };
        }

        private void BuildPacksTab(TabPage page)
        {
            Panel top = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = XpTheme.Current.SubBarTop };
            XpButton btnImportAv = new XpButton { Text = I18n.T("Import Custom Buddy Icon (.gif/.png)...", "Загрузить свою аватарку (.gif/.png)..."), IsPrimary = true, Location = new Point(10, 7), Size = new Size(256, 24), Font = XpTheme.SmallFont };
            XpButton btnSndWelcome = new XpButton { Text = "▶ Welcome", Location = new Point(274, 7), Size = new Size(80, 24), Font = XpTheme.SmallFont };
            XpButton btnSndMail = new XpButton { Text = "▶ Mail", Location = new Point(360, 7), Size = new Size(64, 24), Font = XpTheme.SmallFont };
            _lblPackStatus = new Label { Location = new Point(432, 11), Size = new Size(184, 16), Font = XpTheme.SmallFont, ForeColor = XpTheme.Current.AimNavy };

            btnImportAv.Click += delegate
            {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Images|*.gif;*.png;*.jpg;*.jpeg;*.bmp" })
                {
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        Account me = Store.GetActiveAccount();
                        if (me != null)
                        {
                            me.Avatar = ofd.FileName;
                            Store.Notify("profile");
                            SoundEngine.Play("youveGotPictures");
                            _lblPackStatus.Text = "Set: " + Path.GetFileName(ofd.FileName);
                        }
                    }
                }
            };
            btnSndWelcome.Click += delegate { SoundEngine.Play("welcome", true); };
            btnSndMail.Click += delegate { SoundEngine.Play("youveGotMail", true); };
            top.Controls.AddRange(new Control[] { btnImportAv, btnSndWelcome, btnSndMail, _lblPackStatus });

            _avatarGrid = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White, Padding = new Padding(8) };
            foreach (string avName in RetroAssets.GetAvatarNames())
            {
                string cap = avName;
                Panel tile = new Panel { Size = new Size(92, 74), Margin = new Padding(4), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(248, 250, 253), Cursor = Cursors.Hand };
                PictureBox pb = new PictureBox { Location = new Point(24, 5), Size = new Size(44, 44), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };
                RetroAssets.SetPictureBoxAvatar(pb, cap);
                Label lbl = new Label { Location = new Point(2, 53), Size = new Size(86, 16), Text = Path.GetFileNameWithoutExtension(cap), Font = XpTheme.SmallFont, TextAlign = ContentAlignment.MiddleCenter, AutoEllipsis = true };
                EventHandler pick = delegate
                {
                    Account me = Store.GetActiveAccount();
                    if (me != null)
                    {
                        me.Avatar = cap;
                        Store.Notify("profile");
                        SoundEngine.Play("youveGotPictures");
                        _lblPackStatus.Text = "Applied: " + Path.GetFileNameWithoutExtension(cap);
                    }
                };
                tile.Click += pick; pb.Click += pick; lbl.Click += pick;
                tile.Controls.Add(pb); tile.Controls.Add(lbl);
                _avatarGrid.Controls.Add(tile);
            }

            page.Controls.Add(_avatarGrid);
            page.Controls.Add(top);
            _avatarGrid.BringToFront();
        }
    }
}
