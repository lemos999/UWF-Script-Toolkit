using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PortableUwfManager
{
    internal static class UiTheme
    {
        public static readonly Color Canvas = Color.FromArgb(246, 248, 251);
        public static readonly Color Sidebar = Color.FromArgb(250, 251, 253);
        public static readonly Color Surface = Color.White;
        public static readonly Color Text = Color.FromArgb(31, 41, 55);
        public static readonly Color Muted = Color.FromArgb(107, 114, 128);
        public static readonly Color Primary = Color.FromArgb(37, 99, 235);
        public static readonly Color Selected = Color.FromArgb(232, 240, 255);
        public static readonly Color Hover = Color.FromArgb(237, 242, 249);
        public static readonly Color Border = Color.FromArgb(225, 231, 239);
        public static readonly Color Badge = Color.FromArgb(239, 243, 248);
        public static readonly Color Success = Color.FromArgb(231, 246, 238);
        public static readonly Color Warning = Color.FromArgb(255, 246, 221);
        public static readonly Font BodyFont = new Font("Segoe UI", 9F);
        public static readonly Font BodyBoldFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        public static readonly Font SmallBoldFont = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        public static readonly Font SectionFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        public static readonly Font BrandFont = new Font("Segoe UI", 11F, FontStyle.Bold);
        public static readonly Font LargeBoldFont = new Font("Segoe UI", 17F, FontStyle.Bold);
        public static readonly Font TitleFont = new Font("Segoe UI", 18F, FontStyle.Bold);
        public static readonly Font GuideFont = new Font("Segoe UI", 10F);
        public static readonly Font ConsoleFont = new Font("Consolas", 9F);

        public static void Apply(Control root)
        {
            if (root == null)
            {
                return;
            }

            if (root is Form || root is TabPage || root is TableLayoutPanel || root is FlowLayoutPanel || root is Panel || root is TabControl)
            {
                if (root.BackColor == SystemColors.Control)
                {
                    root.BackColor = Canvas;
                }
            }

            var button = root as Button;
            if (button != null)
            {
                bool navigationButton = String.Equals(button.Tag as string, "navigation", StringComparison.Ordinal);
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Border;
                button.FlatAppearance.BorderSize = navigationButton ? 0 : 1;
                button.FlatAppearance.MouseOverBackColor = Hover;
                button.BackColor = navigationButton ? Sidebar : Surface;
                button.ForeColor = Text;
                button.Cursor = Cursors.Hand;
                button.Font = BodyFont;
                button.Padding = navigationButton ? new Padding(12, 0, 8, 0) : new Padding(8, 2, 8, 2);
            }

            var textBox = root as TextBox;
            if (textBox != null)
            {
                bool guide = String.Equals(textBox.Tag as string, "guide", StringComparison.Ordinal);
                textBox.BorderStyle = guide ? BorderStyle.None : BorderStyle.FixedSingle;
                textBox.BackColor = Surface;
                textBox.ForeColor = Text;
            }

            var listBox = root as ListBox;
            if (listBox != null)
            {
                listBox.BorderStyle = BorderStyle.FixedSingle;
                listBox.BackColor = Surface;
                listBox.ForeColor = Text;
            }

            var combo = root as ComboBox;
            if (combo != null)
            {
                combo.FlatStyle = FlatStyle.Flat;
                combo.BackColor = Surface;
                combo.ForeColor = Text;
            }

            var numeric = root as NumericUpDown;
            if (numeric != null)
            {
                numeric.BorderStyle = BorderStyle.FixedSingle;
                numeric.BackColor = Surface;
                numeric.ForeColor = Text;
            }

            foreach (Control child in root.Controls)
            {
                Apply(child);
            }
        }
    }

    internal sealed class TablessTabControl : TabControl
    {
        private const int TcmAdjustRect = 0x1328;

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == TcmAdjustRect && !DesignMode)
            {
                message.Result = new IntPtr(1);
                return;
            }

            base.WndProc(ref message);
        }
    }

    internal sealed class OperationReviewDialog : Form
    {
        public OperationReviewDialog(OperationPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException("plan");
            }

            Text = UiText.T("작업 검토", "Review operation");
            Width = 720;
            Height = 500 + (Math.Min(plan.Commands == null ? 0 : plan.Commands.Count, 5) * 22);
            MinimumSize = new Size(600, 460);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = UiTheme.BodyFont;
            BackColor = UiTheme.Canvas;

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(22);
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            Controls.Add(root);

            var heading = new TableLayoutPanel();
            heading.Dock = DockStyle.Fill;
            heading.ColumnCount = 1;
            heading.RowCount = 2;
            heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            heading.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.Controls.Add(heading, 0, 0);

            var title = new Label();
            title.Text = plan.Title;
            title.Dock = DockStyle.Fill;
            title.Font = UiTheme.LargeBoldFont;
            title.ForeColor = UiTheme.Text;
            title.AutoEllipsis = true;
            title.TextAlign = ContentAlignment.MiddleLeft;
            heading.Controls.Add(title, 0, 0);

            var subtitle = new Label();
            subtitle.Text = UiText.T("실행 전에 변경 내용을 확인하세요.", "Review the changes before applying them.");
            subtitle.Dock = DockStyle.Fill;
            subtitle.ForeColor = UiTheme.Muted;
            subtitle.TextAlign = ContentAlignment.MiddleLeft;
            heading.Controls.Add(subtitle, 0, 1);

            var warningPanel = new Panel();
            warningPanel.Dock = DockStyle.Fill;
            warningPanel.Padding = new Padding(12, 8, 12, 8);
            warningPanel.BackColor = UiTheme.Warning;
            root.Controls.Add(warningPanel, 0, 1);

            var warningText = new TextBox();
            warningText.Dock = DockStyle.Fill;
            warningText.Multiline = true;
            warningText.ReadOnly = true;
            warningText.ScrollBars = ScrollBars.Vertical;
            warningText.WordWrap = true;
            warningText.BorderStyle = BorderStyle.None;
            warningText.BackColor = UiTheme.Warning;
            warningText.ForeColor = UiTheme.Text;
            warningText.Text = String.IsNullOrWhiteSpace(plan.Warning)
                ? UiText.T("이 작업은 UWF 설정을 변경하며 관리자 권한이 필요합니다.", "This operation changes UWF settings and requires administrator rights.")
                : plan.Warning;
            warningPanel.Controls.Add(warningText);

            var commandArea = new TableLayoutPanel();
            commandArea.Dock = DockStyle.Fill;
            commandArea.Padding = new Padding(0, 12, 0, 10);
            commandArea.ColumnCount = 1;
            commandArea.RowCount = 2;
            commandArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            commandArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.Controls.Add(commandArea, 0, 2);

            var commandHeading = new Label();
            commandHeading.Text = UiText.T("실행될 명령", "Commands to run");
            commandHeading.Dock = DockStyle.Fill;
            commandHeading.Font = UiTheme.BodyBoldFont;
            commandHeading.ForeColor = UiTheme.Text;
            commandHeading.TextAlign = ContentAlignment.MiddleLeft;
            commandArea.Controls.Add(commandHeading, 0, 0);

            var commands = new TextBox();
            commands.Dock = DockStyle.Fill;
            commands.Multiline = true;
            commands.ReadOnly = true;
            commands.ScrollBars = ScrollBars.Both;
            commands.WordWrap = false;
            commands.BorderStyle = BorderStyle.FixedSingle;
            commands.BackColor = UiTheme.Surface;
            commands.ForeColor = UiTheme.Text;
            commands.Font = UiTheme.ConsoleFont;
            commands.Text = BuildCommandList(plan);
            commands.SelectionStart = 0;
            commandArea.Controls.Add(commands, 0, 1);

            var actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            actions.WrapContents = false;
            actions.Padding = new Padding(0, 6, 0, 0);
            root.Controls.Add(actions, 0, 3);

            var continueButton = new Button();
            continueButton.Text = UiText.T("계속", "Continue");
            continueButton.Width = 112;
            continueButton.Height = 32;
            continueButton.DialogResult = DialogResult.OK;
            continueButton.BackColor = UiTheme.Primary;
            continueButton.ForeColor = Color.White;
            continueButton.FlatStyle = FlatStyle.Flat;
            continueButton.FlatAppearance.BorderSize = 0;

            var cancelButton = new Button();
            cancelButton.Text = UiText.T("취소", "Cancel");
            cancelButton.Width = 96;
            cancelButton.Height = 32;
            cancelButton.DialogResult = DialogResult.Cancel;

            actions.Controls.Add(continueButton);
            actions.Controls.Add(cancelButton);
            AcceptButton = continueButton;
            CancelButton = cancelButton;
            warningText.SelectionStart = 0;
            ActiveControl = cancelButton;
        }

        private static string BuildCommandList(OperationPlan plan)
        {
            var result = new StringBuilder();
            if (plan.Commands == null || plan.Commands.Count == 0)
            {
                return UiText.T("실행 명령이 없습니다.", "No commands are available.");
            }

            for (int i = 0; i < plan.Commands.Count; i++)
            {
                var command = plan.Commands[i];
                result.Append(i + 1).Append(". ")
                    .Append(command.FileName).Append(' ')
                    .Append(command.Arguments);
                if (command.ContinueOnFailure)
                {
                    result.Append("  [")
                        .Append(UiText.T("실패해도 계속", "continue on failure"))
                        .Append(']');
                }
                result.AppendLine();
            }

            return result.ToString();
        }
    }

    internal sealed class VolumeSelectionDialog : Form
    {
        private readonly CheckBox allBox;
        private readonly CheckedListBox volumeList;
        public string SelectedText { get; private set; }

        public VolumeSelectionDialog(List<string> volumes, string currentText)
        {
            Text = UiText.T("보호 볼륨 선택", "Select protected volumes");
            Width = 380;
            Height = 360;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(12);
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            Controls.Add(root);

            allBox = new CheckBox();
            allBox.Dock = DockStyle.Fill;
            allBox.Text = UiText.T("모든 볼륨 보호(all)", "Protect all volumes (all)");
            root.Controls.Add(allBox, 0, 0);

            volumeList = new CheckedListBox();
            volumeList.Dock = DockStyle.Fill;
            volumeList.CheckOnClick = true;
            for (int i = 0; i < volumes.Count; i++)
            {
                volumeList.Items.Add(volumes[i]);
            }
            root.Controls.Add(volumeList, 0, 1);

            var note = new Label();
            note.Dock = DockStyle.Fill;
            note.TextAlign = ContentAlignment.MiddleLeft;
            note.Text = UiText.T("보호 해제에는 all을 사용할 수 없습니다.", "all cannot be used for unprotect.");
            root.Controls.Add(note, 0, 2);

            var buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            var ok = new Button();
            ok.Text = UiText.T("확인", "OK");
            ok.Width = 90;
            ok.DialogResult = DialogResult.None;
            var cancel = new Button();
            cancel.Text = UiText.T("취소", "Cancel");
            cancel.Width = 90;
            cancel.DialogResult = DialogResult.Cancel;
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            root.Controls.Add(buttons, 0, 3);

            AcceptButton = ok;
            CancelButton = cancel;

            allBox.CheckedChanged += delegate
            {
                volumeList.Enabled = !allBox.Checked;
            };

            ok.Click += delegate
            {
                var selected = GetCheckedVolumes();
                SelectedText = BuildSelectionText(allBox.Checked, selected);
                if (String.IsNullOrEmpty(SelectedText))
                {
                    MessageBox.Show(this,
                        UiText.T("하나 이상의 볼륨을 선택하세요.", "Select at least one volume."),
                        UiText.T("볼륨 선택", "Volume selection"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            };

            ApplyCurrentSelection(currentText);
        }

        public static string BuildSelectionText(bool all, IList<string> volumes)
        {
            if (all)
            {
                return "all";
            }
            if (volumes == null || volumes.Count == 0)
            {
                return String.Empty;
            }

            var selected = new List<string>();
            for (int i = 0; i < volumes.Count; i++)
            {
                AddUnique(selected, volumes[i]);
            }
            return String.Join(",", selected.ToArray());
        }

        private List<string> GetCheckedVolumes()
        {
            var selected = new List<string>();
            for (int i = 0; i < volumeList.CheckedItems.Count; i++)
            {
                AddUnique(selected, Convert.ToString(volumeList.CheckedItems[i]));
            }
            return selected;
        }

        private void ApplyCurrentSelection(string currentText)
        {
            VolumeSelection selection;
            string error;
            if (!VolumeSelectionParser.TryParse(currentText, true, out selection, out error) || selection == null)
            {
                CheckVolume("C:");
                return;
            }

            if (selection.IsAll)
            {
                allBox.Checked = true;
                volumeList.Enabled = false;
                return;
            }

            for (int i = 0; i < selection.Volumes.Count; i++)
            {
                CheckVolume(selection.Volumes[i]);
            }
        }

        private void CheckVolume(string volume)
        {
            for (int i = 0; i < volumeList.Items.Count; i++)
            {
                if (String.Equals(Convert.ToString(volumeList.Items[i]), volume, StringComparison.OrdinalIgnoreCase))
                {
                    volumeList.SetItemChecked(i, true);
                    return;
                }
            }
        }

        private static void AddUnique(List<string> volumes, string volume)
        {
            if (String.IsNullOrWhiteSpace(volume))
            {
                return;
            }
            for (int i = 0; i < volumes.Count; i++)
            {
                if (String.Equals(volumes[i], volume, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            volumes.Add(volume);
        }
    }
}
