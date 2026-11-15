// Copyright (c) marco3079. Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace EmojiColumn.App
{
    /// <summary>Small editor for the "My description" text of one or more files.</summary>
    internal sealed class DescriptionForm : Form
    {
        private readonly List<string> _files = new List<string>();
        private readonly Label _title;
        private readonly Label _note;
        private readonly TextBox _text;

        public DescriptionForm(IEnumerable<string> files)
        {
            Text = S.AppName;
            Font = SystemFonts.MessageBoxFont;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(14);
            root.ColumnCount = 1;
            root.AutoSize = true;
            Controls.Add(root);

            _title = new Label();
            _title.AutoSize = true;
            _title.Font = new Font(Font.FontFamily, 11F, FontStyle.Bold);
            _title.Margin = new Padding(0, 0, 0, 4);
            root.Controls.Add(_title);

            Label hint = new Label();
            hint.AutoSize = true;
            hint.Text = S.DescHint;
            hint.ForeColor = SystemColors.GrayText;
            hint.Margin = new Padding(0, 0, 0, 8);
            root.Controls.Add(hint);

            _text = new TextBox();
            _text.Multiline = true;
            _text.AcceptsReturn = false; // Enter saves; descriptions are one line in the column
            _text.ScrollBars = ScrollBars.Vertical;
            _text.Width = 460;
            _text.Height = 90;
            _text.MaxLength = 2000;
            _text.Font = new Font("Segoe UI", 10F);
            root.Controls.Add(_text);

            _note = new Label();
            _note.AutoSize = true;
            _note.MaximumSize = new Size(460, 0);
            _note.ForeColor = Color.DarkOrange;
            _note.Visible = false;
            _note.Margin = new Padding(0, 6, 0, 0);
            root.Controls.Add(_note);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Width = 460;
            buttons.Margin = new Padding(0, 12, 0, 0);
            Button cancel = new Button();
            cancel.Text = S.Cancel;
            cancel.AutoSize = true;
            cancel.DialogResult = DialogResult.Cancel;
            Button save = new Button();
            save.Text = S.Save;
            save.AutoSize = true;
            save.Click += delegate { SaveAndClose(); };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);
            root.Controls.Add(buttons);

            AcceptButton = save;
            CancelButton = cancel;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            AddFiles(files);
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
            bool wasEmpty = _files.Count == 0;
            foreach (string f in files)
            {
                if (File.Exists(f) && !_files.Contains(f, StringComparer.OrdinalIgnoreCase)) _files.Add(f);
            }
            _title.Text = _files.Count == 0 ? S.NoFiles : S.DescTitle(_files.Count);

            string[] current = _files.Select(SafeRead).Distinct().ToArray();
            if (current.Length == 1)
            {
                if (wasEmpty || _text.Text.Length == 0) _text.Text = current[0] ?? "";
                _note.Visible = false;
            }
            else
            {
                _note.Text = S.DescMixed;
                _note.Visible = true;
            }
            _text.SelectAll();
        }

        private static string SafeRead(string f)
        {
            try { return EmojiStore.ReadDescription(f); } catch (Exception) { return null; }
        }

        private void SaveAndClose()
        {
            string text = _text.Text.Replace("\r", " ").Replace("\n", " ").Trim();
            List<string> errors = new List<string>();
            foreach (string f in _files)
            {
                try
                {
                    EmojiStore.WriteDescription(f, text);
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
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
