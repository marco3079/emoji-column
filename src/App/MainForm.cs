// Copyright (c) marco3079. Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EmojiColumn.App
{
    /// <summary>Manager window: install, choose file types, uninstall.</summary>
    internal sealed class MainForm : Form
    {
        private readonly Label _status;
        private readonly CheckedListBox _types;
        private readonly TextBox _newType;
        private readonly Button _uninstall;

        public MainForm()
        {
            Text = S.AppName;
            Font = SystemFonts.MessageBoxFont;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(16);
            root.ColumnCount = 1;
            root.AutoSize = true;
            Controls.Add(root);

            Label title = new Label();
            title.AutoSize = true;
            title.Text = "Emoji Column";
            title.Font = new Font(Font.FontFamily, 16F, FontStyle.Bold);
            root.Controls.Add(title);

            Label tagline = new Label();
            tagline.AutoSize = true;
            tagline.Text = S.Tagline;
            tagline.ForeColor = SystemColors.GrayText;
            tagline.Margin = new Padding(3, 0, 3, 10);
            root.Controls.Add(tagline);

            _status = new Label();
            _status.AutoSize = true;
            _status.Font = new Font(Font, FontStyle.Bold);
            _status.Margin = new Padding(3, 0, 3, 12);
            root.Controls.Add(_status);

            GroupBox group = new GroupBox();
            group.Text = S.FileTypes;
            group.Width = 440;
            group.Height = 270;
            group.Padding = new Padding(10);
            root.Controls.Add(group);

            _types = new CheckedListBox();
            _types.Dock = DockStyle.Fill;
            _types.CheckOnClick = true;
            _types.IntegralHeight = false;
            _types.MultiColumn = false;

            FlowLayoutPanel addRow = new FlowLayoutPanel();
            addRow.Dock = DockStyle.Bottom;
            addRow.AutoSize = true;
            addRow.WrapContents = false;
            _newType = new TextBox();
            _newType.Width = 120;
            Button add = new Button();
            add.Text = S.AddType;
            add.AutoSize = true;
            add.Click += delegate { AddType(); };
            _newType.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { AddType(); e.SuppressKeyPress = true; }
            };
            addRow.Controls.Add(_newType);
            addRow.Controls.Add(add);
            group.Controls.Add(_types);
            group.Controls.Add(addRow);

            Label admin = new Label();
            admin.AutoSize = true;
            admin.Text = S.AdminNote;
            admin.ForeColor = SystemColors.GrayText;
            admin.Margin = new Padding(3, 8, 3, 8);
            root.Controls.Add(admin);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.AutoSize = true;
            actions.Width = 440;
            Button install = MakeButton(S.Install, delegate { DoInstall(); });
            _uninstall = MakeButton(S.Uninstall, delegate { DoUninstall(); });
            Button restart = MakeButton(S.RestartExplorer, delegate
            {
                if (Confirm(S.RestartConfirm)) Shell.RestartExplorer();
            });
            actions.Controls.Add(install);
            actions.Controls.Add(_uninstall);
            actions.Controls.Add(restart);
            root.Controls.Add(actions);

            FlowLayoutPanel more = new FlowLayoutPanel();
            more.AutoSize = true;
            more.Width = 440;
            more.Controls.Add(MakeButton(S.HowTo, delegate
            {
                MessageBox.Show(this, S.HowToText, S.HowTo, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }));
            more.Controls.Add(MakeButton(S.TagFiles, delegate { TagFiles(); }));
            root.Controls.Add(more);

            LinkLabel link = new LinkLabel();
            link.AutoSize = true;
            link.Text = S.RepoUrl.Replace("https://", "") + "   ·   v" + Installer.Version;
            link.LinkArea = new LinkArea(0, S.RepoUrl.Length - "https://".Length);
            link.Margin = new Padding(3, 12, 3, 0);
            link.LinkClicked += delegate { Process.Start(S.RepoUrl); };
            root.Controls.Add(link);

            AcceptButton = install;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            RefreshState();
        }

        private static Button MakeButton(string text, EventHandler onClick)
        {
            Button b = new Button();
            b.Text = text;
            b.AutoSize = true;
            b.Padding = new Padding(6, 2, 6, 2);
            b.Click += onClick;
            return b;
        }

        private bool Confirm(string message)
        {
            return MessageBox.Show(this, message, S.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private void RefreshState()
        {
            bool installed = Installer.IsInstalled;
            _status.Text = installed ? "● " + S.StatusInstalled + " (v" + Installer.InstalledVersion + ")" : "○ " + S.StatusNotInstalled;
            _status.ForeColor = installed ? Color.SeaGreen : SystemColors.GrayText;
            _uninstall.Enabled = installed;

            HashSet<string> active = new HashSet<string>(
                installed ? Installer.InstalledExtensions : Installer.DefaultExtensions, StringComparer.OrdinalIgnoreCase);
            IEnumerable<string> all = Installer.DefaultExtensions
                .Concat(Installer.OptionalExtensions)
                .Concat(active)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            _types.Items.Clear();
            foreach (string ext in all) _types.Items.Add(new TypeItem(ext), active.Contains(ext));
        }

        private void AddType()
        {
            string ext = Installer.NormalizeExtension(_newType.Text);
            if (ext == null)
            {
                MessageBox.Show(this, S.InvalidType, S.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            for (int i = 0; i < _types.Items.Count; i++)
            {
                if (((TypeItem)_types.Items[i]).Extension == ext) { _types.SetItemChecked(i, true); _newType.Clear(); return; }
            }
            _types.Items.Add(new TypeItem(ext), true);
            _newType.Clear();
        }

        private void DoInstall()
        {
            string[] exts = _types.CheckedItems.Cast<TypeItem>().Select(t => t.Extension).ToArray();
            if (exts.Length == 0)
            {
                MessageBox.Show(this, S.NoTypes, S.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Run(delegate
            {
                if (Shell.IsAdmin()) { Installer.Install(exts); return 0; }
                return Shell.RunElevated(Shell.JoinArgs("install", "--ext", string.Join(",", exts), "--quiet"));
            }, S.InstallDone);
        }

        private void DoUninstall()
        {
            if (!Confirm(S.UninstallConfirm)) return;
            Run(delegate
            {
                if (Shell.IsAdmin()) { Installer.Uninstall(); return 0; }
                return Shell.RunElevated("uninstall --quiet");
            }, S.UninstallDone);
        }

        private void Run(Func<int> action, string successMessage)
        {
            UseWaitCursor = true;
            Enabled = false;
            try
            {
                int code = action();
                RefreshState();
                if (code == 0)
                {
                    if (Confirm(successMessage)) Shell.RestartExplorer();
                }
                else if (code > 0)
                {
                    MessageBox.Show(this, S.Failed + " exit code " + code, S.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(this, S.Failed + "\n\n" + e.Message, S.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Enabled = true;
                UseWaitCursor = false;
            }
        }

        private void TagFiles()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Multiselect = true;
                dialog.Title = S.TagFiles;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                using (PickerForm picker = new PickerForm(dialog.FileNames))
                {
                    picker.ShowInTaskbar = false;
                    picker.StartPosition = FormStartPosition.CenterParent;
                    picker.ShowDialog(this);
                }
            }
        }

        private sealed class TypeItem
        {
            public readonly string Extension;
            private readonly string _label;

            public TypeItem(string ext)
            {
                Extension = ext;
                string foreign = null;
                try { foreign = Installer.ForeignHandler(ext); } catch (Exception) { }
                _label = foreign == null ? ext : ext + "   (" + S.Chained + ")";
            }

            public override string ToString() { return _label; }
        }
    }
}
