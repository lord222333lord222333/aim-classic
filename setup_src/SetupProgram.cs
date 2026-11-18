using System;
using System.IO;
using System.Windows.Forms;

namespace AIMSetup
{
    internal static class SetupProgram
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool uninstallMode = false;
            string initialDir = null;
            string explicitLang = null;

            string selfName = Path.GetFileName(typeof(SetupProgram).Assembly.Location);
            if (!string.IsNullOrEmpty(selfName) && selfName.Equals("Uninstall.exe", StringComparison.OrdinalIgnoreCase))
            {
                uninstallMode = true;
            }

            if (args != null)
            {
                foreach (string a in args)
                {
                    if (string.IsNullOrEmpty(a)) continue;
                    if (string.Equals(a, "--uninstall", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(a, "/uninstall", StringComparison.OrdinalIgnoreCase))
                    {
                        uninstallMode = true;
                    }
                    else if (a.StartsWith("--dir=", StringComparison.OrdinalIgnoreCase))
                    {
                        initialDir = a.Substring(6).Trim('"');
                    }
                    else if (a.StartsWith("--lang=", StringComparison.OrdinalIgnoreCase))
                    {
                        explicitLang = a.Substring(7).Trim('"').ToLowerInvariant();
                    }
                }
            }

            string chosenLang = "en";
            if (!string.IsNullOrEmpty(explicitLang))
            {
                chosenLang = explicitLang == "ru" ? "ru" : "en";
            }
            else if (!uninstallMode)
            {
                using (LanguageSelectForm langDlg = new LanguageSelectForm())
                {
                    if (langDlg.ShowDialog() != DialogResult.OK) return;
                    chosenLang = langDlg.SelectedLanguage;
                }
            }

            Application.Run(new SetupWizardForm(uninstallMode, initialDir, chosenLang));
        }
    }
}
