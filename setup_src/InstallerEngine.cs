using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace AIMSetup
{
    public class InstallOptions
    {
        public string InstallDir;
        public bool CreateDesktopShortcut = true;
        public bool CreateStartMenuShortcut = true;
        public bool RunAtStartup = false;
        public bool PreserveExistingData = true;
        public bool ClassicAolTodayPromo = false;
        public string InstallLanguage = "en"; // "en" | "ru"

        // Overrides used by automated verification tests so user desktop/registry stay untouched
        public string CustomDesktopDir = null;
        public string CustomStartMenuDir = null;
        public bool SkipRegistry = false;
        public bool SkipSelfCopyUninstall = false;

        public static string DefaultInstallDir()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AOL\\AIM");
        }
    }

    public static class InstallerEngine
    {
        private const string REG_UNINSTALL = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\AIMClassic";
        private const string REG_RUN = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static bool IsDirectoryWritable(string dir)
        {
            try
            {
                if (string.IsNullOrEmpty(dir)) return false;
                if (!Directory.Exists(dir))
                {
                    string parent = Path.GetDirectoryName(dir.TrimEnd('\\', '/'));
                    while (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
                        parent = Path.GetDirectoryName(parent);
                    if (string.IsNullOrEmpty(parent)) return false;
                    dir = parent;
                }
                string probe = Path.Combine(dir, ".aim_perm_probe_" + Guid.NewGuid().ToString("N"));
                File.WriteAllText(probe, "1");
                File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void RunInstall(InstallOptions opts, Action<int, string, string> log)
        {
            string dir = Path.GetFullPath((opts.InstallDir ?? InstallOptions.DefaultInstallDir()).Trim());
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            log(5, "Preparing target directory...", "Output folder: " + dir);

            string exeTarget = Path.Combine(dir, "AIMClassic.exe");
            string icoTarget = Path.Combine(dir, "aim.ico");
            string readmeTarget = Path.Combine(dir, "README.txt");
            string uninstTarget = Path.Combine(dir, "Uninstall.exe");

            if (File.Exists(exeTarget))
            {
                try
                {
                    using (FileStream fs = new FileStream(exeTarget, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                }
                catch
                {
                    throw new IOException("AIMClassic.exe is currently running in the target folder.\r\nPlease close AIM (check the system tray icon -> Exit AIM) and try again.");
                }
            }

            // 1. Extract application icon
            log(15, "Extracting: aim.ico", "Extract: aim.ico");
            byte[] icoBytes = SetupAssets.ReadResourceBytes("AIMSetup.ClientIcon.ico");
            if (icoBytes != null) File.WriteAllBytes(icoTarget, icoBytes);
            Thread.Sleep(90);

            // 2. Decompress embedded AIMClassic.exe payload
            log(22, "Extracting: AIMClassic.exe (Core Client & Assets)...", "Extract: AIMClassic.exe (42 Avatars, 19 WAVs, XP Sprites)");
            int lastReported = 22;
            SetupAssets.ExtractCompressedPayload(exeTarget, delegate(int pct)
            {
                int mapped = 22 + (int)(pct * 0.56);
                if (mapped >= lastReported + 4)
                {
                    lastReported = mapped;
                    log(mapped, string.Format("Extracting: AIMClassic.exe ({0}%)", pct), null);
                }
            });

            // 3. Preserve aim_data.json from folder alongside installer if present and absent in target
            if (opts.PreserveExistingData)
            {
                try
                {
                    string srcDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string srcData = string.IsNullOrEmpty(srcDir) ? null : Path.Combine(srcDir, "aim_data.json");
                    string dstData = Path.Combine(dir, "aim_data.json");
                    if (!string.IsNullOrEmpty(srcData) && File.Exists(srcData) && !File.Exists(dstData))
                    {
                        File.Copy(srcData, dstData, false);
                        log(80, "Preserving user data (aim_data.json)...", "Copy user data: aim_data.json (buddies & API keys preserved)");
                    }
                }
                catch { }
            }

            // 3b. Write language configuration marker consumed on first AIM launch
            string chosenLang = string.Equals(opts.InstallLanguage, "ru", StringComparison.OrdinalIgnoreCase) ? "ru" : "en";
            File.WriteAllText(Path.Combine(dir, "aim_install_lang.cfg"), chosenLang, new UTF8Encoding(false));
            log(81, "Configuring AIM language: " + (chosenLang == "ru" ? "Russian" : "English"), "Write language config: aim_install_lang.cfg (" + chosenLang + ")");

            // 4. Write classic 2005-2008 README.txt
            log(83, "Creating: README.txt", "Extract: README.txt");
            File.WriteAllText(readmeTarget, BuildReadmeText(dir, opts), Encoding.UTF8);
            Thread.Sleep(70);

            // 5. Copy uninstaller executable
            if (!opts.SkipSelfCopyUninstall)
            {
                string selfExe = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(selfExe) && File.Exists(selfExe) &&
                    !string.Equals(Path.GetFullPath(selfExe), Path.GetFullPath(uninstTarget), StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(selfExe, uninstTarget, true);
                    log(86, "Creating: Uninstall.exe", "Extract: Uninstall.exe");
                }
            }

            // 6. Shortcuts
            if (opts.CreateDesktopShortcut)
            {
                string desktopDir = !string.IsNullOrEmpty(opts.CustomDesktopDir)
                    ? opts.CustomDesktopDir
                    : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (!Directory.Exists(desktopDir)) Directory.CreateDirectory(desktopDir);
                string deskLnk = Path.Combine(desktopDir, "AOL Instant Messenger.lnk");
                CreateShortcut(deskLnk, exeTarget, dir, icoTarget, "AOL Instant Messenger 5.9 (AIM Classic)");
                log(90, "Creating Desktop shortcut...", "Create shortcut: " + deskLnk);
            }

            if (opts.CreateStartMenuShortcut)
            {
                string programsDir = !string.IsNullOrEmpty(opts.CustomStartMenuDir)
                    ? opts.CustomStartMenuDir
                    : Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                string aolMenu = Path.Combine(programsDir, "AOL");
                if (!Directory.Exists(aolMenu)) Directory.CreateDirectory(aolMenu);

                string aimLnk = Path.Combine(aolMenu, "AOL Instant Messenger.lnk");
                CreateShortcut(aimLnk, exeTarget, dir, icoTarget, "AOL Instant Messenger 5.9 (AIM Classic)");
                log(93, "Creating Start Menu shortcuts...", "Create folder: Start Menu\\Programs\\AOL");

                if (File.Exists(uninstTarget))
                {
                    string uninstLnk = Path.Combine(aolMenu, "Uninstall AIM.lnk");
                    CreateShortcutWithArgs(uninstLnk, uninstTarget, "--uninstall --dir=\"" + dir + "\"", dir, icoTarget, "Uninstall AOL Instant Messenger");
                }
            }

            // 7. Windows HKCU Registry integration
            if (!opts.SkipRegistry)
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(REG_UNINSTALL))
                    {
                        if (key != null)
                        {
                            key.SetValue("DisplayName", "AOL Instant Messenger 5.9 (AIM Classic)");
                            key.SetValue("DisplayVersion", "5.9.3861");
                            key.SetValue("Publisher", "America Online, Inc. / AIM Classic 2005-2008");
                            key.SetValue("InstallLocation", dir);
                            key.SetValue("DisplayIcon", File.Exists(icoTarget) ? icoTarget : exeTarget);
                            key.SetValue("UninstallString", "\"" + uninstTarget + "\" --uninstall --dir=\"" + dir + "\"");
                            key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                            key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                            key.SetValue("EstimatedSize", 4200, RegistryValueKind.DWord);
                        }
                    }

                    using (RegistryKey runKey = Registry.CurrentUser.CreateSubKey(REG_RUN))
                    {
                        if (runKey != null)
                        {
                            if (opts.RunAtStartup) runKey.SetValue("AIMClassic", "\"" + exeTarget + "\"");
                            else runKey.DeleteValue("AIMClassic", false);
                        }
                    }
                    log(97, "Updating Windows Registry...", "Write registry: HKCU\\...\\Uninstall\\AIMClassic");
                }
                catch { }
            }

            log(100, "Completed", "Installation completed successfully!");
        }

        public static void RunUninstall(string installDir, bool removeUserData, bool skipRegistry, bool allowSelfDeleteCmd, Action<int, string, string> log)
        {
            string dir = Path.GetFullPath(installDir.Trim());
            log(15, "Removing AIM files...", "Target directory: " + dir);

            string exeTarget = Path.Combine(dir, "AIMClassic.exe");
            if (File.Exists(exeTarget))
            {
                File.Delete(exeTarget);
                log(35, "Deleted: AIMClassic.exe", "Delete file: AIMClassic.exe");
            }
            string icoTarget = Path.Combine(dir, "aim.ico");
            if (File.Exists(icoTarget))
            {
                File.Delete(icoTarget);
                log(45, "Deleted: aim.ico", "Delete file: aim.ico");
            }
            string readmeTarget = Path.Combine(dir, "README.txt");
            if (File.Exists(readmeTarget))
            {
                File.Delete(readmeTarget);
                log(55, "Deleted: README.txt", "Delete file: README.txt");
            }
            if (removeUserData)
            {
                string dataTarget = Path.Combine(dir, "aim_data.json");
                if (File.Exists(dataTarget))
                {
                    File.Delete(dataTarget);
                    log(65, "Deleted user data: aim_data.json", "Delete file: aim_data.json");
                }
            }

            // Shortcuts
            try
            {
                string deskLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "AOL Instant Messenger.lnk");
                if (File.Exists(deskLnk)) { File.Delete(deskLnk); log(75, "Removed Desktop shortcut", "Delete shortcut: Desktop\\AOL Instant Messenger.lnk"); }

                string aolMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "AOL");
                if (Directory.Exists(aolMenu))
                {
                    foreach (string lnk in new[] { "AOL Instant Messenger.lnk", "Uninstall AIM.lnk" })
                    {
                        string p = Path.Combine(aolMenu, lnk);
                        if (File.Exists(p)) File.Delete(p);
                    }
                    if (Directory.GetFiles(aolMenu).Length == 0 && Directory.GetDirectories(aolMenu).Length == 0)
                        Directory.Delete(aolMenu);
                    log(82, "Removed Start Menu shortcuts", "Remove Start Menu folder: AOL");
                }
            }
            catch { }

            if (!skipRegistry)
            {
                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(REG_UNINSTALL, false);
                    using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(REG_RUN, true))
                    {
                        if (runKey != null) runKey.DeleteValue("AIMClassic", false);
                    }
                    log(92, "Cleaned Windows Registry...", "Delete registry: HKCU\\...\\Uninstall\\AIMClassic");
                }
                catch { }
            }

            if (allowSelfDeleteCmd)
            {
                try
                {
                    string selfExe = Assembly.GetExecutingAssembly().Location;
                    string uninstPath = Path.Combine(dir, "Uninstall.exe");
                    if (!string.IsNullOrEmpty(selfExe) && File.Exists(uninstPath) &&
                        string.Equals(Path.GetFullPath(selfExe), Path.GetFullPath(uninstPath), StringComparison.OrdinalIgnoreCase))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = string.Format("/c ping -n 3 127.0.0.1 >nul & del /f /q \"{0}\" & rmdir \"{1}\"", uninstPath, dir),
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        Process.Start(psi);
                    }
                }
                catch { }
            }

            log(100, "Uninstall Complete", "AOL Instant Messenger has been removed.");
        }

        public static void CreateShortcut(string shortcutPath, string targetExe, string workingDir, string iconPath, string description)
        {
            CreateShortcutWithArgs(shortcutPath, targetExe, "", workingDir, iconPath, description);
        }

        public static void CreateShortcutWithArgs(string shortcutPath, string targetExe, string arguments, string workingDir, string iconPath, string description)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;
            object shell = Activator.CreateInstance(shellType);
            try
            {
                object lnk = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
                Type lt = lnk.GetType();
                SetComProp(lt, lnk, "TargetPath", targetExe);
                if (!string.IsNullOrEmpty(arguments)) SetComProp(lt, lnk, "Arguments", arguments);
                SetComProp(lt, lnk, "WorkingDirectory", workingDir);
                SetComProp(lt, lnk, "Description", description);
                string iconLoc = (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath)) ? iconPath + ",0" : targetExe + ",0";
                SetComProp(lt, lnk, "IconLocation", iconLoc);
                lt.InvokeMember("Save", BindingFlags.InvokeMethod, null, lnk, null);
                Marshal.ReleaseComObject(lnk);
            }
            finally
            {
                Marshal.ReleaseComObject(shell);
            }
        }

        private static void SetComProp(Type t, object target, string propName, object val)
        {
            t.InvokeMember(propName, BindingFlags.SetProperty, null, target, new object[] { val });
        }

        private static string BuildReadmeText(string dir, InstallOptions opts)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=================================================================");
            sb.AppendLine("  AOL Instant Messenger 5.9 / 6.0 (AIM Classic Edition)");
            sb.AppendLine("  Classic Windows XP Desktop Revival (2005-2008)");
            sb.AppendLine("=================================================================");
            sb.AppendLine();
            sb.AppendLine("Installed Folder: " + dir);
            sb.AppendLine("Main Executable : AIMClassic.exe");
            sb.AppendLine("Installed On    : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            sb.AppendLine("AOL Today Mode  : " + (opts.ClassicAolTodayPromo ? "Classic 2005 AOL.com Start Mode" : "Standard AIM Today"));
            sb.AppendLine();
            sb.AppendLine("Features Included:");
            sb.AppendLine(" - Classic Windows XP Luna Blue UI & 21x21 Caption Buttons");
            sb.AppendLine(" - Screen Name Registration, Profiles & Buddy Management");
            sb.AppendLine(" - 42 Retro 2000s GIF Avatars & 19 Classic WAV Notification Sounds");
            sb.AppendLine(" - Built-in Bilingual AI Companion + API Keys Manager");
            sb.AppendLine("   (OpenAI, OpenRouter, Groq, Google Gemini, Local Ollama)");
            sb.AppendLine();
            sb.AppendLine("To uninstall, open Settings -> Apps -> AOL Instant Messenger 5.9");
            sb.AppendLine("or run Uninstall.exe in this folder.");
            return sb.ToString();
        }
    }
}
