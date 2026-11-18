using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AIMClassic
{
    public class LoginForm : XpForm
    {
        private ComboBox _cboScreenName;
        private TextBox _txtPassword;
        private CheckBox _chkSavePw;
        private CheckBox _chkAuto;
        private CheckBox _chkDialup;
        private ComboBox _cboInitStatus;
        private XpButton _btnSignOn;
        private Panel _pnlConn;
        private Label _lblConn;
        private XpProgressBar _prog;
        private Timer _connTimer;
        private int _connStep;

        public LoginForm()
        {
            Text = "Sign On — AIM";
            Size = new Size(284, 466);
            ShowMaximizeButton = false;
            AllowResize = false;
            StartPosition = FormStartPosition.CenterScreen;
            TitleIconImage = RetroAssets.GetRunningManBitmap(16, "online");
            Icon = RetroAssets.DefaultAppIcon;

            BuildUi();
            Store.Changed += OnStoreChanged;
            FormClosed += delegate
            {
                Store.Changed -= OnStoreChanged;
                if (!Store.State.LoggedIn) Application.Exit();
            };
        }

        private void OnStoreChanged(string topic)
        {
            if (topic == "accounts" && !IsDisposed) RefreshAccountDropdown();
        }

        private void BuildUi()
        {
            // Hero Banner
            Panel hero = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = Color.FromArgb(20, 72, 157) };
            hero.Paint += delegate(object s, PaintEventArgs e)
            {
                using (var br = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, hero.Width, hero.Height),
                    Color.FromArgb(9, 46, 109), Color.FromArgb(53, 126, 226), 135f))
                {
                    e.Graphics.FillRectangle(br, 0, 0, hero.Width, hero.Height);
                }
                using (Pen gold = new Pen(XpTheme.Current.AimGold, 3))
                {
                    e.Graphics.DrawLine(gold, 0, hero.Height - 1, hero.Width, hero.Height - 1);
                }
            };
            PictureBox heroLogo = new PictureBox
            {
                Location = new Point(14, 13),
                Size = new Size(48, 48),
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = RetroAssets.GetAimBadgeLogoBitmap(48)
            };
            Label lblBrand = new Label { Text = "AIM®", ForeColor = Color.White, BackColor = Color.Transparent, Font = new Font("Tahoma", 14f, FontStyle.Bold), Location = new Point(70, 14), AutoSize = true };
            Label lblVer = new Label { Text = "AOL Instant Messenger 5.9/6.0", ForeColor = Color.FromArgb(207, 226, 255), BackColor = Color.Transparent, Font = XpTheme.SmallFont, Location = new Point(72, 41), AutoSize = true };
            hero.Controls.Add(heroLogo);
            hero.Controls.Add(lblBrand);
            hero.Controls.Add(lblVer);
            ClientBody.Controls.Add(hero);

            Panel body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 10, 14, 6) };
            ClientBody.Controls.Add(body);
            body.BringToFront();

            int y = 10;
            Label lblSn = new Label { Text = I18n.T("Screen Name", "Ник (Screen Name)"), Font = XpTheme.UiBold, ForeColor = XpTheme.Current.AimNavy, Location = new Point(14, y), AutoSize = true };
            LinkLabel lnkReg = new LinkLabel { Text = I18n.T("Register / New ID", "Регистрация"), Location = new Point(155, y), AutoSize = true, LinkColor = Color.FromArgb(0, 51, 204) };
            lnkReg.Click += delegate { new AccountManagerForm("register").ShowDialog(this); };
            y += 18;

            _cboScreenName = new ComboBox { Location = new Point(14, y), Width = 248, DropDownStyle = ComboBoxStyle.DropDown };
            _cboScreenName.SelectedIndexChanged += delegate { PopulateSelectedAccount(); };
            y += 28;

            Label lblPw = new Label { Text = "Password", Font = XpTheme.UiBold, ForeColor = XpTheme.Current.AimNavy, Location = new Point(14, y), AutoSize = true };
            LinkLabel lnkForgot = new LinkLabel { Text = "Forgot Password?", Location = new Point(168, y), AutoSize = true, LinkColor = Color.FromArgb(0, 51, 204) };
            lnkForgot.Click += delegate
            {
                string sn = _cboScreenName.Text.Trim();
                Account acc = Store.State.Accounts.FirstOrDefault(a => string.Equals(a.ScreenName, sn, StringComparison.OrdinalIgnoreCase));
                if (acc != null)
                    XpMessageBox.Show(this, string.Format("Saved password hint for \"{0}\":\nPassword: {1}", acc.ScreenName, acc.Password), "AIM Password Recovery", false);
                else
                    XpMessageBox.Show(this, "Enter or select a registered Screen Name, or click 'Register / New ID' to create one.", "AIM Password Recovery", false);
            };
            y += 20;

            _txtPassword = new TextBox { Location = new Point(14, y), Width = 248, UseSystemPasswordChar = true, BorderStyle = BorderStyle.FixedSingle };
            y += 26;

            _chkSavePw = new CheckBox { Text = "Save PW", Location = new Point(14, y), AutoSize = true };
            _chkAuto = new CheckBox { Text = "Auto", Location = new Point(100, y), AutoSize = true };
            _chkDialup = new CheckBox { Text = "56k Dial-Up", Location = new Point(165, y), AutoSize = true };
            y += 26;

            Label lblSt = new Label { Text = "Initial Status", Font = XpTheme.UiBold, ForeColor = XpTheme.Current.AimNavy, Location = new Point(14, y), AutoSize = true };
            y += 18;
            _cboInitStatus = new ComboBox { Location = new Point(14, y), Width = 248, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboInitStatus.Items.AddRange(new object[]
            {
                "Online (Available)",
                "Away (Show Away Message)",
                "Busy (Do Not Disturb)",
                "Invisible (Appear Offline)"
            });
            _cboInitStatus.SelectedIndex = 0;
            y += 32;

            _btnSignOn = new XpButton
            {
                Text = "Sign On",
                IsAimGold = true,
                Location = new Point(14, y),
                Size = new Size(248, 32),
                Image = RetroAssets.GetRunningManBitmap(16, "online")
            };
            _btnSignOn.Click += OnSignOnClick;
            AcceptButton = _btnSignOn;
            y += 40;

            _pnlConn = new Panel { Location = new Point(14, y), Size = new Size(248, 66), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Visible = false };
            _lblConn = new Label { Location = new Point(6, 6), Size = new Size(234, 16), Text = "1. Connecting...", Font = XpTheme.UiBold, ForeColor = XpTheme.Current.AimNavy };
            _prog = new XpProgressBar { Location = new Point(6, 26), Size = new Size(234, 15) };
            XpButton btnCancel = new XpButton { Text = "Cancel", Location = new Point(84, 44), Size = new Size(80, 19) };
            btnCancel.Click += delegate
            {
                if (_connTimer != null) _connTimer.Stop();
                _pnlConn.Visible = false;
                _btnSignOn.Enabled = true;
            };
            _pnlConn.Controls.Add(_lblConn);
            _pnlConn.Controls.Add(_prog);
            _pnlConn.Controls.Add(btnCancel);
            y += 72;

            XpButton btnAcc = new XpButton { Text = "Accounts...", Location = new Point(14, 304), Size = new Size(82, 23) };
            XpButton btnSetup = new XpButton { Text = "Setup", Location = new Point(102, 304), Size = new Size(76, 23) };
            XpButton btnHelp = new XpButton { Text = "Help", Location = new Point(184, 304), Size = new Size(78, 23) };
            btnAcc.Click += delegate { new AccountManagerForm("list").ShowDialog(this); };
            btnSetup.Click += delegate { new SettingsForm("connection").ShowDialog(this); };
            btnHelp.Click += delegate { new HelpAboutForm("help").ShowDialog(this); };

            Panel footer = new Panel { Dock = DockStyle.Bottom, Height = 24, BackColor = XpTheme.Current.FaceDark };
            Label lblFoot = new Label
            {
                Dock = DockStyle.Fill,
                Text = string.Format("  Server: {0}:{1}   |   v5.9.3861", Store.State.Settings.ServerHost, Store.State.Settings.ServerPort),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = XpTheme.SmallFont,
                ForeColor = Color.FromArgb(68, 68, 68)
            };
            footer.Controls.Add(lblFoot);
            ClientBody.Controls.Add(footer);

            body.Controls.Add(lblSn);
            body.Controls.Add(lnkReg);
            body.Controls.Add(_cboScreenName);
            body.Controls.Add(lblPw);
            body.Controls.Add(lnkForgot);
            body.Controls.Add(_txtPassword);
            body.Controls.Add(_chkSavePw);
            body.Controls.Add(_chkAuto);
            body.Controls.Add(_chkDialup);
            body.Controls.Add(lblSt);
            body.Controls.Add(_cboInitStatus);
            body.Controls.Add(_btnSignOn);
            body.Controls.Add(_pnlConn);
            body.Controls.Add(btnAcc);
            body.Controls.Add(btnSetup);
            body.Controls.Add(btnHelp);

            RefreshAccountDropdown();
        }

        private void RefreshAccountDropdown()
        {
            string prev = _cboScreenName.Text;
            _cboScreenName.Items.Clear();
            foreach (Account a in Store.State.Accounts)
            {
                _cboScreenName.Items.Add(a.ScreenName);
            }
            if (!string.IsNullOrEmpty(prev) && _cboScreenName.Items.Contains(prev))
                _cboScreenName.SelectedItem = prev;
            else
                _cboScreenName.SelectedItem = Store.GetActiveAccount().ScreenName;
            PopulateSelectedAccount();
        }

        private void PopulateSelectedAccount()
        {
            string sn = _cboScreenName.Text.Trim();
            Account acc = Store.State.Accounts.FirstOrDefault(a => string.Equals(a.ScreenName, sn, StringComparison.OrdinalIgnoreCase));
            if (acc != null)
            {
                _txtPassword.Text = acc.SavePassword ? acc.Password : "";
                _chkSavePw.Checked = acc.SavePassword;
                _chkAuto.Checked = acc.AutoLogin;
            }
        }

        private void OnSignOnClick(object sender, EventArgs e)
        {
            string sn = _cboScreenName.Text.Trim();
            string pw = _txtPassword.Text;

            if (sn.Length < 3)
            {
                XpMessageBox.Show(this, "Enter a Screen Name (at least 3 characters).", "Sign On Error", false);
                return;
            }
            if (string.IsNullOrEmpty(pw))
            {
                XpMessageBox.Show(this, "Enter your AIM password.", "Sign On Error", false);
                return;
            }

            Account acc = Store.State.Accounts.FirstOrDefault(a => string.Equals(a.ScreenName, sn, StringComparison.OrdinalIgnoreCase));
            if (acc == null)
            {
                DialogResult reg = XpMessageBox.Show(this,
                    string.Format("Screen Name \"{0}\" is not registered yet.\nRegister this new account now?", sn),
                    "New AIM Account", true);
                if (reg != DialogResult.Yes) return;
                string err = Store.RegisterAccount(sn, pw, sn, "", "AIM Icon.gif", "", "", "");
                if (err != null) { XpMessageBox.Show(this, err, "Registration Error", false); return; }
                acc = Store.State.Accounts.First(a => string.Equals(a.ScreenName, sn, StringComparison.OrdinalIgnoreCase));
            }
            else if (acc.Password != pw)
            {
                SoundEngine.Play("alert");
                XpMessageBox.Show(this, "Incorrect password for Screen Name \"" + acc.ScreenName + "\".\n(Use 'Forgot Password?' if needed.)", "Sign On Error", false);
                return;
            }

            acc.SavePassword = _chkSavePw.Checked;
            acc.AutoLogin = _chkAuto.Checked;
            Store.State.ActiveAccountSN = acc.ScreenName;
            string[] statuses = new[] { "online", "away", "busy", "invisible" };
            Store.State.CurrentUser.Status = statuses[Math.Max(0, _cboInitStatus.SelectedIndex)];

            _btnSignOn.Enabled = false;
            _pnlConn.Visible = true;
            _connStep = 0;
            _prog.Value = 15;
            _lblConn.Text = _chkDialup.Checked ? "1. Dialing 56k AOL Access..." : ("1. Connecting to " + Store.State.Settings.ServerHost + "...");
            if (_chkDialup.Checked) SoundEngine.Play("dialupModem");

            if (_connTimer != null) _connTimer.Dispose();
            _connTimer = new Timer { Interval = _chkDialup.Checked ? 1400 : 320 };
            _connTimer.Tick += delegate
            {
                _connStep++;
                if (_connStep == 1)
                {
                    _prog.Value = 55;
                    _lblConn.Text = "2. Verifying Screen Name & Password...";
                }
                else if (_connStep == 2)
                {
                    _prog.Value = 95;
                    _lblConn.Text = "3. Starting OSCAR services...";
                }
                else
                {
                    _connTimer.Stop();
                    _prog.Value = 100;
                    Store.State.LoggedIn = true;
                    Store.Notify("auth");
                    SoundEngine.Play("welcome");
                    AimApp.OpenBuddyList();
                    Close();
                }
            };
            _connTimer.Start();
        }
    }
}
