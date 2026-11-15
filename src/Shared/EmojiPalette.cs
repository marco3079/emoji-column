// Copyright (c) marco3079. Licensed under the MIT License.
using System;

namespace EmojiColumn
{
    /// <summary>
    /// The emojis that have a color icon in the "Emoji" column. Explorer can only
    /// draw icons declared in the property schema, so this list is fixed at build
    /// time: build.ps1 reads it to generate the schema enumeration and the icon DLL.
    /// Index + 1 is the enumeration value and icon resource id is 100 + index + 1.
    /// The second field is the asset name in Microsoft's Fluent Emoji (MIT).
    /// Append new entries at the end only, so existing values never change.
    /// </summary>
    internal static class EmojiPalette
    {
        public static readonly string[][] Entries =
        {
            new[] { "✅", "Check mark button" },
            new[] { "☑️", "Check box with check" },
            new[] { "❌", "Cross mark" },
            new[] { "⚠️", "Warning" },
            new[] { "⏳", "Hourglass not done" },
            new[] { "🕒", "Three oclock" },
            new[] { "📌", "Pushpin" },
            new[] { "⭐", "Star" },
            new[] { "🔥", "Fire" },
            new[] { "❗", "Red exclamation mark" },
            new[] { "❓", "Red question mark" },
            new[] { "💡", "Light bulb" },
            new[] { "🔴", "Red circle" },
            new[] { "🟠", "Orange circle" },
            new[] { "🟡", "Yellow circle" },
            new[] { "🟢", "Green circle" },
            new[] { "🔵", "Blue circle" },
            new[] { "🟣", "Purple circle" },
            new[] { "🟤", "Brown circle" },
            new[] { "⚫", "Black circle" },
            new[] { "⚪", "White circle" },
            new[] { "🟥", "Red square" },
            new[] { "🟩", "Green square" },
            new[] { "🟦", "Blue square" },
            new[] { "💰", "Money bag" },
            new[] { "💳", "Credit card" },
            new[] { "🧾", "Receipt" },
            new[] { "📊", "Bar chart" },
            new[] { "📈", "Chart increasing" },
            new[] { "📉", "Chart decreasing" },
            new[] { "🏦", "Bank" },
            new[] { "💵", "Dollar banknote" },
            new[] { "📄", "Page facing up" },
            new[] { "📁", "File folder" },
            new[] { "🗂️", "Card index dividers" },
            new[] { "📝", "Memo" },
            new[] { "🔒", "Locked" },
            new[] { "🔑", "Key" },
            new[] { "📎", "Paperclip" },
            new[] { "📦", "Package" },
            new[] { "🏷️", "Label" },
            new[] { "🚀", "Rocket" },
            new[] { "🎯", "Bullseye" },
            new[] { "👍", "Thumbs up" },
            new[] { "👎", "Thumbs down" },
            new[] { "❤️", "Red heart" },
            new[] { "🗑️", "Wastebasket" },
            new[] { "💤", "Zzz" },
        };

        /// <summary>Enumeration value (1-based) of <paramref name="emoji"/>, or 0 when it has no color icon.</summary>
        public static uint ValueOf(string emoji)
        {
            if (string.IsNullOrEmpty(emoji)) return 0;
            string key = Normalize(emoji);
            for (int i = 0; i < Entries.Length; i++)
            {
                if (Normalize(Entries[i][0]) == key) return (uint)(i + 1);
            }
            return 0;
        }

        // Ignore emoji/text variation selectors so "⚠" and "⚠️" match.
        private static string Normalize(string s)
        {
            return s.Replace("️", "").Replace("︎", "").Trim();
        }
    }
}
