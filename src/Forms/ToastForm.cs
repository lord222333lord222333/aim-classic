using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AIMClassic
{
    public class ToastForm : Form
    {
        private readonly Action _onClick;
        private readonly Timer _timer;
        private static int _activeCount = 0;

        public ToastForm(string title, string message, string avatarKey, Action onClick)
        {
            _onClick = onClick;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            Size = new Size(256, 86);
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            Font = XpTheme.UiFont;

            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int offset = (_activeCount % 4) * 92;
            Location = new Point(wa.Right - Width - 10, wa.Bottom - Height - 10 - offset);
            _activeCount++;

            Panel head = new Panel { Dock = DockStyle.Top, Height = 21, BackColor = XpTheme.Current.AimHeaderTop };
            Label lblHead = new Label { Text = "AIM® Notification", ForeColor = Color.White, Font = XpTheme.SmallFont, Location = new Point(6, 3), AutoSize = true };
            Label btnX = new Label { Text = "×", ForeColor = Color.White, Font = XpTheme.UiBold, Location = new Point(Width - 18, 2), Size = new Size(16, 16), Cursor = Cursors.Hand };
            btnX.Click += delegate { Close(); };
            head.Controls.Add(lblHead);
            head.Controls.Add(btnX);

            PictureBox pb = new PictureBox { Location = new Point(8, 30), Size = new Size(42, 42), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
            RetroAssets.SetPictureBoxAvatar(pb, avatarKey);

            Label lblTitle = new Label { Text = title, ForeColor = Color.FromArgb(0, 51, 153), Font = XpTheme.UiBold, Location = new Point(58, 28), Size = new Size(188, 17), AutoEllipsis = true };
            Label lblMsg = new Label { Text = message, ForeColor = Color.FromArgb(34, 34, 34), Font = XpTheme.UiFont, Location = new Point(58, 46), Size = new Size(188, 32), AutoEllipsis = true };

            EventHandler clickAct = delegate
            {
                Close();
                if (_onClick != null) _onClick();
            };

            pb.Click += clickAct;
            lblTitle.Click += clickAct;
            lblMsg.Click += clickAct;
            Click += clickAct;

            Controls.Add(head);
            Controls.Add(pb);
            Controls.Add(lblTitle);
            Controls.Add(lblMsg);

            _timer = new Timer { Interval = 5200 };
            _timer.Tick += delegate { _timer.Stop(); Close(); };
            _timer.Start();

            FormClosed += delegate { _activeCount = Math.Max(0, _activeCount - 1); _timer.Dispose(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Rectangle r = new Rectangle(0, 0, Width, Height);
            using (LinearGradientBrush br = new LinearGradientBrush(r, Color.White, Color.FromArgb(227, 239, 252), LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(br, r);
            }
            using (Pen p = new Pen(Color.FromArgb(28, 85, 181), 2))
            {
                e.Graphics.DrawRectangle(p, 1, 1, Width - 2, Height - 2);
            }
        }
    }

    public static class AimApp
    {
        public static LoginForm LoginWin;
        public static BuddyListForm BuddyWin;
        public static GroupChatForm GroupChatWin;
        public static XpDesktopBackdropForm BackdropWin;
        public static readonly Dictionary<string, PrivateChatForm> ImWindows = new Dictionary<string, PrivateChatForm>(StringComparer.OrdinalIgnoreCase);

        public static void Start()
        {
            if (Store.State.Settings.ShowXpBackdrop) EnsureBackdrop(true);
            if (Store.State.LoggedIn)
            {
                OpenBuddyList();
            }
            else
            {
                OpenLogin();
            }
        }

        public static void EnsureBackdrop(bool show)
        {
            if (show)
            {
                if (BackdropWin == null || BackdropWin.IsDisposed)
                {
                    BackdropWin = new XpDesktopBackdropForm();
                    BackdropWin.Show();
                }
                else
                {
                    BackdropWin.Show();
                }
            }
            else if (BackdropWin != null && !BackdropWin.IsDisposed)
            {
                BackdropWin.Close();
                BackdropWin = null;
            }
        }

        public static void OpenLogin()
        {
            if (LoginWin == null || LoginWin.IsDisposed)
            {
                LoginWin = new LoginForm();
                LoginWin.Show();
            }
            else
            {
                LoginWin.Show();
                LoginWin.BringToFront();
            }
        }

        public static void MarshalToUi(Action act)
        {
            if (act == null) return;
            Form target = (BuddyWin != null && !BuddyWin.IsDisposed) ? (Form)BuddyWin
                : ((LoginWin != null && !LoginWin.IsDisposed) ? (Form)LoginWin
                : (Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null));
            if (target != null && !target.IsDisposed && target.IsHandleCreated)
            {
                try
                {
                    if (target.InvokeRequired) target.BeginInvoke(act);
                    else act();
                }
                catch { }
            }
        }

        public static void OpenBuddyList()
        {
            NetworkEngine.Start();
            if (BuddyWin == null || BuddyWin.IsDisposed)
            {
                BuddyWin = new BuddyListForm();
                BuddyWin.Show();
            }
            else
            {
                BuddyWin.Show();
                if (BuddyWin.WindowState == FormWindowState.Minimized) BuddyWin.WindowState = FormWindowState.Normal;
                BuddyWin.BringToFront();
            }
        }

        public static void SignOff()
        {
            NetworkEngine.Stop();
            Store.State.LoggedIn = false;
            SoundEngine.Play("goodbye");
            foreach (PrivateChatForm f in new List<PrivateChatForm>(ImWindows.Values))
            {
                if (!f.IsDisposed) f.Close();
            }
            ImWindows.Clear();
            if (GroupChatWin != null && !GroupChatWin.IsDisposed) GroupChatWin.Close();
            if (BuddyWin != null && !BuddyWin.IsDisposed) BuddyWin.CloseReal();
            Store.Notify("auth");
            OpenLogin();
        }

        public static PrivateChatForm OpenPrivateChat(string buddySN)
        {
            Store.State.UnreadBuddies.Remove(buddySN);
            PrivateChatForm form;
            if (!ImWindows.TryGetValue(buddySN, out form) || form.IsDisposed)
            {
                form = new PrivateChatForm(buddySN);
                ImWindows[buddySN] = form;
                form.FormClosed += delegate { ImWindows.Remove(buddySN); };
                form.Show();
            }
            else
            {
                form.Show();
                if (form.WindowState == FormWindowState.Minimized) form.WindowState = FormWindowState.Normal;
                form.BringToFront();
            }
            if (BuddyWin != null && !BuddyWin.IsDisposed) BuddyWin.RefreshBuddyTree();
            return form;
        }

        public static void ReceiveIncomingIM(string buddySN, string text)
        {
            Buddy b = Store.GetBuddy(buddySN);
            if (b != null && b.Blocked) return;

            PrivateChatForm openWin;
            bool hasOpen = ImWindows.TryGetValue(buddySN, out openWin) && !openWin.IsDisposed;

            Store.AppendChatMessage(buddySN, new ChatMessage
            {
                Sender = buddySN,
                Text = text,
                Color = "#000099"
            });
            SoundEngine.Play(hasOpen ? "imReceive" : "imNew");

            if (hasOpen)
            {
                openWin.ReloadTranscript();
            }
            else if (Store.State.Settings.AutoPopupIM)
            {
                OpenPrivateChat(buddySN);
            }
            else
            {
                Store.State.UnreadBuddies[buddySN] = true;
                if (BuddyWin != null && !BuddyWin.IsDisposed) BuddyWin.RefreshBuddyTree();
            }

            if (Store.State.CurrentUser.Status != "busy")
            {
                ShowToast("IM from " + buddySN, text, b != null ? b.Avatar : "AIM Icon.gif", delegate { OpenPrivateChat(buddySN); });
            }

            if (Store.State.Settings.AutoTranslateChat && !string.IsNullOrWhiteSpace(text))
            {
                string targetLang = Store.State.Settings.TranslateTargetLang ?? "en";
                System.Threading.ThreadPool.QueueUserWorkItem(delegate
                {
                    string method;
                    string tr = I18n.TranslateMessage(text, targetLang, out method);
                    if (!string.IsNullOrWhiteSpace(tr) && !string.Equals(tr.Trim(), text.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        MarshalToUi(delegate
                        {
                            Store.AppendChatMessage(buddySN, new ChatMessage
                            {
                                Sender = "SYSTEM",
                                Text = string.Format("[Auto-Translate -> {0}] {1}", targetLang.ToUpperInvariant(), tr)
                            });
                            PrivateChatForm ow;
                            if (ImWindows.TryGetValue(buddySN, out ow) && !ow.IsDisposed) ow.ReloadTranscript();
                        });
                    }
                });
            }

            if (Store.State.CurrentUser.Status == "away")
            {
                Account me = Store.GetActiveAccount();
                string autoTxt = (Store.State.CurrentUser.AwayMessageText ?? "I am Away.")
                    .Replace("%t", Store.NowTimeStr())
                    .Replace("%n", buddySN);
                Store.AppendChatMessage(buddySN, new ChatMessage
                {
                    Sender = me.ScreenName,
                    Text = "[Auto-Response] " + autoTxt,
                    Color = "#7A5800",
                    Italic = true
                });
                if (ImWindows.TryGetValue(buddySN, out openWin) && !openWin.IsDisposed) openWin.ReloadTranscript();
            }
        }

        public static void OpenGroupChat(string inviteSN = null, ChatRoom targetRoom = null)
        {
            if (GroupChatWin == null || GroupChatWin.IsDisposed)
            {
                GroupChatWin = new GroupChatForm();
                GroupChatWin.Show();
            }
            else
            {
                GroupChatWin.Show();
                GroupChatWin.BringToFront();
            }
            if (targetRoom != null) GroupChatWin.SelectOrAddRoom(targetRoom);
            if (!string.IsNullOrEmpty(inviteSN)) GroupChatWin.InviteBuddy(inviteSN);
        }

        public static void OpenPublicGroupsHub(Form owner = null)
        {
            using (PublicGroupsForm hub = new PublicGroupsForm())
            {
                DialogResult res = owner != null ? hub.ShowDialog(owner) : hub.ShowDialog();
                if (res == DialogResult.OK && hub.JoinedRoom != null)
                {
                    OpenGroupChat(null, hub.JoinedRoom);
                }
            }
        }

        public static MusicPlayerForm MusicWin;
        public static void OpenMusicPlayer(Form owner = null)
        {
            if (MusicWin == null || MusicWin.IsDisposed)
            {
                MusicWin = new MusicPlayerForm();
                if (owner != null) MusicWin.Show(owner);
                else MusicWin.Show();
            }
            else
            {
                MusicWin.Show();
                MusicWin.BringToFront();
            }
        }

        public static Form MailInboxWin;
        public static void OpenMailInbox(Form owner = null)
        {
            if (MailInboxWin == null || MailInboxWin.IsDisposed)
            {
                MailInboxWin = new AimMailForm();
                if (owner != null) MailInboxWin.Show(owner);
                else MailInboxWin.Show();
            }
            else
            {
                MailInboxWin.Show();
                MailInboxWin.BringToFront();
            }
        }

        public static MobileBridgeForm MobileWin;
        public static void OpenMobileBridge(Form owner = null)
        {
            if (MobileWin == null || MobileWin.IsDisposed)
            {
                MobileWin = new MobileBridgeForm();
                if (owner != null) MobileWin.Show(owner);
                else MobileWin.Show();
            }
            else
            {
                MobileWin.Show();
                MobileWin.BringToFront();
            }
        }

        public static void ShowToast(string title, string message, string avatarKey, Action onClick)
        {
            if (Store.State.CurrentUser.Status == "busy") return;
            ToastForm tf = new ToastForm(title, message, avatarKey, onClick);
            tf.Show();
        }

        public static void ApplyThemeToAllForms()
        {
            EnsureBackdrop(Store.State.Settings.ShowXpBackdrop);
            foreach (Form f in Application.OpenForms)
            {
                XpForm xf = f as XpForm;
                if (xf != null) xf.ApplyThemePalette();
            }
        }
    }

    /* Optional 1024x768 Windows XP Bliss Desktop Backdrop Form */
    public class XpDesktopBackdropForm : XpForm
    {
        public XpDesktopBackdropForm()
        {
            Text = "Windows XP Classic Workspace (1024×768)";
            Size = new Size(1024, 768);
            StartPosition = FormStartPosition.CenterScreen;
            AllowResize = false;
            TitleIconImage = RetroAssets.GetRunningManBitmap(16, "online");

            Panel stage = new Panel { Dock = DockStyle.Fill };
            stage.Paint += PaintBliss;
            ClientBody.Controls.Add(stage);
        }

        private void PaintBliss(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int w = ((Control)sender).Width;
            int h = ((Control)sender).Height;
            ThemePalette pal = XpTheme.Current;

            if (pal.Name == "FrutigerAero" || pal.IsGlass)
            {
                Bitmap aeroWall = RetroAssets.GetEmbeddedBannerImage("frutiger_aero_backdrop.jpg");
                if (aeroWall != null)
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(aeroWall, new Rectangle(0, 0, w, h));
                    using (LinearGradientBrush glass = new LinearGradientBrush(
                        new Rectangle(0, 0, w, 34),
                        Color.FromArgb(95, 255, 255, 255),
                        Color.FromArgb(15, 200, 245, 255),
                        LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(glass, 0, 0, w, 34);
                    }
                    using (SolidBrush brSh = new SolidBrush(Color.FromArgb(145, 0, 32, 64)))
                        g.DrawString("Frutiger Aero 2007 — Glossy Aqua Workspace", XpTheme.UiBold, brSh, 15, h - 25);
                    g.DrawString("Frutiger Aero 2007 — Glossy Aqua Workspace", XpTheme.UiBold, Brushes.White, 14, h - 26);
                    return;
                }
            }

            using (LinearGradientBrush sky = new LinearGradientBrush(new Rectangle(0, 0, w, h), Color.FromArgb(30, 102, 200), Color.FromArgb(207, 232, 255), LinearGradientMode.Vertical))
            {
                g.FillRectangle(sky, 0, 0, w, h);
            }
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Brush cloud = new SolidBrush(Color.FromArgb(190, 255, 255, 255)))
            {
                g.FillEllipse(cloud, 90, 75, 280, 75);
                g.FillEllipse(cloud, 580, 55, 340, 88);
            }
            using (GraphicsPath hill1 = new GraphicsPath())
            {
                hill1.AddBezier(0, h * 0.62f, w * 0.3f, h * 0.48f, w * 0.65f, h * 0.64f, w, h * 0.56f);
                hill1.AddLine(w, h, 0, h);
                hill1.CloseFigure();
                using (LinearGradientBrush hb = new LinearGradientBrush(new Rectangle(0, (int)(h * 0.45), w, (int)(h * 0.55)), Color.FromArgb(118, 209, 56), Color.FromArgb(40, 118, 11), LinearGradientMode.Vertical))
                {
                    g.FillPath(hb, hill1);
                }
            }
        }
    }
}
