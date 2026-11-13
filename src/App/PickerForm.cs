// Copyright (c) marco3079. Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace EmojiColumn.App
{
    /// <summary>Small window to pick the emoji for one or more files.</summary>
    internal sealed class PickerForm : Form
    {
        private static readonly string[] Palette =
        {
            "✅", "☑️", "❌", "⚠️", "⏳", "🕒", "📌", "⭐", "🔥", "❗", "❓", "💡",
            "🔴", "🟠", "🟡", "🟢", "🔵", "🟣", "🟤", "⚫", "⚪", "🟥", "🟩", "🟦",
            "💰", "💳", "🧾", "📊", "📈", "📉", "🏦", "💵", "📄", "📁", "🗂️", "📝",
            "🔒", "🔑", "📎", "📦", "🏷️", "🚀", "🎯", "👍", "👎", "❤️", "🗑️", "💤"
        };

        private readonly List<string> _files = new List<string>();
        private readonly Label _title;
        private readonly ListBox _list;
        private readonly TextBox _custom;
        private readonly Font _emojiFont;

        public PickerForm(IEnumerable<string> files)
        {
            Text = S.AppName;
            Font = SystemFonts.MessageBoxFont;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;
            KeyPreview = true;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            _emojiFont = new Font("Segoe UI Emoji", 16F);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(14);
            root.ColumnCount = 1;
            root.AutoSize = true;
            Controls.Add(root);

            _title = new Label();
            _title.AutoSize = true;
            _title.Font = new Font(Font.FontFamily, 11F, FontStyle.Bold);
            _title.Margin = new Padding(0, 0, 0, 8);
            root.Controls.Add(_title);

            _list = new ListBox();
            _list.Width = 520;
            _list.Height = 84;
            _list.IntegralHeight = false;
            _list.HorizontalScrollbar = true;
            _list.Margin = new Padding(0, 0, 0, 10);
            root.Controls.Add(_list);

            FlowLayoutPanel grid = new FlowLayoutPanel();
            grid.Width = 520;
            grid.AutoSize = true;
            grid.Margin = new Padding(0, 0, 0, 10);
            ToolTip tip = new ToolTip();
            foreach (string emoji in Palette)
            {
                Button b = new Button();
                b.Text = emoji;
                b.Font = _emojiFont;
                b.Size = new Size(41, 41);
                b.Margin = new Padding(1);
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 0;
                b.UseCompatibleTextRendering = false;
                b.TabStop = true;
                string value = emoji;
                b.Click += delegate { ApplyAndClose(value); };
                tip.SetToolTip(b, emoji);
                grid.Controls.Add(b);
            }
            root.Controls.Add(grid);

            Label customLabel = new Label();
            customLabel.AutoSize = true;
            customLabel.Text = S.Custom;
            root.Controls.Add(customLabel);

            FlowLayoutPanel customRow = new FlowLayoutPanel();
            customRow.AutoSize = true;
            customRow.WrapContents = false;
            customRow.Margin = new Padding(0, 4, 0, 12);
            _custom = new TextBox();
            _custom.Width = 200;
            _custom.Font = new Font("Segoe UI Emoji", 12F);
            _custom.MaxLength = 32;
            customRow.Controls.Add(_custom);
            Button apply = new Button();
            apply.Text = S.Apply;
            apply.AutoSize = true;
            apply.Click += delegate { ApplyAndClose(_custom.Text); };
            customRow.Controls.Add(apply);
            root.Controls.Add(customRow);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.Anchor = AnchorStyles.Right;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Width = 520;
            Button close = new Button();
            close.Text = S.Close;
            close.AutoSize = true;
            close.DialogResult = DialogResult.Cancel;
            close.Click += delegate { Close(); };
            Button remove = new Button();
            remove.Text = S.Remove;
            remove.AutoSize = true;
            remove.Click += delegate { ApplyAndClose(null); };
            buttons.Controls.Add(close);
            buttons.Controls.Add(remove);
            root.Controls.Add(buttons);

            AcceptButton = apply;
            CancelButton = close;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            AddFiles(files);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _emojiFont.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>Adds files sent by another instance (called from a worker thread).</summary>
        public void AddFilesThreadSafe(string[] files)
        {
            if (IsDisposed) return;
            if (IsHandleCreated) BeginInvoke(new Action(() => AddFiles(files)));
            else AddFiles(files);
        }

        private void AddFiles(IEnumerable<string> files)
        {
            foreach (string f in files)
            {
                if (!File.Exists(f) || _files.Contains(f, StringComparer.OrdinalIgnoreCase)) continue;
                _files.Add(f);
                string current = SafeRead(f);
                _list.Items.Add((current ?? "·") + "   " + Path.GetFileName(f));
            }
            _title.Text = _files.Count == 0 ? S.NoFiles : S.PickTitle(_files.Count);
            if (_files.Count == 1)
            {
                string current = SafeRead(_files[0]);
                if (current != null && _custom.Text.Length == 0) _custom.Text = current;
            }
        }

        private static string SafeRead(string f)
        {
            try { return EmojiStore.Read(f); } catch (Exception) { return null; }
        }

        private void ApplyAndClose(string emoji)
        {
            List<string> errors = new List<string>();
            foreach (string f in _files)
            {
                try
                {
                    EmojiStore.Write(f, emoji);
                    Shell.NotifyItemChanged(f);
                }
                catch (Exception e)
                {
                    errors.Add(Path.GetFileName(f) + ": " + e.Message);
                }
            }
            if (errors.Count > 0)
            {
                MessageBox.Show(this, S.Failed + "\n\n" + string.Join("\n", errors.Take(10)), S.AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (!string.IsNullOrWhiteSpace(emoji))
            {
                WarnIfNotEnabled();
            }
            Close();
        }

        private void WarnIfNotEnabled()
        {
            if (!Installer.IsInstalled) return;
            HashSet<string> enabled = new HashSet<string>(Installer.InstalledExtensions, StringComparer.OrdinalIgnoreCase);
            string[] missing = _files.Select(f => Path.GetExtension(f).ToLowerInvariant())
                .Where(e => e.Length > 0 && !enabled.Contains(e)).Distinct().ToArray();
            if (missing.Length == 0) return;
            MessageBox.Show(this, string.Format(S.NotEnabled, string.Join(", ", missing)), S.AppName,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
