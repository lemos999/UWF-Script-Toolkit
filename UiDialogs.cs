using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PortableUwfManager
{
    // A quiet, warm palette shared by the main window and the safety dialogs.
    internal static class UiTheme
    {
        public static readonly Color Canvas = Color.FromArgb(249, 248, 251);
        public static readonly Color Sidebar = Color.FromArgb(245, 240, 250);
        public static readonly Color SidebarEnd = Color.FromArgb(253, 247, 244);
        public static readonly Color Surface = Color.White;
        public static readonly Color Text = Color.FromArgb(49, 45, 63);
        public static readonly Color Muted = Color.FromArgb(110, 107, 125);
        public static readonly Color Primary = Color.FromArgb(103, 82, 144);
        public static readonly Color PrimaryHover = Color.FromArgb(85, 64, 125);
        public static readonly Color Selected = Color.FromArgb(231, 222, 247);
        public static readonly Color Hover = Color.FromArgb(240, 233, 248);
        public static readonly Color Border = Color.FromArgb(228, 222, 235);
        public static readonly Color Badge = Color.FromArgb(241, 237, 247);
        public static readonly Color Success = Color.FromArgb(227, 244, 235);
        public static readonly Color Warning = Color.FromArgb(255, 244, 226);
        public static readonly Color Lavender = Color.FromArgb(232, 220, 249);
        public static readonly Color Blush = Color.FromArgb(251, 230, 238);
        public static readonly Color Cream = Color.FromArgb(255, 244, 222);

        public static readonly Font BodyFont = new Font("Segoe UI", 9.5F);
        public static readonly Font BodyBoldFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        public static readonly Font SmallBoldFont = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        public static readonly Font SectionFont = new Font("Segoe UI", 10F, FontStyle.Bold);
        public static readonly Font BrandFont = new Font("Segoe UI", 12F, FontStyle.Bold);
        public static readonly Font LargeBoldFont = new Font("Segoe UI", 18F, FontStyle.Bold);
        public static readonly Font TitleFont = new Font("Segoe UI", 19F, FontStyle.Bold);
        public static readonly Font GuideFont = new Font("Segoe UI", 10F);
        public static readonly Font ConsoleFont = new Font("Consolas", 9F);

        public static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            if (diameter <= 1)
            {
                path.AddRectangle(bounds);
                return path;
            }

            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void SetRoundedRegion(Control control, int radius)
        {
            if (control.Width < 2 || control.Height < 2)
            {
                return;
            }

            using (var path = RoundedPath(new Rectangle(0, 0, control.Width, control.Height), radius))
            {
                var oldRegion = control.Region;
                control.Region = new Region(path);
                if (oldRegion != null)
                {
                    oldRegion.Dispose();
                }
            }
        }

        private static void RoundButtonOnResize(object sender, EventArgs e)
        {
            SetRoundedRegion((Control)sender, 10);
        }

        private static void RoundBadgeOnResize(object sender, EventArgs e)
        {
            SetRoundedRegion((Control)sender, 10);
        }

        public static void Apply(Control root)
        {
            if (root == null)
            {
                return;
            }

            if (root is Form || root is TabPage || root is TableLayoutPanel ||
                root is FlowLayoutPanel || root is Panel || root is TabControl)
            {
                if (root.BackColor == SystemColors.Control)
                {
                    root.BackColor = Canvas;
                }
            }

            var button = root as Button;
            if (button != null)
            {
                bool navigation = String.Equals(button.Tag as string, "navigation", StringComparison.Ordinal);
                bool primary = String.Equals(button.Tag as string, "primary", StringComparison.Ordinal);
                button.FlatStyle = FlatStyle.Flat;
                button.UseVisualStyleBackColor = false;
                button.FlatAppearance.BorderColor = primary ? Primary : Border;
                button.FlatAppearance.BorderSize = navigation || primary ? 0 : 1;
                button.FlatAppearance.MouseOverBackColor = primary ? PrimaryHover : Hover;
                button.FlatAppearance.MouseDownBackColor = primary ? PrimaryHover : Selected;
                button.BackColor = primary ? Primary : (navigation ? Sidebar : Surface);
                button.ForeColor = primary ? Color.White : Text;
                button.Cursor = Cursors.Hand;
                button.Font = primary ? BodyBoldFont : BodyFont;
                button.Padding = navigation ? new Padding(14, 0, 8, 0) : new Padding(12, 3, 12, 3);
                button.Resize -= RoundButtonOnResize;
                button.Resize += RoundButtonOnResize;
                SetRoundedRegion(button, 10);
            }

            var badge = root as Label;
            if (badge != null && String.Equals(badge.Tag as string, "badge", StringComparison.Ordinal))
            {
                badge.Resize -= RoundBadgeOnResize;
                badge.Resize += RoundBadgeOnResize;
                SetRoundedRegion(badge, 10);
            }

            var textBox = root as TextBox;
            if (textBox != null)
            {
                string kind = textBox.Tag as string;
                textBox.BorderStyle = kind == "guide" || kind == "warning"
                    ? BorderStyle.None : BorderStyle.FixedSingle;
                textBox.BackColor = kind == "warning" ? Warning : Surface;
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

    // Painted rather than using bitmap assets, so gradients remain crisp at any DPI.
    internal sealed class PastelGradientPanel : Panel
    {
        private readonly Color first;
        private readonly Color middle;
        private readonly Color last;
        private readonly int cornerRadius;

        public PastelGradientPanel(Color first, Color middle, Color last, int cornerRadius)
        {
            this.first = first;
            this.middle = middle;
            this.last = last;
            this.cornerRadius = cornerRadius;
            BackColor = first;
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (cornerRadius > 0)
            {
                UiTheme.SetRoundedRegion(this, cornerRadius);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (ClientSize.Width < 2 || ClientSize.Height < 2)
            {
                base.OnPaintBackground(e);
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var brush = new LinearGradientBrush(ClientRectangle, first, last, LinearGradientMode.Horizontal))
            {
                brush.InterpolationColors = new ColorBlend
                {
                    Colors = new[] { first, middle, last },
                    Positions = new[] { 0F, 0.53F, 1F }
                };
                using (var path = UiTheme.RoundedPath(ClientRectangle, cornerRadius))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }
        }
    }

    internal sealed class SoftCardPanel : Panel
    {
        public SoftCardPanel()
        {
            BackColor = UiTheme.Surface;
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UiTheme.SetRoundedRegion(this, 14);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width < 2 || Height < 2)
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = UiTheme.RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), 14))
            using (var pen = new Pen(UiTheme.Border))
            {
                e.Graphics.DrawPath(pen, path);
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

            var warningPanel = new SoftCardPanel();
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
            warningText.Tag = "warning";
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
            continueButton.Tag = "primary";

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
            UiTheme.Apply(this);
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
            Width = 420;
            Height = 400;
            Font = UiTheme.BodyFont;
            BackColor = UiTheme.Canvas;
            AutoScaleMode = AutoScaleMode.Dpi;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(22);
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
            ok.Tag = "primary";
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
            UiTheme.Apply(this);
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
