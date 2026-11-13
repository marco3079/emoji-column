// Copyright (c) marco3079. Licensed under the MIT License.
using System;

namespace EmojiColumn
{
    /// <summary>Identifiers shared by the app, the handler and the property schema.</summary>
    internal static class Ids
    {
        /// <summary>CLSID of the COM property handler.</summary>
        public const string HandlerClsid = "25F4796A-9D1C-4E44-9C7E-D768BEC844F3";

        /// <summary>FMTID of the "Emoji" property (must match schema/EmojiColumn.propdesc).</summary>
        public static readonly Guid FormatId = new Guid("F95F067F-2802-4D52-BCA0-52F0C3630128");
        public const uint PropertyId = 2;

        /// <summary>Registry key (HKLM, 64-bit view) holding install state.</summary>
        public const string ConfigKey = @"SOFTWARE\EmojiColumn";

        /// <summary>Sub-key mapping ".ext" to the CLSID of the handler that was there before us.</summary>
        public const string ChainKey = @"SOFTWARE\EmojiColumn\Chain";

        public const string PropertyHandlersKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\PropertySystem\PropertyHandlers";
    }
}
