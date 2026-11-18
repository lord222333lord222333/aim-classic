using System;
using System.Windows.Forms;

namespace AIMClassic
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Store.Load();
            if (args != null)
            {
                foreach (string a in args)
                {
                    if (string.Equals(a, "--lang=ru", StringComparison.OrdinalIgnoreCase))
                    {
                        Store.State.Settings.Language = "ru";
                        Store.Save();
                    }
                    else if (string.Equals(a, "--lang=en", StringComparison.OrdinalIgnoreCase))
                    {
                        Store.State.Settings.Language = "en";
                        Store.Save();
                    }
                }
            }
            AimApp.Start();
            Application.Run();
        }
    }
}
