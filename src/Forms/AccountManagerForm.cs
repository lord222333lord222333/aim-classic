using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AIMClassic
{
    public class AccountManagerForm : XpForm
    {
        private TabControl _tabs;
        private ListBox _lstAccounts;
        private PictureBox _pbAccAv;
        private Label _lblAccName;
        private TextBox _txtEditDisp, _txtEditPw, _txtEditEmail;
        private CheckBox _chkEditSave, _chkEditAuto;

        // Registration tab controls
        private TextBox _regSn, _regPw, _regPw2, _regDisp, _regEmail, _regLoc, _regInterests, _regBio;
        private ComboBox _regAvatar;
        private PictureBox _pbRegPreview;

        public AccountManagerForm(string initialTab = "list")
        {
            Text = I18n.T("AIM Account Manager & Registration", "Менеджер аккаунтов и регистрация AIM");
            Size = new Size(495, 435);
            ShowMaximizeButton = false;
            AllowResize = false;
            TitleIconImage = RetroAssets.GetIconBitmap("accounts", 16);

            XpBanner banner = new XpBanner
            {
                BannerTitle = "AIM Account Manager & Registration",
                BannerSubtitle = "Switch accounts, register a new Screen Name, or edit saved credentials",
                BannerIcon = RetroAssets.GetIconBitmap("accounts", 26)
            };
            ClientBody.Controls.Add(banner);

            _tabs = new TabControl { Dock = DockStyle.Fill };
            TabPage tabList = new TabPage(I18n.T("Saved Accounts", "Сохранённые аккаунты"));
            TabPage tabReg = new TabPage(I18n.T("Register (New Screen Name)", "Регистрация (новый ник)"));
            tabList.BackColor = Color.FromArgb(252, 252, 254);
            tabReg.BackColor = Color.FromArgb(252, 252, 254);
            _tabs.TabPages.Add(tabList);
            _tabs.TabPages.Add(tabReg);

            BuildListTab(tabList);
            BuildRegisterTab(tabReg);

            Panel pad = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            pad.Controls.Add(_tabs);
            ClientBody.Controls.Add(pad);
            pad.BringToFront();

            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = XpTheme.Current.FaceDark };
            XpButton btnClose = new XpButton { Text = "Close", Location = new Point(394, 6) };
            btnClose.Click += delegate { Close(); };
            foot.Controls.Add(btnClose);
            ClientBody.Controls.Add(foot);

            if (initialTab == "register" || initialTab == "create") _tabs.SelectedTab = tabReg;
            LoadAccounts();
        }

        private void BuildListTab(TabPage page)
        {
            _lstAccounts = new ListBox { Location = new Point(10, 10), Size = new Size(165, 256), BorderStyle = BorderStyle.FixedSingle };
            _lstAccounts.SelectedIndexChanged += delegate { PopulateSelected(); };

            Panel info = new Panel { Location = new Point(184, 10), Size = new Size(276, 58), BackColor = Color.FromArgb(238, 245, 253), BorderStyle = BorderStyle.FixedSingle };
            _pbAccAv = new PictureBox { Location = new Point(6, 6), Size = new Size(44, 44), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            _lblAccName = new Label { Location = new Point(58, 8), Size = new Size(210, 40), Font = XpTheme.UiBold, ForeColor = XpTheme.Current.AimNavy };
            info.Controls.Add(_pbAccAv);
            info.Controls.Add(_lblAccName);

            int y = 78;
            page.Controls.Add(new Label { Text = "Display Name:", Location = new Point(184, y + 3), AutoSize = true });
            _txtEditDisp = new TextBox { Location = new Point(272, y), Width = 188, BorderStyle = BorderStyle.FixedSingle };
            y += 26;

            page.Controls.Add(new Label { Text = "Password:", Location = new Point(184, y + 3), AutoSize = true });
            _txtEditPw = new TextBox { Location = new Point(272, y), Width = 188, BorderStyle = BorderStyle.FixedSingle };
            y += 26;

            page.Controls.Add(new Label { Text = "Email:", Location = new Point(184, y + 3), AutoSize = true });
            _txtEditEmail = new TextBox { Location = new Point(272, y), Width = 188, BorderStyle = BorderStyle.FixedSingle };
            y += 26;

            _chkEditSave = new CheckBox { Text = "Save password", Location = new Point(184, y), AutoSize = true };
            _chkEditAuto = new CheckBox { Text = "Auto-login", Location = new Point(304, y), AutoSize = true };
            y += 30;

            XpButton btnSave = new XpButton { Text = "Save", IsPrimary = true, Location = new Point(184, y), Size = new Size(80, 24) };
            XpButton btnSwitch = new XpButton { Text = "Sign On As", Location = new Point(270, y), Size = new Size(94, 24) };
            XpButton btnDel = new XpButton { Text = "Delete", Location = new Point(370, y), Size = new Size(90, 24) };

            btnSave.Click += delegate
            {
                Account acc = SelectedAcc();
                if (acc == null) return;
                acc.DisplayName = string.IsNullOrWhiteSpace(_txtEditDisp.Text) ? acc.ScreenName : _txtEditDisp.Text.Trim();
                if (!string.IsNullOrWhiteSpace(_txtEditPw.Text)) acc.Password = _txtEditPw.Text;
                acc.Email = _txtEditEmail.Text.Trim();
                acc.SavePassword = _chkEditSave.Checked;
                acc.AutoLogin = _chkEditAuto.Checked;
                Store.Notify("accounts");
                XpMessageBox.Show(this, "Saved account settings for " + acc.ScreenName, "Account Manager", false);
            };

            btnSwitch.Click += delegate
            {
                Account acc = SelectedAcc();
                if (acc == null) return;
                Store.State.ActiveAccountSN = acc.ScreenName;
                Store.State.LoggedIn = true;
                Store.State.CurrentUser.Status = "online";
                Store.Notify("auth");
                if (AimApp.LoginWin != null && !AimApp.LoginWin.IsDisposed) AimApp.LoginWin.Close();
                SoundEngine.Play("welcome");
                AimApp.OpenBuddyList();
                Close();
            };

            btnDel.Click += delegate
            {
                Account acc = SelectedAcc();
                if (acc == null || Store.State.Accounts.Count <= 1)
                {
                    XpMessageBox.Show(this, "At least one account must remain on this PC.", "Account Manager", false);
                    return;
                }
                if (XpMessageBox.Show(this, "Delete account \"" + acc.ScreenName + "\"?", "Confirm Delete", true) == DialogResult.Yes)
                {
                    Store.State.Accounts.Remove(acc);
                    if (string.Equals(Store.State.ActiveAccountSN, acc.ScreenName, StringComparison.OrdinalIgnoreCase))
                    {
                        Store.State.ActiveAccountSN = Store.State.Accounts[0].ScreenName;
                    }
                    Store.Notify("accounts");
                    LoadAccounts();
                }
            };

            page.Controls.Add(_lstAccounts);
            page.Controls.Add(info);
            page.Controls.Add(_txtEditDisp);
            page.Controls.Add(_txtEditPw);
            page.Controls.Add(_txtEditEmail);
            page.Controls.Add(_chkEditSave);
            page.Controls.Add(_chkEditAuto);
            page.Controls.Add(btnSave);
            page.Controls.Add(btnSwitch);
            page.Controls.Add(btnDel);
        }

        private void BuildRegisterTab(TabPage page)
        {
            int y = 12;
            page.Controls.Add(new Label { Text = "Screen Name *:", Location = new Point(12, y + 3), AutoSize = true, Font = XpTheme.UiBold });
            _regSn = new TextBox { Location = new Point(120, y), Width = 175, BorderStyle = BorderStyle.FixedSingle };
            y += 26;

            page.Controls.Add(new Label { Text = "Password *:", Location = new Point(12, y + 3), AutoSize = true, Font = XpTheme.UiBold });
            _regPw = new TextBox { Location = new Point(120, y), Width = 175, UseSystemPasswordChar = true, BorderStyle = BorderStyle.FixedSingle };
            y += 26;

            page.Controls.Add(new Label { Text = "Confirm PW *:", Location = new Point(12, y + 3), AutoSize = true });
            _regPw2 = new TextBox { Location = new Point(120, y), Width = 175, UseSystemPasswordChar = true, BorderStyle = BorderStyle.FixedSingle };
            y += 26;

            page.Controls.Add(new Label { Text = "Display Name:", Location = new Point(12, y + 3), AutoSize = true });
            _regDisp = new TextBox { Location = new Point(120, y), Width = 175, BorderStyle = BorderStyle.FixedSingle };
            y += 26;

            page.Controls.Add(new Label { Text = "Email:", Location = new Point(12, y + 3), AutoSize = true });
            _regEmail = new TextBox { Location = new Point(120, y), Width = 175, BorderStyle = BorderStyle.FixedSingle };
            y += 26;

            page.Controls.Add(new Label { Text = "Location:", Location = new Point(12, y + 3), AutoSize = true });
            _regLoc = new TextBox { Location = new Point(120, y), Width = 175, BorderStyle = BorderStyle.FixedSingle };
            y += 26;

            page.Controls.Add(new Label { Text = "Interests:", Location = new Point(12, y + 3), AutoSize = true });
            _regInterests = new TextBox { Location = new Point(120, y), Width = 335, BorderStyle = BorderStyle.FixedSingle };
            y += 26;

            page.Controls.Add(new Label { Text = "About / Bio:", Location = new Point(12, y + 3), AutoSize = true });
            _regBio = new TextBox { Location = new Point(120, y), Size = new Size(335, 38), Multiline = true, BorderStyle = BorderStyle.FixedSingle };

            // Classic Avatar Picker on right side of Register Tab
            Label lblAv = new Label { Text = "Classic Buddy Icon:", Location = new Point(312, 12), AutoSize = true, Font = XpTheme.UiBold };
            _pbRegPreview = new PictureBox { Location = new Point(345, 34), Size = new Size(56, 56), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            _regAvatar = new ComboBox { Location = new Point(310, 98), Width = 145, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (string av in RetroAssets.GetAvatarNames()) _regAvatar.Items.Add(av);
            if (_regAvatar.Items.Count > 0) _regAvatar.SelectedIndex = 0;
            _regAvatar.SelectedIndexChanged += delegate
            {
                RetroAssets.SetPictureBoxAvatar(_pbRegPreview, Convert.ToString(_regAvatar.SelectedItem));
            };
            if (_regAvatar.SelectedItem != null) RetroAssets.SetPictureBoxAvatar(_pbRegPreview, Convert.ToString(_regAvatar.SelectedItem));

            XpButton btnCreate = new XpButton { Text = I18n.T("Create Account & Sign On", "Зарегистрироваться и войти"), IsAimGold = true, Location = new Point(256, 240), Size = new Size(200, 28) };
            btnCreate.Click += delegate
            {
                if (_regPw.Text != _regPw2.Text)
                {
                    XpMessageBox.Show(this, "Passwords do not match!", "Registration", false);
                    return;
                }
                string err = Store.RegisterAccount(
                    _regSn.Text.Trim(),
                    _regPw.Text,
                    _regDisp.Text.Trim(),
                    _regEmail.Text.Trim(),
                    Convert.ToString(_regAvatar.SelectedItem),
                    _regLoc.Text.Trim(),
                    _regInterests.Text.Trim(),
                    _regBio.Text.Trim());
                if (err != null)
                {
                    XpMessageBox.Show(this, err, "Registration Error", false);
                    return;
                }
                Store.State.ActiveAccountSN = _regSn.Text.Trim();
                Store.State.LoggedIn = true;
                Store.State.CurrentUser.Status = "online";
                Store.Notify("auth");
                if (AimApp.LoginWin != null && !AimApp.LoginWin.IsDisposed) AimApp.LoginWin.Close();
                SoundEngine.Play("welcome");
                AimApp.OpenBuddyList();
                Close();
            };

            page.Controls.Add(_regSn);
            page.Controls.Add(_regPw);
            page.Controls.Add(_regPw2);
            page.Controls.Add(_regDisp);
            page.Controls.Add(_regEmail);
            page.Controls.Add(_regLoc);
            page.Controls.Add(_regInterests);
            page.Controls.Add(_regBio);
            page.Controls.Add(lblAv);
            page.Controls.Add(_pbRegPreview);
            page.Controls.Add(_regAvatar);
            page.Controls.Add(btnCreate);
        }

        private void LoadAccounts()
        {
            _lstAccounts.Items.Clear();
            foreach (Account a in Store.State.Accounts) _lstAccounts.Items.Add(a.ScreenName);
            int idx = _lstAccounts.Items.IndexOf(Store.GetActiveAccount().ScreenName);
            if (idx >= 0) _lstAccounts.SelectedIndex = idx;
            else if (_lstAccounts.Items.Count > 0) _lstAccounts.SelectedIndex = 0;
        }

        private Account SelectedAcc()
        {
            string sn = _lstAccounts.SelectedItem as string;
            if (string.IsNullOrEmpty(sn)) return null;
            return Store.State.Accounts.FirstOrDefault(a => string.Equals(a.ScreenName, sn, StringComparison.OrdinalIgnoreCase));
        }

        private void PopulateSelected()
        {
            Account a = SelectedAcc();
            if (a == null) return;
            RetroAssets.SetPictureBoxAvatar(_pbAccAv, a.Avatar);
            _lblAccName.Text = a.ScreenName + "\nMember Since: " + a.MemberSince;
            _txtEditDisp.Text = a.DisplayName;
            _txtEditPw.Text = a.Password;
            _txtEditEmail.Text = a.Email;
            _chkEditSave.Checked = a.SavePassword;
            _chkEditAuto.Checked = a.AutoLogin;
        }
    }
}
