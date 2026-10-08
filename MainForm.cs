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
    internal sealed class MainForm : Form
    {
        private readonly UwfController controller;
        private readonly OperationPlan pendingPlan;
        private readonly ToolTip helpTip;
        private TabControl mainTabs;
        private TableLayoutPanel shellRoot;
        private FlowLayoutPanel navigationPanel;
        private Label pageTitleLabel;
        private Label pageDescriptionLabel;
        private Label busyLabel;
        private ProgressBar busyProgress;
        private Button runAsAdminButton;
        private readonly List<Button> navigationButtons;
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
        private bool isBusy;
        private bool operationInProgress;
        private bool setupControlsInitialized;
        private int activePageIndex;
        private long totalPhysicalMemoryMb;
        private long systemVolumeFreeSpaceMb;
        private UwfStatus lastStatus;

        public MainForm()
            : this(null)
        {
        }

        public MainForm(OperationPlan pendingPlan)
        {
            controller = new UwfController();
            this.pendingPlan = pendingPlan;
            navigationButtons = new List<Button>();
            dashboardLabels = new Dictionary<string, Label>();
            overlayProgress = new ProgressBar();
            helpTip = new ToolTip();
            helpTip.AutoPopDelay = 12000;
            helpTip.InitialDelay = 500;
            helpTip.ReshowDelay = 100;

            Text = UiText.T("포터블 UWF 관리자", "Portable UWF Manager");
            Width = 1300;
            Height = 860;
            MinimumSize = new Size(1040, 680);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = UiTheme.BodyFont;
            BackColor = UiTheme.Canvas;

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
            Shown += delegate
            {
                if (this.pendingPlan != null)
                {
                    RunPlan(this.pendingPlan);
                }
                else
                {
                    RefreshStatus();
                }
            };
        }

        private void BuildUi()
        {
            Text = UiText.T("포터블 UWF 관리자", "Portable UWF Manager");

            var previousRoot = shellRoot;
            if (previousRoot != null)
            {
                previousRoot.Visible = false;
            }

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Margin = Padding.Empty;
            root.Padding = Padding.Empty;
            root.ColumnCount = 2;
            root.RowCount = 1;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 228F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            Controls.Add(root);
            shellRoot = root;

            var sidebar = new PastelGradientPanel(UiTheme.Sidebar, Color.FromArgb(248, 242, 251), UiTheme.SidebarEnd, 0);
            sidebar.Dock = DockStyle.Fill;
            root.Controls.Add(sidebar, 0, 0);

            var sidebarLayout = new TableLayoutPanel();
            sidebarLayout.Dock = DockStyle.Fill;
            sidebarLayout.Padding = new Padding(16, 24, 16, 18);
            sidebarLayout.BackColor = Color.Transparent;
            sidebarLayout.ColumnCount = 1;
            sidebarLayout.RowCount = 3;
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            sidebar.Controls.Add(sidebarLayout);

            var brand = new TableLayoutPanel();
            brand.Dock = DockStyle.Fill;
            brand.ColumnCount = 2;
            brand.RowCount = 1;
            brand.BackColor = Color.Transparent;
            brand.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48F));
            brand.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            sidebarLayout.Controls.Add(brand, 0, 0);

            var brandMark = new Label();
            brandMark.Text = "U";
            brandMark.Dock = DockStyle.Fill;
            brandMark.TextAlign = ContentAlignment.MiddleCenter;
            brandMark.Font = new Font("Georgia", 19F, FontStyle.Bold);
            brandMark.ForeColor = UiTheme.Primary;
            brandMark.BackColor = UiTheme.Lavender;
            brandMark.Margin = new Padding(0, 4, 10, 14);
            brandMark.Tag = "badge";
            brand.Controls.Add(brandMark, 0, 0);

            var brandText = new Label();
            brandText.Text = UiText.T("UWF 관리자", "UWF Manager");
            brandText.Dock = DockStyle.Fill;
            brandText.TextAlign = ContentAlignment.MiddleLeft;
            brandText.Font = UiTheme.BrandFont;
            brandText.ForeColor = UiTheme.Text;
            brand.Controls.Add(brandText, 1, 0);

            navigationPanel = new FlowLayoutPanel();
            navigationPanel.Dock = DockStyle.Fill;
            navigationPanel.FlowDirection = FlowDirection.TopDown;
            navigationPanel.WrapContents = false;
            navigationPanel.AutoScroll = true;
            navigationPanel.Margin = Padding.Empty;
            navigationPanel.Padding = new Padding(0, 14, 0, 0);
            navigationPanel.BackColor = Color.Transparent;
            sidebarLayout.Controls.Add(navigationPanel, 0, 1);

            navigationButtons.Clear();
            AddNavigationButton(UiText.T("홈", "Home"), 0);
            AddNavigationButton(UiText.T("상태", "Status"), 1);
            AddNavigationButton(UiText.T("설정", "Setup"), 2);
            AddNavigationButton(UiText.T("예외", "Exclusions"), 3);
            AddNavigationButton(UiText.T("고급", "Advanced"), 4);
            AddNavigationButton(UiText.T("활동 기록", "Activity"), 5);

            var versionLabel = new Label();
            versionLabel.Text = UiText.T("Windows UWF 도구", "Windows UWF tools");
            versionLabel.Dock = DockStyle.Fill;
            versionLabel.TextAlign = ContentAlignment.MiddleLeft;
            versionLabel.ForeColor = UiTheme.Muted;
            versionLabel.Padding = new Padding(10, 0, 0, 0);
            versionLabel.Font = UiTheme.SmallBoldFont;
            sidebarLayout.Controls.Add(versionLabel, 0, 2);

            var content = new TableLayoutPanel();
            content.Dock = DockStyle.Fill;
            content.Padding = new Padding(26, 22, 26, 14);
            content.BackColor = UiTheme.Canvas;
            content.ColumnCount = 1;
            content.RowCount = 4;
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            root.Controls.Add(content, 1, 0);

            var headerFrame = new PastelGradientPanel(UiTheme.Lavender, UiTheme.Blush, UiTheme.Cream, 18);
            headerFrame.Dock = DockStyle.Fill;
            headerFrame.Margin = new Padding(0, 0, 0, 14);
            headerFrame.Padding = new Padding(22, 13, 18, 9);
            content.Controls.Add(headerFrame, 0, 0);

            var header = new TableLayoutPanel();
            header.Dock = DockStyle.Fill;
            header.BackColor = Color.Transparent;
            header.ColumnCount = 2;
            header.RowCount = 2;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 284F));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 43F));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            headerFrame.Controls.Add(header);

            pageTitleLabel = new Label();
            pageTitleLabel.Dock = DockStyle.Fill;
            pageTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
            pageTitleLabel.Font = UiTheme.TitleFont;
            pageTitleLabel.ForeColor = UiTheme.Text;
            header.Controls.Add(pageTitleLabel, 0, 0);

            pageDescriptionLabel = new Label();
            pageDescriptionLabel.Dock = DockStyle.Fill;
            pageDescriptionLabel.TextAlign = ContentAlignment.TopLeft;
            pageDescriptionLabel.ForeColor = UiTheme.Muted;
            header.Controls.Add(pageDescriptionLabel, 0, 1);

            var headerActions = new FlowLayoutPanel();
            headerActions.Dock = DockStyle.Fill;
            headerActions.FlowDirection = FlowDirection.RightToLeft;
            headerActions.WrapContents = false;
            headerActions.Padding = new Padding(0, 8, 0, 0);
            headerActions.BackColor = Color.Transparent;
            runAsAdminButton = CreateButton(UiText.T("관리자 실행", "Run as admin"), RelaunchAsAdmin);
            headerActions.Controls.Add(runAsAdminButton);
            ConfigureLanguageBox();
            languageBox.Width = 108;
            headerActions.Controls.Add(languageBox);
            header.Controls.Add(headerActions, 1, 0);
            header.SetRowSpan(headerActions, 2);

            var statusStrip = new FlowLayoutPanel();
            statusStrip.Dock = DockStyle.Fill;
            statusStrip.FlowDirection = FlowDirection.LeftToRight;
            statusStrip.WrapContents = false;
            statusStrip.Padding = new Padding(2, 4, 0, 0);
            statusStrip.BackColor = UiTheme.Canvas;
            ConfigureBadge(adminLabel);
            ConfigureBadge(uwfLabel);
            ConfigureBadge(osLabel);
            adminLabel.Width = 112;
            uwfLabel.Width = 138;
            osLabel.Width = 230;
            statusStrip.Controls.Add(adminLabel);
            statusStrip.Controls.Add(uwfLabel);
            statusStrip.Controls.Add(osLabel);
            content.Controls.Add(statusStrip, 0, 2);

            mainTabs = new TablessTabControl();
            mainTabs.Dock = DockStyle.Fill;
            mainTabs.Margin = Padding.Empty;
            mainTabs.Padding = new Point(0, 0);
            mainTabs.TabPages.Add(BuildQuickStartTab());
            mainTabs.TabPages.Add(BuildDashboardTab());
            mainTabs.TabPages.Add(BuildSetupTab());
            mainTabs.TabPages.Add(BuildExclusionsTab());
            mainTabs.TabPages.Add(BuildAdvancedTab());
            mainTabs.TabPages.Add(BuildActivityTab());
            content.Controls.Add(mainTabs, 0, 1);

            var busyStrip = new TableLayoutPanel();
            busyStrip.Dock = DockStyle.Fill;
            busyStrip.BackColor = UiTheme.Canvas;
            busyStrip.ColumnCount = 2;
            busyStrip.RowCount = 1;
            busyStrip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            busyStrip.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 168F));
            busyLabel = new Label();
            busyLabel.Dock = DockStyle.Fill;
            busyLabel.TextAlign = ContentAlignment.MiddleLeft;
            busyLabel.ForeColor = UiTheme.Muted;
            busyStrip.Controls.Add(busyLabel, 0, 0);
            busyProgress = new ProgressBar();
            busyProgress.Dock = DockStyle.Fill;
            busyProgress.Visible = false;
            busyProgress.Style = ProgressBarStyle.Marquee;
            busyProgress.MarqueeAnimationSpeed = 28;
            busyStrip.Controls.Add(busyProgress, 1, 0);
            content.Controls.Add(busyStrip, 0, 3);

            UiTheme.Apply(root);
            ShowPage(activePageIndex);
            if (previousRoot != null)
            {
                Controls.Remove(previousRoot);
                previousRoot.Dispose();
            }
        }

        private void AddNavigationButton(string text, int index)
        {
            var button = new Button();
            button.Text = text;
            button.Tag = "navigation";
            button.Width = 192;
            button.Height = 46;
            button.Margin = new Padding(0, 2, 0, 5);
            button.Padding = new Padding(14, 0, 8, 0);
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = UiTheme.Hover;
            button.Click += delegate { ShowPage(index); };
            navigationButtons.Add(button);
            navigationPanel.Controls.Add(button);
        }

        private void ShowPage(int index)
        {
            if (mainTabs == null || index < 0 || index >= mainTabs.TabPages.Count)
            {
                return;
            }

            activePageIndex = index;
            mainTabs.SelectedIndex = index;
            string[] titles = new string[]
            {
                UiText.T("처음 시작하기", "Get started"),
                UiText.T("UWF 상태", "UWF status"),
                UiText.T("보호 설정", "Protection setup"),
                UiText.T("예외 관리", "Exclusion management"),
                UiText.T("고급 작업", "Advanced actions"),
                UiText.T("활동 기록", "Activity log")
            };
            string[] descriptions = new string[]
            {
                UiText.T("기본 상태를 확인하고 권장 설정을 선택합니다.", "Review the system and choose a recommended setup."),
                UiText.T("현재 설정과 재부팅 후 적용될 설정을 확인합니다.", "Review current settings and changes staged for the next restart."),
                UiText.T("오버레이 유형, 크기, 보호 볼륨을 설정합니다.", "Configure the overlay, its size, and protected volumes."),
                UiText.T("재부팅 후에도 보존할 파일과 레지스트리 경로를 관리합니다.", "Manage file and registry paths that should persist after restart."),
                UiText.T("영구 커밋, 서비스 모드, 복구 작업을 실행합니다.", "Commit changes, service Windows, or recover UWF settings."),
                UiText.T("명령 결과와 최근 작업을 확인하고 복사합니다.", "Review and copy recent operation results.")
            };
            pageTitleLabel.Text = titles[index];
            pageDescriptionLabel.Text = descriptions[index];

            for (int i = 0; i < navigationButtons.Count; i++)
            {
                bool selected = i == index;
                navigationButtons[i].BackColor = selected ? UiTheme.Selected : UiTheme.Sidebar;
                navigationButtons[i].ForeColor = selected ? UiTheme.Primary : UiTheme.Text;
                navigationButtons[i].Font = selected ? UiTheme.BodyBoldFont : UiTheme.BodyFont;
            }
        }

        private void SetBusy(bool busy, string message)
        {
            isBusy = busy;
            if (busyLabel != null)
            {
                busyLabel.Text = message ?? String.Empty;
            }
            if (busyProgress != null)
            {
                busyProgress.Visible = busy;
            }
            if (mainTabs != null)
            {
                mainTabs.Enabled = !busy;
            }
            if (navigationPanel != null)
            {
                navigationPanel.Enabled = !busy;
            }
            if (languageBox != null)
            {
                languageBox.Enabled = !busy;
            }
            if (runAsAdminButton != null)
            {
                runAsAdminButton.Enabled = !busy;
            }
            UseWaitCursor = busy;
        }

        private static void ConfigureBadge(Label label)
        {
            label.Dock = DockStyle.None;
            label.Height = 28;
            label.Margin = new Padding(0, 0, 9, 0);
            label.Padding = new Padding(10, 0, 10, 0);
            label.Tag = "badge";
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.BorderStyle = BorderStyle.None;
            label.AutoEllipsis = true;
            label.Font = UiTheme.SmallBoldFont;
            label.BackColor = UiTheme.Badge;
            label.ForeColor = UiTheme.Text;
        }

        private void ConfigureLanguageBox()
        {
            languageBox.DropDownStyle = ComboBoxStyle.DropDownList;
            languageBox.Items.Clear();
            languageBox.Items.Add("한국어");
            languageBox.Items.Add("English");
            languageBox.SelectedIndex = UiText.Current == UiLanguage.Korean ? 0 : 1;
            languageBox.Dock = DockStyle.None;
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
            BuildUi();
            RefreshStatus();
        }

        private static Control CreateCard(Control content, Padding padding)
        {
            var card = new SoftCardPanel();
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(4, 3, 8, 8);
            card.Padding = padding;
            content.Dock = DockStyle.Fill;
            card.Controls.Add(content);
            return card;
        }

        private TabPage BuildQuickStartTab()
        {
            var page = new TabPage(UiText.T("빠른 시작", "Quick start"));
            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(4, 4, 4, 4);
            root.ColumnCount = 2;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 166F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            page.Controls.Add(root);

            var guide = CreateGuideBox(
                UiText.T(
                    "처음 쓰는 순서\r\n\r\n1. 상태 확인: UWF 기능과 관리자 권한 상태를 먼저 봅니다.\r\n2. UWF 기능이 없으면 설치 후 재부팅합니다.\r\n3. 잘 모르겠으면 RAM 추천값을 먼저 사용합니다. RAM은 가볍고 관리가 단순합니다.\r\n4. 게임/패치처럼 쓰기량이 크면 DISK 추천값을 검토합니다.\r\n5. 적용 전에는 항상 작업 계획을 확인하고, 적용 후 재부팅하세요.",
                    "First-use flow\r\n\r\n1. Check status first: verify UWF feature and administrator state.\r\n2. If UWF is missing, install the feature and reboot.\r\n3. If unsure, start with the RAM recommendation. RAM mode is lightweight and simpler.\r\n4. For heavy writes such as game patches, review the DISK recommendation.\r\n5. Always review the operation plan before applying, then reboot."));
            root.Controls.Add(CreateCard(guide, new Padding(22, 17, 18, 13)), 0, 0);

            var buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.TopDown;
            buttons.WrapContents = false;
            buttons.Controls.Add(CreatePrimaryButton(UiText.T("1. 상태 확인", "1. Check status"), RefreshStatus));
            buttons.Controls.Add(CreateButton(UiText.T("2. 관리자 실행", "2. Run as admin"), RelaunchAsAdmin));
            buttons.Controls.Add(CreateButton(UiText.T("3. UWF 기능 설치", "3. Install UWF"), InstallFeature));
            root.Controls.Add(buttons, 1, 0);

            var beginner = CreateGuideBox(
                UiText.T(
                    "추천 기준\r\n\r\nRAM 모드: 재부팅하면 변경이 사라지는 보호 환경에 적합합니다. 저장해야 하는 설정은 예외나 커밋으로 따로 관리하세요.\r\n\r\nDISK 모드: 쓰기량이 큰 환경에 맞지만 C: 여유 공간을 사용합니다. 여유 공간이 부족하면 큰 값을 피하세요.\r\n\r\n예외: overlay를 줄이는 기능이 아닙니다. 반드시 보존해야 하는 작은 설정/데이터에만 쓰세요.",
                    "Recommendation rules\r\n\r\nRAM mode: best for a protected environment where changes disappear after reboot. Persist needed settings through exclusions or commits.\r\n\r\nDISK mode: better for heavy writes, but it uses free space on C:. Avoid large values when free space is low.\r\n\r\nExclusions: they do not reduce overlay usage. Use them only for small settings/data that must persist."));
            root.Controls.Add(CreateCard(beginner, new Padding(22, 16, 18, 13)), 0, 1);

            var recommendButtons = new FlowLayoutPanel();
            recommendButtons.Dock = DockStyle.Fill;
            recommendButtons.FlowDirection = FlowDirection.TopDown;
            recommendButtons.WrapContents = false;
            recommendButtons.Controls.Add(CreateButton(UiText.T("RAM 추천값 넣기", "Use RAM recommendation"), UseRecommendedRam));
            recommendButtons.Controls.Add(CreateButton(UiText.T("DISK 추천값 넣기", "Use DISK recommendation"), UseRecommendedDisk));
            recommendButtons.Controls.Add(CreatePrimaryButton(UiText.T("설정 탭으로 이동", "Go to setup"), GoToSetup));
            root.Controls.Add(recommendButtons, 1, 1);

            var bottom = new Label();
            bottom.Dock = DockStyle.Fill;
            bottom.TextAlign = ContentAlignment.MiddleLeft;
            bottom.ForeColor = UiTheme.Muted;
            bottom.Padding = new Padding(14, 0, 0, 0);
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
            root.Padding = new Padding(8, 2, 8, 4);
            root.ColumnCount = 2;
            root.RowCount = 1;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
            page.Controls.Add(root);

            var left = new TableLayoutPanel();
            left.Dock = DockStyle.Fill;
            left.ColumnCount = 1;
            left.RowCount = 2;
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 250F));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.Controls.Add(left, 0, 0);

            var summaryCard = new SoftCardPanel();
            summaryCard.Dock = DockStyle.Fill;
            summaryCard.BackColor = UiTheme.Surface;
            summaryCard.Padding = new Padding(16);
            summaryCard.Margin = new Padding(0, 0, 12, 10);
            summaryCard.Controls.Add(BuildDashboardSummary());
            left.Controls.Add(summaryCard, 0, 0);

            var reportCard = new SoftCardPanel();
            reportCard.Dock = DockStyle.Fill;
            reportCard.BackColor = UiTheme.Surface;
            reportCard.Padding = new Padding(14);
            reportCard.Margin = new Padding(0, 0, 12, 0);
            statusBox.Dock = DockStyle.Fill;
            reportCard.Controls.Add(statusBox);
            left.Controls.Add(reportCard, 0, 1);

            var buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.TopDown;
            buttons.WrapContents = false;
            buttons.Padding = new Padding(8, 0, 0, 0);
            buttons.Controls.Add(CreatePrimaryButton(UiText.T("새로고침", "Refresh"), RefreshStatus));
            buttons.Controls.Add(CreateButton(UiText.T("보고서 복사", "Copy report"), CopyStatus));
            buttons.Controls.Add(CreateButton(UiText.T("보고서 내보내기", "Export report"), ExportStatus));
            buttons.Controls.Add(CreateButton(UiText.T("UWF 기능 설치", "Install UWF feature"), InstallFeature));
            root.Controls.Add(buttons, 1, 0);
            return page;
        }

        private Control BuildDashboardSummary()
        {
            dashboardLabels.Clear();
            var grid = new TableLayoutPanel();
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = 2;
            grid.RowCount = 2;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));

            grid.Controls.Add(BuildDashboardSession(
                UiText.T("현재 세션", "Current session"),
                new[]
                {
                    UiText.T("필터", "Filter"), "FilterCurrent",
                    UiText.T("오버레이", "Overlay"), "OverlayCurrent",
                    UiText.T("최대 크기", "Max size"), "OverlayMax",
                    UiText.T("남은 공간", "Available"), "OverlayAvailable",
                    UiText.T("보호 볼륨", "Volumes"), "VolumesCurrent",
                    UiText.T("서비스", "Servicing"), "ServicingCurrent",
                    UiText.T("재부팅 필요", "Reboot?"), "RebootNeeded"
                }), 0, 0);
            grid.Controls.Add(BuildDashboardSession(
                UiText.T("다음 세션", "Next session"),
                new[]
                {
                    UiText.T("필터", "Filter"), "FilterNext",
                    UiText.T("오버레이", "Overlay"), "OverlayNext",
                    UiText.T("최대 크기", "Max size"), "OverlayMaxNext",
                    UiText.T("사용량", "Usage"), "OverlayUsage",
                    UiText.T("경고/위험", "Warn/Crit"), "OverlayThresholds",
                    UiText.T("보호 볼륨", "Volumes"), "VolumesNext",
                    UiText.T("서비스", "Servicing"), "ServicingNext",
                    "RAM/DISK", "Recommendation"
                }), 1, 0);

            overlayProgress.Dock = DockStyle.Bottom;
            overlayProgress.Height = 10;
            grid.Controls.Add(overlayProgress, 0, 1);
            grid.SetColumnSpan(overlayProgress, 2);
            return grid;
        }

        private Control BuildDashboardSession(string title, string[] labelsAndKeys)
        {
            var session = new TableLayoutPanel();
            session.Dock = DockStyle.Fill;
            session.Margin = new Padding(3);
            session.ColumnCount = 2;
            session.RowCount = 9;
            session.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            session.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            session.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            for (int i = 0; i < 8; i++)
            {
                session.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            }

            var heading = new Label();
            heading.Text = title;
            heading.Dock = DockStyle.Fill;
            heading.Padding = new Padding(8, 0, 8, 0);
            heading.BackColor = UiTheme.Badge;
            heading.ForeColor = UiTheme.Text;
            heading.Font = UiTheme.SmallBoldFont;
            heading.TextAlign = ContentAlignment.MiddleLeft;
            session.Controls.Add(heading, 0, 0);
            session.SetColumnSpan(heading, 2);

            for (int i = 0; i + 1 < labelsAndKeys.Length; i += 2)
            {
                int row = (i / 2) + 1;
                AddDashboardCell(session, row, 0, labelsAndKeys[i], true);
                AddDashboardCell(session, row, 1, labelsAndKeys[i + 1], false);
            }

            return session;
        }

        private void AddDashboardCell(TableLayoutPanel grid, int row, int column, string textOrKey, bool header)
        {
            var label = new Label();
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.AutoEllipsis = true;
            label.BorderStyle = BorderStyle.None;
            label.Padding = new Padding(8, 0, 8, 0);
            if (header)
            {
                label.Text = textOrKey;
                helpTip.SetToolTip(label, textOrKey);
                label.BackColor = UiTheme.Surface;
                label.ForeColor = UiTheme.Muted;
                label.Font = UiTheme.SmallBoldFont;
            }
            else
            {
                label.Text = "-";
                label.BackColor = UiTheme.Surface;
                label.ForeColor = UiTheme.Text;
                label.Font = UiTheme.BodyBoldFont;
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
            root.AutoScroll = true;
            root.ColumnCount = 2;
            root.RowCount = 8;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 186F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int row = 0; row < 6; row++)
            {
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            }
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
            root.BackColor = UiTheme.Surface;
            page.Controls.Add(CreateCard(root, new Padding(3, 3, 3, 3)));

            overlayTypeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            var previousOverlayType = Convert.ToString(overlayTypeBox.SelectedItem);
            overlayTypeBox.Items.Clear();
            overlayTypeBox.Items.Add("RAM");
            overlayTypeBox.Items.Add("DISK");
            overlayTypeBox.SelectedItem = String.IsNullOrEmpty(previousOverlayType) ? "RAM" : previousOverlayType;
            if (overlayTypeBox.SelectedIndex < 0)
            {
                overlayTypeBox.SelectedIndex = 0;
            }

            volumeBox.DropDownStyle = ComboBoxStyle.DropDown;
            PopulateVolumeChoices();

            overlaySizeBox.Minimum = 1024;
            overlaySizeBox.Maximum = 1048576;
            overlaySizeBox.Increment = 1024;
            overlaySizeBox.ThousandsSeparator = true;
            if (!setupControlsInitialized)
            {
                overlaySizeBox.Value = 4096;
            }

            warningPercentBox.Minimum = 1;
            warningPercentBox.Maximum = 98;
            if (!setupControlsInitialized)
            {
                warningPercentBox.Value = 80;
            }
            criticalPercentBox.Minimum = 2;
            criticalPercentBox.Maximum = 99;
            if (!setupControlsInitialized)
            {
                criticalPercentBox.Value = 95;
            }

            workloadBox.DropDownStyle = ComboBoxStyle.DropDownList;
            int selectedProfileIndex = workloadBox.SelectedIndex;
            workloadBox.Items.Clear();
            workloadBox.Items.Add(UiText.T("가벼움 - 설정/작은 앱", "Light - settings/small apps"));
            workloadBox.Items.Add(UiText.T("보통 - 일반 사용", "Normal - everyday use"));
            workloadBox.Items.Add(UiText.T("무거움 - 게임/패치", "Heavy - games/patches"));
            workloadBox.SelectedIndex = selectedProfileIndex >= 0 && selectedProfileIndex < workloadBox.Items.Count ? selectedProfileIndex : 1;

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
            buttons.WrapContents = true;
            buttons.AutoScroll = true;
            buttons.Controls.Add(CreateButton(UiText.T("RAM 추천값", "RAM recommendation"), UseRecommendedRam));
            buttons.Controls.Add(CreateButton(UiText.T("DISK 추천값", "DISK recommendation"), UseRecommendedDisk));
            buttons.Controls.Add(CreatePrimaryButton(UiText.T("설정 계획 적용", "Apply setup plan"), ApplySetup));
            buttons.Controls.Add(CreateButton(UiText.T("필터 켜기", "Enable filter"), EnableFilter));
            buttons.Controls.Add(CreateButton(UiText.T("필터 끄기", "Disable filter"), DisableFilter));
            buttons.Controls.Add(CreateButton(UiText.T("DISK 공간 정리", "Clean DISK space"), CleanupDiskOverlaySpace));
            buttons.Controls.Add(CreateButton(UiText.T("완전 끄기", "Full off"), FullDisableUwf));
            buttons.Controls.Add(CreateButton(UiText.T("볼륨 보호", "Protect volume"), ProtectVolume));
            buttons.Controls.Add(CreateButton(UiText.T("볼륨 보호 해제", "Unprotect volume"), UnprotectVolume));
            root.Controls.Add(buttons, 0, 7);
            root.SetColumnSpan(buttons, 2);
            setupControlsInitialized = true;
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
            var previousSelection = volumeBox.Text;
            volumeBox.Items.Clear();
            AddVolumeChoice("C:");
            try
            {
                var drives = DriveInfo.GetDrives();
                for (int i = 0; i < drives.Length; i++)
                {
                    if (drives[i].DriveType == DriveType.Fixed)
                    {
                        AddVolumeChoice(drives[i].Name.TrimEnd('\\'));
                    }
                }
            }
            catch
            {
            }
            AddVolumeChoice("all");
            volumeBox.Text = String.IsNullOrWhiteSpace(previousSelection) ? "C:" : previousSelection;
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
                    if (drives[i].DriveType == DriveType.Fixed)
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
            root.BackColor = UiTheme.Surface;
            page.Controls.Add(CreateCard(root, new Padding(3, 3, 3, 3)));

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
            root.AutoScroll = true;
            root.ColumnCount = 1;
            root.RowCount = 10;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 126F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            root.BackColor = UiTheme.Surface;
            page.Controls.Add(CreateCard(root, new Padding(3, 3, 3, 3)));

            root.Controls.Add(CreateSectionLabel(UiText.T("오버레이의 파일 변경을 보호 볼륨에 커밋", "Commit a file from overlay to the protected volume")), 0, 0);
            commitFileBox.Dock = DockStyle.Fill;
            commitFileBox.PlaceholderTextSafe(UiText.T("예: C:\\Path\\file.ini", "Example: C:\\Path\\file.ini"));
            root.Controls.Add(commitFileBox, 0, 1);
            var fileCommitButtons = new FlowLayoutPanel();
            fileCommitButtons.Dock = DockStyle.Fill;
            fileCommitButtons.WrapContents = false;
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
            regCommitButtons.Dock = DockStyle.Fill;
            regCommitButtons.WrapContents = false;
            regCommitButtons.Controls.Add(CreateButton(UiText.T("레지스트리 커밋", "Commit registry"), CommitRegistry));
            regCommitButtons.Controls.Add(CreateButton(UiText.T("레지스트리 삭제 커밋", "Commit registry deletion"), CommitRegistryDeletion));
            root.Controls.Add(regCommitButtons, 0, 6);

            root.Controls.Add(CreateSectionLabel(UiText.T("서비스 모드 및 복구", "Servicing and recovery")), 0, 7);
            var recoveryButtons = new FlowLayoutPanel();
            recoveryButtons.Dock = DockStyle.Fill;
            recoveryButtons.WrapContents = true;
            recoveryButtons.AutoScroll = true;
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

        private TabPage BuildActivityTab()
        {
            var page = new TabPage(UiText.T("활동 기록", "Activity"));
            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(14, 10, 14, 10);
            root.ColumnCount = 1;
            root.RowCount = 2;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.BackColor = UiTheme.Surface;
            page.Controls.Add(CreateCard(root, new Padding(3, 3, 3, 3)));

            var actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            actions.WrapContents = false;
            actions.Controls.Add(CreateButton(UiText.T("기록 지우기", "Clear log"), ClearLog));
            actions.Controls.Add(CreateButton(UiText.T("기록 복사", "Copy log"), CopyLog));
            root.Controls.Add(actions, 0, 0);

            logBox.Dock = DockStyle.Fill;
            logBox.Font = UiTheme.ConsoleFont;
            logBox.WordWrap = false;
            root.Controls.Add(logBox, 0, 1);
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
            label.Font = UiTheme.SectionFont;
            label.TextAlign = ContentAlignment.MiddleLeft;
        }

        private static void ConfigureExclusionListBox(ListBox box)
        {
            box.Dock = DockStyle.Fill;
            box.HorizontalScrollbar = true;
            box.IntegralHeight = false;
            box.Font = UiTheme.ConsoleFont;
        }

        private static TextBox CreateMultilineBox(bool readOnly)
        {
            var box = new TextBox();
            box.Dock = DockStyle.Fill;
            box.Multiline = true;
            box.ScrollBars = ScrollBars.Both;
            box.WordWrap = false;
            box.ReadOnly = readOnly;
            box.Font = UiTheme.ConsoleFont;
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
            box.BackColor = UiTheme.Surface;
            box.BorderStyle = BorderStyle.None;
            box.Tag = "guide";
            box.Font = UiTheme.GuideFont;
            box.Text = text;
            return box;
        }

        private Button CreateButton(string text, Action action)
        {
            var button = new Button();
            button.Text = text;
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.MinimumSize = new Size(150, 36);
            button.Margin = new Padding(4);
            button.Click += delegate { action(); };
            helpTip.SetToolTip(button, text);
            return button;
        }

        private Button CreatePrimaryButton(string text, Action action)
        {
            var button = CreateButton(text, action);
            button.Tag = "primary";
            return button;
        }

        private static void AddRow(TableLayoutPanel table, int row, string labelText, Control control)
        {
            var label = new Label();
            label.Text = labelText;
            label.Dock = DockStyle.Fill;
            label.ForeColor = UiTheme.Muted;
            label.Font = UiTheme.BodyBoldFont;
            label.TextAlign = ContentAlignment.MiddleLeft;
            table.Controls.Add(label, 0, row);
            control.Dock = DockStyle.Left;
            control.Width = Math.Max(control.Width, 320);
            table.Controls.Add(control, 1, row);
        }

        private void RefreshStatus()
        {
            RunInBackground(
                UiText.T("UWF 상태를 확인하는 중…", "Checking UWF status…"),
                ReadStatus,
                delegate(UwfStatus status, Exception error)
                {
                    if (error != null)
                    {
                        ShowError(UiText.T("상태를 읽지 못했습니다.", "Could not read UWF status."), error.Message);
                        return;
                    }

                    ApplyStatus(status);
                    AppendLog(UiText.T("상태를 새로고침했습니다.", "Status refreshed."));
                });
        }

        private UwfStatus ReadStatus()
        {
            var status = controller.GetStatus();
            status.TotalPhysicalMemoryMb = SystemSizing.GetTotalPhysicalMemoryMb();
            status.SystemVolumeFreeSpaceMb = SystemSizing.GetFreeSpaceMb(SystemSizing.GetSystemVolume());
            return status;
        }

        private void ApplyStatus(UwfStatus status)
        {
            if (status == null)
            {
                return;
            }

            lastStatus = status;
            totalPhysicalMemoryMb = status.TotalPhysicalMemoryMb;
            systemVolumeFreeSpaceMb = status.SystemVolumeFreeSpaceMb;
            adminLabel.Text = status.IsAdministrator ? UiText.T("관리자 권한", "Administrator") : UiText.T("일반 권한", "Standard user");
            adminLabel.BackColor = status.IsAdministrator ? UiTheme.Selected : UiTheme.Badge;
            uwfLabel.Text = status.UwfToolExists ? UiText.T("UWF 사용 가능", "UWF available") : UiText.T("UWF 도구 없음", "UWF tool missing");
            uwfLabel.BackColor = status.UwfToolExists ? UiTheme.Selected : UiTheme.Badge;
            osLabel.Text = status.OsCaption;
            osLabel.BackColor = status.IsLikelySupportedEdition ? UiTheme.Selected : UiTheme.Badge;
            UpdateDashboard(status);
            UpdateExclusionLists(status.Snapshot);
            statusBox.Text = status.Report;
        }

        private void PreparePlanFromCurrentStatus(string busyText, Func<UwfStatus, OperationPlan> planFactory)
        {
            RunInBackground(
                busyText,
                ReadStatus,
                delegate(UwfStatus status, Exception error)
                {
                    if (error != null)
                    {
                        ShowError(UiText.T("현재 UWF 상태를 확인할 수 없습니다.", "Could not verify the current UWF state."), error.Message);
                        return;
                    }

                    ApplyStatus(status);
                    try
                    {
                        RunPlan(planFactory(status));
                    }
                    catch (Exception ex)
                    {
                        ShowError(UiText.T("작업 계획을 만들 수 없습니다.", "Could not create the operation plan."), ex.Message);
                    }
                });
        }

        private void UpdateDashboard(UwfStatus status)
        {
            if (status == null || status.Snapshot == null)
            {
                ClearDashboard();
                return;
            }

            var snapshot = status.Snapshot;
            SetDashboard("FilterCurrent", FormatBool(snapshot.FilterCurrentEnabled));
            SetDashboard("FilterNext", FormatBool(snapshot.FilterNextEnabled));
            SetDashboard("OverlayCurrent", snapshot.CurrentOverlayType);
            SetDashboard("OverlayNext", snapshot.NextOverlayType);
            SetDashboard("OverlayMax", FormatMb(snapshot.CurrentMaximumSizeMb));
            SetDashboard("OverlayMaxNext", FormatMb(snapshot.NextMaximumSizeMb));
            SetDashboard("OverlayUsage", FormatMb(snapshot.OverlayConsumptionMb) + " (" + snapshot.GetOverlayUsagePercentText() + ")");
            SetDashboard("OverlayAvailable", FormatMb(snapshot.AvailableSpaceMb));
            SetDashboard("OverlayThresholds", FormatCompactThresholdPair(snapshot.WarningThresholdMb, snapshot.CriticalThresholdMb));
            SetDashboard("VolumesCurrent", snapshot.CurrentProtectedVolumesText());
            SetDashboard("VolumesNext", snapshot.NextProtectedVolumesText());
            SetDashboard("ServicingCurrent", FormatBool(snapshot.ServicingCurrentEnabled));
            SetDashboard("ServicingNext", FormatBool(snapshot.ServicingNextEnabled));
            SetDashboard("RebootNeeded", !snapshot.CanDeterminePendingChanges()
                ? UiText.T("확인 불가", "Unknown")
                : (snapshot.HasPendingChanges() ? UiText.T("예 - 다음 세션 변경 있음", "Yes - next-session changes") : UiText.T("아니오", "No")));

            var profile = GetSelectedWorkloadProfile();
            int ramReco = SizingRules.RecommendRamOverlayMb(status.TotalPhysicalMemoryMb, profile);
            int diskReco = SizingRules.RecommendDiskOverlayMb(status.SystemVolumeFreeSpaceMb, profile);
            SetDashboard("Recommendation", FormatRecommendation(ramReco, diskReco));

            int percent = snapshot.GetOverlayUsagePercent();
            overlayProgress.Value = Math.Max(0, Math.Min(100, percent));
        }

        private void ClearDashboard()
        {
            string[] keys = new string[]
            {
                "FilterCurrent", "FilterNext", "OverlayCurrent", "OverlayNext", "OverlayMax", "OverlayMaxNext", "OverlayUsage",
                "OverlayAvailable", "OverlayThresholds", "VolumesCurrent", "VolumesNext", "ServicingCurrent",
                "ServicingNext", "RebootNeeded", "Recommendation"
            };
            for (int i = 0; i < keys.Length; i++)
            {
                SetDashboard(keys[i], UiText.T("확인 불가", "Unknown"));
            }
            overlayProgress.Value = 0;
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
                var safeValue = value ?? UiText.T("확인 불가", "Unknown");
                label.Text = safeValue;
                helpTip.SetToolTip(label, safeValue);
                if (key == "RebootNeeded" && safeValue.StartsWith(UiText.T("예", "Yes"), StringComparison.OrdinalIgnoreCase))
                {
                    label.BackColor = UiTheme.Warning;
                }
                else if (key == "FilterCurrent" || key == "FilterNext")
                {
                    label.BackColor = safeValue == UiText.T("켜짐", "On") ? UiTheme.Success : UiTheme.Badge;
                }
                else if (String.Equals(safeValue, UiText.T("확인 불가", "Unknown"), StringComparison.OrdinalIgnoreCase))
                {
                    label.BackColor = UiTheme.Badge;
                }
                else
                {
                    label.BackColor = UiTheme.Surface;
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

        private static string FormatCompactMb(int? value)
        {
            if (!value.HasValue || value.Value < 0)
            {
                return UiText.T("확인 불가", "Unknown");
            }
            if (value.Value < 1024)
            {
                return value.Value.ToString("N0") + " MB";
            }
            return (value.Value / 1024.0).ToString("0.#") + " GB";
        }

        private static string FormatCompactThresholdPair(int? warningMb, int? criticalMb)
        {
            if (warningMb.HasValue && criticalMb.HasValue && warningMb.Value >= 1024 && criticalMb.Value >= 1024)
            {
                return FormatGigabytes(warningMb.Value) + " / " + FormatGigabytes(criticalMb.Value) + " GB";
            }
            return FormatCompactMb(warningMb) + " / " + FormatCompactMb(criticalMb);
        }

        private static string FormatRecommendation(int ramMb, int diskMb)
        {
            if (ramMb > 0 && diskMb > 0)
            {
                return FormatGigabytes(ramMb) + " / " + FormatGigabytes(diskMb) + " GB";
            }

            return (ramMb > 0 ? FormatCompactMb(ramMb) : UiText.T("확인 불가", "n/a")) +
                   " / " + (diskMb > 0 ? FormatCompactMb(diskMb) : UiText.T("확인 불가", "n/a"));
        }

        private static string FormatGigabytes(int megabytes)
        {
            return (megabytes / 1024.0).ToString("0.#");
        }

        private void CopyStatus()
        {
            CopyToClipboard(statusBox.Text, UiText.T("상태 보고서", "Status report"));
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
                    try
                    {
                        File.WriteAllText(dialog.FileName, statusBox.Text, new UTF8Encoding(true));
                        AppendLog(UiText.T("보고서를 내보냈습니다: ", "Report exported: ") + dialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        ShowError(UiText.T("보고서를 저장하지 못했습니다.", "Could not save the report."), ex.Message);
                    }
                }
            }
        }

        private void CopyLog()
        {
            CopyToClipboard(logBox.Text, UiText.T("활동 기록", "Activity log"));
        }

        private void CopyToClipboard(string text, string itemName)
        {
            if (String.IsNullOrWhiteSpace(text))
            {
                ShowError(UiText.T("복사할 내용이 없습니다.", "Nothing to copy."), itemName);
                return;
            }

            try
            {
                Clipboard.SetText(text);
                AppendLog(UiText.T("클립보드에 복사했습니다: ", "Copied to clipboard: ") + itemName);
            }
            catch (Exception ex)
            {
                ShowError(UiText.T("클립보드에 복사하지 못했습니다.", "Could not copy to the clipboard."), ex.Message);
            }
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

        private void RelaunchAsAdmin(OperationPlan plan)
        {
            string error;
            if (Elevation.TryRelaunchCurrentProcessAsAdministrator(plan, out error))
            {
                Close();
                return;
            }

            ShowError(UiText.T("관리자 권한으로 작업을 다시 열 수 없습니다.", "Could not reopen this operation with administrator rights."), error);
        }

        private void InstallFeature()
        {
            RunPlan(controller.CreateInstallFeaturePlan());
        }

        private void UseRecommendedRam()
        {
            var profile = GetSelectedWorkloadProfile();
            int recommended = SizingRules.RecommendRamOverlayMb(totalPhysicalMemoryMb, profile);
            if (recommended < Decimal.ToInt32(overlaySizeBox.Minimum))
            {
                ShowError(UiText.T("RAM 추천값을 계산할 수 없습니다.", "RAM recommendation unavailable."),
                    UiText.T("메모리 정보를 확인할 수 없거나, 안전한 최소 크기를 확보할 수 없습니다. 상태를 새로고침하거나 수동으로 설정하세요.",
                        "Memory information is unavailable or the system cannot reserve the minimum size safely. Refresh status or choose a size manually."));
                return;
            }
            SetOverlayPreset("RAM", recommended);
            GoToSetup();
            AppendLog(UiText.T("RAM 추천값을 입력했습니다: ", "Applied RAM recommendation: ") + recommended.ToString() + " MB (" + profile.DisplayName() + ")");
        }

        private void UseRecommendedDisk()
        {
            var profile = GetSelectedWorkloadProfile();
            int recommended = SizingRules.RecommendDiskOverlayMb(systemVolumeFreeSpaceMb, profile);
            if (recommended < Decimal.ToInt32(overlaySizeBox.Minimum))
            {
                ShowError(UiText.T("DISK 추천값을 계산할 수 없습니다.", "DISK recommendation unavailable."),
                    UiText.T("시스템 볼륨의 여유 공간을 확인할 수 없거나 1024MB 오버레이를 예약할 공간이 부족합니다.",
                        "The system volume free space is unavailable or too low for a 1024 MB overlay."));
                return;
            }
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
            ShowPage(2);
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

            var overlayType = Convert.ToString(overlayTypeBox.SelectedItem);
            PreparePlanFromCurrentStatus(
                UiText.T("안전 조건을 확인하는 중…", "Checking setup requirements…"),
                delegate(UwfStatus status)
                {
                    if (String.Equals(overlayType, "DISK", StringComparison.OrdinalIgnoreCase))
                    {
                        if (status.SystemVolumeFreeSpaceMb <= 0)
                        {
                            throw new InvalidOperationException(UiText.T("시스템 볼륨의 여유 공간을 확인할 수 없습니다.",
                                "System volume free space is unavailable."));
                        }
                        if (status.SystemVolumeFreeSpaceMb <= size)
                        {
                            throw new InvalidOperationException(
                                UiText.T("시스템 볼륨의 여유 공간은 오버레이 크기보다 커야 합니다. 현재 여유 공간: ",
                                    "System volume free space must be greater than the overlay size. Current free space: ") +
                                status.SystemVolumeFreeSpaceMb.ToString("N0") + " MB");
                        }
                    }

                    return controller.CreateSetupPlan(overlayType, volumes, size, warning, critical, status.Snapshot);
                });
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
            PreparePlanFromCurrentStatus(
                UiText.T("현재 설정을 확인하는 중…", "Checking current settings…"),
                delegate(UwfStatus status) { return controller.CreateDiskOverlayCleanupPlan(status.Snapshot); });
        }

        private void FullDisableUwf()
        {
            PreparePlanFromCurrentStatus(
                UiText.T("현재 설정을 확인하는 중…", "Checking current settings…"),
                delegate(UwfStatus status) { return controller.CreateFullDisablePlan(status.Snapshot); });
        }

        private void FullResetUwf()
        {
            PreparePlanFromCurrentStatus(
                UiText.T("현재 설정을 확인하는 중…", "Checking current settings…"),
                delegate(UwfStatus status) { return controller.CreateFullResetPlan(status.Snapshot); });
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
            RunPlan(controller.CreateSimplePlan(
                UiText.T("Windows 업데이트 준비", "Prepare Windows servicing"),
                UiText.T("UWF 공식 서비스 모드를 예약합니다. 재시작 후 오버레이를 비우고 필터를 끈 상태에서 업데이트를 적용한 뒤 보호를 다시 켭니다. 모든 사용자 계정에 암호가 설정되어 있어야 하며, 진행 중에는 전원을 끄지 마세요.",
                    "Schedules the UWF servicing flow. After restart, Windows clears the overlay, applies updates with filtering disabled, then restores protection. Every user account must have a password; do not power off during servicing."),
                "uwfmgr.exe",
                "servicing enable"));
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
            RunInBackground(
                UiText.T("UWF 정보를 읽는 중…", "Reading UWF information…"),
                delegate { return controller.RunReadOnly(args); },
                delegate(CommandResult result, Exception error)
                {
                    if (error != null)
                    {
                        ShowError(UiText.T("명령을 실행하지 못했습니다.", "Could not run the command."), error.Message);
                        return;
                    }

                    AppendLog("== " + title + " ==");
                    AppendLog(result.ToDisplayText());
                });
        }

        private void RunPlan(OperationPlan plan)
        {
            if (plan == null || isBusy)
            {
                return;
            }

            DialogResult confirm;
            using (var review = new OperationReviewDialog(plan))
            {
                confirm = review.ShowDialog(this);
            }
            if (confirm != DialogResult.OK)
            {
                AppendLog(UiText.T("취소됨: ", "Canceled: ") + plan.Title);
                return;
            }

            if (plan.RequiresAdministrator && !UwfController.IsAdministrator())
            {
                RelaunchAsAdmin(plan);
                return;
            }

            operationInProgress = true;
            AppendLog(UiText.T("== 적용 중: ", "== Applying: ") + plan.Title + " ==");
            RunInBackground(
                UiText.T("작업을 적용하는 중…", "Applying operation…"),
                delegate { return controller.ExecutePlan(plan); },
                delegate(string report, Exception error)
                {
                    operationInProgress = false;
                    if (error != null)
                    {
                        ShowError(UiText.T("작업 중 오류가 발생했습니다.", "The operation failed unexpectedly."), error.Message);
                    }
                    else
                    {
                        AppendLog(report);
                    }
                    RefreshStatus();
                });
        }

        private void RunInBackground<T>(string busyText, Func<T> operation, Action<T, Exception> completed)
        {
            if (isBusy)
            {
                return;
            }

            SetBusy(true, busyText);
            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                T result = default(T);
                Exception failure = null;
                try
                {
                    result = operation();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }

                if (IsDisposed || Disposing || !IsHandleCreated)
                {
                    return;
                }

                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (IsDisposed || Disposing)
                        {
                            return;
                        }

                        SetBusy(false, String.Empty);
                        completed(result, failure);
                    });
                }
                catch (InvalidOperationException)
                {
                }
            });
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (operationInProgress && e.CloseReason == CloseReason.UserClosing)
            {
                MessageBox.Show(this,
                    UiText.T("작업이 진행 중입니다. 결과가 기록될 때까지 앱을 닫을 수 없습니다.",
                        "An operation is still running. Keep the app open until it finishes."),
                    UiText.T("작업 진행 중", "Operation in progress"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                e.Cancel = true;
            }

            base.OnFormClosing(e);
        }

        private void AppendLog(string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                return;
            }

            logBox.MaxLength = 300000;
            logBox.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text.Replace("\r\n", "\r\n          ") + Environment.NewLine);
            if (logBox.TextLength > 280000)
            {
                var current = logBox.Text;
                int excess = current.Length - 240000;
                int lineBreak = current.IndexOf(Environment.NewLine, Math.Max(0, excess), StringComparison.Ordinal);
                if (lineBreak >= 0)
                {
                    logBox.Select(0, lineBreak + Environment.NewLine.Length);
                    logBox.SelectedText = String.Empty;
                    logBox.SelectionStart = logBox.TextLength;
                    logBox.ScrollToCaret();
                }
            }
        }

        private void ShowError(string title, string message)
        {
            MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppendLog(title + " " + message);
        }

        private static string Quote(string value)
        {
            return Elevation.QuoteArgumentForCreateProcess(value);
        }
    }
}
