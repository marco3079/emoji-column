// Copyright (c) marco3079. Licensed under the MIT License.
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EmojiColumn.App
{
    /// <summary>Reads the color emoji icons from EmojiColumn.Icons.dll (next to the exe).</summary>
    internal static class ColorIcons
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int PrivateExtractIcons(string file, int index, int cx, int cy,
            IntPtr[] icons, int[] ids, int count, int flags);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr icon);

        public static string DllPath
        {
            get { return Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), Installer.IconsDll); }
        }

        /// <summary>Icon reference ("dll,-id") for enumeration value <paramref name="value"/>.</summary>
        public static string Reference(string dllPath, int value)
        {
            return dllPath + ",-" + (100 + value);
        }

        /// <summary>The icon for palette value <paramref name="value"/> as a bitmap, or null.</summary>
        public static Bitmap Load(int value, int size)
        {
            string dll = DllPath;
            if (!File.Exists(dll)) return null;
            IntPtr[] handles = new IntPtr[1];
            int[] ids = new int[1];
            // A negative index selects the icon group by resource id.
            if (PrivateExtractIcons(dll, -(100 + value), size, size, handles, ids, 1, 0) < 1 || handles[0] == IntPtr.Zero) return null;
            try
            {
                using (Icon icon = Icon.FromHandle(handles[0]))
                {
                    return icon.ToBitmap();
                }
            }
            finally
            {
                DestroyIcon(handles[0]);
            }
        }
    }
}
