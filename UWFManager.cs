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
using System.Windows.Forms;

namespace PortableUwfManager
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (args != null && args.Length > 0 && String.Equals(args[0], "--self-test", StringComparison.OrdinalIgnoreCase))
            {
                return SelfTest.Run();
            }

            if (!UwfController.IsAdministrator())
            {
                string error;
                if (Elevation.TryRelaunchCurrentProcessAsAdministrator(args, out error))
                {
                    return 0;
                }

                MessageBox.Show(
                    UiText.T("UWF 설정 변경은 상승된 관리자 권한이 필요합니다.\r\n\r\n" + error,
                        "Changing UWF settings requires elevated administrator rights.\r\n\r\n" + error),
                    UiText.T("관리자 권한 필요", "Administrator required"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return 1;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return 0;
        }
    }

    internal static class Elevation
    {
        public static bool TryRelaunchCurrentProcessAsAdministrator(string[] args, out string error)
        {
            error = String.Empty;
            try
            {
                var exe = Process.GetCurrentProcess().MainModule.FileName;
                var info = new ProcessStartInfo(exe);
                info.UseShellExecute = true;
                info.Verb = "runas";
                info.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                info.Arguments = BuildArgumentString(args);
                Process.Start(info);
                return true;
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1223)
                {
                    error = UiText.T("사용자가 UAC 승인을 취소했습니다.", "The UAC elevation prompt was canceled.");
                }
                else
                {
                    error = ex.Message;
                }
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static string[] GetCurrentProcessArguments()
        {
            var all = Environment.GetCommandLineArgs();
            if (all == null || all.Length <= 1)
            {
                return new string[0];
            }

            var args = new string[all.Length - 1];
            Array.Copy(all, 1, args, 0, args.Length);
            return args;
        }

        public static string BuildArgumentString(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return String.Empty;
            }

            var text = new StringBuilder();
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0)
                {
                    text.Append(' ');
                }
                text.Append(QuoteArgumentForCreateProcess(args[i]));
            }
            return text.ToString();
        }

        internal static string QuoteArgumentForCreateProcess(string argument)
        {
            if (String.IsNullOrEmpty(argument))
            {
                return "\"\"";
            }

            bool needsQuotes = false;
            for (int i = 0; i < argument.Length; i++)
            {
                if (Char.IsWhiteSpace(argument[i]) || argument[i] == '"')
                {
                    needsQuotes = true;
                    break;
                }
            }

            if (!needsQuotes)
            {
                return argument;
            }

            var quoted = new StringBuilder();
            quoted.Append('"');
            int backslashCount = 0;
            for (int i = 0; i < argument.Length; i++)
            {
                char c = argument[i];
                if (c == '\\')
                {
                    backslashCount++;
                    continue;
                }

                if (c == '"')
                {
                    quoted.Append('\\', backslashCount * 2 + 1);
                    quoted.Append('"');
                    backslashCount = 0;
                    continue;
                }

                if (backslashCount > 0)
                {
                    quoted.Append('\\', backslashCount);
                    backslashCount = 0;
                }
                quoted.Append(c);
            }

            if (backslashCount > 0)
            {
                quoted.Append('\\', backslashCount * 2);
            }
            quoted.Append('"');
            return quoted.ToString();
        }
    }

    internal enum UiLanguage
    {
        Korean,
        English
    }

    internal static class UiText
    {
        public static UiLanguage Current = UiLanguage.Korean;

        public static string T(string ko, string en)
        {
            return Current == UiLanguage.Korean ? ko : en;
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly UwfController controller;
        private readonly ToolTip helpTip;
        private TabControl mainTabs;
        private readonly Dictionary<string, Label> dashboardLabels;
        private readonly ProgressBar overlayProgress;
        private readonly ComboBox languageBox;
        private readonly TextBox statusBox;
        private readonly TextBox logBox;
        private readonly Label adminLabel;
        private readonly Label uwfLabel;
        private readonly Label osLabel;
        private readonly ComboBox overlayTypeBox;
        private readonly ComboBox volumeBox;
        private readonly NumericUpDown overlaySizeBox;
        private readonly NumericUpDown warningPercentBox;
        private readonly NumericUpDown criticalPercentBox;
        private readonly ComboBox workloadBox;
        private readonly TextBox fileExclusionBox;
        private readonly TextBox registryExclusionBox;
        private readonly ListBox fileExclusionListBox;
        private readonly ListBox registryExclusionListBox;
        private readonly Label fileExclusionListLabel;
        private readonly Label registryExclusionListLabel;
        private readonly TextBox commitFileBox;
        private readonly TextBox commitRegistryKeyBox;
        private readonly TextBox commitRegistryValueBox;

        public MainForm()
        {
            controller = new UwfController();
            dashboardLabels = new Dictionary<string, Label>();
            overlayProgress = new ProgressBar();
            helpTip = new ToolTip();
            helpTip.AutoPopDelay = 12000;
            helpTip.InitialDelay = 500;
            helpTip.ReshowDelay = 100;

            Text = UiText.T("포터블 UWF 관리자", "Portable UWF Manager");
            Width = 1120;
            Height = 760;
            MinimumSize = new Size(980, 640);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);

            languageBox = new ComboBox();
            statusBox = CreateMultilineBox(true);
            logBox = CreateMultilineBox(true);
            adminLabel = CreateBadgeLabel();
            uwfLabel = CreateBadgeLabel();
            osLabel = CreateBadgeLabel();
            overlayTypeBox = new ComboBox();
            volumeBox = new ComboBox();
            overlaySizeBox = new NumericUpDown();
            warningPercentBox = new NumericUpDown();
            criticalPercentBox = new NumericUpDown();
            workloadBox = new ComboBox();
            fileExclusionBox = new TextBox();
            registryExclusionBox = new TextBox();
            fileExclusionListBox = new ListBox();
            registryExclusionListBox = new ListBox();
            fileExclusionListLabel = new Label();
            registryExclusionListLabel = new Label();
            commitFileBox = new TextBox();
            commitRegistryKeyBox = new TextBox();
            commitRegistryValueBox = new TextBox();

            fileExclusionListBox.DoubleClick += delegate { UseSelectedFileExclusion(); };
            registryExclusionListBox.DoubleClick += delegate { UseSelectedRegistryExclusion(); };

            BuildUi();
            RefreshStatus();
        }

        private void BuildUi()
        {
            Text = UiText.T("포터블 UWF 관리자", "Portable UWF Manager");

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            Controls.Add(root);

            var header = new TableLayoutPanel();
            header.Dock = DockStyle.Fill;
            header.Padding = new Padding(12, 8, 12, 4);
            header.ColumnCount = 6;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
            root.Controls.Add(header, 0, 0);

            var title = new Label();
            title.Text = UiText.T("포터블 UWF 관리자", "Portable UWF Manager");
            title.Dock = DockStyle.Fill;
            title.Font = new Font(Font.FontFamily, 14F, FontStyle.Bold);
            title.TextAlign = ContentAlignment.MiddleLeft;
            header.Controls.Add(title, 0, 0);
            header.Controls.Add(adminLabel, 1, 0);
            header.Controls.Add(uwfLabel, 2, 0);
            header.Controls.Add(osLabel, 3, 0);
            ConfigureLanguageBox();
            header.Controls.Add(languageBox, 4, 0);
            header.Controls.Add(CreateButton(UiText.T("관리자 실행", "Run as admin"), RelaunchAsAdmin), 5, 0);

            mainTabs = new TabControl();
            mainTabs.Dock = DockStyle.Fill;
            root.Controls.Add(mainTabs, 0, 1);

            mainTabs.TabPages.Add(BuildQuickStartTab());
            mainTabs.TabPages.Add(BuildDashboardTab());
            mainTabs.TabPages.Add(BuildSetupTab());
            mainTabs.TabPages.Add(BuildExclusionsTab());
            mainTabs.TabPages.Add(BuildAdvancedTab());

            var logPanel = new TableLayoutPanel();
            logPanel.Dock = DockStyle.Fill;
            logPanel.Padding = new Padding(12, 4, 12, 4);
            logPanel.ColumnCount = 2;
            logPanel.RowCount = 1;
            logPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            logPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            root.Controls.Add(logPanel, 0, 2);
            logPanel.Controls.Add(logBox, 0, 0);

            var logButtons = new FlowLayoutPanel();
            logButtons.Dock = DockStyle.Fill;
            logButtons.FlowDirection = FlowDirection.TopDown;
            logButtons.Controls.Add(CreateButton(UiText.T("로그 복사", "Copy log"), CopyLog));
            logButtons.Controls.Add(CreateButton(UiText.T("로그 지우기", "Clear log"), ClearLog));
            logPanel.Controls.Add(logButtons, 1, 0);

            var footer = new Label();
            footer.Text = UiText.T("상태 조회는 일반 권한으로 가능하지만, 설정 변경은 관리자 권한과 재부팅이 필요할 수 있습니다.",
                "Read-only status works without elevation. Configuration changes require administrator rights and usually require a reboot.");
            footer.Dock = DockStyle.Fill;
            footer.TextAlign = ContentAlignment.MiddleLeft;
            footer.Padding = new Padding(12, 0, 12, 0);
            root.Controls.Add(footer, 0, 3);
        }

        private void ConfigureLanguageBox()
        {
            languageBox.DropDownStyle = ComboBoxStyle.DropDownList;
            languageBox.Items.Clear();
            languageBox.Items.Add("한국어");
            languageBox.Items.Add("English");
            languageBox.SelectedIndex = UiText.Current == UiLanguage.Korean ? 0 : 1;
            languageBox.Dock = DockStyle.Fill;
            languageBox.SelectedIndexChanged -= LanguageChanged;
            languageBox.SelectedIndexChanged += LanguageChanged;
        }

        private void LanguageChanged(object sender, EventArgs e)
        {
            var next = languageBox.SelectedIndex == 1 ? UiLanguage.English : UiLanguage.Korean;
            if (UiText.Current == next)
            {
                return;
            }

            UiText.Current = next;
            Controls.Clear();
            BuildUi();
            RefreshStatus();
        }

        private TabPage BuildQuickStartTab()
        {
            var page = new TabPage(UiText.T("빠른 시작", "Quick start"));
            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(16);
            root.ColumnCount = 2;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            page.Controls.Add(root);

            var guide = CreateGuideBox(
                UiText.T(
                    "처음 쓰는 순서\r\n\r\n1. 상태 확인: UWF 기능과 관리자 권한 상태를 먼저 봅니다.\r\n2. UWF 기능이 없으면 설치 후 재부팅합니다.\r\n3. 잘 모르겠으면 RAM 추천값을 먼저 사용합니다. RAM은 가볍고 관리가 단순합니다.\r\n4. 게임/패치처럼 쓰기량이 크면 DISK 추천값을 검토합니다.\r\n5. 적용 전에는 항상 작업 계획을 확인하고, 적용 후 재부팅하세요.",
                    "First-use flow\r\n\r\n1. Check status first: verify UWF feature and administrator state.\r\n2. If UWF is missing, install the feature and reboot.\r\n3. If unsure, start with the RAM recommendation. RAM mode is lightweight and simpler.\r\n4. For heavy writes such as game patches, review the DISK recommendation.\r\n5. Always review the operation plan before applying, then reboot."));
            root.Controls.Add(guide, 0, 0);

            var buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.TopDown;
            buttons.WrapContents = false;
            buttons.Controls.Add(CreateButton(UiText.T("1. 상태 확인", "1. Check status"), RefreshStatus));
            buttons.Controls.Add(CreateButton(UiText.T("2. 관리자 실행", "2. Run as admin"), RelaunchAsAdmin));
            buttons.Controls.Add(CreateButton(UiText.T("3. UWF 기능 설치", "3. Install UWF"), InstallFeature));
            root.Controls.Add(buttons, 1, 0);

            var beginner = CreateGuideBox(
                UiText.T(
                    "추천 기준\r\n\r\nRAM 모드: 재부팅하면 변경이 사라지는 보호 환경에 적합합니다. 저장해야 하는 설정은 예외나 커밋으로 따로 관리하세요.\r\n\r\nDISK 모드: 쓰기량이 큰 환경에 맞지만 C: 여유 공간을 사용합니다. 여유 공간이 부족하면 큰 값을 피하세요.\r\n\r\n예외: overlay를 줄이는 기능이 아닙니다. 반드시 보존해야 하는 작은 설정/데이터에만 쓰세요.",
                    "Recommendation rules\r\n\r\nRAM mode: best for a protected environment where changes disappear after reboot. Persist needed settings through exclusions or commits.\r\n\r\nDISK mode: better for heavy writes, but it uses free space on C:. Avoid large values when free space is low.\r\n\r\nExclusions: they do not reduce overlay usage. Use them only for small settings/data that must persist."));
            root.Controls.Add(beginner, 0, 1);

            var recommendButtons = new FlowLayoutPanel();
            recommendButtons.Dock = DockStyle.Fill;
            recommendButtons.FlowDirection = FlowDirection.TopDown;
            recommendButtons.WrapContents = false;
            recommendButtons.Controls.Add(CreateButton(UiText.T("RAM 추천값 넣기", "Use RAM recommendation"), UseRecommendedRam));
            recommendButtons.Controls.Add(CreateButton(UiText.T("DISK 추천값 넣기", "Use DISK recommendation"), UseRecommendedDisk));
            recommendButtons.Controls.Add(CreateButton(UiText.T("설정 탭으로 이동", "Go to setup"), GoToSetup));
            root.Controls.Add(recommendButtons, 1, 1);

            var bottom = new Label();
            bottom.Dock = DockStyle.Fill;
            bottom.TextAlign = ContentAlignment.MiddleLeft;
            bottom.Text = UiText.T("안전장치: 위험 예외 경로는 차단하고, 변경 작업은 먼저 계획을 보여준 뒤 실행합니다.",
                "Safety: risky exclusion paths are blocked, and every change shows a plan before execution.");
            root.Controls.Add(bottom, 0, 2);
            root.SetColumnSpan(bottom, 2);
            return page;
        }

        private TabPage BuildDashboardTab()
        {
            var page = new TabPage(UiText.T("대시보드", "Dashboard"));
            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(12);
            root.ColumnCount = 2;
            root.RowCount = 2;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 245F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            page.Controls.Add(root);

            root.Controls.Add(BuildDashboardSummary(), 0, 0);
            root.Controls.Add(statusBox, 0, 1);

            var buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.TopDown;
            buttons.WrapContents = false;
            buttons.Controls.Add(CreateButton(UiText.T("새로고침", "Refresh"), RefreshStatus));
            buttons.Controls.Add(CreateButton(UiText.T("보고서 복사", "Copy report"), CopyStatus));
            buttons.Controls.Add(CreateButton(UiText.T("보고서 내보내기", "Export report"), ExportStatus));
            buttons.Controls.Add(CreateButton(UiText.T("UWF 기능 설치", "Install UWF feature"), InstallFeature));
            root.Controls.Add(buttons, 1, 0);
            root.SetRowSpan(buttons, 2);
            return page;
        }

        private Control BuildDashboardSummary()
        {
            dashboardLabels.Clear();
            var grid = new TableLayoutPanel();
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = 4;
            grid.RowCount = 8;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            for (int i = 0; i < 7; i++)
            {
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            }
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));

            AddDashboardRow(grid, 0, UiText.T("필터 현재", "Filter current"), "FilterCurrent", UiText.T("필터 다음", "Filter next"), "FilterNext");
            AddDashboardRow(grid, 1, UiText.T("오버레이 현재", "Overlay current"), "OverlayCurrent", UiText.T("오버레이 다음", "Overlay next"), "OverlayNext");
            AddDashboardRow(grid, 2, UiText.T("최대 크기", "Maximum size"), "OverlayMax", UiText.T("사용량", "Consumption"), "OverlayUsage");
            AddDashboardRow(grid, 3, UiText.T("남은 공간", "Available space"), "OverlayAvailable", UiText.T("임계값", "Thresholds"), "OverlayThresholds");
            AddDashboardRow(grid, 4, UiText.T("보호 볼륨 현재", "Protected volumes now"), "VolumesCurrent", UiText.T("보호 볼륨 다음", "Protected volumes next"), "VolumesNext");
            AddDashboardRow(grid, 5, UiText.T("서비스 현재", "Servicing current"), "ServicingCurrent", UiText.T("서비스 다음", "Servicing next"), "ServicingNext");
            AddDashboardRow(grid, 6, UiText.T("재부팅 필요", "Reboot needed"), "RebootNeeded", UiText.T("추천 요약", "Recommendation"), "Recommendation");

            overlayProgress.Dock = DockStyle.Bottom;
            overlayProgress.Height = 12;
            grid.Controls.Add(overlayProgress, 0, 7);
            grid.SetColumnSpan(overlayProgress, 4);
            return grid;
        }

        private void AddDashboardRow(TableLayoutPanel grid, int row, string leftName, string leftKey, string rightName, string rightKey)
        {
            AddDashboardCell(grid, row, 0, leftName, true);
            AddDashboardCell(grid, row, 1, leftKey, false);
            AddDashboardCell(grid, row, 2, rightName, true);
            AddDashboardCell(grid, row, 3, rightKey, false);
        }

        private void AddDashboardCell(TableLayoutPanel grid, int row, int column, string textOrKey, bool header)
        {
            var label = new Label();
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.AutoEllipsis = true;
            label.BorderStyle = BorderStyle.FixedSingle;
            label.Padding = new Padding(6, 0, 6, 0);
            if (header)
            {
                label.Text = textOrKey;
                label.BackColor = Color.Gainsboro;
                label.Font = new Font(Font.FontFamily, 9F, FontStyle.Bold);
            }
            else
            {
                label.Text = "-";
                label.BackColor = Color.White;
                dashboardLabels[textOrKey] = label;
            }
            grid.Controls.Add(label, column, row);
        }

        private TabPage BuildSetupTab()
        {
            var page = new TabPage(UiText.T("설정", "Setup"));
            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(16);
            root.ColumnCount = 2;
            root.RowCount = 8;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            page.Controls.Add(root);

            overlayTypeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            overlayTypeBox.Items.Add("RAM");
            overlayTypeBox.Items.Add("DISK");
            overlayTypeBox.SelectedIndex = 0;

            volumeBox.DropDownStyle = ComboBoxStyle.DropDown;
            PopulateVolumeChoices();

            overlaySizeBox.Minimum = 1024;
            overlaySizeBox.Maximum = 1048576;
            overlaySizeBox.Value = 4096;
            overlaySizeBox.Increment = 1024;
            overlaySizeBox.ThousandsSeparator = true;

            warningPercentBox.Minimum = 1;
            warningPercentBox.Maximum = 98;
            warningPercentBox.Value = 80;
            criticalPercentBox.Minimum = 2;
            criticalPercentBox.Maximum = 99;
            criticalPercentBox.Value = 95;

            workloadBox.DropDownStyle = ComboBoxStyle.DropDownList;
            workloadBox.Items.Clear();
            workloadBox.Items.Add(UiText.T("가벼움 - 설정/작은 앱", "Light - settings/small apps"));
            workloadBox.Items.Add(UiText.T("보통 - 일반 사용", "Normal - everyday use"));
            workloadBox.Items.Add(UiText.T("무거움 - 게임/패치", "Heavy - games/patches"));
            workloadBox.SelectedIndex = 1;

            AddRow(root, 0, UiText.T("오버레이 유형", "Overlay type"), overlayTypeBox);
            AddRow(root, 1, UiText.T("보호 볼륨(복수 선택)", "Protected volumes"), CreateVolumeSelectorControl());
            AddRow(root, 2, UiText.T("오버레이 크기(MB)", "Overlay size (MB)"), overlaySizeBox);
            AddRow(root, 3, UiText.T("경고 임계값(%)", "Warning threshold (%)"), warningPercentBox);
            AddRow(root, 4, UiText.T("위험 임계값(%)", "Critical threshold (%)"), criticalPercentBox);
            AddRow(root, 5, UiText.T("사용 강도", "Workload"), workloadBox);

            var hint = new Label();
            hint.Dock = DockStyle.Fill;
            hint.Text = UiText.T("여러 볼륨은 C:,D:처럼 입력할 수 있고, 보호에는 all도 사용할 수 있습니다. 적용 전 변경 계획을 먼저 보여줍니다.",
                "Enter multiple volumes like C:,D:. The protect action also supports all. The app will show the plan before applying changes.");
            hint.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(hint, 0, 6);
            root.SetColumnSpan(hint, 2);

            var buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.LeftToRight;
            buttons.Controls.Add(CreateButton(UiText.T("RAM 추천값", "RAM recommendation"), UseRecommendedRam));
            buttons.Controls.Add(CreateButton(UiText.T("DISK 추천값", "DISK recommendation"), UseRecommendedDisk));
            buttons.Controls.Add(CreateButton(UiText.T("설정 계획 적용", "Apply setup plan"), ApplySetup));
            buttons.Controls.Add(CreateButton(UiText.T("필터 켜기", "Enable filter"), EnableFilter));
            buttons.Controls.Add(CreateButton(UiText.T("필터 끄기", "Disable filter"), DisableFilter));
            buttons.Controls.Add(CreateButton(UiText.T("DISK 공간 정리", "Clean DISK space"), CleanupDiskOverlaySpace));
            buttons.Controls.Add(CreateButton(UiText.T("완전 끄기", "Full off"), FullDisableUwf));
            buttons.Controls.Add(CreateButton(UiText.T("볼륨 보호", "Protect volume"), ProtectVolume));
            buttons.Controls.Add(CreateButton(UiText.T("볼륨 보호 해제", "Unprotect volume"), UnprotectVolume));
            root.Controls.Add(buttons, 0, 7);
            root.SetColumnSpan(buttons, 2);
            return page;
        }

        private Control CreateVolumeSelectorControl()
        {
            var panel = new TableLayoutPanel();
            panel.Width = 520;
            panel.Height = 30;
            panel.ColumnCount = 2;
            panel.RowCount = 1;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.Margin = new Padding(0);

            volumeBox.Dock = DockStyle.Fill;
            panel.Controls.Add(volumeBox, 0, 0);

            var selectButton = CreateButton(UiText.T("볼륨 선택", "Select volumes"), SelectVolumes);
            selectButton.Dock = DockStyle.Fill;
            selectButton.Margin = new Padding(6, 0, 0, 0);
            panel.Controls.Add(selectButton, 1, 0);
            return panel;
        }

        private void PopulateVolumeChoices()
        {
            volumeBox.Items.Clear();
            AddVolumeChoice("C:");
            try
            {
                var drives = DriveInfo.GetDrives();
                for (int i = 0; i < drives.Length; i++)
                {
                    if (drives[i].DriveType == DriveType.Fixed || drives[i].DriveType == DriveType.Removable)
                    {
                        AddVolumeChoice(drives[i].Name.TrimEnd('\\'));
                    }
                }
            }
            catch
            {
            }
            AddVolumeChoice("all");
            volumeBox.Text = "C:";
        }

        private void AddVolumeChoice(string volume)
        {
            if (String.IsNullOrWhiteSpace(volume))
            {
                return;
            }
            for (int i = 0; i < volumeBox.Items.Count; i++)
            {
                if (String.Equals(Convert.ToString(volumeBox.Items[i]), volume, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            volumeBox.Items.Add(volume);
        }

        private void SelectVolumes()
        {
            using (var dialog = new VolumeSelectionDialog(GetSelectableVolumes(), volumeBox.Text))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    volumeBox.Text = dialog.SelectedText;
                    AppendLog(UiText.T("보호 볼륨 선택: ", "Selected protected volumes: ") + dialog.SelectedText);
                }
            }
        }

        private List<string> GetSelectableVolumes()
        {
            var volumes = new List<string>();
            AddSelectableVolume(volumes, "C:");
            try
            {
                var drives = DriveInfo.GetDrives();
                for (int i = 0; i < drives.Length; i++)
                {
                    if (drives[i].DriveType == DriveType.Fixed || drives[i].DriveType == DriveType.Removable)
                    {
                        AddSelectableVolume(volumes, drives[i].Name.TrimEnd('\\'));
                    }
                }
            }
            catch
            {
            }

            VolumeSelection current;
            string error;
            if (VolumeSelectionParser.TryParse(volumeBox.Text, true, out current, out error) && current != null && !current.IsAll)
            {
                for (int i = 0; i < current.Volumes.Count; i++)
                {
                    AddSelectableVolume(volumes, current.Volumes[i]);
                }
            }

            return volumes;
        }

        private static void AddSelectableVolume(List<string> volumes, string volume)
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

        private TabPage BuildExclusionsTab()
        {
            var page = new TabPage(UiText.T("예외", "Exclusions"));
            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(16);
            root.ColumnCount = 2;
            root.RowCount = 5;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 132F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            page.Controls.Add(root);

            root.Controls.Add(CreateSectionLabel(UiText.T("폴더/파일 예외", "Folder or file exclusion")), 0, 0);
            fileExclusionBox.Dock = DockStyle.Fill;
            fileExclusionBox.PlaceholderTextSafe(UiText.T("예: C:\\ProgramData\\Vendor\\Settings", "Example: C:\\ProgramData\\Vendor\\Settings"));
            root.Controls.Add(fileExclusionBox, 0, 1);

            var fileButtons = CreateExclusionButtonGrid();
            AddExclusionButton(fileButtons, 0, 0, UiText.T("파일 선택", "Select file"), SelectFileExclusionFile);
            AddExclusionButton(fileButtons, 1, 0, UiText.T("폴더 선택", "Select folder"), SelectFileExclusionFolder);
            AddExclusionButton(fileButtons, 2, 0, UiText.T("폴더/파일 예외 추가", "Add folder/file exclusion"), AddFileExclusion);
            AddExclusionButton(fileButtons, 0, 1, UiText.T("입력값 제거", "Remove typed"), RemoveFileExclusion);
            AddExclusionButton(fileButtons, 1, 1, UiText.T("선택 사용", "Use selected"), UseSelectedFileExclusion);
            AddExclusionButton(fileButtons, 2, 1, UiText.T("선택 제거", "Remove selected"), RemoveSelectedFileExclusion);
            AddExclusionButton(fileButtons, 0, 2, UiText.T("목록 새로고침", "Refresh list"), RefreshExclusionsOnly);
            root.Controls.Add(fileButtons, 0, 2);
            ConfigureSectionLabel(fileExclusionListLabel, UiText.T("현재 폴더/파일 예외", "Current folder/file exclusions"));
            root.Controls.Add(fileExclusionListLabel, 0, 3);
            ConfigureExclusionListBox(fileExclusionListBox);
            root.Controls.Add(fileExclusionListBox, 0, 4);

            root.Controls.Add(CreateSectionLabel(UiText.T("레지스트리 예외", "Registry exclusion")), 1, 0);
            registryExclusionBox.Dock = DockStyle.Fill;
            registryExclusionBox.PlaceholderTextSafe(UiText.T("예: HKLM\\SOFTWARE\\Vendor\\Product", "Example: HKLM\\SOFTWARE\\Vendor\\Product"));
            root.Controls.Add(registryExclusionBox, 1, 1);

            var regButtons = CreateExclusionButtonGrid();
            AddExclusionButton(regButtons, 0, 0, UiText.T("레지스트리 예외 추가", "Add registry exclusion"), AddRegistryExclusion);
            AddExclusionButton(regButtons, 1, 0, UiText.T("입력값 제거", "Remove typed"), RemoveRegistryExclusion);
            AddExclusionButton(regButtons, 2, 0, UiText.T("선택 사용", "Use selected"), UseSelectedRegistryExclusion);
            AddExclusionButton(regButtons, 0, 1, UiText.T("선택 제거", "Remove selected"), RemoveSelectedRegistryExclusion);
            AddExclusionButton(regButtons, 1, 1, UiText.T("목록 새로고침", "Refresh list"), RefreshExclusionsOnly);
            AddExclusionButton(regButtons, 2, 1, UiText.T("예시 넣기", "Fill example"), FillRegistryExample);
            root.Controls.Add(regButtons, 1, 2);
            ConfigureSectionLabel(registryExclusionListLabel, UiText.T("현재 레지스트리 예외", "Current registry exclusions"));
            root.Controls.Add(registryExclusionListLabel, 1, 3);
            ConfigureExclusionListBox(registryExclusionListBox);
            root.Controls.Add(registryExclusionListBox, 1, 4);
            return page;
        }

        private TableLayoutPanel CreateExclusionButtonGrid()
        {
            var grid = new TableLayoutPanel();
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = 3;
            grid.RowCount = 3;
            grid.Margin = new Padding(0, 4, 0, 4);
            for (int i = 0; i < 3; i++)
            {
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333F));
            }
            return grid;
        }

        private void AddExclusionButton(TableLayoutPanel grid, int column, int row, string text, Action action)
        {
            var button = CreateButton(text, action);
            button.Dock = DockStyle.Fill;
            button.AutoSize = false;
            button.MinimumSize = Size.Empty;
            button.Margin = new Padding(3);
            grid.Controls.Add(button, column, row);
        }

        private TabPage BuildAdvancedTab()
        {
            var page = new TabPage(UiText.T("고급", "Advanced"));
            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(16);
            root.ColumnCount = 1;
            root.RowCount = 10;
            page.Controls.Add(root);

            root.Controls.Add(CreateSectionLabel(UiText.T("오버레이의 파일 변경을 보호 볼륨에 커밋", "Commit a file from overlay to the protected volume")), 0, 0);
            commitFileBox.Dock = DockStyle.Fill;
            commitFileBox.PlaceholderTextSafe(UiText.T("예: C:\\Path\\file.ini", "Example: C:\\Path\\file.ini"));
            root.Controls.Add(commitFileBox, 0, 1);
            var fileCommitButtons = new FlowLayoutPanel();
            fileCommitButtons.Controls.Add(CreateButton(UiText.T("파일 커밋", "Commit file"), CommitFile));
            fileCommitButtons.Controls.Add(CreateButton(UiText.T("파일 삭제 커밋", "Commit file deletion"), CommitFileDeletion));
            root.Controls.Add(fileCommitButtons, 0, 2);

            root.Controls.Add(CreateSectionLabel(UiText.T("레지스트리 키/값 커밋", "Commit a registry key/value")), 0, 3);
            commitRegistryKeyBox.Dock = DockStyle.Fill;
            commitRegistryKeyBox.PlaceholderTextSafe(UiText.T("예: HKLM\\SOFTWARE\\Vendor\\Product", "Example: HKLM\\SOFTWARE\\Vendor\\Product"));
            root.Controls.Add(commitRegistryKeyBox, 0, 4);
            commitRegistryValueBox.Dock = DockStyle.Fill;
            commitRegistryValueBox.PlaceholderTextSafe(UiText.T("선택 값 이름. 비워두면 키를 커밋합니다.", "Optional value name. Leave blank to commit the key."));
            root.Controls.Add(commitRegistryValueBox, 0, 5);
            var regCommitButtons = new FlowLayoutPanel();
            regCommitButtons.Controls.Add(CreateButton(UiText.T("레지스트리 커밋", "Commit registry"), CommitRegistry));
            regCommitButtons.Controls.Add(CreateButton(UiText.T("레지스트리 삭제 커밋", "Commit registry deletion"), CommitRegistryDeletion));
            root.Controls.Add(regCommitButtons, 0, 6);

            root.Controls.Add(CreateSectionLabel(UiText.T("서비스 모드 및 복구", "Servicing and recovery")), 0, 7);
            var recoveryButtons = new FlowLayoutPanel();
            recoveryButtons.Dock = DockStyle.Fill;
            recoveryButtons.Controls.Add(CreateButton(UiText.T("서비스 모드 켜기", "Enable servicing"), EnableServicing));
            recoveryButtons.Controls.Add(CreateButton(UiText.T("서비스 모드 끄기", "Disable servicing"), DisableServicing));
            recoveryButtons.Controls.Add(CreateButton(UiText.T("Windows 업데이트", "Update Windows"), UpdateWindows));
            recoveryButtons.Controls.Add(CreateButton(UiText.T("UWF 설정 초기화", "Reset UWF settings"), ResetSettings));
            recoveryButtons.Controls.Add(CreateButton(UiText.T("DISK 오버레이 공간 정리", "Clean DISK overlay space"), CleanupDiskOverlaySpace));
            recoveryButtons.Controls.Add(CreateButton(UiText.T("UWF 완전 끄기", "UWF full off"), FullDisableUwf));
            recoveryButtons.Controls.Add(CreateButton(UiText.T("UWF 완전 초기화", "UWF full reset"), FullResetUwf));
            recoveryButtons.Controls.Add(CreateButton(UiText.T("안전 재시작", "Safe restart"), SafeRestart));
            recoveryButtons.Controls.Add(CreateButton(UiText.T("안전 종료", "Safe shutdown"), SafeShutdown));
            root.Controls.Add(recoveryButtons, 0, 8);

            var note = new Label();
            note.Dock = DockStyle.Fill;
            note.Text = UiText.T("고급 작업은 변경을 영구 커밋하거나 장치를 재시작할 수 있습니다. 각 계획을 반드시 확인하세요.",
                "Advanced operations can permanently commit changes or restart the device. Review each plan carefully.");
            note.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(note, 0, 9);
            return page;
        }

        private static Label CreateBadgeLabel()
        {
            var label = new Label();
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.BorderStyle = BorderStyle.FixedSingle;
            label.AutoEllipsis = true;
            return label;
        }

        private static Label CreateSectionLabel(string text)
        {
            var label = new Label();
            ConfigureSectionLabel(label, text);
            return label;
        }

        private static void ConfigureSectionLabel(Label label, string text)
        {
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 9F, FontStyle.Bold);
            label.TextAlign = ContentAlignment.MiddleLeft;
        }

        private static void ConfigureExclusionListBox(ListBox box)
        {
            box.Dock = DockStyle.Fill;
            box.HorizontalScrollbar = true;
            box.IntegralHeight = false;
            box.Font = new Font("Consolas", 9F);
        }

        private static TextBox CreateMultilineBox(bool readOnly)
        {
            var box = new TextBox();
            box.Dock = DockStyle.Fill;
            box.Multiline = true;
            box.ScrollBars = ScrollBars.Both;
            box.WordWrap = false;
            box.ReadOnly = readOnly;
            box.Font = new Font("Consolas", 9F);
            return box;
        }

        private static TextBox CreateGuideBox(string text)
        {
            var box = new TextBox();
            box.Dock = DockStyle.Fill;
            box.Multiline = true;
            box.ScrollBars = ScrollBars.Vertical;
            box.WordWrap = true;
            box.ReadOnly = true;
            box.BackColor = SystemColors.Window;
            box.Font = new Font("Segoe UI", 10F);
            box.Text = text;
            return box;
        }

        private Button CreateButton(string text, Action action)
        {
            var button = new Button();
            button.Text = text;
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.MinimumSize = new Size(150, 30);
            button.Margin = new Padding(4);
            button.Click += delegate { action(); };
            helpTip.SetToolTip(button, text);
            return button;
        }

        private static void AddRow(TableLayoutPanel table, int row, string labelText, Control control)
        {
            var label = new Label();
            label.Text = labelText;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            table.Controls.Add(label, 0, row);
            control.Dock = DockStyle.Left;
            control.Width = Math.Max(control.Width, 320);
            table.Controls.Add(control, 1, row);
        }

        private void RefreshStatus()
        {
            var status = controller.GetStatus();
            adminLabel.Text = status.IsAdministrator ? UiText.T("관리자: 예", "Administrator: yes") : UiText.T("관리자: 아니오", "Administrator: no");
            adminLabel.BackColor = status.IsAdministrator ? Color.Honeydew : Color.MistyRose;
            uwfLabel.Text = status.UwfToolExists ? UiText.T("uwfmgr.exe: 있음", "uwfmgr.exe: found") : UiText.T("uwfmgr.exe: 없음", "uwfmgr.exe: missing");
            uwfLabel.BackColor = status.UwfToolExists ? Color.Honeydew : Color.MistyRose;
            osLabel.Text = status.OsCaption;
            osLabel.BackColor = status.IsLikelySupportedEdition ? Color.Honeydew : Color.LemonChiffon;
            UpdateDashboard(status);
            UpdateExclusionLists(status == null ? null : status.Snapshot);
            statusBox.Text = status.Report;
            AppendLog(UiText.T("상태를 새로고침했습니다.", "Status refreshed."));
        }

        private void UpdateDashboard(UwfStatus status)
        {
            if (status == null || status.Snapshot == null)
            {
                SetDashboard("FilterCurrent", "-");
                return;
            }

            var snapshot = status.Snapshot;
            SetDashboard("FilterCurrent", FormatBool(snapshot.FilterCurrentEnabled));
            SetDashboard("FilterNext", FormatBool(snapshot.FilterNextEnabled));
            SetDashboard("OverlayCurrent", snapshot.CurrentOverlayType);
            SetDashboard("OverlayNext", snapshot.NextOverlayType);
            SetDashboard("OverlayMax", FormatPairMb(snapshot.CurrentMaximumSizeMb, snapshot.NextMaximumSizeMb));
            SetDashboard("OverlayUsage", FormatMb(snapshot.OverlayConsumptionMb) + " (" + snapshot.GetOverlayUsagePercentText() + ")");
            SetDashboard("OverlayAvailable", FormatMb(snapshot.AvailableSpaceMb));
            SetDashboard("OverlayThresholds", UiText.T("경고 ", "Warn ") + FormatMb(snapshot.WarningThresholdMb) + " / " + UiText.T("위험 ", "Crit ") + FormatMb(snapshot.CriticalThresholdMb));
            SetDashboard("VolumesCurrent", snapshot.CurrentProtectedVolumesText());
            SetDashboard("VolumesNext", snapshot.NextProtectedVolumesText());
            SetDashboard("ServicingCurrent", FormatBool(snapshot.ServicingCurrentEnabled));
            SetDashboard("ServicingNext", FormatBool(snapshot.ServicingNextEnabled));
            SetDashboard("RebootNeeded", snapshot.HasPendingChanges() ? UiText.T("예 - 다음 세션 변경 있음", "Yes - next-session changes") : UiText.T("아니오", "No"));

            long ramMb = SystemSizing.GetTotalPhysicalMemoryMb();
            long freeMb = SystemSizing.GetFreeSpaceMb(GetSizingVolume(volumeBox.Text));
            var profile = GetSelectedWorkloadProfile();
            int ramReco = SizingRules.RecommendRamOverlayMb(ramMb, profile);
            int diskReco = SizingRules.RecommendDiskOverlayMb(freeMb, profile);
            SetDashboard("Recommendation", "RAM " + ramReco.ToString() + " MB / DISK " + diskReco.ToString() + " MB");

            int percent = snapshot.GetOverlayUsagePercent();
            overlayProgress.Value = Math.Max(0, Math.Min(100, percent));
        }

        private void UpdateExclusionLists(UwfSnapshot snapshot)
        {
            var files = snapshot == null ? null : snapshot.FileExclusions;
            var registryKeys = snapshot == null ? null : snapshot.RegistryExclusions;

            FillListBox(fileExclusionListBox, files);
            FillListBox(registryExclusionListBox, registryKeys);

            ConfigureSectionLabel(fileExclusionListLabel,
                UiText.T("현재 폴더/파일 예외", "Current folder/file exclusions") + " (" + CountItems(files).ToString() + ")");
            ConfigureSectionLabel(registryExclusionListLabel,
                UiText.T("현재 레지스트리 예외", "Current registry exclusions") + " (" + CountItems(registryKeys).ToString() + ")");
        }

        private static int CountItems(IList<string> values)
        {
            return values == null ? 0 : values.Count;
        }

        private static void FillListBox(ListBox box, IList<string> values)
        {
            if (box == null)
            {
                return;
            }

            var selected = Convert.ToString(box.SelectedItem);
            box.BeginUpdate();
            try
            {
                box.Items.Clear();
                if (values != null)
                {
                    for (int i = 0; i < values.Count; i++)
                    {
                        if (!String.IsNullOrWhiteSpace(values[i]))
                        {
                            box.Items.Add(values[i]);
                        }
                    }
                }
            }
            finally
            {
                box.EndUpdate();
            }

            if (!String.IsNullOrEmpty(selected))
            {
                int index = box.FindStringExact(selected);
                if (index >= 0)
                {
                    box.SelectedIndex = index;
                }
            }
        }

        private void SetDashboard(string key, string value)
        {
            Label label;
            if (dashboardLabels.TryGetValue(key, out label))
            {
                label.Text = value;
                if (key == "RebootNeeded" && value.StartsWith(UiText.T("예", "Yes"), StringComparison.OrdinalIgnoreCase))
                {
                    label.BackColor = Color.LemonChiffon;
                }
                else if (key == "FilterCurrent" || key == "FilterNext")
                {
                    label.BackColor = value == UiText.T("켜짐", "On") ? Color.Honeydew : Color.MistyRose;
                }
                else
                {
                    label.BackColor = Color.White;
                }
            }
        }

        private static string FormatBool(bool? value)
        {
            if (!value.HasValue)
            {
                return UiText.T("확인 불가", "Unknown");
            }
            return value.Value ? UiText.T("켜짐", "On") : UiText.T("꺼짐", "Off");
        }

        private static string FormatMb(int? value)
        {
            if (!value.HasValue || value.Value < 0)
            {
                return UiText.T("확인 불가", "Unknown");
            }
            return value.Value.ToString("N0") + " MB";
        }

        private static string FormatPairMb(int? current, int? next)
        {
            return UiText.T("현재 ", "Now ") + FormatMb(current) + " / " + UiText.T("다음 ", "Next ") + FormatMb(next);
        }

        private void CopyStatus()
        {
            Clipboard.SetText(statusBox.Text);
            AppendLog(UiText.T("상태 보고서를 클립보드에 복사했습니다.", "Status copied to clipboard."));
        }

        private void ExportStatus()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = UiText.T("UWF 진단 보고서 내보내기", "Export UWF diagnostic report");
                dialog.Filter = UiText.T("텍스트 보고서 (*.txt)|*.txt|모든 파일 (*.*)|*.*", "Text report (*.txt)|*.txt|All files (*.*)|*.*");
                dialog.FileName = "uwf-diagnostic-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    File.WriteAllText(dialog.FileName, statusBox.Text, Encoding.UTF8);
                    AppendLog(UiText.T("보고서를 내보냈습니다: ", "Report exported: ") + dialog.FileName);
                }
            }
        }

        private void CopyLog()
        {
            Clipboard.SetText(logBox.Text);
        }

        private void ClearLog()
        {
            logBox.Clear();
        }

        private void RelaunchAsAdmin()
        {
            string error;
            if (Elevation.TryRelaunchCurrentProcessAsAdministrator(Elevation.GetCurrentProcessArguments(), out error))
            {
                Close();
                return;
            }

            ShowError(UiText.T("관리자 권한으로 다시 실행할 수 없습니다.", "Could not relaunch as administrator."), error);
        }

        private void InstallFeature()
        {
            RunPlan(controller.CreateInstallFeaturePlan());
        }

        private void UseRecommendedRam()
        {
            long totalRamMb = SystemSizing.GetTotalPhysicalMemoryMb();
            var profile = GetSelectedWorkloadProfile();
            int recommended = SizingRules.RecommendRamOverlayMb(totalRamMb, profile);
            SetOverlayPreset("RAM", recommended);
            GoToSetup();
            AppendLog(UiText.T("RAM 추천값을 입력했습니다: ", "Applied RAM recommendation: ") + recommended.ToString() + " MB (" + profile.DisplayName() + ")");
        }

        private void UseRecommendedDisk()
        {
            var volume = GetSizingVolume(volumeBox.Text);
            long freeMb = SystemSizing.GetFreeSpaceMb(volume);
            var profile = GetSelectedWorkloadProfile();
            int recommended = SizingRules.RecommendDiskOverlayMb(freeMb, profile);
            SetOverlayPreset("DISK", recommended);
            GoToSetup();
            AppendLog(UiText.T("DISK 추천값을 입력했습니다: ", "Applied DISK recommendation: ") + recommended.ToString() + " MB (" + profile.DisplayName() + ")");
        }

        private WorkloadProfile GetSelectedWorkloadProfile()
        {
            return WorkloadProfile.FromIndex(workloadBox.SelectedIndex);
        }

        private void SetOverlayPreset(string overlayType, int sizeMb)
        {
            overlayTypeBox.SelectedItem = overlayType;
            if (sizeMb < Decimal.ToInt32(overlaySizeBox.Minimum))
            {
                sizeMb = Decimal.ToInt32(overlaySizeBox.Minimum);
            }
            if (sizeMb > Decimal.ToInt32(overlaySizeBox.Maximum))
            {
                sizeMb = Decimal.ToInt32(overlaySizeBox.Maximum);
            }
            overlaySizeBox.Value = sizeMb;
            warningPercentBox.Value = 80;
            criticalPercentBox.Value = 95;
        }

        private void GoToSetup()
        {
            if (mainTabs != null && mainTabs.TabPages.Count > 2)
            {
                mainTabs.SelectedIndex = 2;
            }
        }

        private void ApplySetup()
        {
            int size = Decimal.ToInt32(overlaySizeBox.Value);
            int warning = size * Decimal.ToInt32(warningPercentBox.Value) / 100;
            int critical = size * Decimal.ToInt32(criticalPercentBox.Value) / 100;
            if (warning >= critical)
            {
                ShowError(UiText.T("임계값 오류", "Invalid thresholds."), UiText.T("경고 임계값은 위험 임계값보다 낮아야 합니다.", "Warning threshold must be lower than critical threshold."));
                return;
            }

            VolumeSelection volumes;
            string error;
            if (!VolumeSelectionParser.TryParse(volumeBox.Text, true, out volumes, out error))
            {
                ShowError(UiText.T("볼륨 오류", "Invalid volume."), error);
                return;
            }

            RunPlan(controller.CreateSetupPlan(
                Convert.ToString(overlayTypeBox.SelectedItem),
                volumes,
                size,
                warning,
                critical));
        }

        private void EnableFilter()
        {
            RunPlan(controller.CreateSimplePlan(UiText.T("UWF 필터 켜기", "Enable UWF filter"), UiText.T("다음 재시작 후 UWF 보호가 켜집니다.", "UWF protection will be enabled after the next restart."), "uwfmgr.exe", "filter enable"));
        }

        private void DisableFilter()
        {
            RunPlan(controller.CreateSimplePlan(UiText.T("UWF 필터 끄기", "Disable UWF filter"), UiText.T("다음 재시작 후 UWF 보호가 꺼집니다.", "UWF protection will be disabled after the next restart."), "uwfmgr.exe", "filter disable"));
        }

        private void CleanupDiskOverlaySpace()
        {
            var status = controller.GetStatus();
            RunPlan(controller.CreateDiskOverlayCleanupPlan(status == null ? null : status.Snapshot));
        }

        private void FullDisableUwf()
        {
            var status = controller.GetStatus();
            RunPlan(controller.CreateFullDisablePlan(status == null ? null : status.Snapshot));
        }

        private void FullResetUwf()
        {
            var status = controller.GetStatus();
            RunPlan(controller.CreateFullResetPlan(status == null ? null : status.Snapshot));
        }

        private void ProtectVolume()
        {
            VolumeSelection volumes;
            string error;
            if (!VolumeSelectionParser.TryParse(volumeBox.Text, true, out volumes, out error))
            {
                ShowError(UiText.T("볼륨 오류", "Invalid volume."), error);
                return;
            }

            RunPlan(controller.CreateVolumeProtectionPlan(volumes, true));
        }

        private void UnprotectVolume()
        {
            VolumeSelection volumes;
            string error;
            if (!VolumeSelectionParser.TryParse(volumeBox.Text, false, out volumes, out error))
            {
                ShowError(UiText.T("볼륨 오류", "Invalid volume."), error);
                return;
            }

            RunPlan(controller.CreateVolumeProtectionPlan(volumes, false));
        }

        private void AddFileExclusion()
        {
            var path = fileExclusionBox.Text.Trim();
            var validation = SafetyRules.ValidateFileExclusion(path, true);
            if (!validation.Allowed)
            {
                ShowError(UiText.T("폴더/파일 예외 차단", "Blocked folder/file exclusion."), validation.Message);
                return;
            }

            RunPlan(controller.CreateFileExclusionPlan(path, true, validation.Message));
        }

        private void RemoveFileExclusion()
        {
            var path = fileExclusionBox.Text.Trim();
            var validation = SafetyRules.ValidateFileExclusion(path, false);
            if (!validation.Allowed)
            {
                ShowError(UiText.T("폴더/파일 예외 오류", "Invalid folder/file exclusion."), validation.Message);
                return;
            }

            RunPlan(controller.CreateFileExclusionPlan(path, false, UiText.T("이 변경은 재시작 후 적용됩니다.", "This change takes effect after restart.")));
        }

        private void ShowFileExclusions()
        {
            RunReadOnly(UiText.T("폴더/파일 예외 목록", "Folder/file exclusions"), "file get-exclusions all");
        }

        private void RefreshExclusionsOnly()
        {
            var status = controller.GetStatus();
            UpdateDashboard(status);
            UpdateExclusionLists(status == null ? null : status.Snapshot);
            statusBox.Text = status == null ? String.Empty : status.Report;
            AppendLog(UiText.T("예외 목록을 새로고침했습니다.", "Exclusion lists refreshed."));
        }

        private void UseSelectedFileExclusion()
        {
            string path;
            if (!TryGetSelectedListValue(fileExclusionListBox, UiText.T("폴더/파일 예외 선택", "Select folder/file exclusion"), out path))
            {
                return;
            }

            fileExclusionBox.Text = path;
            AppendLog(UiText.T("선택한 폴더/파일 예외를 입력칸에 넣었습니다: ", "Copied selected folder/file exclusion to the input: ") + path);
        }

        private void RemoveSelectedFileExclusion()
        {
            string path;
            if (!TryGetSelectedListValue(fileExclusionListBox, UiText.T("폴더/파일 예외 선택", "Select folder/file exclusion"), out path))
            {
                return;
            }

            fileExclusionBox.Text = path;
            RemoveFileExclusion();
        }

        private bool TryGetSelectedListValue(ListBox box, string title, out string value)
        {
            value = Convert.ToString(box == null ? null : box.SelectedItem);
            if (!String.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            ShowError(title, UiText.T("목록에서 항목을 먼저 선택하세요.", "Select an item from the list first."));
            return false;
        }

        private void SelectFileExclusionFile()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = UiText.T("예외로 추가할 파일 선택", "Select a file to add as an exclusion");
                dialog.Filter = UiText.T("모든 파일 (*.*)|*.*", "All files (*.*)|*.*");
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                dialog.Multiselect = false;

                var initial = GetInitialDirectory(fileExclusionBox.Text);
                if (!String.IsNullOrEmpty(initial))
                {
                    dialog.InitialDirectory = initial;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    fileExclusionBox.Text = dialog.FileName;
                    AppendLog(UiText.T("파일 예외 경로를 선택했습니다: ", "Selected file exclusion path: ") + dialog.FileName);
                }
            }
        }

        private void SelectFileExclusionFolder()
        {
            using (var folder = new FolderBrowserDialog())
            {
                folder.Description = UiText.T("예외로 추가할 폴더를 선택하세요.", "Select a folder to add as an exclusion.");
                folder.ShowNewFolderButton = false;
                var initial = GetInitialDirectory(fileExclusionBox.Text);
                if (!String.IsNullOrEmpty(initial))
                {
                    folder.SelectedPath = initial;
                }

                if (folder.ShowDialog(this) == DialogResult.OK)
                {
                    fileExclusionBox.Text = folder.SelectedPath;
                    AppendLog(UiText.T("폴더 예외 경로를 선택했습니다: ", "Selected folder exclusion path: ") + folder.SelectedPath);
                }
            }
        }

        internal static string GetInitialDirectory(string pathText)
        {
            var fallback = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (String.IsNullOrWhiteSpace(pathText))
            {
                return Directory.Exists(fallback) ? fallback : String.Empty;
            }

            try
            {
                var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(pathText.Trim()));
                if (Directory.Exists(fullPath))
                {
                    return fullPath;
                }
                if (File.Exists(fullPath))
                {
                    return Path.GetDirectoryName(fullPath);
                }

                var parent = Path.GetDirectoryName(fullPath);
                if (!String.IsNullOrEmpty(parent) && Directory.Exists(parent))
                {
                    return parent;
                }
            }
            catch
            {
            }

            return Directory.Exists(fallback) ? fallback : String.Empty;
        }

        private void AddRegistryExclusion()
        {
            var key = registryExclusionBox.Text.Trim();
            var validation = SafetyRules.ValidateRegistryExclusion(key);
            if (!validation.Allowed)
            {
                ShowError(UiText.T("레지스트리 예외 차단", "Blocked registry exclusion."), validation.Message);
                return;
            }

            RunPlan(controller.CreateSimplePlan(UiText.T("레지스트리 예외 추가", "Add registry exclusion"), validation.Message + Environment.NewLine + UiText.T("이 변경은 재시작 후 적용됩니다.", "This change takes effect after restart."), "uwfmgr.exe", "registry add-exclusion " + Quote(SafetyRules.NormalizeRegistryKey(key))));
        }

        private void RemoveRegistryExclusion()
        {
            var key = registryExclusionBox.Text.Trim();
            var validation = SafetyRules.ValidateRegistryExclusion(key);
            if (!validation.Allowed)
            {
                ShowError(UiText.T("레지스트리 예외 오류", "Invalid registry exclusion."), validation.Message);
                return;
            }

            RunPlan(controller.CreateSimplePlan(UiText.T("레지스트리 예외 제거", "Remove registry exclusion"), UiText.T("이 변경은 재시작 후 적용됩니다.", "This change takes effect after restart."), "uwfmgr.exe", "registry remove-exclusion " + Quote(SafetyRules.NormalizeRegistryKey(key))));
        }

        private void ShowRegistryExclusions()
        {
            RunReadOnly(UiText.T("레지스트리 예외 목록", "Registry exclusions"), "registry get-exclusions");
        }

        private void UseSelectedRegistryExclusion()
        {
            string key;
            if (!TryGetSelectedListValue(registryExclusionListBox, UiText.T("레지스트리 예외 선택", "Select registry exclusion"), out key))
            {
                return;
            }

            registryExclusionBox.Text = key;
            AppendLog(UiText.T("선택한 레지스트리 예외를 입력칸에 넣었습니다: ", "Copied selected registry exclusion to the input: ") + key);
        }

        private void RemoveSelectedRegistryExclusion()
        {
            string key;
            if (!TryGetSelectedListValue(registryExclusionListBox, UiText.T("레지스트리 예외 선택", "Select registry exclusion"), out key))
            {
                return;
            }

            registryExclusionBox.Text = key;
            RemoveRegistryExclusion();
        }

        private void FillRegistryExample()
        {
            registryExclusionBox.Text = @"HKLM\SOFTWARE\Vendor\Product";
            AppendLog(UiText.T("레지스트리 예시를 입력했습니다. 실제 제품 경로로 바꾼 뒤 사용하세요.",
                "Filled a registry example. Replace it with the real product key before use."));
        }

        private void CommitFile()
        {
            var path = commitFileBox.Text.Trim();
            if (!Path.IsPathRooted(path))
            {
                ShowError(UiText.T("파일 경로 오류", "Invalid file path."), UiText.T("C:\\Path\\file.ini 같은 전체 경로를 입력하세요.", "Use a fully qualified path such as C:\\Path\\file.ini."));
                return;
            }

            RunPlan(controller.CreateSimplePlan(UiText.T("파일 커밋", "Commit file"), UiText.T("선택한 오버레이 파일 변경을 보호 볼륨에 영구 기록합니다.", "This permanently writes the selected overlay file changes to the protected volume."), "uwfmgr.exe", "file commit " + Quote(path)));
        }

        private void CommitFileDeletion()
        {
            var path = commitFileBox.Text.Trim();
            if (!Path.IsPathRooted(path))
            {
                ShowError(UiText.T("파일 경로 오류", "Invalid file path."), UiText.T("C:\\Path\\file.ini 같은 전체 경로를 입력하세요.", "Use a fully qualified path such as C:\\Path\\file.ini."));
                return;
            }

            RunPlan(controller.CreateSimplePlan(UiText.T("파일 삭제 커밋", "Commit file deletion"), UiText.T("선택한 파일을 오버레이와 물리 볼륨에서 영구 삭제합니다.", "This permanently deletes the selected file from the overlay and physical volume."), "uwfmgr.exe", "file commit-delete " + Quote(path)));
        }

        private void CommitRegistry()
        {
            var key = commitRegistryKeyBox.Text.Trim();
            var validation = SafetyRules.ValidateRegistryKeyShape(key);
            if (!validation.Allowed)
            {
                ShowError(UiText.T("레지스트리 키 오류", "Invalid registry key."), validation.Message);
                return;
            }

            string args = "registry commit " + Quote(SafetyRules.NormalizeRegistryKey(key));
            var valueName = commitRegistryValueBox.Text.Trim();
            if (valueName.Length > 0)
            {
                args += " " + Quote(valueName);
            }

            RunPlan(controller.CreateSimplePlan(UiText.T("레지스트리 커밋", "Commit registry"), UiText.T("선택한 레지스트리 변경을 영구 기록합니다.", "This permanently writes the selected registry change."), "uwfmgr.exe", args));
        }

        private void CommitRegistryDeletion()
        {
            var key = commitRegistryKeyBox.Text.Trim();
            var validation = SafetyRules.ValidateRegistryKeyShape(key);
            if (!validation.Allowed)
            {
                ShowError(UiText.T("레지스트리 키 오류", "Invalid registry key."), validation.Message);
                return;
            }

            string args = "registry commit-delete " + Quote(SafetyRules.NormalizeRegistryKey(key));
            var valueName = commitRegistryValueBox.Text.Trim();
            if (valueName.Length > 0)
            {
                args += " " + Quote(valueName);
            }

            RunPlan(controller.CreateSimplePlan(UiText.T("레지스트리 삭제 커밋", "Commit registry deletion"), UiText.T("선택한 레지스트리 삭제를 영구 반영합니다.", "This permanently commits the selected registry deletion."), "uwfmgr.exe", args));
        }

        private void EnableServicing()
        {
            RunPlan(controller.CreateSimplePlan(UiText.T("서비스 모드 켜기", "Enable servicing mode"), UiText.T("재시작 후 장치가 UWF 서비스 모드로 진입합니다.", "The device enters UWF servicing mode after restart."), "uwfmgr.exe", "servicing enable"));
        }

        private void DisableServicing()
        {
            RunPlan(controller.CreateSimplePlan(UiText.T("서비스 모드 끄기", "Disable servicing mode"), UiText.T("재시작 후 장치가 UWF 서비스 모드에서 나옵니다.", "The device leaves UWF servicing mode after restart."), "uwfmgr.exe", "servicing disable"));
        }

        private void UpdateWindows()
        {
            RunPlan(controller.CreateSimplePlan(UiText.T("Windows 업데이트 서비스 실행", "Run Windows Update servicing"), UiText.T("UWF 서비스 업데이트 흐름을 실행합니다. 시간이 오래 걸릴 수 있습니다.", "This invokes UWF servicing update flow. It can take a long time."), "uwfmgr.exe", "servicing update-windows"));
        }

        private void ResetSettings()
        {
            RunPlan(controller.CreateSimplePlan(UiText.T("UWF 설정 초기화", "Reset UWF settings"), UiText.T("UWF 설정을 원래 상태로 복원하도록 요청합니다. Microsoft는 이 명령이 일부 Windows 10+ 이미지 경로에서 지원되지 않는다고 안내합니다.", "This asks UWF to restore settings to the original state. Microsoft notes this command is not supported for all Windows 10+ image paths."), "uwfmgr.exe", "filter reset-settings"));
        }

        private void SafeRestart()
        {
            RunPlan(controller.CreateSimplePlan(UiText.T("안전 재시작", "Safe restart"), UiText.T("오버레이가 가득 찼거나 거의 찬 상태여도 장치를 즉시 재시작합니다.", "The device will restart immediately, even when overlay is full or near full."), "uwfmgr.exe", "filter restart"));
        }

        private void SafeShutdown()
        {
            RunPlan(controller.CreateSimplePlan(UiText.T("안전 종료", "Safe shutdown"), UiText.T("오버레이가 가득 찼거나 거의 찬 상태여도 장치를 즉시 종료합니다.", "The device will shut down immediately, even when overlay is full or near full."), "uwfmgr.exe", "filter shutdown"));
        }

        private void RunReadOnly(string title, string args)
        {
            var result = controller.RunReadOnly(args);
            AppendLog("== " + title + " ==");
            AppendLog(result.ToDisplayText());
        }

        private void RunPlan(OperationPlan plan)
        {
            if (plan == null)
            {
                return;
            }

            if (plan.RequiresAdministrator && !UwfController.IsAdministrator())
            {
                var choice = MessageBox.Show(this,
                    UiText.T("이 작업은 관리자 권한이 필요합니다. 관리자 권한으로 다시 실행할까요?", "This operation requires administrator rights. Relaunch as administrator now?"),
                    UiText.T("관리자 권한 필요", "Administrator required"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (choice == DialogResult.Yes)
                {
                    RelaunchAsAdmin();
                }
                return;
            }

            var prompt = plan.ToDisplayText();
            var confirm = MessageBox.Show(this, prompt, UiText.T("작업 계획 확인", "Review operation plan"), MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (confirm != DialogResult.OK)
            {
                AppendLog(UiText.T("취소됨: ", "Canceled: ") + plan.Title);
                return;
            }

            AppendLog(UiText.T("== 적용 중: ", "== Applying: ") + plan.Title + " ==");
            var report = controller.ExecutePlan(plan);
            AppendLog(report);
            RefreshStatus();
        }

        private void AppendLog(string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                return;
            }

            logBox.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text.Replace("\r\n", "\r\n          ") + Environment.NewLine);
        }

        private void ShowError(string title, string message)
        {
            MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppendLog(title + " " + message);
        }

        private static string GetSizingVolume(string volumeText)
        {
            VolumeSelection selection;
            string error;
            if (VolumeSelectionParser.TryParse(volumeText, true, out selection, out error) &&
                selection != null &&
                selection.Volumes.Count > 0)
            {
                return selection.Volumes[0];
            }

            return "C:";
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
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

    internal sealed class VolumeSelection
    {
        public bool IsAll;
        public readonly List<string> Volumes = new List<string>();

        public string DisplayText()
        {
            if (IsAll)
            {
                return "all";
            }
            return String.Join(", ", Volumes.ToArray());
        }
    }

    internal static class VolumeSelectionParser
    {
        public static bool TryParse(string text, bool allowAll, out VolumeSelection selection, out string error)
        {
            selection = null;
            error = String.Empty;

            if (String.IsNullOrWhiteSpace(text))
            {
                error = UiText.T("C: 또는 C:,D: 같은 드라이브 문자를 입력하세요.", "Use drive letters such as C: or C:,D:.");
                return false;
            }

            var tokens = text.Split(new char[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                error = UiText.T("C: 또는 C:,D: 같은 드라이브 문자를 입력하세요.", "Use drive letters such as C: or C:,D:.");
                return false;
            }

            var parsed = new VolumeSelection();
            for (int i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i].Trim();
                if (String.Equals(token, "all", StringComparison.OrdinalIgnoreCase))
                {
                    if (!allowAll)
                    {
                        error = UiText.T("all은 볼륨 보호에만 사용할 수 있습니다. 보호 해제는 C:,D:처럼 볼륨을 직접 지정하세요.",
                            "all is supported only for protecting volumes. For unprotect, specify volumes such as C:,D:.");
                        return false;
                    }
                    if (tokens.Length > 1)
                    {
                        error = UiText.T("all은 다른 볼륨과 함께 입력할 수 없습니다.", "all cannot be combined with other volumes.");
                        return false;
                    }
                    parsed.IsAll = true;
                    selection = parsed;
                    return true;
                }

                var volume = NormalizeVolume(token);
                if (volume == null)
                {
                    error = UiText.T("볼륨은 C: 또는 D: 형식이어야 합니다. 여러 개는 C:,D:처럼 입력하세요.",
                        "Volumes must be in C: or D: form. Enter multiple volumes as C:,D:.");
                    return false;
                }

                AddUnique(parsed.Volumes, volume);
            }

            if (parsed.Volumes.Count == 0)
            {
                error = UiText.T("보호할 볼륨을 입력하세요.", "Enter at least one volume.");
                return false;
            }

            selection = parsed;
            return true;
        }

        private static string NormalizeVolume(string volume)
        {
            if (String.IsNullOrWhiteSpace(volume))
            {
                return null;
            }

            volume = volume.Trim();
            if (volume.Length == 1 && Char.IsLetter(volume[0]))
            {
                return Char.ToUpperInvariant(volume[0]) + ":";
            }

            if (volume.Length == 2 && Char.IsLetter(volume[0]) && volume[1] == ':')
            {
                return Char.ToUpperInvariant(volume[0]) + ":";
            }

            return null;
        }

        private static void AddUnique(List<string> volumes, string volume)
        {
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

    internal static class TextBoxCompat
    {
        private const int EmSetCueBanner = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        public static void PlaceholderTextSafe(this TextBox textBox, string text)
        {
            if (textBox.IsHandleCreated)
            {
                SendMessage(textBox.Handle, EmSetCueBanner, IntPtr.Zero, text);
                return;
            }

            textBox.HandleCreated += delegate
            {
                SendMessage(textBox.Handle, EmSetCueBanner, IntPtr.Zero, text);
            };
        }
    }

    internal sealed class UwfController
    {
        private const int MinimumOverlaySizeMb = 1024;
        private const int DefaultWarningThresholdMb = 512;
        private const int DefaultCriticalThresholdMb = 1024;
        private const uint TokenQuery = 0x0008;
        private readonly CommandRunner runner;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool GetTokenInformation(IntPtr tokenHandle, TokenInformationClass tokenInformationClass, out TokenElevation tokenInformation, int tokenInformationLength, out int returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        private enum TokenInformationClass
        {
            TokenElevation = 20
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TokenElevation
        {
            public int TokenIsElevated;
        }

        public UwfController()
        {
            runner = new CommandRunner();
        }

        public UwfStatus GetStatus()
        {
            var status = new UwfStatus();
            status.IsAdministrator = IsAdministrator();
            status.OsCaption = GetOsCaption();
            status.IsLikelySupportedEdition = IsLikelySupportedEdition(status.OsCaption);
            status.UwfToolPath = GetUwfMgrPath();
            status.UwfToolExists = File.Exists(status.UwfToolPath);
            status.Snapshot = QuerySnapshot();

            var report = new StringBuilder();
            report.AppendLine(UiText.T("포터블 UWF 관리자 진단 보고서", "Portable UWF Manager diagnostic report"));
            report.AppendLine(UiText.T("시간: ", "Timestamp: ") + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            report.AppendLine(UiText.T("관리자 권한: ", "Administrator: ") + (status.IsAdministrator ? UiText.T("예", "yes") : UiText.T("아니오", "no")));
            report.AppendLine("OS: " + status.OsCaption);
            report.AppendLine(UiText.T("지원 가능 Edition: ", "Likely supported edition: ") + (status.IsLikelySupportedEdition ? UiText.T("예", "yes") : UiText.T("확인 필요", "check edition")));
            if (!status.IsLikelySupportedEdition && status.Snapshot != null && status.Snapshot.HasObservedState())
            {
                report.AppendLine(UiText.T("Edition 참고: 공식 지원 Edition은 확인 필요하지만 현재 UWF WMI 상태는 읽혔습니다.",
                    "Edition note: official support should be verified, but the current UWF WMI state was readable."));
            }
            report.AppendLine(UiText.T("OS 비트수: ", "OS bitness: ") + (Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit"));
            report.AppendLine(UiText.T("프로세스 비트수: ", "Process bitness: ") + (Environment.Is64BitProcess ? "64-bit" : "32-bit"));
            report.AppendLine("uwfmgr.exe: " + (status.UwfToolExists ? status.UwfToolPath : UiText.T("없음", "not found")));
            report.AppendLine();

            report.AppendLine(UiText.T("WMI 상태 스냅샷", "WMI snapshot"));
            report.AppendLine(status.Snapshot.ToReport());
            report.AppendLine();

            if (!status.UwfToolExists)
            {
                report.AppendLine(UiText.T("UWF 명령줄 도구가 없습니다. 먼저 Client-UnifiedWriteFilter 기능을 설치하세요.",
                    "UWF command-line tool is not present. Install the Client-UnifiedWriteFilter feature first."));
                status.Report = report.ToString();
                return status;
            }

            AppendCommand(report, "uwfmgr.exe get-config", RunReadOnly("get-config"));
            AppendCommand(report, "uwfmgr.exe overlay get-config", RunReadOnly("overlay get-config"));
            AppendCommand(report, "uwfmgr.exe overlay get-consumption", RunReadOnly("overlay get-consumption"));
            AppendCommand(report, "uwfmgr.exe overlay get-availablespace", RunReadOnly("overlay get-availablespace"));
            AppendCommand(report, "uwfmgr.exe servicing get-config", RunReadOnly("servicing get-config"));
            AppendCommand(report, "uwfmgr.exe file get-exclusions all", RunReadOnly("file get-exclusions all"));
            AppendCommand(report, "uwfmgr.exe registry get-exclusions", RunReadOnly("registry get-exclusions"));

            status.Report = report.ToString();
            return status;
        }

        public OperationPlan CreateInstallFeaturePlan()
        {
            var plan = new OperationPlan(UiText.T("UWF 기능 설치", "Install UWF feature"));
            plan.RequiresAdministrator = true;
            plan.Warning = UiText.T("Windows 선택 기능 Client-UnifiedWriteFilter를 설치합니다. 안정적인 UWF 설정을 위해 설치 후 재부팅이 필요합니다.",
                "Installs the Windows optional feature Client-UnifiedWriteFilter. A reboot is required before UWF can be configured reliably.");
            plan.Commands.Add(new CommandSpec("dism.exe", "/Online /Enable-Feature /FeatureName:Client-UnifiedWriteFilter /NoRestart"));
            return plan;
        }

        public OperationPlan CreateSetupPlan(string overlayType, VolumeSelection volumes, int sizeMb, int warningMb, int criticalMb)
        {
            if (!String.Equals(overlayType, "RAM", StringComparison.OrdinalIgnoreCase) &&
                !String.Equals(overlayType, "DISK", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid overlay type.");
            }

            if (volumes == null || (!volumes.IsAll && volumes.Volumes.Count == 0))
            {
                throw new InvalidOperationException("At least one volume is required.");
            }

            var plan = new OperationPlan(UiText.T("UWF " + overlayType + " 오버레이 설정", "Configure UWF " + overlayType + " overlay"));
            plan.RequiresAdministrator = true;
            plan.Warning =
                UiText.T("주의: 오버레이 유형과 최대 크기 변경은 현재 세션에서 UWF가 꺼져 있어야 합니다. " +
                    "아래 설정 중 일부는 다음 부팅에 적용되도록 예약됩니다.",
                    "Review carefully: overlay type and maximum size changes require UWF to be disabled in the current session. " +
                    "All listed settings are staged for the next boot where applicable.");
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay set-type " + overlayType.ToUpperInvariant()));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay set-size " + sizeMb.ToString()));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay set-warningthreshold " + warningMb.ToString()));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay set-criticalthreshold " + criticalMb.ToString()));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "filter enable"));
            AddVolumeCommands(plan, "protect", volumes);
            return plan;
        }

        public OperationPlan CreateVolumeProtectionPlan(VolumeSelection volumes, bool protect)
        {
            if (volumes == null || (!volumes.IsAll && volumes.Volumes.Count == 0))
            {
                throw new InvalidOperationException("At least one volume is required.");
            }
            if (!protect && volumes.IsAll)
            {
                throw new InvalidOperationException("Unprotect all is not supported by uwfmgr.exe.");
            }

            var actionText = protect ? UiText.T("볼륨 보호 ", "Protect volume ") : UiText.T("볼륨 보호 해제 ", "Unprotect volume ");
            var plan = new OperationPlan(actionText + volumes.DisplayText());
            plan.RequiresAdministrator = true;
            plan.Warning = protect
                ? UiText.T("필터가 켜져 있으면 다음 재시작 후 선택한 볼륨이 보호됩니다.", "The selected volumes will be protected after the next restart if the filter is enabled.")
                : UiText.T("다음 재시작 후 선택한 볼륨의 보호가 해제됩니다.", "The selected volumes will stop being protected after the next restart.");
            AddVolumeCommands(plan, protect ? "protect" : "unprotect", volumes);
            return plan;
        }

        public OperationPlan CreateDiskOverlayCleanupPlan(UwfSnapshot snapshot)
        {
            var plan = new OperationPlan(UiText.T("DISK 오버레이 공간 정리", "Clean DISK overlay space"));
            if (!CanChangeOverlayConfigNow(snapshot))
            {
                plan.Warning = UiText.T(
                    "현재 세션에서 UWF가 켜져 있거나 상태를 확인할 수 없습니다. 먼저 필터 끄기를 예약합니다. 재부팅 후 이 작업을 다시 실행하면 DISK 예약 공간을 RAM/1024MB 기준으로 되돌립니다.",
                    "UWF is enabled in the current session, or the current state is unknown. This first schedules the filter to turn off. After reboot, run this action again to release DISK overlay reservation by returning to RAM/1024MB.");
                plan.Commands.Add(new CommandSpec("uwfmgr.exe", "filter disable"));
                return plan;
            }

            plan.Warning = UiText.T(
                "현재 세션에서 UWF가 꺼져 있어야 실행됩니다. DISK 오버레이 예약 파일을 회수하기 위해 다음 세션 오버레이를 RAM, 최소 1024MB, 기본 임계값으로 바꿉니다. 적용 후 재부팅하세요.",
                "This requires UWF to be disabled in the current session. It releases the DISK overlay reservation by staging RAM overlay, the 1024MB minimum size, and default thresholds for the next session. Reboot after applying.");
            AddDiskOverlayReleaseCommands(plan);
            return plan;
        }

        public OperationPlan CreateFullDisablePlan(UwfSnapshot snapshot)
        {
            var plan = new OperationPlan(UiText.T("UWF 완전 끄기", "UWF full off"));
            if (!CanChangeOverlayConfigNow(snapshot))
            {
                plan.Warning = UiText.T(
                    "현재 세션에서 UWF가 켜져 있거나 상태를 확인할 수 없습니다. 1단계로 필터 끄기만 예약합니다. 재부팅 후 다시 실행하면 보호 볼륨 해제와 DISK 오버레이 공간 정리를 마칩니다.",
                    "UWF is enabled in the current session, or the current state is unknown. Step 1 only schedules the filter to turn off. After reboot, run this again to unprotect volumes and clean DISK overlay reservation.");
                plan.Commands.Add(new CommandSpec("uwfmgr.exe", "filter disable"));
                return plan;
            }

            plan.Warning = UiText.T(
                "UWF를 꺼진 상태로 고정하고, 보호 볼륨을 해제하고, 서비스 모드를 끄고, DISK 오버레이 예약 공간을 RAM/1024MB 기준으로 되돌립니다. 적용 후 재부팅하세요.",
                "Keeps UWF off, unprotects protected volumes, disables servicing mode, and releases DISK overlay reservation by returning to RAM/1024MB. Reboot after applying.");
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "filter disable"));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "servicing disable", 120000, true));
            AddVolumeUnprotectCommands(plan, snapshot);
            AddPersistentOverlayClearCommands(plan);
            AddDiskOverlayReleaseCommands(plan);
            return plan;
        }

        public OperationPlan CreateFullResetPlan(UwfSnapshot snapshot)
        {
            var plan = new OperationPlan(UiText.T("UWF 완전 초기화", "UWF full reset"));
            if (!CanChangeOverlayConfigNow(snapshot))
            {
                plan.Warning = UiText.T(
                    "현재 세션에서 UWF가 켜져 있거나 상태를 확인할 수 없습니다. 1단계로 필터 끄기만 예약합니다. 재부팅 후 다시 실행하면 UWF 초기화와 수동 정리를 진행합니다.",
                    "UWF is enabled in the current session, or the current state is unknown. Step 1 only schedules the filter to turn off. After reboot, run this again to reset UWF and apply manual cleanup.");
                plan.Commands.Add(new CommandSpec("uwfmgr.exe", "filter disable"));
                return plan;
            }

            plan.Warning = UiText.T(
                "가능하면 UWF의 기본 reset-settings를 먼저 요청하고, 실패해도 수동 초기화를 계속합니다. 최종적으로 필터 끄기, 보호 볼륨 해제, 서비스 모드 끄기, DISK 오버레이 공간 정리를 예약합니다. 적용 후 재부팅하세요.",
                "Attempts UWF reset-settings first when available, then continues manual reset even if that command is unsupported. Final staged state turns the filter off, unprotects volumes, disables servicing, and cleans DISK overlay reservation. Reboot after applying.");
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "filter reset-settings", 120000, true));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "filter disable"));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "servicing disable", 120000, true));
            AddVolumeUnprotectCommands(plan, snapshot);
            AddPersistentOverlayClearCommands(plan);
            AddDiskOverlayReleaseCommands(plan);
            return plan;
        }

        public OperationPlan CreateSimplePlan(string title, string warning, string fileName, string args)
        {
            var plan = new OperationPlan(title);
            plan.RequiresAdministrator = true;
            plan.Warning = warning;
            plan.Commands.Add(new CommandSpec(fileName, args));
            return plan;
        }

        public OperationPlan CreateFileExclusionPlan(string path, bool add, string warning)
        {
            var title = add
                ? UiText.T("폴더/파일 예외 추가", "Add folder/file exclusion")
                : UiText.T("폴더/파일 예외 제거", "Remove folder/file exclusion");
            var plan = new OperationPlan(title);
            plan.RequiresAdministrator = true;
            plan.Warning = warning + Environment.NewLine + UiText.T(
                "이 장비에서는 uwfmgr.exe 파일 예외 명령이 권한 오류를 낼 수 있어 UWF WMI 공급자로 직접 적용합니다.",
                "This device can return access denied for uwfmgr.exe file exclusion commands, so this operation applies through the UWF WMI provider directly.");
            var action = add ? "add-exclusion" : "remove-exclusion";
            plan.Commands.Add(new CommandSpec("UWF WMI", "folder " + action + " " + Elevation.QuoteArgumentForCreateProcess(path)));
            return plan;
        }

        private static void AddVolumeCommands(OperationPlan plan, string action, VolumeSelection volumes)
        {
            if (volumes.IsAll)
            {
                plan.Commands.Add(new CommandSpec("uwfmgr.exe", "volume " + action + " all"));
                return;
            }

            for (int i = 0; i < volumes.Volumes.Count; i++)
            {
                plan.Commands.Add(new CommandSpec("uwfmgr.exe", "volume " + action + " " + volumes.Volumes[i]));
            }
        }

        private static bool CanChangeOverlayConfigNow(UwfSnapshot snapshot)
        {
            return snapshot != null && snapshot.FilterCurrentEnabled == false;
        }

        private static void AddDiskOverlayReleaseCommands(OperationPlan plan)
        {
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay set-type RAM"));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay set-size " + MinimumOverlaySizeMb.ToString()));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay set-warningthreshold " + DefaultWarningThresholdMb.ToString()));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay set-criticalthreshold " + DefaultCriticalThresholdMb.ToString()));
        }

        private static void AddPersistentOverlayClearCommands(OperationPlan plan)
        {
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay set-persistent off", 120000, true));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay reset-persistentstate on", 120000, true));
        }

        private static void AddVolumeUnprotectCommands(OperationPlan plan, UwfSnapshot snapshot)
        {
            var volumes = GetKnownProtectedVolumes(snapshot);
            for (int i = 0; i < volumes.Count; i++)
            {
                plan.Commands.Add(new CommandSpec("uwfmgr.exe", "volume unprotect " + volumes[i], 120000, true));
            }
        }

        private static List<string> GetKnownProtectedVolumes(UwfSnapshot snapshot)
        {
            var volumes = new List<string>();
            if (snapshot == null)
            {
                return volumes;
            }

            AddUniqueVolumes(volumes, snapshot.CurrentProtectedVolumes);
            AddUniqueVolumes(volumes, snapshot.NextProtectedVolumes);
            return volumes;
        }

        private static void AddUniqueVolumes(List<string> target, List<string> source)
        {
            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                var volume = NormalizeVolumeToken(source[i]);
                if (String.IsNullOrEmpty(volume))
                {
                    continue;
                }

                bool exists = false;
                for (int j = 0; j < target.Count; j++)
                {
                    if (String.Equals(target[j], volume, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    target.Add(volume);
                }
            }
        }

        private static string NormalizeVolumeToken(string volume)
        {
            if (String.IsNullOrWhiteSpace(volume))
            {
                return String.Empty;
            }

            volume = volume.Trim();
            if (volume.Length >= 2 && volume[1] == ':')
            {
                return Char.ToUpperInvariant(volume[0]) + ":";
            }

            return volume;
        }

        public CommandResult RunReadOnly(string args)
        {
            return runner.Run(ResolveExecutable("uwfmgr.exe"), args, 30000);
        }

        private CommandResult TryRunWmiFallback(CommandSpec command)
        {
            if (command == null || !String.Equals(command.FileName, "uwfmgr.exe", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var normalized = NormalizeCommandArguments(command.Arguments);
            if (String.Equals(normalized, "filter enable", StringComparison.OrdinalIgnoreCase))
            {
                return InvokeUwfFilterMethod("Enable");
            }
            if (String.Equals(normalized, "filter disable", StringComparison.OrdinalIgnoreCase))
            {
                return InvokeUwfFilterMethod("Disable");
            }
            var parts = SplitCommandLine(command.Arguments);
            if (parts.Length == 3 &&
                String.Equals(parts[0], "file", StringComparison.OrdinalIgnoreCase))
            {
                if (String.Equals(parts[1], "add-exclusion", StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeUwfFileExclusionMethod("AddExclusion", parts[2]);
                }
                if (String.Equals(parts[1], "remove-exclusion", StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeUwfFileExclusionMethod("RemoveExclusion", parts[2]);
                }
            }
            if (parts.Length == 3 &&
                String.Equals(parts[0], "registry", StringComparison.OrdinalIgnoreCase))
            {
                if (String.Equals(parts[1], "add-exclusion", StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeUwfRegistryExclusionMethod("AddExclusion", parts[2]);
                }
                if (String.Equals(parts[1], "remove-exclusion", StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeUwfRegistryExclusionMethod("RemoveExclusion", parts[2]);
                }
            }
            if (parts.Length == 3 &&
                String.Equals(parts[0], "overlay", StringComparison.OrdinalIgnoreCase) &&
                String.Equals(parts[1], "set-type", StringComparison.OrdinalIgnoreCase))
            {
                if (String.Equals(parts[2], "RAM", StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeUwfOverlayConfigMethod("SetType", 0);
                }
                if (String.Equals(parts[2], "DISK", StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeUwfOverlayConfigMethod("SetType", 1);
                }
            }
            if (parts.Length == 3 &&
                String.Equals(parts[0], "overlay", StringComparison.OrdinalIgnoreCase))
            {
                uint value;
                if (UInt32.TryParse(parts[2], out value))
                {
                    if (String.Equals(parts[1], "set-size", StringComparison.OrdinalIgnoreCase))
                    {
                        return InvokeUwfOverlayConfigMethod("SetMaximumSize", value);
                    }
                    if (String.Equals(parts[1], "set-warningthreshold", StringComparison.OrdinalIgnoreCase))
                    {
                        return InvokeUwfOverlayMethod("SetWarningThreshold", value);
                    }
                    if (String.Equals(parts[1], "set-criticalthreshold", StringComparison.OrdinalIgnoreCase))
                    {
                        return InvokeUwfOverlayMethod("SetCriticalThreshold", value);
                    }
                }
            }

            return null;
        }

        private static string[] SplitCommandLine(string arguments)
        {
            if (String.IsNullOrWhiteSpace(arguments))
            {
                return new string[0];
            }

            var parts = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < arguments.Length; i++)
            {
                char c = arguments[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && Char.IsWhiteSpace(c))
                {
                    if (current.Length > 0)
                    {
                        parts.Add(current.ToString());
                        current.Length = 0;
                    }
                    continue;
                }

                if (c == '\\' && inQuotes && i + 1 < arguments.Length && arguments[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                    continue;
                }

                current.Append(c);
            }

            if (current.Length > 0)
            {
                parts.Add(current.ToString());
            }
            return parts.ToArray();
        }

        private static string[] SplitWhitespace(string arguments)
        {
            if (String.IsNullOrWhiteSpace(arguments))
            {
                return new string[0];
            }

            return arguments.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string NormalizeCommandArguments(string arguments)
        {
            if (String.IsNullOrWhiteSpace(arguments))
            {
                return String.Empty;
            }

            var parts = SplitCommandLine(arguments);
            if (parts.Length == 2)
            {
                return parts[0].ToLowerInvariant() + " " + parts[1].ToLowerInvariant();
            }

            return arguments.Trim();
        }

        private static CommandResult InvokeUwfFilterMethod(string methodName)
        {
            var result = new CommandResult();
            result.FileName = "WMI UWF_Filter";
            result.Arguments = methodName + "()";

            try
            {
                var scope = new ManagementScope(@"\\.\root\standardcimv2\embedded");
                scope.Connect();

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_Filter"))
                {
                    using (obj)
                    {
                        using (var outParams = obj.InvokeMethod(methodName, (ManagementBaseObject)null, null))
                        {
                            if (outParams != null && outParams["ReturnValue"] != null)
                            {
                                result.ExitCode = unchecked((int)Convert.ToUInt32(outParams["ReturnValue"]));
                            }
                            else
                            {
                                result.ExitCode = 0;
                            }
                        }
                    }

                    result.Output = "UWF_Filter." + methodName + " returned " + CommandResult.FormatExitCode(result.ExitCode) + ".";
                    return result;
                }

                result.ExitCode = -1;
                result.Error = "UWF_Filter WMI object was not found.";
            }
            catch (Exception ex)
            {
                result.ExitCode = Marshal.GetHRForException(ex);
                result.Error = ex.Message;
            }

            return result;
        }

        private static CommandResult InvokeUwfOverlayConfigMethod(string methodName, uint value)
        {
            var result = new CommandResult();
            result.FileName = "WMI UWF_OverlayConfig";
            result.Arguments = methodName + "(" + value.ToString() + ")";

            try
            {
                var scope = new ManagementScope(@"\\.\root\standardcimv2\embedded");
                scope.Connect();

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_OverlayConfig"))
                {
                    using (obj)
                    {
                        if (GetNullableBool(obj, "CurrentSession") == true)
                        {
                            continue;
                        }

                        using (var inParams = obj.GetMethodParameters(methodName))
                        {
                            if (String.Equals(methodName, "SetType", StringComparison.OrdinalIgnoreCase))
                            {
                                inParams["type"] = value;
                            }
                            else
                            {
                                inParams["size"] = value;
                            }

                            using (var outParams = obj.InvokeMethod(methodName, inParams, null))
                            {
                                result.ExitCode = GetReturnValue(outParams);
                            }
                        }
                    }

                    result.Output = "UWF_OverlayConfig." + methodName + " returned " + CommandResult.FormatExitCode(result.ExitCode) + ".";
                    return result;
                }

                result.ExitCode = -1;
                result.Error = "Next-session UWF_OverlayConfig WMI object was not found.";
            }
            catch (Exception ex)
            {
                result.ExitCode = Marshal.GetHRForException(ex);
                result.Error = ex.Message;
            }

            return result;
        }

        private static CommandResult InvokeUwfOverlayMethod(string methodName, uint value)
        {
            var result = new CommandResult();
            result.FileName = "WMI UWF_Overlay";
            result.Arguments = methodName + "(" + value.ToString() + ")";

            try
            {
                var scope = new ManagementScope(@"\\.\root\standardcimv2\embedded");
                scope.Connect();

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_Overlay"))
                {
                    using (obj)
                    using (var inParams = obj.GetMethodParameters(methodName))
                    {
                        inParams["size"] = value;
                        using (var outParams = obj.InvokeMethod(methodName, inParams, null))
                        {
                            result.ExitCode = GetReturnValue(outParams);
                        }
                    }

                    result.Output = "UWF_Overlay." + methodName + " returned " + CommandResult.FormatExitCode(result.ExitCode) + ".";
                    return result;
                }

                result.ExitCode = -1;
                result.Error = "UWF_Overlay WMI object was not found.";
            }
            catch (Exception ex)
            {
                result.ExitCode = Marshal.GetHRForException(ex);
                result.Error = ex.Message;
            }

            return result;
        }

        private static CommandResult InvokeUwfFileExclusionMethod(string methodName, string fileName)
        {
            var result = new CommandResult();
            result.FileName = "WMI UWF_Volume";
            result.Arguments = methodName + "(" + fileName + ")";

            try
            {
                var volume = GetPathVolume(fileName);
                if (String.IsNullOrEmpty(volume))
                {
                    result.ExitCode = -1;
                    result.Error = "Could not determine the drive letter for the file exclusion path.";
                    return result;
                }

                var scope = new ManagementScope(@"\\.\root\standardcimv2\embedded");
                scope.Connect();

                var volumeRelativePath = GetVolumeRelativePath(fileName);
                if (String.IsNullOrEmpty(volumeRelativePath))
                {
                    result.ExitCode = -1;
                    result.Error = "Could not determine the volume-relative path for the file exclusion.";
                    return result;
                }

                using (var obj = GetUwfVolumeByDriveLetter(scope, volume, false))
                {
                    result.ExitCode = InvokeUwfVolumeFileMethod(obj, methodName, volumeRelativePath);
                }

                result.Output = "UWF_Volume(next session " + volume + ")." + methodName + "(" + volumeRelativePath + ") returned " + CommandResult.FormatExitCode(result.ExitCode) + ".";
            }
            catch (Exception ex)
            {
                result.ExitCode = Marshal.GetHRForException(ex);
                result.Error = ex.Message;
            }

            return result;
        }

        private static int InvokeUwfVolumeFileMethod(ManagementObject obj, string methodName, string fileName)
        {
            using (var inParams = obj.GetMethodParameters(methodName))
            {
                inParams["FileName"] = fileName;
                using (var outParams = obj.InvokeMethod(methodName, inParams, null))
                {
                    return GetReturnValue(outParams);
                }
            }
        }

        private static ManagementObject GetUwfVolumeByDriveLetter(ManagementScope scope, string volume, bool currentSession)
        {
            var path = BuildUwfVolumeObjectPath(volume, currentSession);
            var obj = new ManagementObject(scope, new ManagementPath(path), null);
            obj.Get();
            return obj;
        }

        internal static string BuildUwfVolumeObjectPath(string volume, bool currentSession)
        {
            return "UWF_Volume.CurrentSession=" + (currentSession ? "True" : "False") +
                ",DriveLetter=\"" + EscapeWmiKeyString(volume) + "\",VolumeName=\"\"";
        }

        private static string EscapeWmiKeyString(string value)
        {
            if (value == null)
            {
                return String.Empty;
            }
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static CommandResult InvokeUwfRegistryExclusionMethod(string methodName, string registryKey)
        {
            var result = new CommandResult();
            result.FileName = "WMI UWF_RegistryFilter";
            result.Arguments = methodName + "(" + registryKey + ")";

            try
            {
                var scope = new ManagementScope(@"\\.\root\standardcimv2\embedded");
                scope.Connect();

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_RegistryFilter"))
                {
                    using (obj)
                    using (var inParams = obj.GetMethodParameters(methodName))
                    {
                        inParams["RegistryKey"] = registryKey;
                        using (var outParams = obj.InvokeMethod(methodName, inParams, null))
                        {
                            result.ExitCode = GetReturnValue(outParams);
                        }
                    }

                    result.Output = "UWF_RegistryFilter." + methodName + " returned " + CommandResult.FormatExitCode(result.ExitCode) + ".";
                    return result;
                }

                result.ExitCode = -1;
                result.Error = "UWF_RegistryFilter WMI object was not found.";
            }
            catch (Exception ex)
            {
                result.ExitCode = Marshal.GetHRForException(ex);
                result.Error = ex.Message;
            }

            return result;
        }

        private static string GetPathVolume(string path)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                return String.Empty;
            }

            try
            {
                var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim()));
                if (fullPath.Length >= 2 && fullPath[1] == ':' && Char.IsLetter(fullPath[0]))
                {
                    return Char.ToUpperInvariant(fullPath[0]) + ":";
                }
            }
            catch
            {
            }

            return String.Empty;
        }

        internal static string GetVolumeRelativePath(string path)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                return String.Empty;
            }

            try
            {
                var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim()));
                if (fullPath.Length >= 3 && fullPath[1] == ':' && fullPath[2] == '\\')
                {
                    return fullPath.Substring(2);
                }
            }
            catch
            {
            }

            return String.Empty;
        }

        private static int GetReturnValue(ManagementBaseObject outParams)
        {
            if (outParams != null && outParams["ReturnValue"] != null)
            {
                return unchecked((int)Convert.ToUInt32(outParams["ReturnValue"]));
            }

            return 0;
        }

        public string ExecutePlan(OperationPlan plan)
        {
            var output = new StringBuilder();
            output.AppendLine(plan.Title);
            output.AppendLine(plan.Warning);
            output.AppendLine();

            if (plan.RequiresAdministrator && !IsAdministrator())
            {
                output.AppendLine(UiText.T("상승된 관리자 권한이 확인되지 않아 변경 명령을 실행하지 않았습니다.",
                    "Elevated administrator rights were not confirmed, so no configuration command was executed."));
                output.AppendLine(UiText.T("앱을 관리자 권한으로 다시 실행한 뒤 작업을 다시 시도하세요.",
                    "Relaunch the app as administrator and try the operation again."));
                return output.ToString();
            }

            for (int i = 0; i < plan.Commands.Count; i++)
            {
                var command = plan.Commands[i];
                output.AppendLine("> " + command.FileName + " " + command.Arguments);
                var result = IsUwfWmiCommand(command)
                    ? ExecuteUwfWmiCommand(command)
                    : runner.Run(ResolveExecutable(command.FileName), command.Arguments, command.TimeoutMilliseconds);
                output.AppendLine(result.ToDisplayText());
                bool failed = result.TimedOut || result.ExitCode != 0;
                if (failed && !IsUwfWmiCommand(command))
                {
                    var fallback = TryRunWmiFallback(command);
                    if (fallback != null)
                    {
                        output.AppendLine(UiText.T("uwfmgr.exe가 실패하여 UWF WMI 공급자로 한 번 더 시도합니다.",
                            "uwfmgr.exe failed, so retrying through the UWF WMI provider."));
                        output.AppendLine("> " + fallback.FileName + " " + fallback.Arguments);
                        output.AppendLine(fallback.ToDisplayText());
                        result = fallback;
                        failed = result.TimedOut || result.ExitCode != 0;
                    }
                }

                if (failed)
                {
                    if (command.ContinueOnFailure)
                    {
                        output.AppendLine(UiText.T("선택 명령이 실패했지만 다음 명령을 계속 실행합니다.", "Optional command failed; continuing with the next command."));
                        continue;
                    }
                    output.AppendLine(UiText.T("명령이 실패하여 중단했습니다.", "Stopped because the command failed."));
                    break;
                }
            }

            return output.ToString();
        }

        private static bool IsUwfWmiCommand(CommandSpec command)
        {
            return command != null && String.Equals(command.FileName, "UWF WMI", StringComparison.OrdinalIgnoreCase);
        }

        private static CommandResult ExecuteUwfWmiCommand(CommandSpec command)
        {
            var result = new CommandResult();
            result.FileName = command.FileName;
            result.Arguments = command.Arguments;

            var parts = SplitCommandLine(command.Arguments);
            if (parts.Length == 3 &&
                (String.Equals(parts[0], "folder", StringComparison.OrdinalIgnoreCase) ||
                 String.Equals(parts[0], "file", StringComparison.OrdinalIgnoreCase)))
            {
                if (String.Equals(parts[1], "add-exclusion", StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeUwfFileExclusionMethod("AddExclusion", parts[2]);
                }
                if (String.Equals(parts[1], "remove-exclusion", StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeUwfFileExclusionMethod("RemoveExclusion", parts[2]);
                }
            }

            result.ExitCode = -1;
            result.Error = "Unsupported UWF WMI command.";
            return result;
        }

        public static bool IsAdministrator()
        {
            bool isAdministrator;
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                isAdministrator = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }

            if (!isAdministrator)
            {
                return false;
            }

            bool elevated;
            if (TryGetProcessElevation(out elevated))
            {
                return elevated;
            }

            return isAdministrator;
        }

        private static bool TryGetProcessElevation(out bool elevated)
        {
            elevated = false;
            IntPtr token = IntPtr.Zero;
            try
            {
                if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TokenQuery, out token))
                {
                    return false;
                }

                TokenElevation elevation;
                int returnLength;
                int size = Marshal.SizeOf(typeof(TokenElevation));
                if (!GetTokenInformation(token, TokenInformationClass.TokenElevation, out elevation, size, out returnLength))
                {
                    return false;
                }

                elevated = elevation.TokenIsElevated != 0;
                return true;
            }
            finally
            {
                if (token != IntPtr.Zero)
                {
                    CloseHandle(token);
                }
            }
        }

        private static string GetOsCaption()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var caption = Convert.ToString(obj["Caption"]);
                        if (!String.IsNullOrWhiteSpace(caption))
                        {
                            return caption;
                        }
                    }
                }
            }
            catch
            {
            }

            return Environment.OSVersion.ToString();
        }

        private static bool IsLikelySupportedEdition(string caption)
        {
            if (String.IsNullOrEmpty(caption))
            {
                return false;
            }

            return caption.IndexOf("Enterprise", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   caption.IndexOf("Education", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   caption.IndexOf("IoT", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetUwfMgrPath()
        {
            return GetSystemToolPath("uwfmgr.exe");
        }

        private static string ResolveExecutable(string fileName)
        {
            if (String.Equals(fileName, "uwfmgr.exe", StringComparison.OrdinalIgnoreCase))
            {
                var full = GetUwfMgrPath();
                if (File.Exists(full))
                {
                    return full;
                }
            }

            if (String.Equals(fileName, "dism.exe", StringComparison.OrdinalIgnoreCase))
            {
                var full = GetSystemToolPath("dism.exe");
                if (File.Exists(full))
                {
                    return full;
                }
            }

            return fileName;
        }

        private static string GetSystemToolPath(string exeName)
        {
            var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                var sysnative = Path.Combine(windows, "Sysnative\\" + exeName);
                if (File.Exists(sysnative))
                {
                    return sysnative;
                }
            }

            var system32 = Path.Combine(windows, "System32\\" + exeName);
            if (File.Exists(system32))
            {
                return system32;
            }

            return system32;
        }

        private static void AppendCommand(StringBuilder report, string title, CommandResult result)
        {
            report.AppendLine("== " + title + " ==");
            report.AppendLine(result.ToDisplayText());
            report.AppendLine();
        }

        private static UwfSnapshot QuerySnapshot()
        {
            var snapshot = new UwfSnapshot();
            try
            {
                var scope = new ManagementScope(@"\\.\root\standardcimv2\embedded");
                scope.Connect();

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_Filter"))
                {
                    snapshot.FilterCurrentEnabled = GetNullableBool(obj, "CurrentEnabled");
                    snapshot.FilterNextEnabled = GetNullableBool(obj, "NextEnabled");
                }

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_OverlayConfig"))
                {
                    bool current = GetNullableBool(obj, "CurrentSession") == true;
                    if (current)
                    {
                        snapshot.CurrentOverlayType = OverlayTypeText(GetNullableInt(obj, "Type"));
                        snapshot.CurrentMaximumSizeMb = GetNullableInt(obj, "MaximumSize");
                    }
                    else
                    {
                        snapshot.NextOverlayType = OverlayTypeText(GetNullableInt(obj, "Type"));
                        snapshot.NextMaximumSizeMb = GetNullableInt(obj, "MaximumSize");
                    }
                }

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_Overlay"))
                {
                    snapshot.OverlayConsumptionMb = GetNullableInt(obj, "OverlayConsumption");
                    snapshot.AvailableSpaceMb = GetNullableInt(obj, "AvailableSpace");
                    snapshot.WarningThresholdMb = GetNullableInt(obj, "WarningOverlayThreshold");
                    snapshot.CriticalThresholdMb = GetNullableInt(obj, "CriticalOverlayThreshold");
                }

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_Servicing"))
                {
                    bool current = GetNullableBool(obj, "CurrentSession") == true;
                    bool? enabled = GetNullableBool(obj, "ServicingEnabled");
                    if (!enabled.HasValue)
                    {
                        enabled = GetNullableBool(obj, "ServiceEnabled");
                    }
                    if (current)
                    {
                        snapshot.ServicingCurrentEnabled = enabled;
                    }
                    else
                    {
                        snapshot.ServicingNextEnabled = enabled;
                    }
                }

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_Volume"))
                {
                    var drive = Convert.ToString(obj["DriveLetter"]);
                    if (String.IsNullOrWhiteSpace(drive))
                    {
                        continue;
                    }
                    bool current = GetNullableBool(obj, "CurrentSession") == true;
                    bool isProtected = GetNullableBool(obj, "Protected") == true;
                    if (isProtected && current)
                    {
                        snapshot.CurrentProtectedVolumes.Add(drive);
                    }
                    if (isProtected && !current)
                    {
                        snapshot.NextProtectedVolumes.Add(drive);
                    }
                }

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_ExcludedFile"))
                {
                    AddUniqueText(snapshot.FileExclusions, Convert.ToString(obj["FileName"]));
                }

                AddFileExclusionsFromKnownVolumes(scope, snapshot);

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_ExcludedRegistryKey"))
                {
                    AddUniqueText(snapshot.RegistryExclusions, Convert.ToString(obj["RegistryKey"]));
                }
            }
            catch (Exception ex)
            {
                snapshot.Error = ex.Message;
            }

            return snapshot;
        }

        private static void AddFileExclusionsFromKnownVolumes(ManagementScope scope, UwfSnapshot snapshot)
        {
            var volumes = GetCandidateDriveLetters(snapshot);
            for (int i = 0; i < volumes.Count; i++)
            {
                AddFileExclusionsFromVolume(scope, snapshot, volumes[i], false);
                AddFileExclusionsFromVolume(scope, snapshot, volumes[i], true);
            }
        }

        private static List<string> GetCandidateDriveLetters(UwfSnapshot snapshot)
        {
            var volumes = new List<string>();
            AddUniqueVolumes(volumes, snapshot.CurrentProtectedVolumes);
            AddUniqueVolumes(volumes, snapshot.NextProtectedVolumes);

            try
            {
                var drives = DriveInfo.GetDrives();
                for (int i = 0; i < drives.Length; i++)
                {
                    var name = drives[i].Name;
                    if (!String.IsNullOrEmpty(name) && name.Length >= 2 && name[1] == ':')
                    {
                        AddUniqueVolume(volumes, Char.ToUpperInvariant(name[0]) + ":");
                    }
                }
            }
            catch
            {
            }

            return volumes;
        }

        private static void AddUniqueVolume(List<string> target, string volume)
        {
            if (String.IsNullOrWhiteSpace(volume))
            {
                return;
            }

            volume = NormalizeVolumeToken(volume);
            for (int i = 0; i < target.Count; i++)
            {
                if (String.Equals(target[i], volume, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            target.Add(volume);
        }

        private static void AddFileExclusionsFromVolume(ManagementScope scope, UwfSnapshot snapshot, string volume, bool currentSession)
        {
            try
            {
                using (var obj = GetUwfVolumeByDriveLetter(scope, volume, currentSession))
                using (var outParams = obj.InvokeMethod("GetExclusions", (ManagementBaseObject)null, null))
                {
                    if (outParams == null || outParams["ExcludedFiles"] == null)
                    {
                        return;
                    }

                    var exclusions = outParams["ExcludedFiles"] as Array;
                    if (exclusions == null)
                    {
                        return;
                    }

                    foreach (var exclusion in exclusions)
                    {
                        var item = exclusion as ManagementBaseObject;
                        var fileName = item == null ? Convert.ToString(exclusion) : Convert.ToString(item["FileName"]);
                        AddUniqueText(snapshot.FileExclusions, ToFullPathFromVolume(volume, fileName));
                    }
                }
            }
            catch
            {
            }
        }

        internal static string ToFullPathFromVolume(string volume, string fileName)
        {
            if (String.IsNullOrWhiteSpace(fileName))
            {
                return String.Empty;
            }

            fileName = fileName.Trim();
            if (fileName.Length >= 2 && fileName[1] == ':')
            {
                return fileName;
            }

            if (String.IsNullOrWhiteSpace(volume))
            {
                return fileName;
            }

            volume = NormalizeVolumeToken(volume);
            if (fileName.StartsWith("\\", StringComparison.Ordinal))
            {
                return volume + fileName;
            }

            return volume + "\\" + fileName;
        }

        private static List<ManagementObject> QueryWmi(ManagementScope scope, string className)
        {
            var results = new List<ManagementObject>();
            try
            {
                using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM " + className)))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        results.Add(obj);
                    }
                }
            }
            catch
            {
            }
            return results;
        }

        private static void AddUniqueText(List<string> target, string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return;
            }

            value = value.Trim();
            for (int i = 0; i < target.Count; i++)
            {
                if (String.Equals(target[i], value, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            target.Add(value);
        }

        private static bool? GetNullableBool(ManagementObject obj, string property)
        {
            try
            {
                var value = obj[property];
                if (value == null)
                {
                    return null;
                }
                return Convert.ToBoolean(value);
            }
            catch
            {
                return null;
            }
        }

        private static int? GetNullableInt(ManagementObject obj, string property)
        {
            try
            {
                var value = obj[property];
                if (value == null)
                {
                    return null;
                }
                return Convert.ToInt32(value);
            }
            catch
            {
                return null;
            }
        }

        private static string OverlayTypeText(int? type)
        {
            if (!type.HasValue)
            {
                return UiText.T("확인 불가", "Unknown");
            }
            return type.Value == 1 ? "DISK" : "RAM";
        }
    }

    internal sealed class UwfStatus
    {
        public bool IsAdministrator;
        public bool UwfToolExists;
        public bool IsLikelySupportedEdition;
        public string UwfToolPath;
        public string OsCaption;
        public string Report;
        public UwfSnapshot Snapshot;
    }

    internal sealed class UwfSnapshot
    {
        public bool? FilterCurrentEnabled;
        public bool? FilterNextEnabled;
        public string CurrentOverlayType = UiText.T("확인 불가", "Unknown");
        public string NextOverlayType = UiText.T("확인 불가", "Unknown");
        public int? CurrentMaximumSizeMb;
        public int? NextMaximumSizeMb;
        public int? OverlayConsumptionMb;
        public int? AvailableSpaceMb;
        public int? WarningThresholdMb;
        public int? CriticalThresholdMb;
        public bool? ServicingCurrentEnabled;
        public bool? ServicingNextEnabled;
        public readonly List<string> CurrentProtectedVolumes = new List<string>();
        public readonly List<string> NextProtectedVolumes = new List<string>();
        public readonly List<string> FileExclusions = new List<string>();
        public readonly List<string> RegistryExclusions = new List<string>();
        public string Error;

        public bool HasPendingChanges()
        {
            if (FilterCurrentEnabled != FilterNextEnabled)
            {
                return true;
            }
            if (!String.Equals(CurrentOverlayType, NextOverlayType, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (CurrentMaximumSizeMb != NextMaximumSizeMb)
            {
                return true;
            }
            if (ServicingCurrentEnabled != ServicingNextEnabled)
            {
                return true;
            }
            if (!String.Equals(CurrentProtectedVolumesText(), NextProtectedVolumesText(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        public bool HasObservedState()
        {
            if (!String.IsNullOrEmpty(Error))
            {
                return false;
            }

            return FilterCurrentEnabled.HasValue ||
                FilterNextEnabled.HasValue ||
                CurrentMaximumSizeMb.HasValue ||
                NextMaximumSizeMb.HasValue ||
                OverlayConsumptionMb.HasValue ||
                AvailableSpaceMb.HasValue ||
                WarningThresholdMb.HasValue ||
                CriticalThresholdMb.HasValue ||
                ServicingCurrentEnabled.HasValue ||
                ServicingNextEnabled.HasValue ||
                CurrentProtectedVolumes.Count > 0 ||
                NextProtectedVolumes.Count > 0 ||
                FileExclusions.Count > 0 ||
                RegistryExclusions.Count > 0;
        }

        public string CurrentProtectedVolumesText()
        {
            return JoinVolumes(CurrentProtectedVolumes);
        }

        public string NextProtectedVolumesText()
        {
            return JoinVolumes(NextProtectedVolumes);
        }

        public string FileExclusionsText()
        {
            return JoinItems(FileExclusions);
        }

        public string RegistryExclusionsText()
        {
            return JoinItems(RegistryExclusions);
        }

        public int GetOverlayUsagePercent()
        {
            int? maximum = CurrentMaximumSizeMb.HasValue ? CurrentMaximumSizeMb : NextMaximumSizeMb;
            if (!OverlayConsumptionMb.HasValue || !maximum.HasValue || maximum.Value <= 0)
            {
                return 0;
            }
            return (int)Math.Round((double)OverlayConsumptionMb.Value * 100.0 / (double)maximum.Value);
        }

        public string GetOverlayUsagePercentText()
        {
            int percent = GetOverlayUsagePercent();
            if (percent <= 0 && !OverlayConsumptionMb.HasValue)
            {
                return UiText.T("확인 불가", "Unknown");
            }
            return percent.ToString() + "%";
        }

        public string ToReport()
        {
            var report = new StringBuilder();
            if (!String.IsNullOrEmpty(Error))
            {
                report.AppendLine(UiText.T("WMI 오류: ", "WMI error: ") + Error);
                return report.ToString();
            }

            report.AppendLine("UWF_Filter: CurrentEnabled=" + FormatBool(FilterCurrentEnabled) + ", NextEnabled=" + FormatBool(FilterNextEnabled));
            report.AppendLine("UWF_OverlayConfig: CurrentType=" + CurrentOverlayType + ", NextType=" + NextOverlayType +
                ", CurrentMaximumSize=" + FormatMb(CurrentMaximumSizeMb) + ", NextMaximumSize=" + FormatMb(NextMaximumSizeMb));
            report.AppendLine("UWF_Overlay: Consumption=" + FormatMb(OverlayConsumptionMb) + ", AvailableSpace=" + FormatMb(AvailableSpaceMb) +
                ", Warning=" + FormatMb(WarningThresholdMb) + ", Critical=" + FormatMb(CriticalThresholdMb));
            report.AppendLine("UWF_Volume: CurrentProtected=" + CurrentProtectedVolumesText() + ", NextProtected=" + NextProtectedVolumesText());
            report.AppendLine("UWF_ExcludedFile: " + FileExclusionsText());
            report.AppendLine("UWF_ExcludedRegistryKey: " + RegistryExclusionsText());
            report.AppendLine("UWF_Servicing: Current=" + FormatBool(ServicingCurrentEnabled) + ", Next=" + FormatBool(ServicingNextEnabled));
            report.AppendLine("PendingRebootOrNextSessionChanges=" + (HasPendingChanges() ? "Yes" : "No"));
            return report.ToString();
        }

        private static string JoinVolumes(List<string> volumes)
        {
            return JoinItems(volumes);
        }

        private static string JoinItems(List<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return UiText.T("없음", "None");
            }
            return String.Join(", ", values.ToArray());
        }

        private static string FormatBool(bool? value)
        {
            if (!value.HasValue)
            {
                return UiText.T("확인 불가", "Unknown");
            }
            return value.Value ? UiText.T("켜짐", "On") : UiText.T("꺼짐", "Off");
        }

        private static string FormatMb(int? value)
        {
            if (!value.HasValue)
            {
                return UiText.T("확인 불가", "Unknown");
            }
            return value.Value.ToString() + " MB";
        }
    }

    internal sealed class OperationPlan
    {
        public readonly string Title;
        public readonly List<CommandSpec> Commands;
        public string Warning;
        public bool RequiresAdministrator;

        public OperationPlan(string title)
        {
            Title = title;
            Warning = String.Empty;
            Commands = new List<CommandSpec>();
            RequiresAdministrator = true;
        }

        public string ToDisplayText()
        {
            var text = new StringBuilder();
            text.AppendLine(Title);
            text.AppendLine();
            if (!String.IsNullOrEmpty(Warning))
            {
                text.AppendLine(Warning);
                text.AppendLine();
            }
            text.AppendLine(UiText.T("실행 명령:", "Commands:"));
            for (int i = 0; i < Commands.Count; i++)
            {
                var commandText = "  " + Commands[i].FileName + " " + Commands[i].Arguments;
                if (Commands[i].ContinueOnFailure)
                {
                    commandText += UiText.T(" (실패해도 계속)", " (continue on failure)");
                }
                text.AppendLine(commandText);
            }
            text.AppendLine();
            text.AppendLine(UiText.T("계속할까요?", "Continue?"));
            return text.ToString();
        }
    }

    internal sealed class CommandSpec
    {
        public readonly string FileName;
        public readonly string Arguments;
        public readonly int TimeoutMilliseconds;
        public readonly bool ContinueOnFailure;

        public CommandSpec(string fileName, string arguments)
            : this(fileName, arguments, 120000)
        {
        }

        public CommandSpec(string fileName, string arguments, int timeoutMilliseconds)
            : this(fileName, arguments, timeoutMilliseconds, false)
        {
        }

        public CommandSpec(string fileName, string arguments, int timeoutMilliseconds, bool continueOnFailure)
        {
            FileName = fileName;
            Arguments = arguments;
            TimeoutMilliseconds = timeoutMilliseconds;
            ContinueOnFailure = continueOnFailure;
        }
    }

    internal sealed class CommandRunner
    {
        public CommandResult Run(string fileName, string arguments, int timeoutMilliseconds)
        {
            var result = new CommandResult();
            result.FileName = fileName;
            result.Arguments = arguments;
            var output = new StringBuilder();
            var error = new StringBuilder();

            try
            {
                var info = new ProcessStartInfo();
                info.FileName = fileName;
                info.Arguments = arguments;
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;

                using (var process = new Process())
                {
                    process.StartInfo = info;
                    process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
                    {
                        if (e.Data != null)
                        {
                            output.AppendLine(e.Data);
                        }
                    };
                    process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
                    {
                        if (e.Data != null)
                        {
                            error.AppendLine(e.Data);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    if (!process.WaitForExit(timeoutMilliseconds))
                    {
                        result.TimedOut = true;
                        try
                        {
                            process.Kill();
                        }
                        catch
                        {
                        }
                    }
                    else
                    {
                        process.WaitForExit();
                        result.ExitCode = process.ExitCode;
                    }
                }
            }
            catch (Exception ex)
            {
                result.ExitCode = -1;
                error.AppendLine(ex.Message);
            }

            result.Output = output.ToString();
            result.Error = error.ToString();
            return result;
        }
    }

    internal sealed class CommandResult
    {
        private const int HResultAccessDenied = unchecked((int)0x80070005);

        public string FileName;
        public string Arguments;
        public int ExitCode;
        public string Output;
        public string Error;
        public bool TimedOut;

        public string ToDisplayText()
        {
            var text = new StringBuilder();
            text.AppendLine(UiText.T("종료 코드: ", "ExitCode: ") + FormatExitCode(ExitCode) + (TimedOut ? UiText.T(" (시간 초과)", " (timed out)") : String.Empty));
            var hint = DecodeExitCode(ExitCode);
            if (!String.IsNullOrEmpty(hint))
            {
                text.AppendLine(UiText.T("해석: ", "Meaning: ") + hint);
            }
            if (!String.IsNullOrWhiteSpace(Output))
            {
                text.AppendLine("[stdout]");
                text.AppendLine(Output.TrimEnd());
            }
            if (!String.IsNullOrWhiteSpace(Error))
            {
                text.AppendLine("[stderr]");
                text.AppendLine(Error.TrimEnd());
            }
            return text.ToString();
        }

        public static string FormatExitCode(int exitCode)
        {
            if (exitCode < 0)
            {
                return exitCode.ToString() + " (" + ToHResultHex(exitCode) + ")";
            }

            return exitCode.ToString();
        }

        public static string DecodeExitCode(int exitCode)
        {
            if (exitCode == 0)
            {
                return String.Empty;
            }

            if (exitCode == HResultAccessDenied)
            {
                return UiText.T(
                    "권한 거부(E_ACCESSDENIED)입니다. WMI 상태 스냅샷이 정상이라면 대시보드는 WMI 값을 기준으로 보고, uwfmgr.exe CLI 상세 출력만 보조 진단으로 취급하세요.",
                    "Access denied (E_ACCESSDENIED). If the WMI snapshot is populated, treat the dashboard as WMI-backed and the uwfmgr.exe CLI output as supplemental diagnostics.");
            }

            if (exitCode < 0)
            {
                return UiText.T("Windows HRESULT 오류입니다. 위 16진수 코드로 권한, 정책, 실행 비트수 문제를 확인하세요.",
                    "Windows HRESULT failure. Use the hexadecimal code above to check permissions, policy, and process bitness.");
            }

            return String.Empty;
        }

        private static string ToHResultHex(int exitCode)
        {
            return "0x" + ((uint)exitCode).ToString("X8");
        }
    }

    internal sealed class ValidationResult
    {
        public readonly bool Allowed;
        public readonly string Message;

        public ValidationResult(bool allowed, string message)
        {
            Allowed = allowed;
            Message = message;
        }
    }

    internal static class SafetyRules
    {
        public static ValidationResult ValidateFileExclusion(string path, bool adding)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                return Block(UiText.T("파일 또는 폴더의 전체 경로를 입력하세요.", "Enter a fully qualified file or folder path."));
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim()));
            }
            catch (Exception ex)
            {
                return Block(UiText.T("경로 오류: ", "Invalid path: ") + ex.Message);
            }

            if (!Path.IsPathRooted(fullPath) || fullPath.Length < 3 || fullPath[1] != ':')
            {
                return Block(UiText.T("C:\\ProgramData\\Vendor 같은 드라이브 포함 전체 경로를 입력하세요.", "Use a fully qualified drive path such as C:\\ProgramData\\Vendor."));
            }

            var normalized = NormalizePath(fullPath);
            var root = Char.ToUpperInvariant(normalized[0]) + ":\\";
            if (String.Equals(normalized, root, StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized.TrimEnd('\\'), root.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                return Block(UiText.T("C: 또는 C:\\ 같은 볼륨 루트는 예외로 지정할 수 없습니다.", "Volume roots such as C: or C:\\ are unsupported as exclusions."));
            }

            string drive = root;
            string windows = NormalizePath(Path.Combine(drive, "Windows"));
            string system32 = NormalizePath(Path.Combine(windows, "System32"));
            string drivers = NormalizePath(Path.Combine(system32, "Drivers"));

            if (EqualsPath(normalized, windows) || EqualsPath(normalized, system32) || EqualsPath(normalized, drivers))
            {
                return Block(UiText.T("Windows, System32, System32\\Drivers 자체는 예외로 지정하지 마세요. 안전한 하위 폴더나 특정 파일만 지정하세요.",
                    "Do not exclude Windows, System32, or System32\\Drivers itself. Use a specific safe subfolder or file only."));
            }

            string[] blockedExact = new string[]
            {
                Path.Combine(system32, "config\\DEFAULT"),
                Path.Combine(system32, "config\\SAM"),
                Path.Combine(system32, "config\\SECURITY"),
                Path.Combine(system32, "config\\SOFTWARE"),
                Path.Combine(system32, "config\\SYSTEM"),
                Path.Combine(windows, "BOOTSTAT.DAT"),
                Path.Combine(drive, "Boot\\BOOTSTAT.DAT"),
                Path.Combine(drive, "EFI\\Microsoft\\Boot\\BOOTSTAT.DAT"),
                Path.Combine(drive, "pagefile.sys"),
                Path.Combine(drive, "swapfile.sys"),
                Path.Combine(drive, "hiberfil.sys")
            };

            for (int i = 0; i < blockedExact.Length; i++)
            {
                if (EqualsPath(normalized, blockedExact[i]))
                {
                    return Block(UiText.T("이 경로는 UWF 예외로 지원되지 않거나 안전하지 않습니다.", "This path is unsupported or unsafe for UWF exclusions."));
                }
            }

            if (normalized.EndsWith("\\NTUSER.DAT", StringComparison.OrdinalIgnoreCase))
            {
                return Block(UiText.T("NTUSER.DAT 같은 사용자 프로필 레지스트리 하이브는 예외로 지정하면 안 됩니다.",
                    "User profile registry hives such as NTUSER.DAT must not be excluded."));
            }

            if (adding)
            {
                if (!File.Exists(normalized) && !Directory.Exists(normalized))
                {
                    var parent = Path.GetDirectoryName(normalized);
                    if (String.IsNullOrEmpty(parent) || !Directory.Exists(parent))
                    {
                        return Block(UiText.T("경로 또는 상위 폴더가 없습니다. 먼저 폴더를 만든 뒤 예외를 추가하세요.",
                            "The path or its parent folder does not exist. Create the folder first, then add the exclusion."));
                    }
                }
            }

            return Allow(UiText.T("허용 가능한 경로입니다. 예외는 소량의 설정/데이터 보존용이며 오버레이 사용량을 줄이는 기능이 아닙니다.",
                "Allowed path. Remember: exclusions persist small configuration data; they do not reduce overlay consumption."));
        }

        public static ValidationResult ValidateRegistryExclusion(string key)
        {
            var shape = ValidateRegistryKeyShape(key);
            if (!shape.Allowed)
            {
                return shape;
            }

            var normalized = NormalizeRegistryKey(key);
            if (String.Equals(normalized, @"HKEY_LOCAL_MACHINE\SECURITY\Policy\Secrets\$MACHINE.ACC", StringComparison.OrdinalIgnoreCase))
            {
                return Block(UiText.T("머신 계정 비밀 키는 예외로 지정하지 마세요.", "Do not exclude the machine account secret."));
            }

            string[] allowedRoots = new string[]
            {
                @"HKEY_LOCAL_MACHINE\BCD00000000",
                @"HKEY_LOCAL_MACHINE\SYSTEM",
                @"HKEY_LOCAL_MACHINE\SOFTWARE",
                @"HKEY_LOCAL_MACHINE\SAM",
                @"HKEY_LOCAL_MACHINE\SECURITY",
                @"HKEY_LOCAL_MACHINE\COMPONENTS"
            };

            for (int i = 0; i < allowedRoots.Length; i++)
            {
                if (IsSameOrChild(normalized, allowedRoots[i]) && !String.Equals(normalized, allowedRoots[i], StringComparison.OrdinalIgnoreCase))
                {
                    return Allow(UiText.T("허용 가능한 레지스트리 키입니다. 이 키 아래의 모든 하위 키도 UWF 필터링을 우회합니다.",
                        "Allowed registry key. All subkeys below this key will also bypass UWF filtering."));
                }
            }

            return Block(UiText.T("레지스트리 예외는 지원되는 HKEY_LOCAL_MACHINE 루트 아래의 하위 키여야 합니다.",
                "Registry exclusions must be subkeys under supported HKEY_LOCAL_MACHINE roots."));
        }

        public static ValidationResult ValidateRegistryKeyShape(string key)
        {
            if (String.IsNullOrWhiteSpace(key))
            {
                return Block(UiText.T("레지스트리 키 경로를 입력하세요.", "Enter a registry key path."));
            }

            var normalized = NormalizeRegistryKey(key);
            if (!normalized.StartsWith(@"HKEY_LOCAL_MACHINE\", StringComparison.OrdinalIgnoreCase))
            {
                return Block(UiText.T("HKLM / HKEY_LOCAL_MACHINE 레지스트리 키를 사용하세요.", "Use an HKLM / HKEY_LOCAL_MACHINE registry key."));
            }

            return Allow(UiText.T("레지스트리 키 형식이 유효합니다.", "Registry key shape is valid."));
        }

        public static string NormalizeRegistryKey(string key)
        {
            key = key.Trim().TrimEnd('\\');
            if (key.StartsWith(@"HKLM\", StringComparison.OrdinalIgnoreCase))
            {
                key = @"HKEY_LOCAL_MACHINE\" + key.Substring(5);
            }
            return key;
        }

        private static bool IsSameOrChild(string path, string root)
        {
            return String.Equals(path, root, StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path).TrimEnd('\\');
        }

        private static bool EqualsPath(string left, string right)
        {
            return String.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);
        }

        private static ValidationResult Block(string message)
        {
            return new ValidationResult(false, message);
        }

        private static ValidationResult Allow(string message)
        {
            return new ValidationResult(true, message);
        }
    }

    internal static class SystemSizing
    {
        public static long GetTotalPhysicalMemoryMb()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var bytes = Convert.ToInt64(obj["TotalPhysicalMemory"]);
                        return bytes / 1024 / 1024;
                    }
                }
            }
            catch
            {
            }

            return 8192;
        }

        public static long GetFreeSpaceMb(string volume)
        {
            try
            {
                var root = volume;
                if (!root.EndsWith("\\", StringComparison.Ordinal))
                {
                    root += "\\";
                }
                var drive = new DriveInfo(root);
                return drive.AvailableFreeSpace / 1024 / 1024;
            }
            catch
            {
                return 8192;
            }
        }
    }

    internal sealed class WorkloadProfile
    {
        public readonly string Key;
        public readonly int WeightPercent;
        public readonly int DiskFreeSpacePercent;

        private WorkloadProfile(string key, int weightPercent, int diskFreeSpacePercent)
        {
            Key = key;
            WeightPercent = weightPercent;
            DiskFreeSpacePercent = diskFreeSpacePercent;
        }

        public static WorkloadProfile FromIndex(int index)
        {
            if (index <= 0)
            {
                return new WorkloadProfile("light", 8, 25);
            }
            if (index >= 2)
            {
                return new WorkloadProfile("heavy", 18, 60);
            }
            return new WorkloadProfile("normal", 12, 40);
        }

        public string DisplayName()
        {
            if (Key == "light")
            {
                return UiText.T("가벼움", "Light");
            }
            if (Key == "heavy")
            {
                return UiText.T("무거움", "Heavy");
            }
            return UiText.T("보통", "Normal");
        }
    }

    internal static class SizingRules
    {
        public static int RecommendRamOverlayMb(long totalRamMb, WorkloadProfile profile)
        {
            if (profile == null)
            {
                profile = WorkloadProfile.FromIndex(1);
            }

            long byPercent = totalRamMb * profile.WeightPercent / 100;
            long reserveForWindows = totalRamMb / 4;
            long upperBound = totalRamMb - reserveForWindows;
            if (upperBound < 1024)
            {
                upperBound = 1024;
            }

            return ClampAndRound(byPercent, 1024, upperBound, 256);
        }

        public static int RecommendDiskOverlayMb(long freeDiskMb, WorkloadProfile profile)
        {
            if (profile == null)
            {
                profile = WorkloadProfile.FromIndex(1);
            }

            long keepFree = freeDiskMb / 5;
            long upperBound = freeDiskMb - keepFree;
            if (upperBound < 1024)
            {
                upperBound = 1024;
            }

            long byPercent = freeDiskMb * profile.DiskFreeSpacePercent / 100;
            return ClampAndRound(byPercent, 1024, upperBound, 1024);
        }

        private static int ClampAndRound(long valueMb, long minimumMb, long maximumMb, int quantumMb)
        {
            if (valueMb < minimumMb)
            {
                valueMb = minimumMb;
            }
            if (valueMb > maximumMb)
            {
                valueMb = maximumMb;
            }

            long rounded = (valueMb / quantumMb) * quantumMb;
            if (rounded < minimumMb)
            {
                rounded = minimumMb;
            }
            if (rounded > Int32.MaxValue)
            {
                return Int32.MaxValue;
            }
            return (int)rounded;
        }
    }

    internal static class SelfTest
    {
        public static int Run()
        {
            var failures = new List<string>();

            if (UiText.Current != UiLanguage.Korean)
            {
                failures.Add("default language is not Korean");
            }
            if (UiText.T("한글", "English") != "한글")
            {
                failures.Add("Korean localization default failed");
            }
            ExpectEqualText(failures, "plain", Elevation.QuoteArgumentForCreateProcess("plain"), "plain elevation argument");
            ExpectEqualText(failures, "\"has space\"", Elevation.QuoteArgumentForCreateProcess("has space"), "spaced elevation argument");
            ExpectEqualText(failures, "\"quoted\\\"value\"", Elevation.QuoteArgumentForCreateProcess("quoted\"value"), "quoted elevation argument");
            ExpectEqualText(failures, "alpha \"beta gamma\"", Elevation.BuildArgumentString(new[] { "alpha", "beta gamma" }), "elevation argument string");

            ExpectBlocked(failures, SafetyRules.ValidateFileExclusion(@"C:\", false), "volume root");
            ExpectBlocked(failures, SafetyRules.ValidateFileExclusion(@"C:\Windows", false), "windows folder");
            ExpectBlocked(failures, SafetyRules.ValidateFileExclusion(@"C:\Windows\System32", false), "system32 folder");
            ExpectBlocked(failures, SafetyRules.ValidateFileExclusion(@"C:\Users\Test\NTUSER.DAT", false), "ntuser.dat");
            ExpectAllowed(failures, SafetyRules.ValidateFileExclusion(@"C:\ProgramData\Vendor\settings.ini", false), "normal file path");
            ExpectBlocked(failures, SafetyRules.ValidateRegistryExclusion(@"HKCU\Software\Test"), "non-HKLM registry");
            ExpectBlocked(failures, SafetyRules.ValidateRegistryExclusion(@"HKLM\SECURITY\Policy\Secrets\$MACHINE.ACC"), "machine secret");
            ExpectAllowed(failures, SafetyRules.ValidateRegistryExclusion(@"HKLM\SOFTWARE\Vendor\Product"), "normal registry key");
            ExpectEqual(failures, 1024, SizingRules.RecommendRamOverlayMb(8192, WorkloadProfile.FromIndex(0)), "ram recommendation light 8GB");
            ExpectEqual(failures, 1792, SizingRules.RecommendRamOverlayMb(16384, WorkloadProfile.FromIndex(1)), "ram recommendation normal 16GB");
            ExpectEqual(failures, 5888, SizingRules.RecommendRamOverlayMb(32768, WorkloadProfile.FromIndex(2)), "ram recommendation heavy 32GB");
            ExpectEqual(failures, 1024, SizingRules.RecommendDiskOverlayMb(3000, WorkloadProfile.FromIndex(0)), "disk recommendation low space");
            ExpectEqual(failures, 6144, SizingRules.RecommendDiskOverlayMb(16384, WorkloadProfile.FromIndex(1)), "disk recommendation normal");
            ExpectEqual(failures, 119808, SizingRules.RecommendDiskOverlayMb(200000, WorkloadProfile.FromIndex(2)), "disk recommendation dynamic heavy");
            ExpectContains(failures, CommandResult.FormatExitCode(unchecked((int)0x80070005)), "0x80070005", "hresult hex display");
            ExpectContains(failures, new CommandResult { ExitCode = unchecked((int)0x80070005) }.ToDisplayText(), "권한 거부", "access denied hint");
            ExpectDirectory(failures, MainForm.GetInitialDirectory(String.Empty), "empty browse initial directory");
            var commonData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (!String.IsNullOrEmpty(commonData))
            {
                ExpectEqualText(failures, Path.GetFullPath(commonData).TrimEnd('\\'), MainForm.GetInitialDirectory(commonData).TrimEnd('\\'), "existing browse initial directory");
            }
            ExpectVolumeSelection(failures, "C:,D:", false, false, 2, "parse two volumes");
            ExpectVolumeSelection(failures, "c: C:", false, false, 1, "dedupe volumes");
            ExpectVolumeSelection(failures, "all", true, true, 0, "parse all for protect");
            ExpectVolumeParseBlocked(failures, "all", false, "block all for unprotect");
            ExpectEqualText(failures, "C:,D:", VolumeSelectionDialog.BuildSelectionText(false, new List<string> { "C:", "D:" }), "dialog multi-volume selection text");
            ExpectEqualText(failures, "all", VolumeSelectionDialog.BuildSelectionText(true, new List<string> { "C:" }), "dialog all selection text");
            ExpectEqualText(failures, "UWF_Volume.CurrentSession=False,DriveLetter=\"C:\",VolumeName=\"\"",
                UwfController.BuildUwfVolumeObjectPath("C:", false), "next-session volume WMI object path");
            ExpectEqualText(failures, @"\Users\Lemos\.codex",
                UwfController.GetVolumeRelativePath(@"C:\Users\Lemos\.codex"), "WMI file exclusion volume-relative path");
            ExpectEqualText(failures, @"C:\Users\Lemos\.codex",
                UwfController.ToFullPathFromVolume("C:", @"\Users\Lemos\.codex"), "WMI file exclusion full path display");
            try
            {
                using (var dialog = new VolumeSelectionDialog(new List<string> { "C:", "D:" }, "C:,D:"))
                {
                }
            }
            catch (Exception ex)
            {
                failures.Add("volume selection dialog construction failed: " + ex.Message);
            }

            var controller = new UwfController();
            var setup = controller.CreateSetupPlan("RAM", ParseRequired("C:"), 4096, 3276, 3891);
            if (setup.Commands.Count != 6)
            {
                failures.Add("setup plan command count");
            }
            var fileExclusionPlan = controller.CreateFileExclusionPlan(@"C:\Users\Lemos\.codex", true, "test warning");
            ExpectContains(failures, fileExclusionPlan.Title, "폴더/파일", "file exclusion plan uses folder/file wording");
            if (fileExclusionPlan.Commands.Count != 1)
            {
                failures.Add("file exclusion WMI plan command count");
            }
            if (fileExclusionPlan.Commands.Count > 0)
            {
                ExpectEqualText(failures, "UWF WMI", fileExclusionPlan.Commands[0].FileName, "file exclusion plan uses WMI command");
                ExpectContains(failures, fileExclusionPlan.Commands[0].Arguments, "folder add-exclusion", "file exclusion plan uses folder command wording");
            }
            var multiSetup = controller.CreateSetupPlan("RAM", ParseRequired("C:,D:"), 4096, 3276, 3891);
            if (multiSetup.Commands.Count != 7)
            {
                failures.Add("multi-volume setup plan command count");
            }
            if (multiSetup.Commands.Count > 6)
            {
                ExpectEqualText(failures, "volume protect D:", multiSetup.Commands[6].Arguments, "multi-volume setup second protect command");
            }
            var protectAll = controller.CreateVolumeProtectionPlan(ParseRequired("all"), true);
            if (protectAll.Commands.Count != 1)
            {
                failures.Add("protect all command count");
            }
            if (protectAll.Commands.Count > 0)
            {
                ExpectEqualText(failures, "volume protect all", protectAll.Commands[0].Arguments, "protect all command");
            }
            var cleanupBlockedSnapshot = new UwfSnapshot();
            cleanupBlockedSnapshot.FilterCurrentEnabled = true;
            var cleanupBlocked = controller.CreateDiskOverlayCleanupPlan(cleanupBlockedSnapshot);
            ExpectEqual(failures, 1, cleanupBlocked.Commands.Count, "cleanup blocked command count");
            if (cleanupBlocked.Commands.Count > 0)
            {
                ExpectEqualText(failures, "filter disable", cleanupBlocked.Commands[0].Arguments, "cleanup blocked disables filter first");
            }
            var cleanupReadySnapshot = new UwfSnapshot();
            cleanupReadySnapshot.FilterCurrentEnabled = false;
            cleanupReadySnapshot.CurrentProtectedVolumes.Add("C:");
            cleanupReadySnapshot.NextProtectedVolumes.Add("D:");
            var cleanupReady = controller.CreateDiskOverlayCleanupPlan(cleanupReadySnapshot);
            ExpectEqual(failures, 4, cleanupReady.Commands.Count, "cleanup ready command count");
            if (cleanupReady.Commands.Count >= 4)
            {
                ExpectEqualText(failures, "overlay set-type RAM", cleanupReady.Commands[0].Arguments, "cleanup set RAM");
                ExpectEqualText(failures, "overlay set-size 1024", cleanupReady.Commands[1].Arguments, "cleanup minimum size");
                ExpectEqualText(failures, "overlay set-warningthreshold 512", cleanupReady.Commands[2].Arguments, "cleanup warning threshold");
                ExpectEqualText(failures, "overlay set-criticalthreshold 1024", cleanupReady.Commands[3].Arguments, "cleanup critical threshold");
            }
            var fullOff = controller.CreateFullDisablePlan(cleanupReadySnapshot);
            ExpectContains(failures, PlanArgumentsText(fullOff), "volume unprotect C:", "full off unprotects current volume");
            ExpectContains(failures, PlanArgumentsText(fullOff), "volume unprotect D:", "full off unprotects next volume");
            ExpectContains(failures, PlanArgumentsText(fullOff), "overlay set-type RAM", "full off releases disk overlay");
            var fullReset = controller.CreateFullResetPlan(cleanupReadySnapshot);
            if (fullReset.Commands.Count == 0 || !fullReset.Commands[0].ContinueOnFailure)
            {
                failures.Add("full reset reset-settings should continue on failure");
            }
            ExpectContains(failures, PlanArgumentsText(fullReset), "filter reset-settings", "full reset requests reset-settings");
            ExpectContains(failures, PlanArgumentsText(fullReset), "filter disable", "full reset disables filter");

            var snapshot = new UwfSnapshot();
            snapshot.FilterCurrentEnabled = true;
            snapshot.FilterNextEnabled = false;
            if (!snapshot.HasPendingChanges())
            {
                failures.Add("pending change detection");
            }
            if (!snapshot.HasObservedState())
            {
                failures.Add("observed WMI state detection");
            }
            var exclusionSnapshot = new UwfSnapshot();
            exclusionSnapshot.FileExclusions.Add(@"C:\ProgramData\Vendor\settings.ini");
            exclusionSnapshot.RegistryExclusions.Add(@"HKLM\SOFTWARE\Vendor\Product");
            ExpectContains(failures, exclusionSnapshot.ToReport(), @"C:\ProgramData\Vendor\settings.ini", "snapshot reports file exclusions");
            ExpectContains(failures, exclusionSnapshot.ToReport(), @"HKLM\SOFTWARE\Vendor\Product", "snapshot reports registry exclusions");

            if (failures.Count > 0)
            {
                for (int i = 0; i < failures.Count; i++)
                {
                    Console.Error.WriteLine(failures[i]);
                }
                return 2;
            }

            Console.WriteLine("Self-test passed.");
            return 0;
        }

        private static string PlanArgumentsText(OperationPlan plan)
        {
            var text = new StringBuilder();
            if (plan == null)
            {
                return String.Empty;
            }

            for (int i = 0; i < plan.Commands.Count; i++)
            {
                text.AppendLine(plan.Commands[i].Arguments);
            }
            return text.ToString();
        }

        private static void ExpectBlocked(List<string> failures, ValidationResult result, string name)
        {
            if (result.Allowed)
            {
                failures.Add("Expected blocked: " + name);
            }
        }

        private static void ExpectAllowed(List<string> failures, ValidationResult result, string name)
        {
            if (!result.Allowed)
            {
                failures.Add("Expected allowed: " + name + " - " + result.Message);
            }
        }

        private static void ExpectEqual(List<string> failures, int expected, int actual, string name)
        {
            if (expected != actual)
            {
                failures.Add("Expected " + expected.ToString() + " but got " + actual.ToString() + ": " + name);
            }
        }

        private static void ExpectContains(List<string> failures, string value, string expected, string name)
        {
            if (value == null || value.IndexOf(expected, StringComparison.Ordinal) < 0)
            {
                failures.Add("Expected text containing '" + expected + "': " + name);
            }
        }

        private static void ExpectDirectory(List<string> failures, string path, string name)
        {
            if (String.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                failures.Add("Expected existing directory: " + name);
            }
        }

        private static void ExpectEqualText(List<string> failures, string expected, string actual, string name)
        {
            if (!String.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("Expected '" + expected + "' but got '" + actual + "': " + name);
            }
        }

        private static VolumeSelection ParseRequired(string text)
        {
            VolumeSelection selection;
            string error;
            if (!VolumeSelectionParser.TryParse(text, true, out selection, out error))
            {
                throw new InvalidOperationException(error);
            }
            return selection;
        }

        private static void ExpectVolumeSelection(List<string> failures, string text, bool allowAll, bool expectedAll, int expectedCount, string name)
        {
            VolumeSelection selection;
            string error;
            if (!VolumeSelectionParser.TryParse(text, allowAll, out selection, out error))
            {
                failures.Add("Expected volume parse success: " + name + " - " + error);
                return;
            }
            if (selection.IsAll != expectedAll)
            {
                failures.Add("Unexpected all flag: " + name);
            }
            if (selection.Volumes.Count != expectedCount)
            {
                failures.Add("Unexpected volume count: " + name);
            }
        }

        private static void ExpectVolumeParseBlocked(List<string> failures, string text, bool allowAll, string name)
        {
            VolumeSelection selection;
            string error;
            if (VolumeSelectionParser.TryParse(text, allowAll, out selection, out error))
            {
                failures.Add("Expected volume parse blocked: " + name);
            }
        }
    }
}
