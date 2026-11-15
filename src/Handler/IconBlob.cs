// Copyright (c) marco3079. Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace EmojiColumn.Handler
{
    /// <summary>
    /// Builds the value of the icon column. A property shown with
    /// <c>drawControl="IconList"</c> holds a serialized property store
    /// (MS-PROPSTORE) that says which enumerated property to draw:
    /// <list type="bullet">
    /// <item>System.PropList.StatusIcons = "prop:EmojiColumn.EmojiIndex"</item>
    /// <item>System.PropList.StatusIconsDisplayFlag = 2 (icon only)</item>
    /// <item>EmojiColumn.EmojiIndex = n, whose &lt;enum&gt; in the schema points to the icon</item>
    /// </list>
    /// The serialization is done by Windows itself (memory property store), and the
    /// result is cached per value since there are only a few dozen of them.
    /// </summary>
    internal static class IconBlob
    {
        private const string IndexPropertyName = "EmojiColumn.EmojiIndex";
        private static readonly Dictionary<uint, byte[]> Cache = new Dictionary<uint, byte[]>();

        /// <summary>Serialized store for enumeration value <paramref name="value"/> (0 = none → null).</summary>
        public static byte[] For(uint value)
        {
            if (value == 0) return null;
            lock (Cache)
            {
                byte[] data;
                if (!Cache.TryGetValue(value, out data))
                {
                    data = Build(value);
                    Cache[value] = data;
                }
                return data;
            }
        }

        private static byte[] Build(uint value)
        {
            PropertyKey statusIcons, displayFlag;
            if (NativeMethods.PSGetPropertyKeyFromName("System.PropList.StatusIcons", out statusIcons) < 0) return null;
            if (NativeMethods.PSGetPropertyKeyFromName("System.PropList.StatusIconsDisplayFlag", out displayFlag) < 0) return null;
            PropertyKey index = new PropertyKey(Ids.FormatId, Ids.IconIndexPropertyId);

            Guid iid = typeof(IPropertyStore).GUID;
            object obj;
            if (NativeMethods.PSCreateMemoryPropertyStore(ref iid, out obj) < 0) return null;
            try
            {
                IPropertyStore store = (IPropertyStore)obj;
                Set(store, statusIcons, PropVariant.FromString("prop:" + IndexPropertyName));
                Set(store, displayFlag, PropVariant.FromUInt32(2));
                Set(store, index, PropVariant.FromUInt32(value));

                IntPtr ptr; uint size;
                if (((IPersistSerializedPropStorage)obj).GetPropertyStorage(out ptr, out size) < 0 || ptr == IntPtr.Zero) return null;
                try
                {
                    byte[] data = new byte[size];
                    Marshal.Copy(ptr, data, 0, (int)size);
                    return data;
                }
                finally
                {
                    NativeMethods.CoTaskMemFree(ptr);
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(obj);
            }
        }

        private static void Set(IPropertyStore store, PropertyKey key, PropVariant value)
        {
            try { store.SetValue(ref key, ref value); }
            finally { NativeMethods.PropVariantClear(ref value); }
        }
    }
}
