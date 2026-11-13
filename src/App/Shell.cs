// Copyright (c) marco3079. Licensed under the MIT License.
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace EmojiColumn.App
{
    /// <summary>Thin wrappers over the Windows Shell / process APIs the app needs.</summary>
    internal static class Shell
    {
        private const uint SHCNE_UPDATEITEM = 0x00002000;
        private const uint SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;
        private const uint SHCNF_PATHW = 0x0005;
        private const uint SHCNF_FLUSHNOWAIT = 0x3000;
        private const uint MOVEFILE_DELAY_UNTIL_REBOOT = 0x4;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern void SHChangeNotify(uint eventId, uint flags, string item1, IntPtr item2);

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(uint eventId, uint flags, IntPtr item1, IntPtr item2);

        [DllImport("propsys.dll", CharSet = CharSet.Unicode)]
        private static extern int PSRegisterPropertySchema(string path);

        [DllImport("propsys.dll", CharSet = CharSet.Unicode)]
        private static extern int PSUnregisterPropertySchema(string path);

        [DllImport("propsys.dll")]
        private static extern int PSRefreshPropertySchema();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool MoveFileEx(string existing, string newName, uint flags);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AttachConsole(int processId);

        /// <summary>Tells Explorer that a file's properties changed so the column refreshes.</summary>
        public static void NotifyItemChanged(string path)
        {
            SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATHW | SHCNF_FLUSHNOWAIT, path, IntPtr.Zero);
        }

        public static void NotifyAssociationsChanged()
        {
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }

        public static void RegisterSchema(string path)
        {
            int hr = PSRegisterPropertySchema(path);
            if (hr < 0) Marshal.ThrowExceptionForHR(hr);
            PSRefreshPropertySchema();
        }

        public static void UnregisterSchema(string path)
        {
            PSUnregisterPropertySchema(path); // best effort
            PSRefreshPropertySchema();
        }

        public static void DeleteOnReboot(string path)
        {
            MoveFileEx(path, null, MOVEFILE_DELAY_UNTIL_REBOOT);
        }

        public static bool IsAdmin()
        {
            using (WindowsIdentity id = WindowsIdentity.GetCurrent())
            {
                return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// <summary>Runs this exe elevated with <paramref name="args"/> and waits. Returns the exit code, or -1 if the user refused UAC.</summary>
        public static int RunElevated(string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName, args);
            psi.Verb = "runas";
            psi.UseShellExecute = true;
            try
            {
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit();
                    return p.ExitCode;
                }
            }
            catch (Win32Exception e)
            {
                if (e.NativeErrorCode == 1223) return -1; // ERROR_CANCELLED
                throw;
            }
        }

        public static void RestartExplorer()
        {
            foreach (Process p in Process.GetProcessesByName("explorer"))
            {
                try { p.Kill(); p.WaitForExit(5000); } catch (Exception) { }
                p.Dispose();
            }
            Thread.Sleep(1500);
            // Winlogon normally restarts the shell by itself; start it if it did not
            // (never from an elevated process: the shell would run as administrator).
            if (Process.GetProcessesByName("explorer").Length == 0 && !IsAdmin())
            {
                Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
            }
        }

        public static bool AttachParentConsole()
        {
            return AttachConsole(-1);
        }

        public static string Quote(string s)
        {
            return "\"" + s.Replace("\"", "\\\"") + "\"";
        }

        public static string JoinArgs(params string[] parts)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string p in parts)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(p.IndexOfAny(new[] { ' ', '\t', '"' }) >= 0 || p.Length == 0 ? Quote(p) : p);
            }
            return sb.ToString();
        }
    }
}
