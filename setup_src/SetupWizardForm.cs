using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace AIMSetup
{
    public class LanguageSelectForm : XpSetupForm
    {
        private RadioButton _radEn, _radRu;
        public string SelectedLanguage { get; private set; }

        public LanguageSelectForm()
        {
            SelectedLanguage = "en";
            Text = "Installer Language — AIM Setup";
            Size = new Size(396, 224);
            ShowMinimizeButton = false;

            Panel top = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.White };
            top.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Color.FromArgb(172, 168, 153)))
                    e.Graphics.DrawLine(p, 0, top.Height - 1, top.Width, top.Height - 1);
            };
            PictureBox pic = new PictureBox
            {
                Location = new Point(14, 9),
                Size = new Size(38, 38),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = SetupAssets.GetAimBadgeBitmap(38)
            };
            Label lblH = new Label
            {
                Text = "Select AIM Setup & Version Language\r\nВыберите язык установки и версии AIM",
                Location = new Point(62, 11),
                Size = new Size(314, 36),
                Font = SetupTheme.UiBold,
                ForeColor = SetupTheme.AimNavy,
                UseMnemonic = false
            };
            top.Controls.AddRange(new Control[] { pic, lblH });

            Panel body = new Panel { Dock = DockStyle.Fill, BackColor = SetupTheme.Face, Padding = new Padding(16, 14, 16, 8) };
            _radEn = new RadioButton
            {
                Text = "English Version (AOL Instant Messenger 5.9 — Classic 2005)",
                Location = new Point(18, 18),
                AutoSize = true,
                Checked = true,
                Font = SetupTheme.UiBold,
                UseMnemonic = false
            };
            _radRu = new RadioButton
            {
                Text = "Русская версия (AOL Instant Messenger 5.9 — Русский)",
                Location = new Point(18, 46),
                AutoSize = true,
                Checked = false,
                Font = SetupTheme.UiBold,
                UseMnemonic = false
            };
            Label lblSub = new Label
            {
                Text = "You can also switch EN / RU or translate chat messages inside AIM later.",
                Location = new Point(18, 76),
                Size = new Size(354, 18),
                Font = SetupTheme.SmallFont,
                ForeColor = Color.FromArgb(80, 90, 110),
                UseMnemonic = false
            };
            body.Controls.AddRange(new Control[] { _radEn, _radRu, lblSub });

            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 42, BackColor = SetupTheme.FaceDark };
            XpSetupButton btnOk = new XpSetupButton { Text = "OK", IsPrimary = true, Location = new Point(214, 9), Size = new Size(80, 24) };
            XpSetupButton btnCan = new XpSetupButton { Text = "Cancel", Location = new Point(300, 9), Size = new Size(80, 24) };
            btnOk.Click += delegate
            {
                SelectedLanguage = _radRu.Checked ? "ru" : "en";
                DialogResult = DialogResult.OK;
                Close();
            };
            btnCan.Click += delegate
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            foot.Controls.AddRange(new Control[] { btnOk, btnCan });

            ClientBody.Controls.Add(body);
            ClientBody.Controls.Add(top);
            ClientBody.Controls.Add(foot);
            body.BringToFront();

            AcceptButton = btnOk;
            CancelButton = btnCan;
        }
    }

    public class SetupWizardForm : XpSetupForm
    {
        private readonly bool _uninstallMode;
        private readonly string _initialDir;
        private string _lang;
        private int _step = 0; // 0=Welcome, 1=License, 2=Options, 3=Folder, 4=Installing, 5=Finish; 10=UninstConfirm, 11=UninstDone
        private bool _isRunningWork = false;
        private string _installedExePath = null;
        private string _installedDir = null;

        private Panel _bodyHost;
        private XpSetupButton _btnBack, _btnNext, _btnCancel;

        // Pages
        private Panel _pnlWelcome, _pnlLicense, _pnlOptions, _pnlFolder, _pnlInstalling, _pnlFinish;
        private Panel _pnlUninstConfirm, _pnlUninstDone;

        // Controls
        private RadioButton _radAccept, _radDecline;
        private CheckBox _chkDesk, _chkStartMenu, _chkStartup, _chkPreserve, _chkPromoHome;
        private ComboBox _cboInstallLang;
        private TextBox _txtDir;
        private Label _lblSpaceAvail;
        private Label _lblInstallTitle, _lblInstallSub, _lblInstallStep;
        private XpSetupProgressBar _progBar;
        private ListBox _lstLog;
        private CheckBox _chkLaunchNow, _chkOpenReadme, _chkOpenFolder;
        private Label _lblFinishPath;
        private CheckBox _chkUninstDeleteData;
        private Label _lblUninstDir;

        public SetupWizardForm(bool uninstallMode, string initialDir)
            : this(uninstallMode, initialDir, "en")
        {
        }

        public SetupWizardForm(bool uninstallMode, string initialDir, string lang)
        {
            _uninstallMode = uninstallMode;
            _initialDir = initialDir;
            _lang = string.Equals(lang, "ru", StringComparison.OrdinalIgnoreCase) ? "ru" : "en";
            Text = uninstallMode
                ? T("AOL Instant Messenger 5.9 Uninstall", "Удаление AOL Instant Messenger 5.9")
                : T("AOL Instant Messenger 5.9 Setup", "Установка AOL Instant Messenger 5.9");
            Size = new Size(516, 408);

            BuildShell();
            BuildPages();
            GoToStep(_uninstallMode ? 10 : 0);
        }

        private string T(string en, string ru)
        {
            return _lang == "ru" ? ru : en;
        }

        private void BuildShell()
        {
            Panel footer = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = SetupTheme.Face };
            footer.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen pDark = new Pen(Color.FromArgb(160, 160, 160)))
                using (Pen pLight = new Pen(Color.White))
                {
                    e.Graphics.DrawLine(pDark, 0, 0, footer.Width, 0);
                    e.Graphics.DrawLine(pLight, 0, 1, footer.Width, 1);
                }
            };

            Label lblBrandMark = new Label
            {
                Text = "AIM Setup v5.9 (2005–2008)",
                Location = new Point(12, 16),
                AutoSize = true,
                Font = SetupTheme.SmallFont,
                ForeColor = Color.FromArgb(115, 112, 102)
            };

            _btnBack = new XpSetupButton { Text = T("< Back", "< Назад"), Location = new Point(242, 12), Size = new Size(80, 23) };
            _btnNext = new XpSetupButton { Text = T("Next >", "Далее >"), IsPrimary = true, Location = new Point(326, 12), Size = new Size(80, 23) };
            _btnCancel = new XpSetupButton { Text = T("Cancel", "Отмена"), Location = new Point(418, 12), Size = new Size(80, 23) };

            _btnBack.Click += OnBackClick;
            _btnNext.Click += OnNextClick;
            _btnCancel.Click += OnCancelClick;

            footer.Controls.AddRange(new Control[] { lblBrandMark, _btnBack, _btnNext, _btnCancel });
            ClientBody.Controls.Add(footer);

            _bodyHost = new Panel { Dock = DockStyle.Fill, BackColor = SetupTheme.Face };
            ClientBody.Controls.Add(_bodyHost);
            _bodyHost.BringToFront();

            AcceptButton = _btnNext;
        }

        private void BuildPages()
        {
            BuildWelcomePage();
            BuildLicensePage();
            BuildOptionsPage();
            BuildFolderPage();
            BuildInstallingPage();
            BuildFinishPage();
            BuildUninstallPages();
        }

        private PictureBox CreateSidebarBox()
        {
            return new PictureBox
            {
                Dock = DockStyle.Left,
                Width = 164,
                Image = SetupAssets.GetInstallerSidebarArt(164, 332),
                SizeMode = PictureBoxSizeMode.StretchImage
            };
        }

        private Panel CreateTopHeaderBanner(string title, string subtitle, out Label lblTitleOut, out Label lblSubOut)
        {
            Panel banner = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.White };
            banner.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen pDark = new Pen(Color.FromArgb(172, 168, 153)))
                using (Pen pLight = new Pen(Color.White))
                {
                    e.Graphics.DrawLine(pDark, 0, banner.Height - 2, banner.Width, banner.Height - 2);
                    e.Graphics.DrawLine(pLight, 0, banner.Height - 1, banner.Width, banner.Height - 1);
                }
            };

            lblTitleOut = new Label
            {
                Text = title,
                Location = new Point(18, 9),
                Size = new Size(422, 18),
                Font = new Font("Tahoma", 9.75f, FontStyle.Bold),
                ForeColor = SetupTheme.AimNavy,
                UseMnemonic = false
            };
            lblSubOut = new Label
            {
                Text = subtitle,
                Location = new Point(28, 30),
                Size = new Size(412, 20),
                Font = SetupTheme.UiFont,
                ForeColor = Color.FromArgb(40, 40, 40),
                UseMnemonic = false
            };
            PictureBox badge = new PictureBox
            {
                Location = new Point(452, 9),
                Size = new Size(40, 40),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = SetupAssets.GetAimBadgeBitmap(40)
            };
            banner.Controls.AddRange(new Control[] { lblTitleOut, lblSubOut, badge });
            return banner;
        }

        private void BuildWelcomePage()
        {
            _pnlWelcome = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            _pnlWelcome.Controls.Add(CreateSidebarBox());

            Label lblTitle = new Label
            {
                Text = T("Welcome to the AOL Instant Messenger Setup Wizard", "Добро пожаловать в мастер установки AIM"),
                Location = new Point(182, 18),
                Size = new Size(312, 44),
                Font = SetupTheme.WizardTitleFont,
                ForeColor = SetupTheme.AimNavy,
                UseMnemonic = false
            };
            Label lblBody = new Label
            {
                Location = new Point(182, 70),
                Size = new Size(312, 232),
                Font = SetupTheme.UiFont,
                ForeColor = Color.Black,
                UseMnemonic = false,
                Text = T(
                    "This wizard will guide you through the installation of AOL Instant Messenger (AIM 5.9 / 6.0 Classic Edition).\r\n\r\n" +
                    "Included in this 2005–2008 Setup package:\r\n" +
                    "  • Windows XP Luna Blue AIM Desktop Client\r\n" +
                    "  • Real Friends P2P & Shared AIM Relay Networking\r\n" +
                    "  • Chat Translator (EN <-> RU), Updates & Support Center\r\n" +
                    "  • 42 Retro 2000s GIF Buddy Icons & 19 WAV Sounds\r\n" +
                    "  • Built-in Bilingual AI Companion + API Keys Manager\r\n\r\n" +
                    "Click Next to continue.",
                    "Этот мастер установит AOL Instant Messenger (AIM 5.9 / 6.0 Classic Edition) на ваш компьютер.\r\n\r\n" +
                    "В пакет 2005–2008 включено:\r\n" +
                    "  • Классический клиент Windows XP Luna Blue\r\n" +
                    "  • Реальные друзья по P2P и Shared Relay-серверу\r\n" +
                    "  • Переводчик чата (EN <-> RU), центр обновлений и поддержки\r\n" +
                    "  • 42 классических GIF-аватара и 19 звуков WAV\r\n" +
                    "  • ИИ-боты и менеджер API ключей\r\n\r\n" +
                    "Нажмите 'Далее >' для продолжения.")
            };

            _pnlWelcome.Controls.AddRange(new Control[] { lblTitle, lblBody });
            _bodyHost.Controls.Add(_pnlWelcome);
        }

        private void BuildLicensePage()
        {
            _pnlLicense = new Panel { Dock = DockStyle.Fill, BackColor = SetupTheme.Face };
            Label t, sub;
            _pnlLicense.Controls.Add(CreateTopHeaderBanner(
                T("License Agreement", "Лицензионное соглашение"),
                T("Please review the license terms before installing AOL Instant Messenger.", "Прочтите условия соглашения перед установкой AIM."),
                out t, out sub));

            Label lblPrompt = new Label
            {
                Text = T("Please read the following License Agreement. You must accept these terms to install AIM:", "Для продолжения установки необходимо принять условия соглашения:"),
                Location = new Point(18, 67),
                Size = new Size(474, 16),
                UseMnemonic = false
            };

            TextBox txtEula = new TextBox
            {
                Location = new Point(18, 87),
                Size = new Size(474, 174),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = SetupTheme.UiFont,
                Text =
                    "AMERICA ONLINE, INC. / AIM CLASSIC (2005-2008 EDITION)\r\n" +
                    "END USER SOFTWARE LICENSE AGREEMENT\r\n\r\n" +
                    "IMPORTANT — READ CAREFULLY: This classic AIM 5.9 / 6.0 desktop package replicates the authentic Windows XP AOL Instant Messenger experience from 2005-2008.\r\n\r\n" +
                    "1. GRANT OF LICENSE. You may install and use this standalone desktop client (AIMClassic.exe) in either English or Russian for real P2P/Relay friend messaging, AI bots, file transfers, and retro customization.\r\n\r\n" +
                    "2. EMBEDDED ASSETS & SOUNDS. Classic 2000s buddy icons and original WAV chimes are embedded directly inside the application.\r\n\r\n" +
                    "3. OPTIONAL API KEYS FOR BOTS. Offline AI works without any key. External OpenAI, OpenRouter, Groq, Gemini, or local Ollama requests use only endpoints you configure."
            };
            txtEula.SelectionLength = 0;

            _radAccept = new RadioButton { Text = T("I accept the terms in the License Agreement", "Я принимаю условия лицензионного соглашения"), Location = new Point(18, 271), AutoSize = true, UseMnemonic = false };
            _radDecline = new RadioButton { Text = T("I do not accept the terms in the License Agreement", "Я не принимаю условия соглашения"), Location = new Point(18, 294), AutoSize = true, Checked = true, UseMnemonic = false };
            _radAccept.CheckedChanged += delegate { if (_step == 1) _btnNext.Enabled = _radAccept.Checked; };

            _pnlLicense.Controls.AddRange(new Control[] { lblPrompt, txtEula, _radAccept, _radDecline });
            _bodyHost.Controls.Add(_pnlLicense);
        }

        private void BuildOptionsPage()
        {
            _pnlOptions = new Panel { Dock = DockStyle.Fill, BackColor = SetupTheme.Face };
            Label t, sub;
            _pnlOptions.Controls.Add(CreateTopHeaderBanner(
                T("Choose Version Language & Setup Options", "Выбор версии языка и параметров установки"),
                T("Select installed AIM language, shortcuts, and classic 2005–2008 features.", "Выберите язык версии AIM, ярлыки и параметры."),
                out t, out sub));

            GroupBox grpMain = new GroupBox
            {
                Text = T("Version Language && Shortcuts", "Язык версии и ярлыки Windows"),
                Location = new Point(18, 66),
                Size = new Size(474, 166),
                Font = SetupTheme.UiBold
            };
            grpMain.Controls.Add(new Label
            {
                Text = T("AIM Interface Language:", "Язык устанавливаемой версии:"),
                Location = new Point(14, 24),
                AutoSize = true,
                Font = SetupTheme.UiBold,
                ForeColor = SetupTheme.AimNavy
            });
            _cboInstallLang = new ComboBox
            {
                Location = new Point(214, 21),
                Width = 242,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = SetupTheme.UiFont
            };
            _cboInstallLang.Items.AddRange(new object[] { "English Version (Default 2005 AIM)", "Русская версия (Russian Language)" });
            _cboInstallLang.SelectedIndex = _lang == "ru" ? 1 : 0;

            CheckBox chkCore = new CheckBox { Text = T("AIM 5.9 Core + P2P / Relay + Updates + Support (Required)", "AIM 5.9 Ядро + P2P/Реле + Обновления и Поддержка"), Location = new Point(14, 52), AutoSize = true, Checked = true, Enabled = false, Font = SetupTheme.UiFont, UseMnemonic = false };
            CheckBox chkMedia = new CheckBox { Text = T("Classic 2000s Buddy Icons (42 GIFs) & Sound Pack (19 WAVs)", "Аватары 2000-х (42 GIF) и классические звуки (19 WAV)"), Location = new Point(14, 74), AutoSize = true, Checked = true, Enabled = false, Font = SetupTheme.UiFont, UseMnemonic = false };
            _chkDesk = new CheckBox { Text = T("Add shortcut to Desktop (\"AOL Instant Messenger\")", "Создать ярлык на рабочем столе"), Location = new Point(14, 97), AutoSize = true, Checked = true, Font = SetupTheme.UiFont, UseMnemonic = false };
            _chkStartMenu = new CheckBox { Text = T("Add shortcut to Start Menu (Programs -> AOL)", "Добавить в меню «Пуск» (Programs -> AOL)"), Location = new Point(14, 119), AutoSize = true, Checked = true, Font = SetupTheme.UiFont, UseMnemonic = false };
            _chkStartup = new CheckBox { Text = T("Launch AIM automatically when Windows starts", "Запускать AIM автоматически с Windows"), Location = new Point(14, 141), AutoSize = true, Checked = false, Font = SetupTheme.UiFont };
            grpMain.Controls.AddRange(new Control[] { _cboInstallLang, chkCore, chkMedia, _chkDesk, _chkStartMenu, _chkStartup });

            GroupBox grpExtras = new GroupBox
            {
                Text = T("User Data && Classic 2005 Preferences", "Данные пользователя и ретро-режим"),
                Location = new Point(18, 238),
                Size = new Size(474, 80),
                Font = SetupTheme.UiBold
            };
            _chkPreserve = new CheckBox
            {
                Text = T("Preserve existing accounts, buddies & API keys (aim_data.json)", "Сохранить аккаунты, друзей и API ключи (aim_data.json)"),
                Location = new Point(14, 22),
                AutoSize = true,
                Checked = true,
                Font = SetupTheme.UiFont,
                UseMnemonic = false
            };
            _chkPromoHome = new CheckBox
            {
                Text = T("Include 2005 AOL.com Start Mode badge in README (safe offline)", "Отметка 2005 AOL.com Start Mode в README"),
                Location = new Point(14, 47),
                AutoSize = true,
                Checked = false,
                Font = SetupTheme.UiFont,
                UseMnemonic = false
            };
            grpExtras.Controls.AddRange(new Control[] { _chkPreserve, _chkPromoHome });

            _pnlOptions.Controls.AddRange(new Control[] { grpMain, grpExtras });
            _bodyHost.Controls.Add(_pnlOptions);
        }

        private void BuildFolderPage()
        {
            _pnlFolder = new Panel { Dock = DockStyle.Fill, BackColor = SetupTheme.Face };
            Label t, sub;
            _pnlFolder.Controls.Add(CreateTopHeaderBanner(
                T("Choose Install Location", "Выбор папки установки"),
                T("Choose the folder in which to install AOL Instant Messenger.", "Укажите папку для установки AOL Instant Messenger."),
                out t, out sub));

            Label lblInfo = new Label
            {
                Text = T(
                    "Setup will install AOL Instant Messenger in the following folder. Click Install to start the installation.",
                    "Программа установит AIM в указанную ниже папку. Нажмите 'Установить >' для начала."),
                Location = new Point(18, 68),
                Size = new Size(474, 34),
                UseMnemonic = false
            };

            GroupBox grpDir = new GroupBox
            {
                Text = T("Destination Folder", "Папка назначения"),
                Location = new Point(18, 110),
                Size = new Size(474, 72),
                Font = SetupTheme.UiBold
            };
            string defaultDir = !string.IsNullOrEmpty(_initialDir) ? _initialDir : InstallOptions.DefaultInstallDir();
            _txtDir = new TextBox { Location = new Point(14, 28), Width = 354, Text = defaultDir, BorderStyle = BorderStyle.FixedSingle, Font = SetupTheme.UiFont };
            _txtDir.TextChanged += delegate { UpdateDiskSpaceLabel(); };

            XpSetupButton btnBrowse = new XpSetupButton { Text = T("Browse...", "Обзор..."), Location = new Point(376, 26), Size = new Size(84, 23) };
            btnBrowse.Click += delegate
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.Description = T("Choose destination folder for AIM 5.9:", "Выберите папку для установки AIM 5.9:");
                    fbd.SelectedPath = _txtDir.Text;
                    if (fbd.ShowDialog(this) == DialogResult.OK)
                    {
                        string sel = fbd.SelectedPath;
                        if (!sel.EndsWith("\\AIM", StringComparison.OrdinalIgnoreCase))
                            sel = Path.Combine(sel, "AIM");
                        _txtDir.Text = sel;
                    }
                }
            };
            grpDir.Controls.AddRange(new Control[] { _txtDir, btnBrowse });

            LinkLabel lnkRec = new LinkLabel { Text = T("Use recommended folder (AppData\\Local\\AOL\\AIM)", "Рекомендуемая папка (AppData\\Local\\AOL\\AIM)"), Location = new Point(20, 194), AutoSize = true, LinkColor = Color.FromArgb(0, 51, 204) };
            lnkRec.Click += delegate { _txtDir.Text = InstallOptions.DefaultInstallDir(); };

            LinkLabel lnkProgFiles = new LinkLabel { Text = T("Use Program Files\\AIM (Administrator prompt)", "Program Files\\AIM (запрос прав администратора)"), Location = new Point(20, 215), AutoSize = true, LinkColor = Color.FromArgb(0, 51, 204) };
            lnkProgFiles.Click += delegate
            {
                _txtDir.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "AOL\\AIM");
            };

            Label lblReq = new Label { Text = T("Space required: 4.3 MB", "Требуется места: 4.3 МБ"), Location = new Point(18, 274), AutoSize = true };
            _lblSpaceAvail = new Label { Text = T("Space available: --", "Доступно: --"), Location = new Point(18, 294), AutoSize = true };
            UpdateDiskSpaceLabel();

            _pnlFolder.Controls.AddRange(new Control[] { lblInfo, grpDir, lnkRec, lnkProgFiles, lblReq, _lblSpaceAvail });
            _bodyHost.Controls.Add(_pnlFolder);
        }

        private void BuildInstallingPage()
        {
            _pnlInstalling = new Panel { Dock = DockStyle.Fill, BackColor = SetupTheme.Face };
            _pnlInstalling.Controls.Add(CreateTopHeaderBanner(
                T("Installing", "Установка"),
                T("Please wait while AOL Instant Messenger is being installed.", "Подождите, выполняется установка AOL Instant Messenger."),
                out _lblInstallTitle, out _lblInstallSub));

            _lblInstallStep = new Label { Text = T("Preparing installation...", "Подготовка..."), Location = new Point(18, 70), Size = new Size(474, 18), UseMnemonic = false };
            _progBar = new XpSetupProgressBar { Location = new Point(18, 92), Size = new Size(474, 20) };
            _lstLog = new ListBox { Location = new Point(18, 122), Size = new Size(474, 190), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, Font = SetupTheme.SmallFont };

            _pnlInstalling.Controls.AddRange(new Control[] { _lblInstallStep, _progBar, _lstLog });
            _bodyHost.Controls.Add(_pnlInstalling);
        }

        private void BuildFinishPage()
        {
            _pnlFinish = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            _pnlFinish.Controls.Add(CreateSidebarBox());

            Label lblTitle = new Label
            {
                Text = T("Completing the AOL Instant Messenger Setup Wizard", "Установка AOL Instant Messenger завершена"),
                Location = new Point(182, 20),
                Size = new Size(312, 42),
                Font = SetupTheme.WizardTitleFont,
                ForeColor = SetupTheme.AimNavy,
                UseMnemonic = false
            };
            Label lblBody = new Label
            {
                Text = T(
                    "AOL Instant Messenger 5.9 (AIM Classic) has been installed on your computer.\r\n\r\nClick Finish to close this wizard.",
                    "AOL Instant Messenger 5.9 (AIM Classic) успешно установлен на компьютер.\r\n\r\nНажмите 'Готово' для завершения."),
                Location = new Point(182, 72),
                Size = new Size(312, 68),
                UseMnemonic = false
            };

            _chkLaunchNow = new CheckBox { Text = T("Run AOL Instant Messenger 5.9 now", "Запустить AOL Instant Messenger 5.9 сейчас"), Location = new Point(186, 154), AutoSize = true, Checked = true, BackColor = Color.White, Font = SetupTheme.UiBold };
            _chkOpenReadme = new CheckBox { Text = T("Show README.txt", "Открыть README.txt"), Location = new Point(186, 180), AutoSize = true, Checked = false, BackColor = Color.White };
            _chkOpenFolder = new CheckBox { Text = T("Open installation folder in Explorer", "Открыть папку установки"), Location = new Point(186, 206), AutoSize = true, Checked = false, BackColor = Color.White };

            _lblFinishPath = new Label
            {
                Text = T("Installed folder:", "Папка установки:"),
                Location = new Point(182, 258),
                Size = new Size(312, 46),
                Font = SetupTheme.SmallFont,
                ForeColor = Color.FromArgb(90, 90, 90),
                UseMnemonic = false
            };

            _pnlFinish.Controls.AddRange(new Control[] { lblTitle, lblBody, _chkLaunchNow, _chkOpenReadme, _chkOpenFolder, _lblFinishPath });
            _bodyHost.Controls.Add(_pnlFinish);
        }

        private void BuildUninstallPages()
        {
            _pnlUninstConfirm = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            _pnlUninstConfirm.Controls.Add(CreateSidebarBox());

            Label lblT = new Label
            {
                Text = T("AOL Instant Messenger 5.9 Uninstall", "Удаление AOL Instant Messenger 5.9"),
                Location = new Point(182, 20),
                Size = new Size(312, 42),
                Font = SetupTheme.WizardTitleFont,
                ForeColor = SetupTheme.AimNavy,
                UseMnemonic = false
            };
            Label lblB = new Label
            {
                Text = T(
                    "This wizard will remove AOL Instant Messenger 5.9 (AIM Classic) and its shortcuts from your computer.\r\n\r\nUninstalling from:",
                    "Мастер удалит AOL Instant Messenger 5.9 и его ярлыки с вашего компьютера.\r\n\r\nУдаление из:"),
                Location = new Point(182, 72),
                Size = new Size(312, 54),
                UseMnemonic = false
            };
            string uDir = !string.IsNullOrEmpty(_initialDir) ? _initialDir : Path.GetDirectoryName(typeof(SetupWizardForm).Assembly.Location);
            _lblUninstDir = new Label
            {
                Text = uDir,
                Location = new Point(182, 130),
                Size = new Size(308, 36),
                Font = SetupTheme.UiBold,
                ForeColor = SetupTheme.AimNavy,
                UseMnemonic = false
            };
            _chkUninstDeleteData = new CheckBox
            {
                Text = T("Also delete saved accounts, buddies & AI API keys (aim_data.json)", "Также удалить аккаунты, контакты и API ключи (aim_data.json)"),
                Location = new Point(184, 182),
                Size = new Size(308, 40),
                BackColor = Color.White,
                Checked = false,
                UseMnemonic = false
            };
            _pnlUninstConfirm.Controls.AddRange(new Control[] { lblT, lblB, _lblUninstDir, _chkUninstDeleteData });
            _bodyHost.Controls.Add(_pnlUninstConfirm);

            _pnlUninstDone = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            _pnlUninstDone.Controls.Add(CreateSidebarBox());
            Label lblDoneT = new Label
            {
                Text = T("AOL Instant Messenger Uninstall Complete", "Удаление AIM завершено"),
                Location = new Point(182, 20),
                Size = new Size(312, 42),
                Font = SetupTheme.WizardTitleFont,
                ForeColor = SetupTheme.AimNavy,
                UseMnemonic = false
            };
            Label lblDoneB = new Label
            {
                Text = T("AOL Instant Messenger 5.9 has been removed from your computer.\r\n\r\nClick Finish to close Setup.", "AOL Instant Messenger 5.9 успешно удалён.\r\n\r\nНажмите 'Готово' для выхода."),
                Location = new Point(182, 74),
                Size = new Size(312, 80),
                UseMnemonic = false
            };
            _pnlUninstDone.Controls.AddRange(new Control[] { lblDoneT, lblDoneB });
            _bodyHost.Controls.Add(_pnlUninstDone);
        }

        public void GoToStep(int step)
        {
            _step = step;
            Panel[] all = new Panel[] { _pnlWelcome, _pnlLicense, _pnlOptions, _pnlFolder, _pnlInstalling, _pnlFinish, _pnlUninstConfirm, _pnlUninstDone };
            foreach (Panel p in all) p.Visible = false;

            if (step == 0)
            {
                _pnlWelcome.Visible = true;
                _btnBack.Enabled = false;
                _btnNext.Enabled = true;
                _btnNext.Text = T("Next >", "Далее >");
                _btnCancel.Enabled = true;
            }
            else if (step == 1)
            {
                _pnlLicense.Visible = true;
                _btnBack.Enabled = true;
                _btnNext.Enabled = _radAccept.Checked;
                _btnNext.Text = T("Next >", "Далее >");
                _btnCancel.Enabled = true;
            }
            else if (step == 2)
            {
                _pnlOptions.Visible = true;
                _btnBack.Enabled = true;
                _btnNext.Enabled = true;
                _btnNext.Text = T("Next >", "Далее >");
                _btnCancel.Enabled = true;
            }
            else if (step == 3)
            {
                _pnlFolder.Visible = true;
                _btnBack.Enabled = true;
                _btnNext.Enabled = true;
                _btnNext.Text = T("Install >", "Установить >");
                _btnCancel.Enabled = true;
                UpdateDiskSpaceLabel();
            }
            else if (step == 4)
            {
                _pnlInstalling.Visible = true;
                _btnBack.Enabled = false;
                _btnNext.Enabled = false;
                _btnNext.Text = _uninstallMode ? T("Uninstalling...", "Удаление...") : T("Installing...", "Установка...");
                _btnCancel.Enabled = false;
            }
            else if (step == 5)
            {
                _pnlFinish.Visible = true;
                _btnBack.Enabled = false;
                _btnNext.Enabled = true;
                _btnNext.Text = T("Finish", "Готово");
                _btnCancel.Enabled = false;
            }
            else if (step == 10)
            {
                _pnlUninstConfirm.Visible = true;
                _btnBack.Enabled = false;
                _btnNext.Enabled = true;
                _btnNext.Text = T("Uninstall", "Удалить");
                _btnCancel.Enabled = true;
            }
            else if (step == 11)
            {
                _pnlUninstDone.Visible = true;
                _btnBack.Enabled = false;
                _btnNext.Enabled = true;
                _btnNext.Text = T("Finish", "Готово");
                _btnCancel.Enabled = false;
            }
        }

        private void UpdateDiskSpaceLabel()
        {
            try
            {
                string root = Path.GetPathRoot(_txtDir.Text.Trim());
                if (!string.IsNullOrEmpty(root))
                {
                    DriveInfo di = new DriveInfo(root);
                    double gb = di.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    _lblSpaceAvail.Text = string.Format(T("Space available ({0}): {1:0.0} GB", "Свободно ({0}): {1:0.0} ГБ"), root, gb);
                    return;
                }
            }
            catch { }
            _lblSpaceAvail.Text = T("Space available: Ready", "Свободно: готово");
        }

        private void OnBackClick(object sender, EventArgs e)
        {
            if (_isRunningWork) return;
            if (_step > 0 && _step <= 3) GoToStep(_step - 1);
        }

        private void OnNextClick(object sender, EventArgs e)
        {
            if (_isRunningWork) return;

            if (_step == 0 || _step == 1 || _step == 2)
            {
                GoToStep(_step + 1);
            }
            else if (_step == 3)
            {
                StartInstallation();
            }
            else if (_step == 5)
            {
                RunPostInstallActions();
                Close();
            }
            else if (_step == 10)
            {
                StartUninstall();
            }
            else if (_step == 11)
            {
                Close();
            }
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            if (_isRunningWork) return;
            if (XpSetupMsgBox.Show(this, T("Are you sure you want to exit AOL Instant Messenger Setup?", "Вы уверены, что хотите отменить установку AIM?"), "AIM Setup", true) == DialogResult.Yes)
                Close();
        }

        private void StartInstallation()
        {
            string dir = _txtDir.Text.Trim();
            if (string.IsNullOrEmpty(dir))
            {
                XpSetupMsgBox.Show(this, T("Please choose a valid destination folder.", "Выберите корректную папку для установки."), "AIM Setup", false);
                return;
            }

            string selectedInstallLang = (_cboInstallLang != null && _cboInstallLang.SelectedIndex == 1) ? "ru" : "en";

            if (!InstallerEngine.IsDirectoryWritable(dir))
            {
                DialogResult ask = XpSetupMsgBox.Show(this,
                    T("Writing to this folder requires Administrator permission:\r\n" + dir + "\r\n\r\nClick Yes to restart Setup as Administrator, or No to pick another folder (like AppData\\Local\\AOL\\AIM).",
                      "Для записи в эту папку нужны права администратора:\r\n" + dir + "\r\n\r\nНажмите Да для перезапуска от имени администратора или Нет для выбора другой папки."),
                    "Administrator Permission Required", true);
                if (ask == DialogResult.Yes)
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = typeof(SetupWizardForm).Assembly.Location,
                            Arguments = string.Format("--dir=\"{0}\" --lang={1}", dir, selectedInstallLang),
                            Verb = "runas",
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                        Close();
                    }
                    catch { }
                }
                return;
            }

            InstallOptions opts = new InstallOptions
            {
                InstallDir = dir,
                CreateDesktopShortcut = _chkDesk.Checked,
                CreateStartMenuShortcut = _chkStartMenu.Checked,
                RunAtStartup = _chkStartup.Checked,
                PreserveExistingData = _chkPreserve.Checked,
                ClassicAolTodayPromo = _chkPromoHome.Checked,
                InstallLanguage = selectedInstallLang
            };

            _installedDir = Path.GetFullPath(dir);
            _installedExePath = Path.Combine(_installedDir, "AIMClassic.exe");

            _lblInstallTitle.Text = T("Installing", "Установка");
            _lblInstallSub.Text = T("Please wait while AOL Instant Messenger is being installed.", "Подождите, выполняется установка...");
            _lstLog.Items.Clear();
            _progBar.Value = 0;
            _isRunningWork = true;
            GoToStep(4);

            Thread worker = new Thread(delegate()
            {
                try
                {
                    InstallerEngine.RunInstall(opts, delegate(int pct, string status, string logLine)
                    {
                        ReportProgressSafe(pct, status, logLine);
                    });

                    BeginInvoke((MethodInvoker)delegate
                    {
                        _isRunningWork = false;
                        SetupAssets.PlayCompletionChime();
                        _lblFinishPath.Text = T("Installed folder:\r\n", "Установлено в:\r\n") + _installedDir + "  [" + selectedInstallLang.ToUpperInvariant() + "]";
                        GoToStep(5);
                    });
                }
                catch (Exception ex)
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        _isRunningWork = false;
                        XpSetupMsgBox.Show(this, T("Installation could not complete:\r\n\r\n", "Не удалось завершить установку:\r\n\r\n") + ex.Message, "AIM Setup Error", false);
                        GoToStep(3);
                    });
                }
            });
            worker.IsBackground = true;
            worker.Start();
        }

        private void StartUninstall()
        {
            string dir = _lblUninstDir.Text.Trim();
            bool removeData = _chkUninstDeleteData.Checked;

            _lblInstallTitle.Text = T("Uninstalling", "Удаление");
            _lblInstallSub.Text = T("Please wait while AOL Instant Messenger is being removed.", "Подождите, выполняется удаление AIM...");
            _lstLog.Items.Clear();
            _progBar.Value = 0;
            _isRunningWork = true;
            GoToStep(4);

            Thread worker = new Thread(delegate()
            {
                try
                {
                    InstallerEngine.RunUninstall(dir, removeData, false, true, delegate(int pct, string status, string logLine)
                    {
                        ReportProgressSafe(pct, status, logLine);
                    });
                    BeginInvoke((MethodInvoker)delegate
                    {
                        _isRunningWork = false;
                        GoToStep(11);
                    });
                }
                catch (Exception ex)
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        _isRunningWork = false;
                        XpSetupMsgBox.Show(this, "Uninstall error:\r\n\r\n" + ex.Message, "AIM Uninstall Error", false);
                        GoToStep(10);
                    });
                }
            });
            worker.IsBackground = true;
            worker.Start();
        }

        private void ReportProgressSafe(int pct, string status, string logLine)
        {
            if (IsDisposed) return;
            BeginInvoke((MethodInvoker)delegate
            {
                if (IsDisposed) return;
                _progBar.Value = pct;
                if (!string.IsNullOrEmpty(status)) _lblInstallStep.Text = status;
                if (!string.IsNullOrEmpty(logLine))
                {
                    _lstLog.Items.Add(logLine);
                    _lstLog.TopIndex = Math.Max(0, _lstLog.Items.Count - 1);
                }
            });
        }

        private void RunPostInstallActions()
        {
            try
            {
                if (_chkOpenReadme.Checked && !string.IsNullOrEmpty(_installedDir))
                {
                    string rd = Path.Combine(_installedDir, "README.txt");
                    if (File.Exists(rd)) Process.Start("notepad.exe", "\"" + rd + "\"");
                }
                if (_chkOpenFolder.Checked && !string.IsNullOrEmpty(_installedDir) && Directory.Exists(_installedDir))
                {
                    Process.Start("explorer.exe", "\"" + _installedDir + "\"");
                }
                if (_chkLaunchNow.Checked && !string.IsNullOrEmpty(_installedExePath) && File.Exists(_installedExePath))
                {
                    string selectedInstallLang = (_cboInstallLang != null && _cboInstallLang.SelectedIndex == 1) ? "ru" : "en";
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = _installedExePath,
                        Arguments = "--lang=" + selectedInstallLang,
                        WorkingDirectory = _installedDir,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
            }
            catch { }
        }
    }
}
