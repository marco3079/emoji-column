// Copyright (c) marco3079. Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace EmojiColumn.App
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string command = args.Length == 0 ? "" : args[0].TrimStart('-', '/').ToLowerInvariant();
            string[] rest = args.Skip(1).ToArray();
            bool quiet = rest.Any(a => a == "--quiet" || a == "/quiet");

            try
            {
                switch (command)
                {
                    case "":
                        Application.Run(new MainForm());
                        return 0;
                    case "pick":
                        return Pick(Files(rest));
                    case "set":
                        if (rest.Length < 2) return UsageError();
                        return Set(rest[0], Files(rest.Skip(1)));
                    case "clear":
                        return Set(null, Files(rest));
                    case "get":
                        return Get(Files(rest));
                    case "install":
                        return Install(ParseExtensions(rest), quiet);
                    case "uninstall":
                        return Uninstall(quiet);
                    case "help":
                    case "?":
                    case "h":
                        Print(S.Usage);
                        return 0;
                    default:
                        return UsageError();
                }
            }
            catch (Exception e)
            {
                if (quiet || command == "set" || command == "clear" || command == "get") PrintError(e.Message);
                else MessageBox.Show(S.Failed + "\n\n" + e.Message, S.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        private static List<string> Files(IEnumerable<string> args)
        {
            return args.Where(a => !a.StartsWith("--")).SelectMany(Expand).Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>Expands "*.csv"-style wildcards (cmd and PowerShell don't do it for native programs).</summary>
        private static IEnumerable<string> Expand(string arg)
        {
            string full = Path.GetFullPath(arg.Replace('*', '_').Replace('?', '_'));
            if (arg.IndexOfAny(new[] { '*', '?' }) < 0) return new[] { Path.GetFullPath(arg) };
            string dir = Path.GetDirectoryName(full);
            string pattern = Path.GetFileName(arg);
            return Directory.Exists(dir) ? Directory.GetFiles(dir, pattern) : new string[0];
        }

        private static IEnumerable<string> ParseExtensions(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--ext")
                {
                    return args[i + 1].Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                }
            }
            return Installer.IsInstalled ? Installer.InstalledExtensions : Installer.DefaultExtensions;
        }

        private static int Pick(List<string> files)
        {
            using (SingleInstance instance = SingleInstance.TryBecomePrimary(files))
            {
                if (instance == null) return 0; // handed over to the running picker
                PickerForm form = new PickerForm(files);
                instance.OnFilesReceived(form.AddFilesThreadSafe);
                Application.Run(form);
            }
            return 0;
        }

        private static int Set(string emoji, List<string> files)
        {
            int failed = 0;
            foreach (string f in files)
            {
                try
                {
                    EmojiStore.Write(f, emoji);
                    Shell.NotifyItemChanged(f);
                }
                catch (Exception e)
                {
                    failed++;
                    PrintError(f + ": " + e.Message);
                }
            }
            return failed == 0 ? 0 : 1;
        }

        private static int Get(List<string> files)
        {
            StringWriterHelper sb = new StringWriterHelper();
            foreach (string f in files) sb.Line((EmojiStore.Read(f) ?? "-") + "\t" + f);
            Print(sb.ToString());
            return 0;
        }

        private static int Install(IEnumerable<string> extensions, bool quiet)
        {
            if (!Shell.IsAdmin())
            {
                // Elevated child does the work silently; restarting Explorer happens
                // here, so the new Explorer does not inherit administrator rights.
                int code = Shell.RunElevated(Shell.JoinArgs("install", "--ext", string.Join(",", extensions), "--quiet"));
                if (code == 0 && !quiet) AskRestart(S.InstallDone);
                return code < 0 ? 1 : code;
            }
            Installer.Install(extensions);
            if (!quiet) AskRestart(S.InstallDone);
            return 0;
        }

        private static int Uninstall(bool quiet)
        {
            if (!quiet && MessageBox.Show(S.UninstallConfirm, S.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return 1;
            }
            if (!Shell.IsAdmin())
            {
                int code = Shell.RunElevated("uninstall --quiet");
                if (code == 0 && !quiet) AskRestart(S.UninstallDone);
                return code < 0 ? 1 : code;
            }
            Installer.Uninstall();
            if (!quiet) AskRestart(S.UninstallDone);
            return 0;
        }

        internal static void AskRestart(string message)
        {
            if (MessageBox.Show(message, S.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                Shell.RestartExplorer();
            }
        }

        private static int UsageError()
        {
            Print(S.Usage);
            return 2;
        }

        private static bool _consoleAttached;

        private static void Print(string text)
        {
            if (!_consoleAttached) _consoleAttached = Shell.AttachParentConsole();
            if (_consoleAttached)
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                Console.WriteLine();
                Console.Write(text);
            }
            else
            {
                MessageBox.Show(text, S.AppName);
            }
        }

        private static void PrintError(string text)
        {
            if (!_consoleAttached) _consoleAttached = Shell.AttachParentConsole();
            if (_consoleAttached) Console.Error.WriteLine(text);
        }

        private sealed class StringWriterHelper
        {
            private readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder();
            public void Line(string s) { _sb.AppendLine(s); }
            public override string ToString() { return _sb.ToString(); }
        }
    }
}
