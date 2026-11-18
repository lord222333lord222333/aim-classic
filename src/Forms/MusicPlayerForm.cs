using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace AIMClassic
{
    public class MusicTrack
    {
        public string Title;
        public string Artist;
        public string FilePath;
        public bool IsBuiltIn;
    }

    public static class MusicEngine
    {
        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern int mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);

        private const string ALIAS = "aim_music_bgm";
        private static bool _initialized = false;
        private static readonly Random _rng = new Random();

        public static readonly List<MusicTrack> Tracks = new List<MusicTrack>();
        public static int CurrentIndex { get; private set; }
        public static bool IsPlaying { get; private set; }
        public static bool IsPaused { get; private set; }
        public static bool Shuffle { get; set; }
        public static bool RepeatAll { get; set; }

        public static event Action StateChanged;

        static MusicEngine()
        {
            RepeatAll = true;
            CurrentIndex = 0;
        }

        public static MusicTrack CurrentTrack
        {
            get
            {
                if (CurrentIndex >= 0 && CurrentIndex < Tracks.Count) return Tracks[CurrentIndex];
                return null;
            }
        }

        public static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            EnsureBuiltInTracks();

            if (Store.State.Settings.MusicPlaylistPaths != null)
            {
                foreach (string p in Store.State.Settings.MusicPlaylistPaths)
                {
                    if (!string.IsNullOrEmpty(p) && File.Exists(p))
                        AddTrackInternal(p, false);
                }
            }
        }

        public static void AddTrack(string path)
        {
            EnsureInitialized();
            if (AddTrackInternal(path, true))
            {
                Store.Save();
                NotifyChanged();
            }
        }

        private static bool AddTrackInternal(string path, bool persist)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
            if (Tracks.Any(t => string.Equals(t.FilePath, path, StringComparison.OrdinalIgnoreCase))) return false;
            string name = Path.GetFileNameWithoutExtension(path);
            string artist = "Local Library";
            string title = name;
            int dash = name.IndexOf(" - ");
            if (dash > 0)
            {
                artist = name.Substring(0, dash).Trim();
                title = name.Substring(dash + 3).Trim();
            }
            Tracks.Add(new MusicTrack { Title = title, Artist = artist, FilePath = path, IsBuiltIn = false });
            if (persist && !Store.State.Settings.MusicPlaylistPaths.Contains(path))
                Store.State.Settings.MusicPlaylistPaths.Add(path);
            return true;
        }

        public static void ClearUserTracks()
        {
            EnsureInitialized();
            bool wasUserPlaying = CurrentTrack != null && !CurrentTrack.IsBuiltIn && IsPlaying;
            if (wasUserPlaying) Stop();
            Tracks.RemoveAll(t => !t.IsBuiltIn);
            Store.State.Settings.MusicPlaylistPaths.Clear();
            Store.Save();
            if (CurrentIndex >= Tracks.Count) CurrentIndex = 0;
            NotifyChanged();
        }

        public static void Play(int index = -1)
        {
            EnsureInitialized();
            if (Tracks.Count == 0) return;

            if (index >= 0 && index < Tracks.Count)
            {
                CurrentIndex = index;
            }
            else if (IsPaused)
            {
                mciSendString("resume " + ALIAS, null, 0, IntPtr.Zero);
                IsPaused = false;
                IsPlaying = true;
                UpdateStatusShare();
                NotifyChanged();
                return;
            }

            if (CurrentIndex < 0 || CurrentIndex >= Tracks.Count) CurrentIndex = 0;
            MusicTrack tr = Tracks[CurrentIndex];
            if (tr == null || !File.Exists(tr.FilePath)) return;

            mciSendString("stop " + ALIAS, null, 0, IntPtr.Zero);
            mciSendString("close " + ALIAS, null, 0, IntPtr.Zero);

            string openCmd = string.Format("open \"{0}\" alias {1}", tr.FilePath, ALIAS);
            int rc = mciSendString(openCmd, null, 0, IntPtr.Zero);
            if (rc == 0)
            {
                mciSendString("set " + ALIAS + " time format milliseconds", null, 0, IntPtr.Zero);
                ApplyVolume(Store.State.Settings.MusicVolume);
                mciSendString("play " + ALIAS, null, 0, IntPtr.Zero);
                IsPlaying = true;
                IsPaused = false;
                UpdateStatusShare();
            }
            else
            {
                IsPlaying = false;
                IsPaused = false;
            }
            NotifyChanged();
        }

        public static void Pause()
        {
            if (!IsPlaying) return;
            if (IsPaused)
            {
                mciSendString("resume " + ALIAS, null, 0, IntPtr.Zero);
                IsPaused = false;
            }
            else
            {
                mciSendString("pause " + ALIAS, null, 0, IntPtr.Zero);
                IsPaused = true;
            }
            NotifyChanged();
        }

        public static void Stop()
        {
            mciSendString("stop " + ALIAS, null, 0, IntPtr.Zero);
            mciSendString("close " + ALIAS, null, 0, IntPtr.Zero);
            IsPlaying = false;
            IsPaused = false;
            NotifyChanged();
        }

        public static void PlayNext()
        {
            EnsureInitialized();
            if (Tracks.Count == 0) return;
            if (Shuffle && Tracks.Count > 1)
                CurrentIndex = _rng.Next(Tracks.Count);
            else
                CurrentIndex = (CurrentIndex + 1) % Tracks.Count;
            Play(CurrentIndex);
        }

        public static void PlayPrev()
        {
            EnsureInitialized();
            if (Tracks.Count == 0) return;
            CurrentIndex = (CurrentIndex - 1 + Tracks.Count) % Tracks.Count;
            Play(CurrentIndex);
        }

        public static void ApplyVolume(int percent)
        {
            int v = Math.Max(0, Math.Min(100, percent));
            Store.State.Settings.MusicVolume = v;
            mciSendString("setaudio " + ALIAS + " volume to " + (v * 10), null, 0, IntPtr.Zero);
        }

        public static void SeekRatio(double ratio)
        {
            int len = GetLengthMs();
            if (len <= 0) return;
            int target = Math.Max(0, Math.Min(len - 50, (int)(len * ratio)));
            mciSendString(string.Format("seek {0} to {1}", ALIAS, target), null, 0, IntPtr.Zero);
            if (IsPlaying && !IsPaused) mciSendString("play " + ALIAS, null, 0, IntPtr.Zero);
        }

        public static int GetPositionMs()
        {
            if (!IsPlaying && !IsPaused) return 0;
            StringBuilder sb = new StringBuilder(64);
            if (mciSendString("status " + ALIAS + " position", sb, sb.Capacity, IntPtr.Zero) == 0)
            {
                int ms;
                if (int.TryParse(sb.ToString().Trim(), out ms)) return ms;
            }
            return 0;
        }

        public static int GetLengthMs()
        {
            if (!IsPlaying && !IsPaused) return 0;
            StringBuilder sb = new StringBuilder(64);
            if (mciSendString("status " + ALIAS + " length", sb, sb.Capacity, IntPtr.Zero) == 0)
            {
                int ms;
                if (int.TryParse(sb.ToString().Trim(), out ms)) return ms;
            }
            return 0;
        }

        public static string GetMciMode()
        {
            StringBuilder sb = new StringBuilder(64);
            if (mciSendString("status " + ALIAS + " mode", sb, sb.Capacity, IntPtr.Zero) == 0)
                return sb.ToString().Trim().ToLowerInvariant();
            return "stopped";
        }

        public static void TickCheckAutoAdvance()
        {
            if (!IsPlaying || IsPaused) return;
            string mode = GetMciMode();
            int pos = GetPositionMs();
            int len = GetLengthMs();
            if (mode == "stopped" || (len > 200 && pos >= len - 80))
            {
                if (RepeatAll || Tracks.Count > 1) PlayNext();
                else Stop();
            }
        }

        private static void UpdateStatusShare()
        {
            if (!Store.State.Settings.ShareMusicInStatus || CurrentTrack == null) return;
            if (Store.State.CurrentUser != null)
            {
                Store.State.CurrentUser.CustomStatusNote = "♪ " + CurrentTrack.Title + " — " + CurrentTrack.Artist;
                Store.Notify("music");
                NetworkEngine.BroadcastStatusChange();
            }
        }

        private static void NotifyChanged()
        {
            Action ev = StateChanged;
            if (ev != null) ev();
        }

        private static void EnsureBuiltInTracks()
        {
            string dir = Path.Combine(Path.GetTempPath(), "AIMClassic_Music");
            try { Directory.CreateDirectory(dir); } catch { return; }

            var defs = new[]
            {
                new { File = "01_frutiger_aero_breeze.wav", Title = "Frutiger Aero Breeze", Artist = "Aqua Synth 2007", Style = 0 },
                new { File = "02_late_night_aim_2006.wav", Title = "Late Night AIM 2006", Artist = "Lo-Fi Buddy Chords", Style = 1 },
                new { File = "03_xp_welcome_dream.wav", Title = "XP Welcome Dream", Artist = "Luna Glass Pad", Style = 2 },
                new { File = "04_winamp_chip_groove.wav", Title = "Winamp Retro Groove", Artist = "2000s Chiptune", Style = 3 }
            };

            foreach (var d in defs)
            {
                string path = Path.Combine(dir, d.File);
                if (!File.Exists(path) || new FileInfo(path).Length < 4096)
                {
                    RenderSynthTrackWav(path, d.Style);
                }
                if (File.Exists(path))
                {
                    Tracks.Add(new MusicTrack { Title = d.Title, Artist = d.Artist, FilePath = path, IsBuiltIn = true });
                }
            }
        }

        private static void RenderSynthTrackWav(string path, int style)
        {
            try
            {
                const int sr = 22050;
                const int seconds = 18;
                int totalSamples = sr * seconds;
                byte[] pcm = new byte[totalSamples * 2];

                double[][] chords;
                if (style == 0)
                {
                    chords = new[] {
                        new[] { 261.63, 329.63, 392.00, 493.88 }, // Cmaj7
                        new[] { 174.61, 220.00, 261.63, 329.63 }, // Fmaj7
                        new[] { 220.00, 261.63, 329.63, 392.00 }, // Am7
                        new[] { 196.00, 246.94, 293.66, 392.00 }  // G6
                    };
                }
                else if (style == 1)
                {
                    chords = new[] {
                        new[] { 220.00, 261.63, 329.63, 392.00 }, // Am7
                        new[] { 196.00, 246.94, 293.66, 349.23 }, // G7
                        new[] { 174.61, 220.00, 261.63, 329.63 }, // Fmaj7
                        new[] { 164.81, 196.00, 246.94, 293.66 }  // Em7
                    };
                }
                else if (style == 2)
                {
                    chords = new[] {
                        new[] { 293.66, 369.99, 440.00, 554.37 }, // Dmaj7
                        new[] { 246.94, 293.66, 369.99, 440.00 }, // Bm7
                        new[] { 196.00, 246.94, 293.66, 369.99 }, // Gmaj7
                        new[] { 220.00, 277.18, 329.63, 392.00 }  // A7
                    };
                }
                else
                {
                    chords = new[] {
                        new[] { 261.63, 329.63, 392.00, 523.25 },
                        new[] { 220.00, 261.63, 329.63, 440.00 },
                        new[] { 174.61, 220.00, 261.63, 349.23 },
                        new[] { 196.00, 246.94, 293.66, 392.00 }
                    };
                }

                int chordLen = totalSamples / 4;
                int arpStep = sr / (style == 3 ? 8 : 4);
                for (int i = 0; i < totalSamples; i++)
                {
                    double t = (double)i / sr;
                    int bar = Math.Min(3, i / chordLen);
                    int barPos = i % chordLen;
                    double barProgress = (double)barPos / chordLen;
                    double env = Math.Sin(Math.PI * Math.Min(1.0, barProgress * 1.15)) * (1.0 - 0.12 * barProgress);

                    double[] ch = chords[bar];
                    double sum = 0;
                    for (int n = 0; n < ch.Length; n++)
                    {
                        double ph = 2.0 * Math.PI * ch[n] * t;
                        if (style == 3)
                            sum += 0.22 * Math.Sin(ph) + 0.06 * Math.Sign(Math.Sin(ph));
                        else
                            sum += 0.26 * Math.Sin(ph) + 0.08 * Math.Sin(ph * 2.0);
                    }

                    // Gentle bell/arpeggio overlay note
                    int arpIdx = (i / arpStep) % ch.Length;
                    double arpFreq = ch[arpIdx] * (style == 0 || style == 2 ? 2.0 : 1.0);
                    double arpEnv = Math.Exp(-((i % arpStep) / (double)sr) * (style == 3 ? 9.0 : 4.5));
                    sum += (style == 3 ? 0.18 : 0.15) * arpEnv * Math.Sin(2.0 * Math.PI * arpFreq * t);

                    // Master smooth fade-in/out on loop boundary
                    double loopEnv = 1.0;
                    if (i < sr / 2) loopEnv = i / (double)(sr / 2);
                    else if (i > totalSamples - sr / 2) loopEnv = (totalSamples - i) / (double)(sr / 2);

                    short sample = (short)Math.Max(-28000, Math.Min(28000, sum * env * loopEnv * 11500));
                    pcm[i * 2] = (byte)(sample & 0xFF);
                    pcm[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
                }

                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                    bw.Write(36 + pcm.Length);
                    bw.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                    bw.Write(16);
                    bw.Write((short)1);
                    bw.Write((short)1);
                    bw.Write(sr);
                    bw.Write(sr * 2);
                    bw.Write((short)2);
                    bw.Write((short)16);
                    bw.Write(Encoding.ASCII.GetBytes("data"));
                    bw.Write(pcm.Length);
                    bw.Write(pcm);
                }
            }
            catch { }
        }
    }

    /* ======================================================================
       Classic 2006 AIM / Winamp Retro Music Player Window
       ====================================================================== */
    public class MusicPlayerForm : XpForm
    {
        private Panel _oledDisplay;
        private Panel _seekTrack, _volTrack;
        private ListBox _lstTracks;
        private CheckBox _chkShareStatus, _chkShuffle, _chkRepeat;
        private System.Windows.Forms.Timer _uiTimer;
        private readonly float[] _bars = new float[18];
        private readonly Random _vizRng = new Random();
        private readonly Action _stateObs;

        public MusicPlayerForm()
        {
            Text = I18n.T("AIM® Music Player (2006)", "AIM® Музыкальный плеер (2006)");
            Size = new Size(466, 468);
            MinimumSize = new Size(420, 400);
            StartPosition = FormStartPosition.Manual;
            Location = new Point(180, 90);
            TitleIconImage = RetroAssets.GetIconBitmap("music", 16);

            MusicEngine.EnsureInitialized();

            XpBanner banner = new XpBanner
            {
                BannerTitle = I18n.T("AIM® Retro Music Player", "Музыкальный плеер AIM® Retro"),
                BannerSubtitle = I18n.T("Play built-in Frutiger Aero / 2006 synth tracks or your MP3 & WAV files", "Встроенные 2006 ретро-треки или ваши MP3 / WAV файлы"),
                BannerIcon = RetroAssets.GetIconBitmap("music", 26)
            };
            ClientBody.Controls.Add(banner);

            // Retro dark OLED Display with Spectrum Visualizer
            _oledDisplay = new Panel
            {
                Dock = DockStyle.Top,
                Height = 96,
                BackColor = Color.FromArgb(10, 20, 36)
            };
            _oledDisplay.Paint += OnPaintOled;

            // Seek bar & Volume strip
            Panel seekPanel = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = XpTheme.Current.SubBarTop, Padding = new Padding(8, 6, 8, 6) };
            _seekTrack = new Panel { Location = new Point(10, 8), Size = new Size(300, 16), Cursor = Cursors.Hand };
            _seekTrack.Paint += OnPaintSeekBar;
            _seekTrack.MouseDown += delegate(object s, MouseEventArgs e)
            {
                double ratio = Math.Max(0.0, Math.Min(1.0, (double)e.X / Math.Max(1, _seekTrack.Width)));
                MusicEngine.SeekRatio(ratio);
                _seekTrack.Invalidate();
            };

            Label lblVol = new Label { Text = "Vol:", Location = new Point(318, 9), AutoSize = true, Font = XpTheme.SmallFont, ForeColor = XpTheme.Current.AimNavy };
            _volTrack = new Panel { Location = new Point(346, 8), Size = new Size(96, 16), Cursor = Cursors.Hand };
            _volTrack.Paint += OnPaintVolBar;
            _volTrack.MouseDown += OnVolMouse;
            _volTrack.MouseMove += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left) OnVolMouse(s, e); };
            seekPanel.Controls.AddRange(new Control[] { _seekTrack, lblVol, _volTrack });

            // Transport & File Controls
            Panel transPanel = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = XpTheme.Current.Face };
            XpButton btnPrev = new XpButton { Text = "|◄", Location = new Point(10, 6), Size = new Size(46, 26) };
            XpButton btnPlay = new XpButton { Text = "▶ Play", IsPrimary = true, Location = new Point(62, 6), Size = new Size(76, 26) };
            XpButton btnPause = new XpButton { Text = "❚❚", Location = new Point(144, 6), Size = new Size(44, 26) };
            XpButton btnStop = new XpButton { Text = "■", Location = new Point(194, 6), Size = new Size(44, 26) };
            XpButton btnNext = new XpButton { Text = "►|", Location = new Point(244, 6), Size = new Size(46, 26) };

            _chkShuffle = new CheckBox { Text = I18n.T("Shuffle", "Случайно"), Location = new Point(300, 10), AutoSize = true, Checked = MusicEngine.Shuffle, Font = XpTheme.SmallFont };
            _chkRepeat = new CheckBox { Text = I18n.T("Repeat", "Повтор"), Location = new Point(376, 10), AutoSize = true, Checked = MusicEngine.RepeatAll, Font = XpTheme.SmallFont };
            _chkShuffle.CheckedChanged += delegate { MusicEngine.Shuffle = _chkShuffle.Checked; };
            _chkRepeat.CheckedChanged += delegate { MusicEngine.RepeatAll = _chkRepeat.Checked; };

            _chkShareStatus = new CheckBox
            {
                Text = I18n.T("Show '♪ Now Playing' in my AIM Mood Status", "Показывать '♪ Сейчас играет' в статусе AIM"),
                Location = new Point(12, 38),
                AutoSize = true,
                Checked = Store.State.Settings.ShareMusicInStatus,
                Font = XpTheme.UiBold,
                UseMnemonic = false
            };
            _chkShareStatus.CheckedChanged += delegate
            {
                Store.State.Settings.ShareMusicInStatus = _chkShareStatus.Checked;
                Store.Save();
            };

            btnPrev.Click += delegate { MusicEngine.PlayPrev(); };
            btnPlay.Click += delegate { MusicEngine.Play(_lstTracks.SelectedIndex >= 0 ? _lstTracks.SelectedIndex : MusicEngine.CurrentIndex); };
            btnPause.Click += delegate { MusicEngine.Pause(); };
            btnStop.Click += delegate { MusicEngine.Stop(); };
            btnNext.Click += delegate { MusicEngine.PlayNext(); };

            transPanel.Controls.AddRange(new Control[] { btnPrev, btnPlay, btnPause, btnStop, btnNext, _chkShuffle, _chkRepeat, _chkShareStatus });

            // Playlist Area
            Panel listWrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 4, 10, 6) };
            _lstTracks = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, Font = XpTheme.UiFont };
            _lstTracks.DoubleClick += delegate
            {
                if (_lstTracks.SelectedIndex >= 0) MusicEngine.Play(_lstTracks.SelectedIndex);
            };
            listWrap.Controls.Add(_lstTracks);

            // Bottom Footer
            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 38, Width = ClientBody.ClientSize.Width, BackColor = XpTheme.Current.FaceDark };
            XpButton btnAddFiles = new XpButton { Text = I18n.T("+ Add MP3 / WAV...", "+ Добавить MP3..."), Location = new Point(10, 7), Size = new Size(124, 24), Font = XpTheme.SmallFont };
            XpButton btnAddFolder = new XpButton { Text = I18n.T("+ Folder...", "+ Папка..."), Location = new Point(140, 7), Size = new Size(86, 24), Font = XpTheme.SmallFont };
            XpButton btnClear = new XpButton { Text = I18n.T("Clear Custom", "Очистить"), Location = new Point(232, 7), Size = new Size(92, 24), Font = XpTheme.SmallFont };
            XpButton btnClose = new XpButton { Text = I18n.T("Hide", "Скрыть"), Location = new Point(Math.Max(332, ClientBody.ClientSize.Width - 88), 7), Size = new Size(78, 24), Anchor = AnchorStyles.Top | AnchorStyles.Right };

            btnAddFiles.Click += delegate
            {
                using (OpenFileDialog ofd = new OpenFileDialog { Multiselect = true, Filter = "Audio Files|*.mp3;*.wav;*.wma;*.mid|All Files|*.*" })
                {
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        foreach (string fn in ofd.FileNames) MusicEngine.AddTrack(fn);
                        RefreshPlaylist();
                    }
                }
            };
            btnAddFolder.Click += delegate
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    if (fbd.ShowDialog(this) == DialogResult.OK)
                    {
                        foreach (string ext in new[] { "*.mp3", "*.wav", "*.wma" })
                        {
                            foreach (string fn in Directory.GetFiles(fbd.SelectedPath, ext))
                                MusicEngine.AddTrack(fn);
                        }
                        RefreshPlaylist();
                    }
                }
            };
            btnClear.Click += delegate { MusicEngine.ClearUserTracks(); RefreshPlaylist(); };
            btnClose.Click += delegate { Hide(); };
            foot.Controls.AddRange(new Control[] { btnAddFiles, btnAddFolder, btnClear, btnClose });

            ClientBody.Controls.Add(listWrap);
            ClientBody.Controls.Add(transPanel);
            ClientBody.Controls.Add(seekPanel);
            ClientBody.Controls.Add(_oledDisplay);
            ClientBody.Controls.Add(foot);
            listWrap.BringToFront();

            _uiTimer = new System.Windows.Forms.Timer { Interval = 110 };
            _uiTimer.Tick += delegate
            {
                MusicEngine.TickCheckAutoAdvance();
                for (int i = 0; i < _bars.Length; i++)
                {
                    if (MusicEngine.IsPlaying && !MusicEngine.IsPaused)
                        _bars[i] = _bars[i] * 0.45f + (float)(_vizRng.NextDouble() * 0.95) * 0.55f;
                    else
                        _bars[i] *= 0.72f;
                }
                _oledDisplay.Invalidate();
                _seekTrack.Invalidate();
            };
            _uiTimer.Start();

            _stateObs = delegate
            {
                if (!IsDisposed)
                {
                    try { BeginInvoke((Action)RefreshPlaylist); } catch { }
                }
            };
            MusicEngine.StateChanged += _stateObs;
            FormClosing += delegate(object s, FormClosingEventArgs e)
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    Hide();
                }
            };
            FormClosed += delegate
            {
                MusicEngine.StateChanged -= _stateObs;
                if (_uiTimer != null) _uiTimer.Dispose();
            };

            RefreshPlaylist();
        }

        private void OnVolMouse(object s, MouseEventArgs e)
        {
            int pct = (int)Math.Round(Math.Max(0.0, Math.Min(1.0, (double)e.X / Math.Max(1, _volTrack.Width))) * 100.0);
            MusicEngine.ApplyVolume(pct);
            _volTrack.Invalidate();
        }

        private void RefreshPlaylist()
        {
            _lstTracks.BeginUpdate();
            _lstTracks.Items.Clear();
            for (int i = 0; i < MusicEngine.Tracks.Count; i++)
            {
                MusicTrack t = MusicEngine.Tracks[i];
                string mark = (i == MusicEngine.CurrentIndex && MusicEngine.IsPlaying) ? (MusicEngine.IsPaused ? "❚❚ " : "▶ ") : "   ";
                _lstTracks.Items.Add(string.Format("{0}{1:00}. {2} — {3}", mark, i + 1, t.Artist, t.Title));
            }
            if (MusicEngine.CurrentIndex >= 0 && MusicEngine.CurrentIndex < _lstTracks.Items.Count)
                _lstTracks.SelectedIndex = MusicEngine.CurrentIndex;
            _lstTracks.EndUpdate();
            _oledDisplay.Invalidate();
            _volTrack.Invalidate();
        }

        private void OnPaintOled(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int w = _oledDisplay.Width;
            int h = _oledDisplay.Height;
            using (LinearGradientBrush br = new LinearGradientBrush(new Rectangle(0, 0, w, h),
                Color.FromArgb(10, 22, 42), Color.FromArgb(18, 52, 84), LinearGradientMode.Vertical))
            {
                g.FillRectangle(br, 0, 0, w, h);
            }
            using (SolidBrush sh = new SolidBrush(Color.FromArgb(24, 255, 255, 255)))
                g.FillRectangle(sh, 0, 0, w, h / 2);

            MusicTrack cur = MusicEngine.CurrentTrack;
            string title = cur != null ? cur.Title : "No Track Loaded";
            string artist = cur != null ? cur.Artist : "Click + Add MP3 or Play Synth";
            string stateBadge = MusicEngine.IsPlaying ? (MusicEngine.IsPaused ? "PAUSED" : "PLAYING ♪") : "READY";

            int pos = MusicEngine.GetPositionMs() / 1000;
            int len = MusicEngine.GetLengthMs() / 1000;
            string clock = string.Format("{0:00}:{1:00} / {2:00}:{3:00}", pos / 60, pos % 60, len / 60, len % 60);

            TextRenderer.DrawText(g, stateBadge, new Font("Tahoma", 7.25f, FontStyle.Bold), new Point(12, 8),
                MusicEngine.IsPlaying ? Color.FromArgb(118, 255, 168) : Color.FromArgb(148, 212, 255), TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, clock, new Font("Consolas", 10.5f, FontStyle.Bold), new Rectangle(w - 135, 6, 123, 20),
                Color.FromArgb(124, 248, 255), TextFormatFlags.Right | TextFormatFlags.NoPrefix);

            TextRenderer.DrawText(g, title, new Font("Tahoma", 10.5f, FontStyle.Bold), new Rectangle(12, 28, w - 200, 20),
                Color.White, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, artist, XpTheme.UiFont, new Rectangle(12, 50, w - 200, 18),
                Color.FromArgb(164, 226, 255), TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            // Animated Equalizer Spectrum Bars on right side of OLED
            int eqX = w - 184;
            int eqY = 30;
            int eqH = 54;
            for (int i = 0; i < _bars.Length; i++)
            {
                int bh = Math.Max(3, (int)(_bars[i] * eqH));
                Rectangle brRect = new Rectangle(eqX + i * 9, eqY + eqH - bh, 7, bh);
                using (LinearGradientBrush bEq = new LinearGradientBrush(
                    new Rectangle(eqX + i * 9, eqY, 7, eqH),
                    Color.FromArgb(110, 255, 224),
                    Color.FromArgb(20, 178, 92),
                    LinearGradientMode.Vertical))
                {
                    g.FillRectangle(bEq, brRect);
                }
            }
        }

        private void OnPaintSeekBar(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int w = _seekTrack.Width;
            int h = _seekTrack.Height;
            g.FillRectangle(Brushes.White, 0, 4, w - 1, h - 8);
            g.DrawRectangle(Pens.SteelBlue, 0, 4, w - 1, h - 8);
            int len = MusicEngine.GetLengthMs();
            int pos = MusicEngine.GetPositionMs();
            double ratio = len > 0 ? Math.Max(0.0, Math.Min(1.0, (double)pos / len)) : 0.0;
            int fillW = (int)((w - 4) * ratio);
            if (fillW > 0)
            {
                using (LinearGradientBrush br = new LinearGradientBrush(new Rectangle(2, 6, fillW, h - 11),
                    Color.FromArgb(72, 210, 255), Color.FromArgb(18, 134, 210), LinearGradientMode.Vertical))
                    g.FillRectangle(br, 2, 6, fillW, h - 11);
            }
        }

        private void OnPaintVolBar(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int w = _volTrack.Width;
            int h = _volTrack.Height;
            g.FillRectangle(Brushes.White, 0, 4, w - 1, h - 8);
            g.DrawRectangle(Pens.SteelBlue, 0, 4, w - 1, h - 8);
            int volW = (int)((w - 4) * (Math.Max(0, Math.Min(100, Store.State.Settings.MusicVolume)) / 100.0));
            if (volW > 0)
            {
                using (LinearGradientBrush br = new LinearGradientBrush(new Rectangle(2, 6, volW, h - 11),
                    Color.FromArgb(110, 232, 118), Color.FromArgb(30, 152, 48), LinearGradientMode.Vertical))
                    g.FillRectangle(br, 2, 6, volW, h - 11);
            }
        }
    }
}
