// Copyright (c) marco3079. Licensed under the MIT License.
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace EmojiColumn
{
    /// <summary>
    /// Reads and writes the emoji tag of a file. The tag is stored in an NTFS
    /// alternate data stream (<c>file.ext:emoji</c>), so the file name and the
    /// file content are never touched.
    /// </summary>
    internal static class EmojiStore
    {
        public const string StreamName = "emoji";
        private const int MaxBytes = 256;

        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_ALL = 0x7; // read | write | delete
        private const uint OPEN_EXISTING = 3;
        private const uint CREATE_ALWAYS = 2;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private const int ERROR_FILE_NOT_FOUND = 2;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFileW(string name, uint access, uint share,
            IntPtr security, uint disposition, uint flags, IntPtr template);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFileW(string name);

        private static string StreamPath(string path)
        {
            return path + ":" + StreamName;
        }

        /// <summary>Returns the emoji of <paramref name="path"/>, or null when it has none.</summary>
        public static string Read(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            using (SafeFileHandle h = CreateFileW(StreamPath(path), GENERIC_READ, FILE_SHARE_ALL,
                       IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero))
            {
                if (h.IsInvalid) return null;
                using (FileStream fs = new FileStream(h, FileAccess.Read))
                {
                    byte[] buffer = new byte[MaxBytes];
                    int total = 0, n;
                    while (total < MaxBytes && (n = fs.Read(buffer, total, MaxBytes - total)) > 0) total += n;
                    string value = new UTF8Encoding(false).GetString(buffer, 0, total).Trim('﻿', ' ', '\r', '\n', '\t', '\0');
                    return value.Length == 0 ? null : value;
                }
            }
        }

        /// <summary>Sets the emoji of <paramref name="path"/>. Null or empty removes it.</summary>
        public static void Write(string path, string emoji)
        {
            if (string.IsNullOrWhiteSpace(emoji)) { Clear(path); return; }
            byte[] data = new UTF8Encoding(false).GetBytes(emoji.Trim());
            if (data.Length > MaxBytes) throw new ArgumentException("Emoji text is too long (max " + MaxBytes + " bytes).");

            PreservingTimestamps(path, delegate
            {
                using (SafeFileHandle h = CreateFileW(StreamPath(path), GENERIC_WRITE, 0,
                           IntPtr.Zero, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero))
                {
                    if (h.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not write emoji to " + path);
                    using (FileStream fs = new FileStream(h, FileAccess.Write))
                    {
                        fs.Write(data, 0, data.Length);
                    }
                }
            });
        }

        /// <summary>Removes the emoji of <paramref name="path"/> (no-op when it has none).</summary>
        public static void Clear(string path)
        {
            if (Read(path) == null) return;
            PreservingTimestamps(path, delegate
            {
                if (!DeleteFileW(StreamPath(path)))
                {
                    int err = Marshal.GetLastWin32Error();
                    if (err != ERROR_FILE_NOT_FOUND) throw new Win32Exception(err, "Could not remove emoji from " + path);
                }
            });
        }

        /// <summary>
        /// Writing an alternate stream bumps the file's "Date modified" on NTFS.
        /// Tagging a file must not reorder a folder sorted by date, so restore it.
        /// Read-only files are temporarily made writable.
        /// </summary>
        private static void PreservingTimestamps(string path, Action action)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("File not found.", path);
            FileAttributes attrs = File.GetAttributes(path);
            DateTime modified = File.GetLastWriteTimeUtc(path);
            DateTime accessed = File.GetLastAccessTimeUtc(path);
            bool readOnly = (attrs & FileAttributes.ReadOnly) != 0;
            if (readOnly) File.SetAttributes(path, attrs & ~FileAttributes.ReadOnly);
            try
            {
                action();
            }
            finally
            {
                try
                {
                    File.SetLastWriteTimeUtc(path, modified);
                    File.SetLastAccessTimeUtc(path, accessed);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                if (readOnly) File.SetAttributes(path, attrs);
            }
        }
    }
}
