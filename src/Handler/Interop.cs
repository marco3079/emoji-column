// Copyright (c) marco3079. Licensed under the MIT License.
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace EmojiColumn.Handler
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PropertyKey
    {
        public Guid FormatId;
        public uint PropertyId;

        public PropertyKey(Guid formatId, uint propertyId)
        {
            FormatId = formatId;
            PropertyId = propertyId;
        }

        public bool Matches(PropertyKey other)
        {
            return FormatId == other.FormatId && PropertyId == other.PropertyId;
        }
    }

    /// <summary>
    /// Raw PROPVARIANT (24 bytes on x64, 16 on x86). Values coming from a chained
    /// handler are copied verbatim with PropVariantCopy, so the layout only has to
    /// be big enough and correctly aligned.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct PropVariant
    {
        [FieldOffset(0)] public ushort VarType;
        [FieldOffset(8)] public IntPtr Pointer;
        [FieldOffset(8)] public long Int64;
        [FieldOffset(16)] public IntPtr Extra;

        public const ushort VT_EMPTY = 0;
        public const ushort VT_LPWSTR = 31;

        public static PropVariant FromString(string value)
        {
            PropVariant pv = new PropVariant();
            if (!string.IsNullOrEmpty(value))
            {
                pv.VarType = VT_LPWSTR;
                pv.Pointer = Marshal.StringToCoTaskMemUni(value); // freed by caller's PropVariantClear
            }
            return pv;
        }
    }

    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPropertyStore
    {
        [PreserveSig] int GetCount(out uint count);
        [PreserveSig] int GetAt(uint index, out PropertyKey key);
        [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant value);
        [PreserveSig] int SetValue(ref PropertyKey key, ref PropVariant value);
        [PreserveSig] int Commit();
    }

    [ComImport, Guid("C8E2D566-186E-4D49-BF41-6909EAD56ACC"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPropertyStoreCapabilities
    {
        [PreserveSig] int IsPropertyWritable(ref PropertyKey key);
    }

    [ComImport, Guid("B7D14566-0509-4CCE-A71F-0A554233BD9B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInitializeWithFile
    {
        [PreserveSig] int Initialize([MarshalAs(UnmanagedType.LPWStr)] string filePath, uint mode);
    }

    [ComImport, Guid("B824B49D-22AC-4161-AC8A-9916E8FA3F7F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInitializeWithStream
    {
        [PreserveSig] int Initialize(IStream stream, uint mode);
    }

    internal static class NativeMethods
    {
        public const int S_OK = 0;
        public const int S_FALSE = 1;
        public const int E_INVALIDARG = unchecked((int)0x80070057);
        public const int E_UNEXPECTED = unchecked((int)0x8000FFFF);
        public const int STG_E_ACCESSDENIED = unchecked((int)0x80030005);
        public const int ERROR_ALREADY_INITIALIZED = unchecked((int)0x800704DF);

        public const uint STGM_READ = 0x0;
        public const uint STGM_READWRITE = 0x2;
        public const uint STGM_SHARE_DENY_NONE = 0x40;

        [DllImport("ole32.dll")]
        public static extern int PropVariantCopy(out PropVariant dest, ref PropVariant source);

        [DllImport("ole32.dll")]
        public static extern int PropVariantClear(ref PropVariant value);

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int SHCreateStreamOnFileEx(string file, uint mode, uint attributes,
            [MarshalAs(UnmanagedType.Bool)] bool create, IStream template, out IStream stream);
    }
}
