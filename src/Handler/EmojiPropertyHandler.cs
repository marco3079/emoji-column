// Copyright (c) marco3079. Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Win32;

namespace EmojiColumn.Handler
{
    /// <summary>
    /// Windows Shell property handler that exposes the <c>EmojiColumn.Emoji</c>
    /// property, read from the file's <c>:emoji</c> alternate data stream.
    ///
    /// When the extension already had a property handler before Emoji Column was
    /// installed (for example Office documents), that handler is created and every
    /// one of its properties is passed through, so nothing the user had is lost.
    /// </summary>
    [ComVisible(true)]
    [Guid(Ids.HandlerClsid)]
    [ClassInterface(ClassInterfaceType.None)]
    public sealed class EmojiPropertyHandler : IInitializeWithFile, IPropertyStore, IPropertyStoreCapabilities
    {
        private static readonly PropertyKey EmojiKey = new PropertyKey(Ids.FormatId, Ids.PropertyId);

        private bool _initialized;
        private string _emoji;

        // Read-only access (Explorer columns, tooltips): the chained handler's values
        // are copied here and the handler is released at once, so the file is not
        // kept open by Explorer.
        private List<PropertyKey> _cachedKeys;
        private List<PropVariant> _cachedValues;

        // Read/write access (Details pane edits): the chained handler stays alive.
        private IPropertyStore _inner;
        private IStream _innerStream;

        ~EmojiPropertyHandler()
        {
            if (_cachedValues == null) return;
            for (int i = 0; i < _cachedValues.Count; i++)
            {
                PropVariant pv = _cachedValues[i];
                NativeMethods.PropVariantClear(ref pv);
            }
        }

        // ---------------------------------------------------------------- IInitializeWithFile

        public int Initialize(string filePath, uint mode)
        {
            if (_initialized) return NativeMethods.ERROR_ALREADY_INITIALIZED;
            if (string.IsNullOrEmpty(filePath)) return NativeMethods.E_INVALIDARG;
            _initialized = true;

            try { _emoji = EmojiStore.Read(filePath); }
            catch (Exception) { _emoji = null; }

            try { InitializeChained(filePath, mode); }
            catch (Exception) { ReleaseInner(); }

            return NativeMethods.S_OK;
        }

        private void InitializeChained(string filePath, uint mode)
        {
            Guid clsid;
            if (!TryGetChainedHandler(Path.GetExtension(filePath), out clsid)) return;

            Type type = Type.GetTypeFromCLSID(clsid, false);
            if (type == null) return;
            object handler = Activator.CreateInstance(type);

            int hr = NativeMethods.E_UNEXPECTED;
            IInitializeWithStream byStream = handler as IInitializeWithStream;
            IInitializeWithFile byFile = handler as IInitializeWithFile;
            if (byStream != null)
            {
                IStream stream;
                hr = NativeMethods.SHCreateStreamOnFileEx(filePath, mode | NativeMethods.STGM_SHARE_DENY_NONE, 0, false, null, out stream);
                if (hr >= 0)
                {
                    _innerStream = stream;
                    hr = byStream.Initialize(stream, mode);
                }
            }
            else if (byFile != null)
            {
                hr = byFile.Initialize(filePath, mode);
            }

            _inner = handler as IPropertyStore;
            if (hr < 0 || _inner == null) { _inner = null; ReleaseCom(handler); ReleaseInner(); return; }

            bool writable = (mode & 0x3) != 0;
            if (!writable) CacheAndRelease();
        }

        private void CacheAndRelease()
        {
            List<PropertyKey> keys = new List<PropertyKey>();
            List<PropVariant> values = new List<PropVariant>();
            uint count;
            if (_inner.GetCount(out count) >= 0)
            {
                for (uint i = 0; i < count; i++)
                {
                    PropertyKey key;
                    if (_inner.GetAt(i, out key) < 0 || key.Matches(EmojiKey)) continue;
                    PropVariant value;
                    if (_inner.GetValue(ref key, out value) < 0) continue;
                    keys.Add(key);
                    values.Add(value); // ownership moves to the cache
                }
            }
            _cachedKeys = keys;
            _cachedValues = values;
            ReleaseInner();
        }

        private void ReleaseInner()
        {
            if (_inner != null) { ReleaseCom(_inner); _inner = null; }
            if (_innerStream != null) { ReleaseCom(_innerStream); _innerStream = null; }
        }

        private static void ReleaseCom(object o)
        {
            if (o != null && Marshal.IsComObject(o)) Marshal.FinalReleaseComObject(o);
        }

        private static bool TryGetChainedHandler(string extension, out Guid clsid)
        {
            clsid = Guid.Empty;
            if (string.IsNullOrEmpty(extension)) return false;
            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (RegistryKey chain = hklm.OpenSubKey(Ids.ChainKey))
            {
                if (chain == null) return false;
                string value = chain.GetValue(extension.ToLowerInvariant()) as string;
                if (string.IsNullOrEmpty(value)) return false;
                try { clsid = new Guid(value); } catch (FormatException) { return false; }
                return clsid != new Guid(Ids.HandlerClsid);
            }
        }

        // ---------------------------------------------------------------- IPropertyStore

        private uint BaseCount
        {
            get
            {
                if (_cachedKeys != null) return (uint)_cachedKeys.Count;
                uint count;
                if (_inner != null && _inner.GetCount(out count) >= 0) return count;
                return 0;
            }
        }

        public int GetCount(out uint count)
        {
            count = BaseCount + 1;
            return NativeMethods.S_OK;
        }

        public int GetAt(uint index, out PropertyKey key)
        {
            key = new PropertyKey();
            uint baseCount = BaseCount;
            if (index == baseCount) { key = EmojiKey; return NativeMethods.S_OK; }
            if (index > baseCount) return NativeMethods.E_INVALIDARG;
            if (_cachedKeys != null) { key = _cachedKeys[(int)index]; return NativeMethods.S_OK; }
            return _inner.GetAt(index, out key);
        }

        public int GetValue(ref PropertyKey key, out PropVariant value)
        {
            value = new PropVariant();
            if (key.Matches(EmojiKey))
            {
                value = PropVariant.FromString(_emoji);
                return NativeMethods.S_OK;
            }
            if (_cachedKeys != null)
            {
                for (int i = 0; i < _cachedKeys.Count; i++)
                {
                    if (!_cachedKeys[i].Matches(key)) continue;
                    PropVariant source = _cachedValues[i];
                    return NativeMethods.PropVariantCopy(out value, ref source);
                }
                return NativeMethods.S_OK; // unknown key: VT_EMPTY
            }
            if (_inner != null) return _inner.GetValue(ref key, out value);
            return NativeMethods.S_OK;
        }

        public int SetValue(ref PropertyKey key, ref PropVariant value)
        {
            if (key.Matches(EmojiKey) || _inner == null) return NativeMethods.STG_E_ACCESSDENIED;
            return _inner.SetValue(ref key, ref value);
        }

        public int Commit()
        {
            if (_inner == null) return NativeMethods.S_OK;
            return _inner.Commit();
        }

        // ---------------------------------------------------------------- IPropertyStoreCapabilities

        public int IsPropertyWritable(ref PropertyKey key)
        {
            if (key.Matches(EmojiKey) || _inner == null) return NativeMethods.S_FALSE;
            IPropertyStoreCapabilities caps = _inner as IPropertyStoreCapabilities;
            return caps != null ? caps.IsPropertyWritable(ref key) : NativeMethods.S_OK;
        }
    }
}
