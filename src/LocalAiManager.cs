using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace AIMClassic
{
    public class LocalBrainPreset
    {
        public string Tag;
        public string DisplayName;
        public string SizeLabel;
        public string DescEn;
        public string DescRu;
    }

    public static class LocalAiManager
    {
        private static readonly JavaScriptSerializer _json = new JavaScriptSerializer { MaxJsonLength = 10485760 };
        private static readonly object _probeLock = new object();

        public const string OLLAMA_SETUP_PRIMARY_URL = "https://ollama.com/download/OllamaSetup.exe";
        public const string OLLAMA_SETUP_MIRROR_URL = "https://github.com/ollama/ollama/releases/latest/download/OllamaSetup.exe";
        public const string OLLAMA_BASE_URL = "http://127.0.0.1:11434";

        public static string ActiveBaseUrl { get; private set; }
        public static string LastProbeError { get; private set; }
        public static bool CachedRunning { get; private set; }
        public static List<string> CachedModels { get; private set; }
        public static bool PromptShownThisSession { get; set; }

        static LocalAiManager()
        {
            ActiveBaseUrl = OLLAMA_BASE_URL;
            CachedModels = new List<string>();
        }

        public static readonly LocalBrainPreset[] RecommendedBrains = new LocalBrainPreset[]
        {
            new LocalBrainPreset { Tag = "qwen3:1.7b", DisplayName = "Qwen 3 1.7B [Recommended RU+EN]", SizeLabel = "~1.2 GB", DescEn = "Best compact bilingual (Russian + English) brain for coding & work", DescRu = "Лучший компактный мозг (Русский + English + код) для работы" },
            new LocalBrainPreset { Tag = "gemma3:1b", DisplayName = "Google Gemma 3 1B [Lightest]", SizeLabel = "~815 MB", DescEn = "Smallest & fastest Google local brain — runs smoothly on older PCs", DescRu = "Самая лёгкая и быстрая модель от Google (~815 МБ)" },
            new LocalBrainPreset { Tag = "llama3.2:1b", DisplayName = "Meta Llama 3.2 1B [Fast]", SizeLabel = "~1.3 GB", DescEn = "Official Meta Llama compact fast assistant", DescRu = "Официальная быстрая Meta Llama 3.2 (1.3 ГБ)" },
            new LocalBrainPreset { Tag = "deepseek-r1:1.5b", DisplayName = "DeepSeek R1 1.5B [Reasoning]", SizeLabel = "~1.1 GB", DescEn = "Compact thinking brain for math, logic & step-by-step coding", DescRu = "Думающий мозг DeepSeek R1 для логики, математики и кода" },
            new LocalBrainPreset { Tag = "llama3.2:3b", DisplayName = "Meta Llama 3.2 3B [Balanced]", SizeLabel = "~2.0 GB", DescEn = "Higher quality Meta Llama brain (needs 4+ GB free RAM)", DescRu = "Умная сбалансированная Llama 3.2 3B (нужно от 4 ГБ ОЗУ)" },
            new LocalBrainPreset { Tag = "qwen3:4b", DisplayName = "Qwen 3 4B [Pro Work]", SizeLabel = "~2.6 GB", DescEn = "Strong local workstation brain for serious tasks (needs 6+ GB RAM)", DescRu = "Мощный рабочий мозг Qwen 3 4B (нужно от 6 ГБ ОЗУ)" },
            new LocalBrainPreset { Tag = "llama3.3", DisplayName = "Meta Llama 3.3 70B [High-End]", SizeLabel = "~42 GB", DescEn = "Server / workstation-class giant model (requires 48+ GB RAM/VRAM)", DescRu = "Огромная серверная модель (нужно 48+ ГБ ОЗУ/видеопамяти)" }
        };

        public static string FindOllamaExecutable()
        {
            try
            {
                string localProg = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

                string[] candidates = new string[]
                {
                    Path.Combine(localProg, "Programs", "Ollama", "ollama.exe"),
                    Path.Combine(progFiles, "Ollama", "ollama.exe"),
                    Path.Combine(progFilesX86, "Ollama", "ollama.exe")
                };
                foreach (string c in candidates)
                {
                    if (!string.IsNullOrEmpty(c) && File.Exists(c)) return c;
                }

                string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
                foreach (string dir in pathEnv.Split(Path.PathSeparator))
                {
                    if (string.IsNullOrWhiteSpace(dir)) continue;
                    string probe = Path.Combine(dir.Trim(), "ollama.exe");
                    if (File.Exists(probe)) return probe;
                }
            }
            catch { }
            return null;
        }

        private static List<string> CandidateBaseUrls()
        {
            List<string> urls = new List<string>();
            urls.Add("http://127.0.0.1:11434");
            string envHost = (Environment.GetEnvironmentVariable("OLLAMA_HOST") ?? "").Trim();
            if (!string.IsNullOrEmpty(envHost))
            {
                string h = envHost;
                if (!h.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !h.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    h = "http://" + h;
                if (!h.Contains(":") || h.LastIndexOf(':') < 6) h += ":11434";
                h = h.Replace("0.0.0.0", "127.0.0.1").TrimEnd('/');
                if (!urls.Contains(h)) urls.Insert(0, h);
            }
            if (!urls.Contains("http://localhost:11434")) urls.Add("http://localhost:11434");
            return urls;
        }

        public static bool ProbeOllama(int timeoutMs, out List<string> installedModels)
        {
            installedModels = new List<string>();
            int effectiveTimeout = Math.Max(1500, timeoutMs);
            string lastErr = "";

            foreach (string baseUrl in CandidateBaseUrls())
            {
                try
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(baseUrl + "/api/tags");
                    req.Method = "GET";
                    req.Proxy = null;
                    req.Timeout = effectiveTimeout;
                    req.ReadWriteTimeout = effectiveTimeout;
                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    {
                        string raw = sr.ReadToEnd();
                        Dictionary<string, object> doc = _json.Deserialize<Dictionary<string, object>>(raw);
                        if (doc != null && doc.ContainsKey("models"))
                        {
                            object[] arr = doc["models"] as object[];
                            if (arr != null)
                            {
                                foreach (object item in arr)
                                {
                                    Dictionary<string, object> md = item as Dictionary<string, object>;
                                    if (md != null && md.ContainsKey("name"))
                                    {
                                        string n = Convert.ToString(md["name"]);
                                        if (!string.IsNullOrWhiteSpace(n) && !installedModels.Contains(n))
                                            installedModels.Add(n);
                                    }
                                }
                            }
                        }
                    }
                    lock (_probeLock)
                    {
                        ActiveBaseUrl = baseUrl;
                        LastProbeError = "";
                        CachedRunning = true;
                        CachedModels = new List<string>(installedModels);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    lastErr = baseUrl + ": " + ex.Message;
                }
            }

            lock (_probeLock)
            {
                CachedRunning = false;
                LastProbeError = lastErr;
            }
            return false;
        }

        public static void ProbeOllamaAsync(Action onDone)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                List<string> mods;
                ProbeOllama(1500, out mods);
                if (onDone != null) onDone();
            });
        }

        public static bool TryStartOllamaDaemon(out string message)
        {
            List<string> mods;
            if (ProbeOllama(1500, out mods))
            {
                message = "Ollama is running on " + ActiveBaseUrl;
                return true;
            }
            string exe = FindOllamaExecutable();
            if (string.IsNullOrEmpty(exe))
            {
                message = "Ollama executable not found on this PC.";
                return false;
            }
            string exeDir = Path.GetDirectoryName(exe);
            StringBuilder errLog = new StringBuilder();
            bool bindBusy = false;

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = "serve",
                    WorkingDirectory = exeDir ?? "",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
                Process proc = new Process { StartInfo = psi };
                proc.ErrorDataReceived += delegate(object s, DataReceivedEventArgs ev)
                {
                    if (!string.IsNullOrEmpty(ev.Data))
                    {
                        lock (errLog) errLog.AppendLine(ev.Data);
                        if (ev.Data.IndexOf("Only one usage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            ev.Data.IndexOf("address already in use", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            bindBusy = true;
                        }
                    }
                };
                proc.OutputDataReceived += delegate { };
                proc.Start();
                proc.BeginErrorReadLine();
                proc.BeginOutputReadLine();

                for (int i = 0; i < 20; i++)
                {
                    Thread.Sleep(600);
                    if (ProbeOllama(1500, out mods))
                    {
                        message = "Started Ollama server on " + ActiveBaseUrl;
                        return true;
                    }
                    if (bindBusy && ProbeOllama(2200, out mods))
                    {
                        message = "Connected to running Ollama server on " + ActiveBaseUrl;
                        return true;
                    }
                    if (proc.HasExited && !bindBusy)
                        break;
                }

                // Fallback: try launching GUI tray launcher ("ollama app.exe") if present
                string trayExe = string.IsNullOrEmpty(exeDir) ? "" : Path.Combine(exeDir, "ollama app.exe");
                if (!string.IsNullOrEmpty(trayExe) && File.Exists(trayExe))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = trayExe, WorkingDirectory = exeDir, UseShellExecute = true });
                        for (int k = 0; k < 12; k++)
                        {
                            Thread.Sleep(650);
                            if (ProbeOllama(1500, out mods))
                            {
                                message = "Started Ollama App on " + ActiveBaseUrl;
                                return true;
                            }
                        }
                    }
                    catch { }
                }

                string capturedErr;
                lock (errLog) capturedErr = errLog.ToString().Trim();
                if (!string.IsNullOrEmpty(capturedErr))
                {
                    if (capturedErr.Length > 140) capturedErr = capturedErr.Substring(0, 140) + "...";
                    message = "Ollama could not bind/respond (" + capturedErr + ")";
                }
                else
                {
                    message = "Ollama did not respond on 127.0.0.1:11434 (" + (LastProbeError ?? "timeout") + ")";
                }
                return false;
            }
            catch (Exception ex)
            {
                message = "Failed to launch Ollama: " + ex.Message;
                return false;
            }
        }

        public static void ActivateLocalOllamaProfile(string modelTag)
        {
            AppSettings s = Store.State.Settings;
            if (s.ApiProfiles == null) s.ApiProfiles = new List<ApiKeyProfile>();
            ApiKeyProfile ollamaProf = s.ApiProfiles.FirstOrDefault(p => string.Equals(p.Provider, "Ollama", StringComparison.OrdinalIgnoreCase));
            if (ollamaProf == null)
            {
                ollamaProf = new ApiKeyProfile
                {
                    Id = "api_ollama",
                    Name = "Local Ollama",
                    Provider = "Ollama",
                    Endpoint = OLLAMA_BASE_URL + "/v1/chat/completions",
                    ApiKey = "",
                    Model = modelTag
                };
                s.ApiProfiles.Add(ollamaProf);
            }
            ollamaProf.Endpoint = OLLAMA_BASE_URL + "/v1/chat/completions";
            ollamaProf.Model = modelTag;
            s.AiEnabled = true;
            s.ActiveApiProfileId = ollamaProf.Id;
            s.AiEndpoint = ollamaProf.Endpoint;
            s.AiApiKey = "";
            s.AiModel = modelTag;
            Store.Notify("settings");
        }

        public static bool EnsureOrAutoCheckWithoutKeys()
        {
            AppSettings s = Store.State.Settings;
            if (!s.AiEnabled || AiEngine.HasAnyCloudApiKey(s)) return true;

            List<string> mods;
            if (ProbeOllama(700, out mods) && mods.Count > 0)
            {
                ActivateLocalOllamaProfile(mods[0]);
                return true;
            }
            // Also check if installed on disk with models once started
            if (!string.IsNullOrEmpty(FindOllamaExecutable()))
            {
                string msg;
                if (TryStartOllamaDaemon(out msg) && ProbeOllama(800, out mods) && mods.Count > 0)
                {
                    ActivateLocalOllamaProfile(mods[0]);
                    return true;
                }
            }
            return false;
        }

        public static bool DownloadFileWithProgress(string url, string destPath, ref bool cancelRequested, Action<int, long, long> onProgress, out string error)
        {
            error = "";
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Tls;
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.AllowAutoRedirect = true;
                req.UserAgent = "AIMClassic/5.9";
                req.Timeout = 20000;
                req.ReadWriteTimeout = 30000;

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    long total = resp.ContentLength;
                    long downloaded = 0;
                    byte[] buf = new byte[65536];
                    using (Stream rs = resp.GetResponseStream())
                    using (FileStream fs = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        int read;
                        DateTime lastUi = DateTime.MinValue;
                        while ((read = rs.Read(buf, 0, buf.Length)) > 0)
                        {
                            if (cancelRequested)
                            {
                                error = "Cancelled by user.";
                                return false;
                            }
                            fs.Write(buf, 0, read);
                            downloaded += read;
                            if ((DateTime.UtcNow - lastUi).TotalMilliseconds >= 120)
                            {
                                lastUi = DateTime.UtcNow;
                                int pct = total > 0 ? (int)Math.Min(100, (downloaded * 100L) / total) : -1;
                                if (onProgress != null) onProgress(pct, downloaded, total);
                            }
                        }
                    }
                    if (onProgress != null) onProgress(100, downloaded, downloaded);
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool PullModelWithProgress(string modelTag, ref bool cancelRequested, Action<int, string, long, long> onProgress, out string error)
        {
            error = "";
            try
            {
                string pullUrl = (ActiveBaseUrl ?? OLLAMA_BASE_URL) + "/api/pull";
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(pullUrl);
                req.Method = "POST";
                req.Proxy = null;
                req.ServicePoint.Expect100Continue = false;
                req.ContentType = "application/json";
                req.Timeout = 300000;
                req.ReadWriteTimeout = 60000;
                byte[] body = Encoding.UTF8.GetBytes(_json.Serialize(new { model = modelTag.Trim(), stream = true }));
                req.ContentLength = body.Length;
                using (Stream rs = req.GetRequestStream()) rs.Write(body, 0, body.Length);

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (cancelRequested)
                        {
                            error = "Cancelled by user.";
                            return false;
                        }
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        Dictionary<string, object> obj = _json.Deserialize<Dictionary<string, object>>(line);
                        if (obj == null) continue;
                        if (obj.ContainsKey("error") && obj["error"] != null)
                        {
                            error = Convert.ToString(obj["error"]);
                            return false;
                        }
                        string st = obj.ContainsKey("status") ? Convert.ToString(obj["status"]) : "downloading";
                        long completed = obj.ContainsKey("completed") ? Convert.ToInt64(obj["completed"]) : 0;
                        long total = obj.ContainsKey("total") ? Convert.ToInt64(obj["total"]) : 0;
                        int pct = total > 0 ? (int)Math.Min(100, (completed * 100L) / total) : (st == "success" ? 100 : -1);
                        if (onProgress != null) onProgress(pct, st, completed, total);
                        if (string.Equals(st, "success", StringComparison.OrdinalIgnoreCase)) return true;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }

    /* ======================================================================
       Automatic Local AI Brain Installer Dialog (Ollama + Llama / Qwen / Gemma)
       ====================================================================== */
    public class OllamaSetupForm : XpForm
    {
        private Label _lblEnvStatus, _lblModelDesc, _lblProgressInfo;
        private ComboBox _cboBrain;
        private XpProgressBar _progressBar;
        private XpButton _btnInstall, _btnActivateExisting, _btnSkip;
        private volatile bool _busy = false;
        private bool _cancelRequested = false;

        public bool ActivatedSuccessfully { get; private set; }

        public OllamaSetupForm(bool autoReasonNoKeys)
        {
            Text = I18n.T("Local AI Brain Downloader — Ollama + Llama / Qwen / Gemma", "Скачать локальный ИИ-мозг — Ollama + Llama / Qwen");
            Size = new Size(572, 434);
            ShowMaximizeButton = false;
            AllowResize = false;
            StartPosition = FormStartPosition.CenterParent;
            TitleIconImage = RetroAssets.GetIconBitmap("apikey", 16);

            XpBanner banner = new XpBanner
            {
                BannerTitle = autoReasonNoKeys
                    ? I18n.T("No Cloud API Keys Found — Use Free Local Ollama AI!", "Нет API-ключей — скачайте бесплатный локальный мозг Ollama!")
                    : I18n.T("Download Local AI Brain (Ollama Engine + Models)", "Установка локального ИИ-мозга Ollama"),
                BannerSubtitle = I18n.T("Automatically installs Ollama on Windows and pulls Llama / Qwen / Gemma locally (no keys needed)", "Автоматически установит Ollama и скачает локальный мозг (Llama, Qwen, Gemma) без ключей"),
                BannerIcon = RetroAssets.GetIconBitmap("update", 26)
            };
            ClientBody.Controls.Add(banner);

            int y = 58;
            GroupBox grpState = new GroupBox
            {
                Text = I18n.T("Current PC AI Status", "Состояние локального ИИ на этом ПК"),
                Location = new Point(14, y),
                Size = new Size(528, 78),
                Font = XpTheme.UiBold
            };
            _lblEnvStatus = new Label
            {
                Location = new Point(12, 22),
                Size = new Size(504, 48),
                Font = XpTheme.UiFont,
                ForeColor = XpTheme.Current.AimNavy,
                UseMnemonic = false,
                Text = I18n.T("Checking local Ollama installation...", "Проверка установленной Ollama...")
            };
            grpState.Controls.Add(_lblEnvStatus);
            y += 88;

            ClientBody.Controls.Add(new Label
            {
                Text = I18n.T("Choose AI Brain to Download:", "Выберите модель (мозг ИИ) для скачивания:"),
                Location = new Point(14, y + 3),
                AutoSize = true,
                Font = XpTheme.UiBold
            });
            y += 24;

            _cboBrain = new ComboBox
            {
                Location = new Point(14, y),
                Width = 528,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            foreach (LocalBrainPreset p in LocalAiManager.RecommendedBrains)
            {
                _cboBrain.Items.Add(string.Format("{0}  —  {1} ({2})", p.Tag, p.DisplayName, p.SizeLabel));
            }
            _cboBrain.SelectedIndex = 0;
            _cboBrain.SelectedIndexChanged += delegate { UpdateSelectedBrainHint(); };
            _cboBrain.TextChanged += delegate { UpdateSelectedBrainHint(); };
            y += 28;

            _lblModelDesc = new Label
            {
                Location = new Point(14, y),
                Size = new Size(528, 30),
                Font = XpTheme.SmallFont,
                ForeColor = Color.FromArgb(46, 76, 118),
                UseMnemonic = false
            };
            y += 38;

            _lblProgressInfo = new Label
            {
                Location = new Point(14, y),
                Size = new Size(528, 22),
                Font = XpTheme.UiBold,
                UseMnemonic = false,
                Text = I18n.T("Ready. Click 'Download & Install AI Brain' to set up everything automatically.", "Готово. Нажмите кнопку ниже — программа сама скачает Ollama и модель.")
            };
            y += 24;

            _progressBar = new XpProgressBar
            {
                Location = new Point(14, y),
                Size = new Size(528, 22),
                Value = 0
            };
            y += 34;

            _btnInstall = new XpButton
            {
                Text = I18n.T("Download & Install Ollama + Brain", "Скачать и установить Ollama + Мозг"),
                IsPrimary = true,
                Location = new Point(14, y),
                Size = new Size(236, 30)
            };
            _btnActivateExisting = new XpButton
            {
                Text = I18n.T("Activate Existing Model", "Запустить скачанную"),
                Location = new Point(256, y),
                Size = new Size(162, 30),
                Font = XpTheme.SmallFont
            };
            _btnSkip = new XpButton
            {
                Text = I18n.T("Later (Offline Bot)", "Позже (бот)"),
                Location = new Point(424, y),
                Size = new Size(118, 30),
                Font = XpTheme.SmallFont
            };

            _btnInstall.Click += OnStartAutoInstallClick;
            _btnActivateExisting.Click += OnActivateExistingClick;
            _btnSkip.Click += delegate
            {
                if (_busy)
                {
                    _cancelRequested = true;
                    _lblProgressInfo.Text = I18n.T("Cancelling...", "Отмена...");
                }
                else
                {
                    Close();
                }
            };

            ClientBody.Controls.AddRange(new Control[]
            {
                grpState, _cboBrain, _lblModelDesc, _lblProgressInfo, _progressBar,
                _btnInstall, _btnActivateExisting, _btnSkip
            });

            UpdateSelectedBrainHint();
            RefreshEnvironmentStatusAsync();

            FormClosing += delegate(object s, FormClosingEventArgs e)
            {
                if (_busy)
                {
                    _cancelRequested = true;
                }
            };
        }

        private string SelectedModelTag()
        {
            string raw = (_cboBrain.Text ?? "").Trim();
            int dash = raw.IndexOf("  —");
            if (dash > 0) raw = raw.Substring(0, dash).Trim();
            else if (raw.Contains(" ")) raw = raw.Split(' ')[0].Trim();
            return string.IsNullOrEmpty(raw) ? "qwen3:1.7b" : raw;
        }

        private void UpdateSelectedBrainHint()
        {
            string tag = SelectedModelTag();
            LocalBrainPreset match = LocalAiManager.RecommendedBrains.FirstOrDefault(x => string.Equals(x.Tag, tag, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                _lblModelDesc.Text = string.Format("{0} ({1}): {2}", match.DisplayName, match.SizeLabel, I18n.IsRussian ? match.DescRu : match.DescEn);
            }
            else
            {
                _lblModelDesc.Text = I18n.T("Custom Ollama library tag (e.g. mistral, phi4, codellama).", "Пользовательское имя модели Ollama (например mistral, phi4, codellama).");
            }
        }

        private void RefreshEnvironmentStatusAsync()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                string exe = LocalAiManager.FindOllamaExecutable();
                List<string> mods;
                bool running = LocalAiManager.ProbeOllama(850, out mods);
                if (!IsDisposed)
                {
                    try
                    {
                        BeginInvoke((Action)delegate
                        {
                            if (IsDisposed) return;
                            string exeState = string.IsNullOrEmpty(exe)
                                ? I18n.T("Ollama Engine: NOT installed (will auto-download OllamaSetup.exe)", "Движок Ollama: НЕ установлен (будет скачан автоматически)")
                                : string.Format(I18n.T("Ollama Engine: Installed ({0})", "Движок Ollama: Установлен ({0})"), running ? "RUNNING" : "stopped");
                            string modState = running && mods.Count > 0
                                ? string.Format(I18n.T("Downloaded Models ({0}): {1}", "Скачанные модели ({0}): {1}"), mods.Count, string.Join(", ", mods.Take(4).ToArray()))
                                : I18n.T("Downloaded Models: None yet (select one below to download!)", "Скачанные модели: пока нет (выберите модель ниже и скачайте!)");
                            _lblEnvStatus.Text = exeState + "\r\n" + modState;
                            _btnActivateExisting.Enabled = !string.IsNullOrEmpty(exe) || running;
                        });
                    }
                    catch { }
                }
            });
        }

        private void OnActivateExistingClick(object sender, EventArgs e)
        {
            if (_busy) return;
            _btnActivateExisting.Enabled = false;
            _lblProgressInfo.Text = I18n.T("Starting local Ollama server and checking models...", "Запуск локальной Ollama и проверка моделей...");
            ThreadPool.QueueUserWorkItem(delegate
            {
                string startMsg;
                bool started = LocalAiManager.TryStartOllamaDaemon(out startMsg);
                List<string> mods;
                bool running = LocalAiManager.ProbeOllama(900, out mods);
                MarshalUi(delegate
                {
                    _btnActivateExisting.Enabled = true;
                    if (running && mods.Count > 0)
                    {
                        LocalAiManager.ActivateLocalOllamaProfile(mods[0]);
                        ActivatedSuccessfully = true;
                        SoundEngine.Play("connectedChime");
                        XpMessageBox.Show(this, string.Format(I18n.T("Local Ollama brain '{0}' is ACTIVE! You can chat without API keys.", "Локальный мозг '{0}' активирован! Теперь можно общаться без ключей."), mods[0]), "Ollama Active", false);
                        Close();
                    }
                    else if (running)
                    {
                        _lblProgressInfo.Text = I18n.T("Ollama server started, no models yet — click 'Download & Install'!", "Ollama запущена, но моделей нет — нажмите 'Скачать и установить'!");
                        RefreshEnvironmentStatusAsync();
                    }
                    else
                    {
                        _lblProgressInfo.Text = startMsg;
                    }
                });
            });
        }

        private void OnStartAutoInstallClick(object sender, EventArgs e)
        {
            if (_busy) return;
            _busy = true;
            _cancelRequested = false;
            string modelTag = SelectedModelTag();
            _btnInstall.Enabled = false;
            _btnActivateExisting.Enabled = false;
            _cboBrain.Enabled = false;
            _btnSkip.Text = I18n.T("Cancel", "Отмена");

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    // Phase 1: Install OllamaSetup.exe if missing
                    List<string> mods;
                    bool running = LocalAiManager.ProbeOllama(900, out mods);
                    string exe = LocalAiManager.FindOllamaExecutable();

                    if (!running && string.IsNullOrEmpty(exe))
                    {
                        string tempSetup = Path.Combine(Path.GetTempPath(), "AIM_OllamaSetup.exe");
                        MarshalUi(delegate
                        {
                            _progressBar.Value = 4;
                            _lblProgressInfo.Text = I18n.T("Step 1/3: Downloading Ollama for Windows (~200+ MB)...", "Шаг 1/3: Скачивание установщика Ollama для Windows...");
                        });

                        string dlErr;
                        bool dlOk = LocalAiManager.DownloadFileWithProgress(LocalAiManager.OLLAMA_SETUP_PRIMARY_URL, tempSetup, ref _cancelRequested, delegate(int pct, long got, long tot)
                        {
                            MarshalUi(delegate
                            {
                                if (pct >= 0) _progressBar.Value = Math.Max(4, pct);
                                _lblProgressInfo.Text = tot > 0
                                    ? string.Format(I18n.T("Step 1/3: Downloading Ollama — {0}% ({1:0.0} / {2:0.0} MB)", "Шаг 1/3: Скачивание Ollama — {0}% ({1:0.0} / {2:0.0} МБ)"), pct, got / 1048576.0, tot / 1048576.0)
                                    : string.Format(I18n.T("Step 1/3: Downloading Ollama — {0:0.0} MB...", "Шаг 1/3: Скачивание Ollama — {0:0.0} МБ..."), got / 1048576.0);
                            });
                        }, out dlErr);

                        if (!dlOk && !_cancelRequested)
                        {
                            dlOk = LocalAiManager.DownloadFileWithProgress(LocalAiManager.OLLAMA_SETUP_MIRROR_URL, tempSetup, ref _cancelRequested, delegate(int pct, long got, long tot)
                            {
                                MarshalUi(delegate
                                {
                                    if (pct >= 0) _progressBar.Value = Math.Max(4, pct);
                                    _lblProgressInfo.Text = string.Format("Step 1/3 (GitHub mirror): {0:0.0} MB", got / 1048576.0);
                                });
                            }, out dlErr);
                        }

                        if (!dlOk)
                        {
                            FinishWithError(I18n.T("Download failed: ", "Ошибка скачивания: ") + dlErr);
                            return;
                        }

                        MarshalUi(delegate
                        {
                            _progressBar.Value = 100;
                            _lblProgressInfo.Text = I18n.T("Step 2/3: Installing Ollama silently (please wait)...", "Шаг 2/3: Установка Ollama (подождите немного)...");
                        });

                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = tempSetup,
                            Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-",
                            UseShellExecute = true
                        };
                        using (Process p = Process.Start(psi))
                        {
                            if (p != null) p.WaitForExit();
                        }
                        try { if (File.Exists(tempSetup)) File.Delete(tempSetup); } catch { }
                    }

                    // Phase 2: Ensure daemon running on localhost:11434
                    MarshalUi(delegate
                    {
                        _progressBar.Value = 15;
                        _lblProgressInfo.Text = I18n.T("Step 2/3: Starting local Ollama server...", "Шаг 2/3: Запуск локального сервера Ollama...");
                    });
                    string startMsg;
                    if (!LocalAiManager.TryStartOllamaDaemon(out startMsg))
                    {
                        FinishWithError(startMsg);
                        return;
                    }

                    // Phase 3: Pull requested brain model via /api/pull
                    MarshalUi(delegate
                    {
                        _progressBar.Value = 5;
                        _lblProgressInfo.Text = string.Format(I18n.T("Step 3/3: Downloading brain '{0}'...", "Шаг 3/3: Скачивание мозга '{0}'..."), modelTag);
                    });

                    string pullErr;
                    bool pulled = LocalAiManager.PullModelWithProgress(modelTag, ref _cancelRequested, delegate(int pct, string st, long got, long tot)
                    {
                        MarshalUi(delegate
                        {
                            if (pct >= 0) _progressBar.Value = Math.Max(2, pct);
                            if (tot > 0)
                                _lblProgressInfo.Text = string.Format("Step 3/3 ({0}): {1}% — {2:0.0} / {3:0.0} MB", modelTag, pct, got / 1048576.0, tot / 1048576.0);
                            else
                                _lblProgressInfo.Text = string.Format("Step 3/3 ({0}): {1}", modelTag, st);
                        });
                    }, out pullErr);

                    if (!pulled)
                    {
                        FinishWithError(I18n.T("Failed downloading model: ", "Ошибка загрузки модели: ") + pullErr);
                        return;
                    }

                    // Phase 4: Probe & activate
                    LocalAiManager.ProbeOllama(900, out mods);
                    MarshalUi(delegate
                    {
                        _busy = false;
                        _progressBar.Value = 100;
                        LocalAiManager.ActivateLocalOllamaProfile(modelTag);
                        ActivatedSuccessfully = true;
                        SoundEngine.Play("connectedChime");
                        _lblProgressInfo.Text = string.Format(I18n.T("DONE! Brain '{0}' installed and ready for offline AI chat!", "ГОТОВО! Мозг '{0}' установлен и активен!"), modelTag);
                        XpMessageBox.Show(this, string.Format(I18n.T("Success! Local AI Brain '{0}' is downloaded & active.\r\nAll AI buddies can now reply using Ollama with NO API keys required!", "Готово! Локальный ИИ-мозг '{0}' скачан и активирован.\r\nТеперь ИИ-боты работают через Ollama без API-ключей!"), modelTag), "Ollama AI Ready", false);
                        Close();
                    });
                }
                catch (Exception ex)
                {
                    FinishWithError(ex.Message);
                }
            });
        }

        private void FinishWithError(string err)
        {
            MarshalUi(delegate
            {
                _busy = false;
                _btnInstall.Enabled = true;
                _btnActivateExisting.Enabled = true;
                _cboBrain.Enabled = true;
                _btnSkip.Text = I18n.T("Close", "Закрыть");
                _lblProgressInfo.Text = I18n.T("Stopped: ", "Остановлено: ") + err;
                SoundEngine.Play("buddyAlert");
            });
        }

        private void MarshalUi(Action act)
        {
            if (IsDisposed) return;
            try { BeginInvoke(act); } catch { }
        }
    }
}
