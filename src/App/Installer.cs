// Copyright (c) marco3079. Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;

namespace EmojiColumn.App
{
    /// <summary>
    /// Registers / unregisters Emoji Column with Windows. Everything lives in
    /// HKLM (64-bit view) because the Shell only reads property handlers there.
    /// Every change is reversible: a handler that existed before is remembered
    /// under <see cref="Ids.ChainKey"/> and restored on uninstall.
    /// </summary>
    internal static class Installer
    {
        public const string ExeName = "EmojiColumn.exe";
        public const string HandlerDll = "EmojiColumn.Handler.dll";
        public const string SchemaFile = "EmojiColumn.propdesc";

        private const string HandlerClass = "EmojiColumn.Handler.EmojiPropertyHandler";
        private const string ManagedCategory = "{62C8FE65-4EBB-45E7-B440-6E39B2CDBF29}";
        private const string UninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\EmojiColumn";
        private const string MenuKey = @"SOFTWARE\Classes\*\shell\EmojiColumn";

        private static string ClsidBraced { get { return "{" + Ids.HandlerClsid + "}"; } }
        private static string ClsidKey { get { return @"SOFTWARE\Classes\CLSID\" + ClsidBraced; } }

        /// <summary>Extensions that get the column by default (none of them has a handler on a clean Windows).</summary>
        public static readonly string[] DefaultExtensions =
        {
            ".csv", ".txt", ".pdf", ".md", ".json", ".xml", ".log", ".zip", ".7z", ".rar",
            ".html", ".ini", ".sql", ".ps1", ".bat", ".py", ".js", ".cs", ".svg"
        };

        /// <summary>Common types shown in the manager that usually have a handler already (opt-in, chained).</summary>
        public static readonly string[] OptionalExtensions =
        {
            ".xlsx", ".xls", ".docx", ".doc", ".pptx", ".jpg", ".jpeg", ".png", ".mp4", ".mp3", ".ofx"
        };

        public static string InstallDir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "EmojiColumn"); }
        }

        public static string Version
        {
            get
            {
                Version v = Assembly.GetExecutingAssembly().GetName().Version;
                return v.Major + "." + v.Minor + "." + v.Build;
            }
        }

        private static RegistryKey Hklm()
        {
            return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        }

        // ------------------------------------------------------------------ state

        public static bool IsInstalled
        {
            get
            {
                using (RegistryKey hklm = Hklm())
                using (RegistryKey k = hklm.OpenSubKey(Ids.ConfigKey))
                {
                    return k != null && k.GetValue("Version") != null;
                }
            }
        }

        public static string InstalledVersion
        {
            get
            {
                using (RegistryKey hklm = Hklm())
                using (RegistryKey k = hklm.OpenSubKey(Ids.ConfigKey))
                {
                    return k == null ? null : k.GetValue("Version") as string;
                }
            }
        }

        public static string[] InstalledExtensions
        {
            get
            {
                using (RegistryKey hklm = Hklm())
                using (RegistryKey k = hklm.OpenSubKey(Ids.ConfigKey))
                {
                    string[] v = k == null ? null : k.GetValue("Extensions") as string[];
                    return v ?? new string[0];
                }
            }
        }

        /// <summary>CLSID of the handler that is not ours and currently owns (or owned, before us) the extension, or null.</summary>
        public static string ForeignHandler(string ext)
        {
            using (RegistryKey hklm = Hklm())
            {
                using (RegistryKey chain = hklm.OpenSubKey(Ids.ChainKey))
                {
                    string chained = chain == null ? null : chain.GetValue(ext) as string;
                    if (!string.IsNullOrEmpty(chained)) return chained;
                }
                using (RegistryKey k = hklm.OpenSubKey(Ids.PropertyHandlersKey + "\\" + ext))
                {
                    string current = k == null ? null : k.GetValue(null) as string;
                    if (string.IsNullOrEmpty(current) || IsOurs(current)) return null;
                    return current;
                }
            }
        }

        private static bool IsOurs(string clsid)
        {
            return string.Equals(clsid, ClsidBraced, StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizeExtension(string ext)
        {
            if (ext == null) return null;
            ext = ext.Trim().ToLowerInvariant();
            if (ext.StartsWith("*")) ext = ext.Substring(1);
            if (!ext.StartsWith(".")) ext = "." + ext;
            if (ext.Length < 2 || ext.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || ext.IndexOf('.', 1) >= 0) return null;
            return ext;
        }

        // ------------------------------------------------------------------ install

        public static void Install(IEnumerable<string> extensions)
        {
            string[] exts = extensions.Select(NormalizeExtension).Where(e => e != null).Distinct().ToArray();
            if (exts.Length == 0) exts = DefaultExtensions;

            string dir = InstallDir;
            Directory.CreateDirectory(dir);
            string sourceDir = AppDomain.CurrentDomain.BaseDirectory;
            foreach (string f in new[] { ExeName, HandlerDll, SchemaFile })
            {
                CopySafe(Path.Combine(sourceDir, f), Path.Combine(dir, f));
            }
            string exe = Path.Combine(dir, ExeName);
            string dll = Path.Combine(dir, HandlerDll);

            RegisterComServer(dll);
            Shell.RegisterSchema(Path.Combine(dir, SchemaFile));

            string[] previous = InstalledExtensions;
            foreach (string ext in previous.Except(exts)) UnregisterExtension(ext);
            foreach (string ext in exts) RegisterExtension(ext);

            RegisterContextMenu(exe);

            using (RegistryKey hklm = Hklm())
            {
                using (RegistryKey k = hklm.CreateSubKey(Ids.ConfigKey))
                {
                    k.SetValue("Version", Version);
                    k.SetValue("InstallDir", dir);
                    k.SetValue("Extensions", exts, RegistryValueKind.MultiString);
                }
                using (RegistryKey u = hklm.CreateSubKey(UninstallKey))
                {
                    u.SetValue("DisplayName", "Emoji Column");
                    u.SetValue("DisplayVersion", Version);
                    u.SetValue("Publisher", "marco3079");
                    u.SetValue("DisplayIcon", exe + ",0");
                    u.SetValue("InstallLocation", dir);
                    u.SetValue("URLInfoAbout", S.RepoUrl);
                    u.SetValue("HelpLink", S.RepoUrl + "/issues");
                    u.SetValue("UninstallString", Shell.Quote(exe) + " uninstall");
                    u.SetValue("QuietUninstallString", Shell.Quote(exe) + " uninstall --quiet");
                    u.SetValue("NoModify", 1, RegistryValueKind.DWord);
                    u.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    u.SetValue("EstimatedSize", 200, RegistryValueKind.DWord);
                }
            }

            Shell.NotifyAssociationsChanged();
        }

        private static void CopySafe(string source, string target)
        {
            if (!File.Exists(source)) throw new FileNotFoundException("Missing file next to the installer.", source);
            if (string.Equals(Path.GetFullPath(source), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase)) return;
            try
            {
                File.Copy(source, target, true);
            }
            catch (IOException)
            {
                // The handler DLL is loaded by Explorer: move it aside (allowed while
                // loaded), install the new one, and delete the old one on reboot.
                string old = target + ".old-" + DateTime.Now.Ticks;
                File.Move(target, old);
                Shell.DeleteOnReboot(old);
                File.Copy(source, target, true);
            }
        }

        private static void RegisterComServer(string dll)
        {
            string assembly = AssemblyName.GetAssemblyName(dll).FullName;
            using (RegistryKey hklm = Hklm())
            {
                hklm.DeleteSubKeyTree(ClsidKey, false);
                using (RegistryKey k = hklm.CreateSubKey(ClsidKey))
                {
                    k.SetValue(null, "Emoji Column Property Handler");
                    // Needed for handlers that use IInitializeWithFile (we need the path
                    // to read the :emoji stream), see "Initializing Property Handlers" on MSDN.
                    k.SetValue("DisableProcessIsolation", 1, RegistryValueKind.DWord);
                    using (RegistryKey s = k.CreateSubKey("InprocServer32"))
                    {
                        s.SetValue(null, "mscoree.dll");
                        s.SetValue("ThreadingModel", "Both");
                        s.SetValue("Class", HandlerClass);
                        s.SetValue("Assembly", assembly);
                        s.SetValue("RuntimeVersion", "v4.0.30319");
                        // Same format as RegAsm /codebase: NOT percent-escaped, or the CLR
                        // fails with 0x80070002 when the path contains spaces.
                        s.SetValue("CodeBase", "file:///" + dll.Replace('\\', '/'));
                    }
                    using (k.CreateSubKey(@"Implemented Categories\" + ManagedCategory)) { }
                }
            }
        }

        private static void RegisterExtension(string ext)
        {
            using (RegistryKey hklm = Hklm())
            using (RegistryKey k = hklm.CreateSubKey(Ids.PropertyHandlersKey + "\\" + ext))
            {
                string current = k.GetValue(null) as string;
                if (!string.IsNullOrEmpty(current) && !IsOurs(current))
                {
                    using (RegistryKey chain = hklm.CreateSubKey(Ids.ChainKey))
                    {
                        chain.SetValue(ext, current);
                    }
                }
                k.SetValue(null, ClsidBraced);
            }
        }

        private static void UnregisterExtension(string ext)
        {
            using (RegistryKey hklm = Hklm())
            {
                string chained = null;
                using (RegistryKey chain = hklm.OpenSubKey(Ids.ChainKey, true))
                {
                    if (chain != null)
                    {
                        chained = chain.GetValue(ext) as string;
                        chain.DeleteValue(ext, false);
                    }
                }
                string path = Ids.PropertyHandlersKey + "\\" + ext;
                using (RegistryKey k = hklm.OpenSubKey(path, true))
                {
                    if (k == null || !IsOurs(k.GetValue(null) as string)) return;
                    if (!string.IsNullOrEmpty(chained))
                    {
                        k.SetValue(null, chained);
                        return;
                    }
                }
                hklm.DeleteSubKeyTree(path, false);
            }
        }

        private static readonly string[][] QuickEmojis =
        {
            new[] { "âœ…", "Done", "ConcluÃ­do" },
            new[] { "â³", "Pending", "Pendente" },
            new[] { "âš ï¸", "Attention", "AtenÃ§Ã£o" },
            new[] { "âŒ", "Problem", "Problema" },
            new[] { "â­", "Important", "Importante" },
            new[] { "ðŸ”¥", "Urgent", "Urgente" },
            new[] { "ðŸ’°", "Money", "Financeiro" },
            new[] { "ðŸ“Œ", "Pinned", "Fixado" },
        };

        private static void RegisterContextMenu(string exe)
        {
            string q = Shell.Quote(exe);
            using (RegistryKey hklm = Hklm())
            {
                hklm.DeleteSubKeyTree(MenuKey, false);
                using (RegistryKey root = hklm.CreateSubKey(MenuKey))
                {
                    root.SetValue("MUIVerb", S.MenuRoot);
                    root.SetValue("Icon", exe + ",0");
                    root.SetValue("SubCommands", "");
                    root.SetValue("MultiSelectModel", "Player");

                    int n = 1;
                    foreach (string[] e in QuickEmojis)
                    {
                        AddMenuItem(root, n++, e[0] + "   " + (S.Pt ? e[2] : e[1]), q + " set " + e[0] + " \"%1\"", false);
                    }
                    AddMenuItem(root, n++, S.MenuChoose, q + " pick \"%1\"", true);
                    AddMenuItem(root, n, S.MenuRemove, q + " clear \"%1\"", false);
                }
            }
        }

        private static void AddMenuItem(RegistryKey root, int index, string text, string command, bool separatorBefore)
        {
            using (RegistryKey item = root.CreateSubKey(@"shell\" + index.ToString("00")))
            {
                item.SetValue("MUIVerb", text);
                if (separatorBefore) item.SetValue("CommandFlags", 0x20, RegistryValueKind.DWord); // ECF_SEPARATORBEFORE
                using (RegistryKey cmd = item.CreateSubKey("command"))
                {
                    cmd.SetValue(null, command);
                }
            }
        }

        // ------------------------------------------------------------------ uninstall

        public static void Uninstall()
        {
            foreach (string ext in InstalledExtensions) UnregisterExtension(ext);

            string dir = InstallDir;
            using (RegistryKey hklm = Hklm())
            {
                string configured;
                using (RegistryKey k = hklm.OpenSubKey(Ids.ConfigKey))
                {
                    configured = k == null ? null : k.GetValue("InstallDir") as string;
                }
                if (!string.IsNullOrEmpty(configured)) dir = configured;

                // Restore anything still chained (e.g. extensions removed from the list by hand).
                using (RegistryKey chain = hklm.OpenSubKey(Ids.ChainKey))
                {
                    if (chain != null)
                    {
                        foreach (string ext in chain.GetValueNames()) UnregisterExtension(ext);
                    }
                }

                hklm.DeleteSubKeyTree(MenuKey, false);
                hklm.DeleteSubKeyTree(ClsidKey, false);
                hklm.DeleteSubKeyTree(UninstallKey, false);
                hklm.DeleteSubKeyTree(Ids.ConfigKey, false);
            }

            Shell.UnregisterSchema(Path.Combine(dir, SchemaFile));
            Shell.NotifyAssociationsChanged();
            RemoveInstallDir(dir);
        }

        private static void RemoveInstallDir(string dir)
        {
            if (!Directory.Exists(dir)) return;
            string self = Process.GetCurrentProcess().MainModule.FileName;
            bool runningFromDir = self.StartsWith(dir.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase);
            foreach (string f in Directory.GetFiles(dir))
            {
                if (runningFromDir && string.Equals(f, self, StringComparison.OrdinalIgnoreCase)) continue;
                try { File.Delete(f); }
                catch (Exception) { Shell.DeleteOnReboot(f); } // DLL still loaded by Explorer
            }
            if (runningFromDir)
            {
                // Delete our own exe and the folder once this process has exited.
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe",
                    "/c ping 127.0.0.1 -n 3 > nul & del /f /q " + Shell.Quote(self) + " & rmdir /q " + Shell.Quote(dir));
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.UseShellExecute = false;
                Process.Start(psi);
            }
            else
            {
                try { Directory.Delete(dir, false); } catch (Exception) { Shell.DeleteOnReboot(dir); }
            }
        }
    }
}
