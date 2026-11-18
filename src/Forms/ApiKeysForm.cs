using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace AIMClassic
{
    public class ApiKeysPanel : UserControl
    {
        private CheckBox _chkEnabled;
        private ListBox _lstProfiles;
        private TextBox _txtName, _txtEndpoint, _txtKey;
        private ComboBox _cboProvider, _cboModel;
        private CheckBox _chkShowKey;
        private Label _lblModelHint, _lblTestStatus;
        private XpButton _btnTest, _btnFetch;
        private bool _syncing = false;

        public ApiKeysPanel()
        {
            BackColor = XpTheme.Current.Face;
            Dock = DockStyle.Fill;
            Font = XpTheme.UiFont;
            Padding = new Padding(8);

            _chkEnabled = new CheckBox
            {
                Text = I18n.T("Enable AI chat replies for bots (uses built-in offline AI when key is empty)", "Включить ИИ-ответы в чатах (без ключа — встроенный оффлайн-бот)"),
                UseMnemonic = false,
                Location = new Point(10, 8),
                AutoSize = true,
                Font = XpTheme.UiBold,
                Checked = Store.State.Settings.AiEnabled
            };
            _chkEnabled.CheckedChanged += delegate
            {
                if (!_syncing) Store.State.Settings.AiEnabled = _chkEnabled.Checked;
            };

            Label lblList = new Label { Text = I18n.T("Saved API Profiles:", "Сохранённые API профили:"), Location = new Point(10, 34), AutoSize = true, Font = XpTheme.UiBold, ForeColor = XpTheme.Current.AimNavy };
            _lstProfiles = new ListBox { Location = new Point(10, 54), Size = new Size(176, 198), BorderStyle = BorderStyle.FixedSingle, HorizontalScrollbar = true };
            _lstProfiles.SelectedIndexChanged += delegate { PopulateSelected(); };

            XpButton btnNew = new XpButton { Text = I18n.T("+ New", "+ Новый"), Location = new Point(10, 256), Size = new Size(85, 22), Font = XpTheme.SmallFont };
            XpButton btnDel = new XpButton { Text = I18n.T("Delete", "Удалить"), Location = new Point(101, 256), Size = new Size(85, 22), Font = XpTheme.SmallFont };
            btnNew.Click += OnAddProfile;
            btnDel.Click += OnDeleteProfile;

            XpButton btnDownloadOllama = new XpButton
            {
                Text = I18n.T("Install Ollama Brain...", "Скачать Ollama (Без ключа)"),
                IsAimGold = true,
                Location = new Point(10, 284),
                Size = new Size(176, 26),
                Font = XpTheme.SmallFont,
                UseMnemonic = false
            };
            btnDownloadOllama.Click += delegate
            {
                using (OllamaSetupForm dlg = new OllamaSetupForm(false))
                {
                    dlg.ShowDialog(FindForm());
                    if (dlg.ActivatedSuccessfully) SyncFromStore();
                }
            };

            int rx = 198;
            int y = 36;
            Controls.Add(new Label { Text = I18n.T("Provider:", "Провайдер:"), Location = new Point(rx, y + 3), AutoSize = true });
            _cboProvider = new ComboBox { Location = new Point(rx + 82, y), Width = 266, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (AiProviderDef provDef in AiEngine.Providers) _cboProvider.Items.Add(provDef.Key);
            _cboProvider.SelectedIndexChanged += OnProviderChanged;
            y += 28;

            Controls.Add(new Label { Text = I18n.T("Profile Name:", "Название:"), Location = new Point(rx, y + 3), AutoSize = true });
            _txtName = new TextBox { Location = new Point(rx + 82, y), Width = 266, BorderStyle = BorderStyle.FixedSingle };
            y += 28;

            Controls.Add(new Label { Text = "Endpoint URL:", Location = new Point(rx, y + 3), AutoSize = true });
            _txtEndpoint = new TextBox { Location = new Point(rx + 82, y), Width = 266, BorderStyle = BorderStyle.FixedSingle };
            y += 28;

            Controls.Add(new Label { Text = "API Key:", Location = new Point(rx, y + 3), AutoSize = true, Font = XpTheme.UiBold });
            _txtKey = new TextBox { Location = new Point(rx + 82, y), Width = 266, UseSystemPasswordChar = true, BorderStyle = BorderStyle.FixedSingle };
            y += 23;

            _chkShowKey = new CheckBox { Text = I18n.T("Show API Key", "Показать ключ"), Location = new Point(rx + 82, y), AutoSize = true };
            _chkShowKey.CheckedChanged += delegate { _txtKey.UseSystemPasswordChar = !_chkShowKey.Checked; };
            y += 23;

            Controls.Add(new Label { Text = I18n.T("Model:", "Модель:"), Location = new Point(rx, y + 3), AutoSize = true, Font = XpTheme.UiBold });
            _cboModel = new ComboBox { Location = new Point(rx + 82, y), Width = 148, DropDownStyle = ComboBoxStyle.DropDown };
            _cboModel.TextChanged += delegate { UpdateModelHint(); };

            XpButton btnCatalog = new XpButton
            {
                Text = I18n.T("Catalog...", "Каталог..."),
                IsPrimary = true,
                Location = new Point(rx + 234, y - 1),
                Size = new Size(64, 23),
                Font = XpTheme.SmallFont
            };
            btnCatalog.Click += OnOpenCatalog;

            _btnFetch = new XpButton
            {
                Text = I18n.T("Fetch", "С API"),
                Location = new Point(rx + 302, y - 1),
                Size = new Size(46, 23),
                Font = XpTheme.SmallFont
            };
            _btnFetch.Click += OnFetchModelsClick;
            y += 24;

            _lblModelHint = new Label
            {
                Location = new Point(rx + 82, y),
                Size = new Size(266, 16),
                Font = XpTheme.SmallFont,
                ForeColor = Color.FromArgb(0, 90, 148),
                AutoEllipsis = true,
                UseMnemonic = false
            };
            y += 21;

            XpButton btnSave = new XpButton { Text = I18n.T("Save & Activate", "Сохранить и активировать"), IsPrimary = true, Location = new Point(rx, y), Size = new Size(178, 25), UseMnemonic = false };
            _btnTest = new XpButton { Text = I18n.T("Test API Key", "Проверить API ключ"), Location = new Point(rx + 184, y), Size = new Size(164, 25) };
            btnSave.Click += delegate { SaveAndActivateCurrent(true); };
            _btnTest.Click += OnTestClick;
            y += 32;

            _lblTestStatus = new Label
            {
                Location = new Point(rx, y),
                Size = new Size(348, 66),
                BackColor = Color.FromArgb(255, 255, 225),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(5),
                Font = XpTheme.SmallFont,
                UseMnemonic = false,
                Text = I18n.T(
                    "Supports 55+ work & chat models: Gemini 4 (Argon, Hydrogen, Carbon), GPT-6 Astra/Sol/Luna, Claude 5, DeepSeek V4, Grok 4, OpenRouter & Local Ollama!",
                    "Поддержка 55+ моделей для работы: Gemini 4 Argon/Hydrogen, ChatGPT GPT-6 Astra, Claude 5, DeepSeek V4, Grok 4, OpenRouter и Ollama!")
            };

            Controls.AddRange(new Control[]
            {
                _chkEnabled, lblList, _lstProfiles, btnNew, btnDel, btnDownloadOllama,
                _cboProvider, _txtName, _txtEndpoint, _txtKey, _chkShowKey,
                _cboModel, btnCatalog, _btnFetch, _lblModelHint,
                btnSave, _btnTest, _lblTestStatus
            });

            RefreshList(Store.State.Settings.ActiveApiProfileId);
        }

        public void SyncFromStore()
        {
            _syncing = true;
            _chkEnabled.Checked = Store.State.Settings.AiEnabled;
            _syncing = false;
            RefreshList(Store.State.Settings.ActiveApiProfileId);
        }

        public void ApplyCurrentChanges()
        {
            ApiKeyProfile p = SelectedProfile();
            if (p != null)
            {
                p.Provider = Convert.ToString(_cboProvider.SelectedItem);
                p.Name = string.IsNullOrWhiteSpace(_txtName.Text) ? p.Provider : _txtName.Text.Trim();
                p.Endpoint = _txtEndpoint.Text.Trim();
                p.ApiKey = _txtKey.Text.Trim();
                p.Model = _cboModel.Text.Trim();

                AppSettings s = Store.State.Settings;
                s.AiEnabled = _chkEnabled.Checked;
                if (p.Id == s.ActiveApiProfileId || !string.IsNullOrWhiteSpace(p.ApiKey))
                {
                    s.ActiveApiProfileId = p.Id;
                    s.AiEndpoint = p.Endpoint;
                    s.AiApiKey = p.ApiKey;
                    s.AiModel = p.Model;
                }
            }
            else
            {
                Store.State.Settings.AiEnabled = _chkEnabled.Checked;
            }
        }

        private void RefreshList(string selectId)
        {
            _lstProfiles.Items.Clear();
            int selIdx = 0;
            for (int i = 0; i < Store.State.Settings.ApiProfiles.Count; i++)
            {
                ApiKeyProfile p = Store.State.Settings.ApiProfiles[i];
                bool isAct = (p.Id == Store.State.Settings.ActiveApiProfileId);
                string hasKey = !string.IsNullOrWhiteSpace(p.ApiKey) || p.Provider == "Ollama" ? "✓" : "○";
                _lstProfiles.Items.Add(string.Format("{0} {1}{2}", hasKey, p.Name, isAct ? I18n.T(" [Active]", " [Активен]") : ""));
                if (p.Id == selectId) selIdx = i;
            }
            if (_lstProfiles.Items.Count > 0) _lstProfiles.SelectedIndex = Math.Min(selIdx, _lstProfiles.Items.Count - 1);
        }

        private ApiKeyProfile SelectedProfile()
        {
            int idx = _lstProfiles.SelectedIndex;
            if (idx >= 0 && idx < Store.State.Settings.ApiProfiles.Count)
                return Store.State.Settings.ApiProfiles[idx];
            return null;
        }

        private void PopulateSelected()
        {
            ApiKeyProfile p = SelectedProfile();
            if (p == null) return;
            _syncing = true;
            _cboProvider.SelectedItem = _cboProvider.Items.Contains(p.Provider) ? p.Provider : "Custom";
            _txtName.Text = p.Name ?? "";
            _txtEndpoint.Text = p.Endpoint ?? "";
            _txtKey.Text = p.ApiKey ?? "";
            PopulateModelSuggestions(p.Provider);
            _cboModel.Text = p.Model ?? "";
            _syncing = false;
            UpdateModelHint();
        }

        private void PopulateModelSuggestions(string provider)
        {
            _cboModel.Items.Clear();
            List<AiModelPreset> list = AiEngine.GetModelsForProvider(provider);
            foreach (AiModelPreset m in list)
            {
                _cboModel.Items.Add(m.Id);
            }
        }

        private void UpdateModelHint()
        {
            AiModelPreset preset = AiEngine.GetModelPreset(_cboModel.Text);
            if (preset != null)
            {
                _lblModelHint.Text = (I18n.IsRussian ? preset.DescRu : preset.DescEn);
            }
            else if (!string.IsNullOrWhiteSpace(_cboModel.Text))
            {
                _lblModelHint.Text = I18n.T("Custom model ID — ready to call selected endpoint.", "Пользовательская модель для выбранного API.");
            }
            else
            {
                _lblModelHint.Text = "";
            }
        }

        private void OnProviderChanged(object sender, EventArgs e)
        {
            if (_syncing) return;
            string prov = Convert.ToString(_cboProvider.SelectedItem);
            PopulateModelSuggestions(prov);
            AiProviderDef def = AiEngine.GetProviderDef(prov);
            if (!string.IsNullOrEmpty(def.DefaultEndpoint)) _txtEndpoint.Text = def.DefaultEndpoint;
            if (!string.IsNullOrEmpty(def.DefaultModel)) _cboModel.Text = def.DefaultModel;
            if (prov == "Ollama") _txtKey.Text = "";
            UpdateModelHint();
        }

        private void OnOpenCatalog(object sender, EventArgs e)
        {
            using (ModelCatalogForm dlg = new ModelCatalogForm(_cboModel.Text))
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.SelectedModel != null)
                {
                    AiModelPreset chosen = dlg.SelectedModel;
                    ApiKeyProfile match = Store.State.Settings.ApiProfiles.FirstOrDefault(
                        x => string.Equals(x.Provider, chosen.Provider, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        match.Model = chosen.Id;
                        if (string.IsNullOrWhiteSpace(match.Endpoint))
                            match.Endpoint = AiEngine.GetProviderDef(chosen.Provider).DefaultEndpoint;
                        RefreshList(match.Id);
                    }
                    else
                    {
                        AiProviderDef pd = AiEngine.GetProviderDef(chosen.Provider);
                        ApiKeyProfile np = new ApiKeyProfile
                        {
                            Id = "api_" + DateTime.Now.Ticks,
                            Name = pd.DisplayName,
                            Provider = chosen.Provider,
                            Endpoint = pd.DefaultEndpoint,
                            ApiKey = "",
                            Model = chosen.Id
                        };
                        Store.State.Settings.ApiProfiles.Add(np);
                        RefreshList(np.Id);
                    }
                    _cboModel.Text = chosen.Id;
                    SaveAndActivateCurrent(true);
                }
            }
        }

        private void OnFetchModelsClick(object sender, EventArgs e)
        {
            string ep = _txtEndpoint.Text.Trim();
            string key = _txtKey.Text.Trim();
            _btnFetch.Enabled = false;
            _lblTestStatus.Text = I18n.T("Fetching model list from endpoint...", "Загрузка списка моделей с API...");
            ThreadPool.QueueUserWorkItem(delegate
            {
                List<string> fetched;
                string err;
                bool ok = AiEngine.FetchModelsFromEndpoint(ep, key, out fetched, out err);
                if (!IsDisposed)
                {
                    try
                    {
                        BeginInvoke((Action)delegate
                        {
                            if (IsDisposed) return;
                            _btnFetch.Enabled = true;
                            if (ok && fetched.Count > 0)
                            {
                                string cur = _cboModel.Text;
                                _cboModel.Items.Clear();
                                foreach (string m in fetched) _cboModel.Items.Add(m);
                                if (!string.IsNullOrWhiteSpace(cur)) _cboModel.Text = cur;
                                else _cboModel.SelectedIndex = 0;
                                _lblTestStatus.Text = string.Format(I18n.T("Loaded {0} live models from API! Pick one in 'Model' dropdown.", "Загружено {0} моделей с сервера API!"), fetched.Count);
                                SoundEngine.Play("connectedChime");
                            }
                            else
                            {
                                _lblTestStatus.Text = I18n.T("Fetch failed: ", "Ошибка загрузки: ") + (string.IsNullOrEmpty(err) ? "No models returned" : err);
                            }
                        });
                    }
                    catch { }
                }
            });
        }

        private void OnAddProfile(object sender, EventArgs e)
        {
            ApiKeyProfile np = new ApiKeyProfile
            {
                Id = "api_" + DateTime.Now.Ticks,
                Name = "Custom Key " + (Store.State.Settings.ApiProfiles.Count + 1),
                Provider = "OpenAI",
                Endpoint = "https://api.openai.com/v1/chat/completions",
                ApiKey = "",
                Model = "gpt-6-astra"
            };
            Store.State.Settings.ApiProfiles.Add(np);
            Store.Save();
            RefreshList(np.Id);
        }

        private void OnDeleteProfile(object sender, EventArgs e)
        {
            ApiKeyProfile p = SelectedProfile();
            if (p == null || Store.State.Settings.ApiProfiles.Count <= 1) return;
            Store.State.Settings.ApiProfiles.Remove(p);
            if (Store.State.Settings.ActiveApiProfileId == p.Id)
                Store.State.Settings.ActiveApiProfileId = Store.State.Settings.ApiProfiles[0].Id;
            Store.Notify("settings");
            RefreshList(Store.State.Settings.ActiveApiProfileId);
        }

        private void SaveAndActivateCurrent(bool showAlert)
        {
            ApiKeyProfile p = SelectedProfile();
            if (p == null) return;
            p.Provider = Convert.ToString(_cboProvider.SelectedItem);
            p.Name = string.IsNullOrWhiteSpace(_txtName.Text) ? p.Provider : _txtName.Text.Trim();
            p.Endpoint = _txtEndpoint.Text.Trim();
            p.ApiKey = _txtKey.Text.Trim();
            p.Model = _cboModel.Text.Trim();

            AppSettings s = Store.State.Settings;
            s.AiEnabled = _chkEnabled.Checked;
            s.ActiveApiProfileId = p.Id;
            s.AiEndpoint = p.Endpoint;
            s.AiApiKey = p.ApiKey;
            s.AiModel = p.Model;
            Store.Notify("settings");
            SoundEngine.Play("talkClick");
            RefreshList(p.Id);
            if (showAlert)
            {
                _lblTestStatus.Text = string.Format(
                    I18n.T("Saved! Active profile \"{0}\" ({1}).", "Сохранено! Активен профиль \"{0}\" ({1})."),
                    p.Name, string.IsNullOrEmpty(p.ApiKey) && p.Provider != "Ollama" ? I18n.T("offline bot without key", "оффлайн-бот без ключа") : p.Model);
            }
        }

        private void OnTestClick(object sender, EventArgs e)
        {
            SaveAndActivateCurrent(false);
            string ep = _txtEndpoint.Text.Trim();
            string key = _txtKey.Text.Trim();
            string mod = _cboModel.Text.Trim();

            _btnTest.Enabled = false;
            _lblTestStatus.Text = I18n.T("Testing connection to ", "Проверка соединения с ") + ep + "...";

            ThreadPool.QueueUserWorkItem(delegate
            {
                string msg;
                bool ok = AiEngine.TestConnection(ep, key, mod, out msg);
                if (!IsDisposed)
                {
                    try
                    {
                        BeginInvoke((Action)delegate
                        {
                            if (IsDisposed) return;
                            _btnTest.Enabled = true;
                            _lblTestStatus.Text = (ok ? I18n.T("SUCCESS: ", "УСПЕХ: ") : I18n.T("ERROR: ", "ОШИБКА: ")) + msg;
                            SoundEngine.Play(ok ? "connectedChime" : "buddyAlert");
                        });
                    }
                    catch { }
                }
            });
        }
    }

    public class ApiKeysForm : XpForm
    {
        public ApiKeysForm()
        {
            Text = I18n.T("API Keys & AI Work Models — AIM", "API Ключи и ИИ-модели для работы — AIM");
            Size = new Size(588, 476);
            ShowMaximizeButton = false;
            AllowResize = false;
            TitleIconImage = RetroAssets.GetIconBitmap("apikey", 16);

            XpBanner banner = new XpBanner
            {
                BannerTitle = I18n.T("AI Models & API Keys Center (55+ Models)", "Центр ИИ-моделей и API Ключей (55+ моделей)"),
                BannerSubtitle = I18n.T("Gemini 4 Argon/Hydrogen, GPT-6 Astra, Claude 5, DeepSeek V4, Grok 4, OpenRouter, Ollama", "Gemini 4 Argon/Hydrogen, ChatGPT GPT-6 Astra, Claude 5, DeepSeek V4, Grok 4, OpenRouter"),
                BannerIcon = RetroAssets.GetIconBitmap("apikey", 26)
            };
            ClientBody.Controls.Add(banner);

            ApiKeysPanel panel = new ApiKeysPanel { Dock = DockStyle.Fill };
            ClientBody.Controls.Add(panel);
            panel.BringToFront();

            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 38, BackColor = XpTheme.Current.FaceDark };
            XpButton btnClose = new XpButton { Text = I18n.T("Close", "Закрыть"), Location = new Point(482, 7), Size = new Size(84, 24) };
            btnClose.Click += delegate { Close(); };
            foot.Controls.Add(btnClose);
            ClientBody.Controls.Add(foot);
        }
    }

    /* ======================================================================
       Searchable 55+ AI Work Models Catalog Dialog (Argon, Hydrogen, Astra...)
       ====================================================================== */
    public class ModelCatalogForm : XpForm
    {
        private TextBox _txtSearch;
        private ComboBox _cboProvFilter;
        private ListView _lvModels;
        private Label _lblDesc;
        public AiModelPreset SelectedModel { get; private set; }

        public ModelCatalogForm(string preselectModelId)
        {
            Text = I18n.T("AI Work Models Catalog (Argon, Hydrogen, Astra, Claude, DeepSeek...)", "Каталог ИИ-моделей для работы (Argon, Hydrogen, Astra, Claude...)");
            Size = new Size(654, 486);
            ShowMaximizeButton = false;
            MinimumSize = new Size(580, 420);
            TitleIconImage = RetroAssets.GetIconBitmap("apikey", 16);

            XpBanner banner = new XpBanner
            {
                BannerTitle = I18n.T("AI Work Models Catalog", "Каталог нейросетей и моделей для работы"),
                BannerSubtitle = I18n.T("Pick Gemini 4 Argon / Hydrogen, GPT-6 Astra, Claude 5, DeepSeek V4, Grok 4 & more", "Выберите Gemini 4 Argon / Hydrogen, GPT-6 Astra, Claude 5, DeepSeek V4, Grok 4 и др."),
                BannerIcon = RetroAssets.GetIconBitmap("apikey", 26)
            };
            ClientBody.Controls.Add(banner);

            Panel filterBar = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = XpTheme.Current.SubBarTop };
            filterBar.Controls.Add(new Label { Text = I18n.T("Search:", "Поиск:"), Location = new Point(10, 11), AutoSize = true, Font = XpTheme.UiBold });
            _txtSearch = new TextBox { Location = new Point(62, 8), Width = 240, BorderStyle = BorderStyle.FixedSingle };
            _txtSearch.TextChanged += delegate { PopulateModels(null); };

            filterBar.Controls.Add(new Label { Text = I18n.T("Provider:", "Провайдер:"), Location = new Point(316, 11), AutoSize = true, Font = XpTheme.UiBold });
            _cboProvFilter = new ComboBox { Location = new Point(386, 8), Width = 242, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboProvFilter.Items.Add("All Providers (" + AiEngine.ModelCatalog.Length + " Models)");
            foreach (AiProviderDef pd in AiEngine.Providers)
            {
                if (pd.Key != "Custom") _cboProvFilter.Items.Add(pd.Key);
            }
            _cboProvFilter.SelectedIndex = 0;
            _cboProvFilter.SelectedIndexChanged += delegate { PopulateModels(null); };
            filterBar.Controls.AddRange(new Control[] { _txtSearch, _cboProvFilter });

            _lvModels = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false,
                BackColor = Color.White,
                Font = XpTheme.UiFont
            };
            _lvModels.Columns.Add(I18n.T("Provider", "Провайдер"), 86);
            _lvModels.Columns.Add(I18n.T("Display Name", "Название"), 168);
            _lvModels.Columns.Add("Model ID", 190);
            _lvModels.Columns.Add(I18n.T("Role / Tier", "Назначение"), 166);
            _lvModels.SelectedIndexChanged += delegate
            {
                if (_lvModels.SelectedItems.Count > 0)
                {
                    SelectedModel = _lvModels.SelectedItems[0].Tag as AiModelPreset;
                    if (SelectedModel != null)
                        _lblDesc.Text = string.Format("[{0}] {1}: {2}", SelectedModel.Provider, SelectedModel.DisplayName, I18n.IsRussian ? SelectedModel.DescRu : SelectedModel.DescEn);
                }
            };
            _lvModels.DoubleClick += delegate { ConfirmSelection(); };

            Panel wrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 6, 8, 6) };
            wrap.Controls.Add(_lvModels);

            _lblDesc = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                BackColor = Color.FromArgb(255, 255, 225),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8, 5, 8, 4),
                Font = XpTheme.SmallFont,
                UseMnemonic = false,
                Text = I18n.T("Double-click any model or click 'Use Selected Model' below.", "Дважды кликните по модели или нажмите кнопку выбора ниже.")
            };

            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 38, Width = ClientBody.ClientSize.Width, BackColor = XpTheme.Current.FaceDark };
            XpButton btnOk = new XpButton { Text = I18n.T("Use Selected Model", "Выбрать модель"), IsPrimary = true, Location = new Point(Math.Max(180, ClientBody.ClientSize.Width - 256), 7), Size = new Size(156, 24), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            XpButton btnCancel = new XpButton { Text = I18n.T("Cancel", "Отмена"), Location = new Point(Math.Max(342, ClientBody.ClientSize.Width - 94), 7), Size = new Size(84, 24), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnOk.Click += delegate { ConfirmSelection(); };
            btnCancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            foot.Controls.AddRange(new Control[] { btnOk, btnCancel });

            ClientBody.Controls.Add(wrap);
            ClientBody.Controls.Add(_lblDesc);
            ClientBody.Controls.Add(filterBar);
            ClientBody.Controls.Add(foot);
            banner.SendToBack();
            wrap.BringToFront();

            PopulateModels(preselectModelId);
        }

        private void PopulateModels(string preselectId)
        {
            string q = (_txtSearch.Text ?? "").Trim().ToLowerInvariant();
            string provFilter = _cboProvFilter.SelectedIndex > 0 ? Convert.ToString(_cboProvFilter.SelectedItem) : "";

            _lvModels.BeginUpdate();
            _lvModels.Items.Clear();
            foreach (AiModelPreset m in AiEngine.ModelCatalog)
            {
                if (!string.IsNullOrEmpty(provFilter) && !string.Equals(m.Provider, provFilter, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrEmpty(q))
                {
                    bool hit = m.Id.ToLowerInvariant().Contains(q) ||
                               m.DisplayName.ToLowerInvariant().Contains(q) ||
                               m.Provider.ToLowerInvariant().Contains(q) ||
                               m.Category.ToLowerInvariant().Contains(q) ||
                               m.DescEn.ToLowerInvariant().Contains(q) ||
                               m.DescRu.ToLowerInvariant().Contains(q);
                    if (!hit) continue;
                }
                ListViewItem lvi = new ListViewItem(m.Provider);
                lvi.SubItems.Add(m.DisplayName);
                lvi.SubItems.Add(m.Id);
                lvi.SubItems.Add(m.Category);
                lvi.Tag = m;
                if (m.Id == "gemini-4-argon" || m.Id == "gemini-4-hydrogen" || m.Id == "gpt-6-astra")
                {
                    lvi.Font = XpTheme.UiBold;
                    lvi.ForeColor = XpTheme.Current.AimNavy;
                }
                _lvModels.Items.Add(lvi);
                if (!string.IsNullOrEmpty(preselectId) && string.Equals(m.Id, preselectId, StringComparison.OrdinalIgnoreCase))
                    lvi.Selected = true;
            }
            if (_lvModels.SelectedItems.Count == 0 && _lvModels.Items.Count > 0)
                _lvModels.Items[0].Selected = true;
            _lvModels.EndUpdate();
        }

        private void ConfirmSelection()
        {
            if (SelectedModel == null && _lvModels.SelectedItems.Count > 0)
                SelectedModel = _lvModels.SelectedItems[0].Tag as AiModelPreset;
            if (SelectedModel == null) return;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
