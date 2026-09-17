using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using IdeaMemoryManager.Common;
using IdeaMemoryManager.Common.Localization;
using IdeaMemoryManager.Config;
using IdeaMemoryManager.Core.Automation;
using IdeaMemoryManager.Core.Engine;
using IdeaMemoryManager.Core.Models;
using IdeaMemoryManager.Interop;
using IdeaMemoryManager.UI.Controls;
using IdeaMemoryManager.UI.Tray;

namespace IdeaMemoryManager.UI.Views
{
    /// <summary>
    /// Modern dark floating panel displaying real-time memory metrics,
    /// dynamic sparkline trend chart, expandable topology drawer, and deep GC controls.
    /// </summary>
    public class MainForm : Form
    {
        private const int CollapsedHeight = 515;
        private const int ExpandedHeight = 705;

        private readonly MemoryCleanEngine _engine = new();
        private readonly SmartScheduler _scheduler;
        private readonly TrayService _tray;
        private readonly ToolTip _toolTip = new();

        private Label _lblTitle;
        private Label _lblVersion;
        private Button _btnLang;
        private Button _btnPin;
        private Button _btnMin;
        private Button _btnClose;

        private Label _lblTotalMemory;
        private SparklineControl _sparkline;
        private Label _lblSubtitle;
        private Label _lblIdeaMem;
        private Label _lblJavaMem;
        private Label _lblNodeMem;
        private Label _lblLifetimeStats;
        private Label _lblStatus;
        private Button _btnClean;
        private Button _btnToggleDrawer;
        private ProcessDrawerControl _drawer;

        private CheckBox _chkDeepGc;
        private CheckBox _chkSmartThreshold;
        private CheckBox _chkAuto;
        private ComboBox _cmbInterval;
        private CheckBox _chkAutoStart;

        private System.Windows.Forms.Timer _refreshTimer;
        private bool _isCleaning = false;
        private bool _isDrawerExpanded = false;
        private MemoryStats _lastStats;

        public MainForm()
        {
            ConfigManager.Load();
            _scheduler = new SmartScheduler(_engine);

            Icon appIcon = LoadAppIcon();
            if (appIcon != null)
            {
                this.Icon = appIcon;
            }

            _tray = new TrayService(appIcon);

            InitializeComponent();
            SetupEvents();
            ApplyLocalization();
            RefreshStats();

            _refreshTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            _refreshTimer.Tick += (s, e) => { if (!_isCleaning) RefreshStats(); };
            _refreshTimer.Start();
        }

        private Icon LoadAppIcon()
        {
            try
            {
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "app.ico");
                if (File.Exists(icoPath))
                {
                    return new Icon(icoPath);
                }
                string exePath = Environment.ProcessPath ?? Application.ExecutablePath;
                return Icon.ExtractAssociatedIcon(exePath);
            }
            catch
            {
                return null;
            }
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(390, CollapsedHeight);
            this.BackColor = Color.FromArgb(30, 31, 34);
            this.DoubleBuffered = true;
            this.TopMost = ConfigManager.Current.IsPinned;
            this.ShowInTaskbar = true;
            this.AutoScaleMode = AutoScaleMode.Dpi;

            // Title label
            _lblTitle = new Label
            {
                Text = I18n.T("AppTitle"),
                ForeColor = Color.FromArgb(220, 224, 230),
                Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold),
                Location = new Point(14, 11),
                AutoSize = true
            };

            _lblVersion = new Label
            {
                Text = "v1.1.0",
                ForeColor = Color.FromArgb(88, 166, 255),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(38, 48, 65),
                Size = new Size(44, 18),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _btnLang = CreateHeaderButton(I18n.CurrentLanguage == AppLanguage.English ? "中" : "EN", 44);
            _btnPin = CreateHeaderButton("📌", 30);
            _btnPin.ForeColor = ConfigManager.Current.IsPinned ? Color.FromArgb(53, 116, 240) : Color.Gray;

            _btnMin = CreateHeaderButton("—", 30);
            _btnClose = CreateHeaderButton("✕", 30);

            // Large RAM numeric label
            _lblTotalMemory = new Label
            {
                Text = "-- GB",
                ForeColor = Color.FromArgb(56, 239, 125),
                Font = new Font("Segoe UI", 28f, FontStyle.Bold),
                Location = new Point(16, 38),
                Size = new Size(358, 52),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 60s Sparkline chart
            _sparkline = new SparklineControl
            {
                Location = new Point(16, 94),
                Size = new Size(358, 40)
            };

            _lblSubtitle = new Label
            {
                Text = I18n.T("SubtitleScanning"),
                ForeColor = Color.FromArgb(140, 145, 155),
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(16, 140),
                Size = new Size(358, 18)
            };

            // Process category memory panels
            _lblIdeaMem = CreateSubMetricLabel(new Point(16, 164));
            _lblJavaMem = CreateSubMetricLabel(new Point(16, 186));
            _lblNodeMem = CreateSubMetricLabel(new Point(16, 208));

            // Lifetime stats banner
            _lblLifetimeStats = new Label
            {
                Text = string.Format(I18n.T("LifetimeStats"), "--", 0),
                ForeColor = Color.FromArgb(230, 180, 80),
                Font = new Font("Segoe UI", 8.0f, FontStyle.Bold),
                Location = new Point(16, 236),
                Size = new Size(358, 18)
            };

            // Checkbox options
            _chkDeepGc = CreateCheckBox(I18n.T("DeepGcOption"), new Point(16, 264), ConfigManager.Current.DeepGcEnabled);
            _chkSmartThreshold = CreateCheckBox(I18n.T("SmartThresholdOption"), new Point(16, 288), ConfigManager.Current.SmartThresholdEnabled);

            _chkAuto = CreateCheckBox(I18n.T("AutoSchedule"), new Point(16, 312), ConfigManager.Current.AutoCleanEnabled);
            _chkAuto.AutoSize = true;

            _cmbInterval = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(43, 45, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.0f),
                Location = new Point(245, 311),
                Size = new Size(128, 22)
            };

            _chkAutoStart = CreateCheckBox(I18n.T("AutoStart"), new Point(16, 338), ConfigManager.Current.AutoStartWithWindows);

            // Status message label
            _lblStatus = new Label
            {
                Text = I18n.T("StatusReady"),
                ForeColor = Color.FromArgb(140, 145, 155),
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(16, 370),
                Size = new Size(358, 24),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Primary clean action button
            _btnClean = new Button
            {
                Text = I18n.T("BtnClean"),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(35, 134, 54),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Location = new Point(16, 400),
                Size = new Size(358, 44),
                Cursor = Cursors.Hand
            };
            _btnClean.FlatAppearance.BorderSize = 0;

            // Expandable drawer toggle button
            _btnToggleDrawer = new Button
            {
                Text = string.Format(I18n.T("ShowTopology"), 0),
                ForeColor = Color.FromArgb(160, 165, 175),
                BackColor = Color.FromArgb(38, 40, 44),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(16, 454),
                Size = new Size(358, 30),
                Cursor = Cursors.Hand
            };
            _btnToggleDrawer.FlatAppearance.BorderSize = 0;

            // Process drawer list control
            _drawer = new ProcessDrawerControl
            {
                Location = new Point(16, 494),
                Size = new Size(358, 190),
                Visible = false
            };

            // Layout controls
            Controls.AddRange(new Control[]
            {
                _lblTitle, _lblVersion, _btnLang, _btnPin, _btnMin, _btnClose,
                _lblTotalMemory, _sparkline, _lblSubtitle,
                _lblIdeaMem, _lblJavaMem, _lblNodeMem,
                _lblLifetimeStats,
                _chkDeepGc, _chkSmartThreshold,
                _chkAuto, _cmbInterval, _chkAutoStart,
                _lblStatus, _btnClean, _btnToggleDrawer, _drawer
            });

            RepositionHeaderButtons();
        }

        private Button CreateHeaderButton(string text, int width)
        {
            var btn = new Button
            {
                Text = text,
                ForeColor = Color.FromArgb(160, 165, 175),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.0f),
                Size = new Size(width, 24),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 52, 56);
            return btn;
        }

        private Label CreateSubMetricLabel(Point location)
        {
            return new Label
            {
                ForeColor = Color.FromArgb(180, 185, 195),
                Font = new Font("Segoe UI", 8.5f),
                Location = location,
                Size = new Size(358, 20),
                Cursor = Cursors.Hand
            };
        }

        private CheckBox CreateCheckBox(string text, Point location, bool isChecked)
        {
            var chk = new CheckBox
            {
                Text = text,
                ForeColor = Color.FromArgb(200, 205, 215),
                Font = new Font("Segoe UI", 8.5f),
                Location = location,
                Size = new Size(358, 22),
                Checked = isChecked,
                Cursor = Cursors.Hand
            };
            return chk;
        }

        private void RepositionHeaderButtons()
        {
            _lblVersion.Location = new Point(_lblTitle.Right + 8, 13);
            _btnClose.Location = new Point(this.Width - 36, 8);
            _btnMin.Location = new Point(_btnClose.Left - 32, 8);
            _btnPin.Location = new Point(_btnMin.Left - 32, 8);
            _btnLang.Location = new Point(_btnPin.Left - 48, 8);
        }

        private void SetupEvents()
        {
            this.MouseDown += Form_MouseDown;
            _lblTitle.MouseDown += Form_MouseDown;

            _btnLang.Click += (s, e) =>
            {
                var nextLang = I18n.CurrentLanguage == AppLanguage.English ? AppLanguage.Chinese : AppLanguage.English;
                I18n.SetLanguage(nextLang);
                ConfigManager.Current.Language = nextLang;
                ConfigManager.Save();
                _btnLang.Text = nextLang == AppLanguage.English ? "中" : "EN";
                ApplyLocalization();
                RefreshStats();
                RepositionHeaderButtons();
            };

            _btnPin.Click += (s, e) =>
            {
                var cfg = ConfigManager.Current;
                cfg.IsPinned = !cfg.IsPinned;
                this.TopMost = cfg.IsPinned;
                _btnPin.ForeColor = cfg.IsPinned ? Color.FromArgb(53, 116, 240) : Color.Gray;
                ConfigManager.Save();
            };

            _btnMin.Click += (s, e) => 
            { 
                this.WindowState = FormWindowState.Minimized;
                _engine.TriggerSelfTrim();
            };

            _btnClose.Click += (s, e) => 
            { 
                this.Hide(); 
                _engine.TriggerSelfTrim();
            };

            _btnClean.Click += async (s, e) => { await TriggerCleanAsync(true); };

            _btnToggleDrawer.Click += (s, e) =>
            {
                _isDrawerExpanded = !_isDrawerExpanded;
                this.Height = _isDrawerExpanded ? ExpandedHeight : CollapsedHeight;
                _drawer.Visible = _isDrawerExpanded;
                _btnToggleDrawer.Text = _isDrawerExpanded 
                    ? I18n.T("HideTopology") 
                    : string.Format(I18n.T("ShowTopology"), _lastStats?.TotalProcesses ?? 0);
            };

            _drawer.OnWhitelistChanged += () => RefreshStats();

            _chkDeepGc.CheckedChanged += (s, e) =>
            {
                ConfigManager.Current.DeepGcEnabled = _chkDeepGc.Checked;
                ConfigManager.Save();
            };

            _chkSmartThreshold.CheckedChanged += (s, e) =>
            {
                ConfigManager.Current.SmartThresholdEnabled = _chkSmartThreshold.Checked;
                ConfigManager.Save();
                _scheduler.ApplySettings();
            };

            _chkAuto.CheckedChanged += (s, e) =>
            {
                ConfigManager.Current.AutoCleanEnabled = _chkAuto.Checked;
                ConfigManager.Save();
                _scheduler.ApplySettings();
            };

            _cmbInterval.SelectedIndexChanged += (s, e) =>
            {
                ConfigManager.Current.IntervalMinutes = _cmbInterval.SelectedIndex switch
                {
                    0 => 15,
                    2 => 60,
                    3 => 120,
                    _ => 30
                };
                ConfigManager.Save();
                _scheduler.ApplySettings();
            };

            _chkAutoStart.CheckedChanged += (s, e) =>
            {
                ConfigManager.SetAutoStart(_chkAutoStart.Checked);
            };

            // JVM Telemetry Tooltips on hover
            _lblJavaMem.MouseEnter += async (s, e) => await ShowJvmHeapTooltipAsync(_lblJavaMem);
            _lblIdeaMem.MouseEnter += async (s, e) => await ShowJvmHeapTooltipAsync(_lblIdeaMem);

            _tray.OnShowRequested += () =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
            };

            _tray.OnCleanRequested += async () => { await TriggerCleanAsync(false); };
            _tray.OnExitRequested += () =>
            {
                _tray.Dispose();
                Application.Exit();
            };

            _scheduler.OnAutoCleanCompleted += (msg) =>
            {
                this.Invoke((MethodInvoker)delegate
                {
                    _lblStatus.Text = msg;
                    _lblStatus.ForeColor = Color.FromArgb(46, 160, 67);
                    RefreshStats();
                });
            };

            _scheduler.OnStatusDeferred += (msg) =>
            {
                this.Invoke((MethodInvoker)delegate
                {
                    _lblStatus.Text = msg;
                    _lblStatus.ForeColor = Color.FromArgb(230, 180, 80);
                });
            };
        }

        private async Task ShowJvmHeapTooltipAsync(Control target)
        {
            if (_lastStats?.MainIdeaProcess == null) return;
            try
            {
                var heap = await Task.Run(() => _engine.QueryMainJvmHeap(_lastStats.MainIdeaProcess));
                if (heap.Available)
                {
                    string text = $"{I18n.T("JvmTelemetryTitle")}\n" +
                                  $"Eden Space: {ByteSizeFormatter.Format(heap.EdenUsedBytes)} / {ByteSizeFormatter.Format(heap.EdenCapacityBytes)}\n" +
                                  $"Metaspace: {ByteSizeFormatter.Format(heap.MetaspaceUsedBytes)}";
                    _toolTip.Show(text, target, 0, target.Height + 2, 4000);
                }
            }
            catch { }
        }

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(this.Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HT_CAPTION, 0);
            }
        }

        private void ApplyLocalization()
        {
            _lblTitle.Text = I18n.T("AppTitle");
            _chkDeepGc.Text = I18n.T("DeepGcOption");
            _chkSmartThreshold.Text = I18n.T("SmartThresholdOption");
            _chkAuto.Text = I18n.T("AutoSchedule");
            _chkAutoStart.Text = I18n.T("AutoStart");

            if (!_isCleaning)
            {
                _lblStatus.Text = I18n.T("StatusReady");
                _btnClean.Text = I18n.T("BtnClean");
            }

            int prevIdx = _cmbInterval.SelectedIndex;
            _cmbInterval.Items.Clear();
            _cmbInterval.Items.AddRange(new object[]
            {
                I18n.T("Interval15m"),
                I18n.T("Interval30m"),
                I18n.T("Interval1h"),
                I18n.T("Interval2h")
            });

            _cmbInterval.SelectedIndex = prevIdx >= 0 ? prevIdx : ConfigManager.Current.IntervalMinutes switch
            {
                15 => 0,
                60 => 2,
                120 => 3,
                _ => 1
            };

            _btnToggleDrawer.Text = _isDrawerExpanded 
                ? I18n.T("HideTopology") 
                : string.Format(I18n.T("ShowTopology"), _lastStats?.TotalProcesses ?? 0);
        }

        private void RefreshStats()
        {
            try
            {
                _lastStats = _engine.GetCurrentStats();
                double totalGb = _lastStats.TotalBytes / (1024.0 * 1024.0 * 1024.0);

                _lblTotalMemory.Text = ByteSizeFormatter.FormatGb(_lastStats.TotalBytes);
                _sparkline.AddDataPoint(totalGb);

                _lblSubtitle.Text = string.Format(I18n.T("ProcessesCount"), _lastStats.TotalProcesses);
                _lblIdeaMem.Text = string.Format(I18n.T("IdeaHost"), ByteSizeFormatter.FormatMb(_lastStats.IdeaBytes));
                _lblJavaMem.Text = string.Format(I18n.T("JavaServices"), ByteSizeFormatter.FormatMb(_lastStats.JavaBytes));
                _lblNodeMem.Text = string.Format(I18n.T("NodeServices"), ByteSizeFormatter.FormatMb(_lastStats.NodeBytes));

                long totalSaved = ConfigManager.Current.TotalSavedBytes;
                int count = ConfigManager.Current.TotalCleanCount;
                _lblLifetimeStats.Text = string.Format(I18n.T("LifetimeStats"), ByteSizeFormatter.Format(totalSaved), count);

                if (!_isDrawerExpanded)
                {
                    _btnToggleDrawer.Text = string.Format(I18n.T("ShowTopology"), _lastStats.TotalProcesses);
                }

                _drawer.SetTargets(_lastStats.Targets);
            }
            catch { }
        }

        private async Task TriggerCleanAsync(bool isManual)
        {
            if (_isCleaning) return;
            _isCleaning = true;

            _btnClean.Enabled = false;
            _btnClean.Text = I18n.T("BtnCleaning");
            _lblStatus.Text = I18n.T("StatusCleaning");
            _lblStatus.ForeColor = Color.FromArgb(53, 116, 240);

            var report = await Task.Run(() => _engine.ExecuteClean(ConfigManager.Current.DeepGcEnabled));

            _isCleaning = false;
            _btnClean.Enabled = true;
            _btnClean.Text = I18n.T("BtnClean");

            if (report.Success)
            {
                string msg = string.Format(I18n.T("CleanSuccess"), ByteSizeFormatter.Format(report.SavedBytes));
                _lblStatus.Text = msg;
                _lblStatus.ForeColor = Color.FromArgb(46, 160, 67);
                RefreshStats();

                if (isManual)
                {
                    _tray.ShowNotification(I18n.T("AppTitle"), $"{msg} ({report.ElapsedMilliseconds} ms)");
                }
            }
            else
            {
                _lblStatus.Text = $"Error: {report.ErrorMessage}";
                _lblStatus.ForeColor = Color.FromArgb(248, 81, 73);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var borderPen = new Pen(Color.FromArgb(60, 63, 68), 1);
            e.Graphics.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
        }
    }
}