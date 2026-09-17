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
using IdeaMemoryManager.Interop;
using IdeaMemoryManager.UI.Tray;

namespace IdeaMemoryManager.UI.Views
{
    /// <summary>
    /// Modern dark floating panel displaying real-time memory metrics,
    /// manual optimization trigger, and background automation controls.
    /// </summary>
    public class MainForm : Form
    {
        private readonly MemoryCleanEngine _engine = new();
        private readonly SmartScheduler _scheduler;
        private readonly TrayService _tray;

        private Label _lblTitle;
        private Label _lblVersion;
        private Button _btnLang;
        private Button _btnPin;
        private Button _btnMin;
        private Button _btnClose;
        private Label _lblTotalMemory;
        private Label _lblSubtitle;
        private Label _lblIdeaMem;
        private Label _lblJavaMem;
        private Label _lblNodeMem;
        private Label _lblLifetimeStats;
        private Label _lblStatus;
        private Button _btnClean;

        private CheckBox _chkDeepGc;
        private CheckBox _chkSmartThreshold;
        private CheckBox _chkAuto;
        private ComboBox _cmbInterval;
        private CheckBox _chkAutoStart;

        private System.Windows.Forms.Timer _refreshTimer;
        private bool _isCleaning = false;

        public MainForm()
        {
            ConfigManager.Load();
            _scheduler = new SmartScheduler(_engine);

            // Load custom application icon
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

            _refreshTimer = new System.Windows.Forms.Timer { Interval = 2500 };
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
            this.Size = new Size(390, 530);
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

            // Version badge (anchored dynamically after _lblTitle)
            _lblVersion = new Label
            {
                Text = "v1.0.0",
                ForeColor = Color.FromArgb(88, 166, 255),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(38, 48, 65),
                Size = new Size(44, 18),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Language switch button (EN / 中) with adequate 44px width to prevent letter truncation
            _btnLang = new Button
            {
                Text = I18n.CurrentLanguage == AppLanguage.English ? "中" : "EN",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(160, 164, 170),
                BackColor = Color.FromArgb(45, 47, 52),
                Size = new Size(44, 24),
                Location = new Point(250, 8),
                Font = new Font("Microsoft YaHei UI", 8.5f),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _btnLang.FlatAppearance.BorderSize = 0;

            // Pin / unpin top-most button
            _btnPin = new Button
            {
                Text = "📌",
                FlatStyle = FlatStyle.Flat,
                ForeColor = ConfigManager.Current.IsPinned ? Color.FromArgb(53, 116, 240) : Color.Gray,
                BackColor = Color.Transparent,
                Size = new Size(24, 24),
                Location = new Point(302, 8),
                Cursor = Cursors.Hand
            };
            _btnPin.FlatAppearance.BorderSize = 0;

            // Minimize button
            _btnMin = new Button
            {
                Text = "—",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(160, 164, 170),
                BackColor = Color.Transparent,
                Size = new Size(24, 24),
                Location = new Point(330, 8),
                Cursor = Cursors.Hand
            };
            _btnMin.FlatAppearance.BorderSize = 0;

            // Close button
            _btnClose = new Button
            {
                Text = "✕",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(160, 164, 170),
                BackColor = Color.Transparent,
                Size = new Size(24, 24),
                Location = new Point(358, 8),
                Cursor = Cursors.Hand
            };
            _btnClose.FlatAppearance.BorderSize = 0;

            // Main memory metric header with generous 66px vertical space to prevent clipping
            _lblTotalMemory = new Label
            {
                Text = "0.00 GB",
                ForeColor = Color.FromArgb(240, 246, 252),
                Font = new Font("Segoe UI", 32f, FontStyle.Bold),
                Location = new Point(12, 38),
                Size = new Size(366, 66),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _lblSubtitle = new Label
            {
                Text = I18n.T("SubtitleScanning"),
                ForeColor = Color.FromArgb(139, 148, 158),
                Font = new Font("Microsoft YaHei UI", 8.5f),
                Location = new Point(12, 106),
                Size = new Size(366, 18),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Metrics details card
            Panel pnlDetails = new Panel
            {
                Location = new Point(16, 130),
                Size = new Size(358, 102),
                BackColor = Color.FromArgb(39, 41, 45)
            };

            _lblIdeaMem = new Label
            {
                Text = string.Format(I18n.T("IdeaHost"), "..."),
                ForeColor = Color.FromArgb(201, 209, 217),
                Font = new Font("Microsoft YaHei UI", 9f),
                Location = new Point(14, 12),
                AutoSize = true
            };

            _lblJavaMem = new Label
            {
                Text = string.Format(I18n.T("JavaServices"), "..."),
                ForeColor = Color.FromArgb(201, 209, 217),
                Font = new Font("Microsoft YaHei UI", 9f),
                Location = new Point(14, 40),
                AutoSize = true
            };

            _lblNodeMem = new Label
            {
                Text = string.Format(I18n.T("NodeServices"), "..."),
                ForeColor = Color.FromArgb(201, 209, 217),
                Font = new Font("Microsoft YaHei UI", 9f),
                Location = new Point(14, 68),
                AutoSize = true
            };

            pnlDetails.Controls.Add(_lblIdeaMem);
            pnlDetails.Controls.Add(_lblJavaMem);
            pnlDetails.Controls.Add(_lblNodeMem);

            // Checkbox options
            _chkDeepGc = new CheckBox
            {
                Text = I18n.T("DeepGcOption"),
                ForeColor = Color.FromArgb(88, 166, 255),
                Font = new Font("Microsoft YaHei UI", 8.5f),
                Location = new Point(20, 240),
                AutoSize = true,
                Checked = ConfigManager.Current.DeepGcEnabled
            };

            _chkSmartThreshold = new CheckBox
            {
                Text = I18n.T("SmartThresholdOption"),
                ForeColor = Color.FromArgb(201, 209, 217),
                Font = new Font("Microsoft YaHei UI", 8.5f),
                Location = new Point(20, 264),
                AutoSize = true,
                Checked = ConfigManager.Current.SmartThresholdEnabled
            };

            // Status message
            _lblStatus = new Label
            {
                Text = I18n.T("StatusReady"),
                ForeColor = Color.FromArgb(139, 148, 158),
                Font = new Font("Microsoft YaHei UI", 8.5f),
                Location = new Point(12, 290),
                Size = new Size(366, 18),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Optimization action button
            _btnClean = new Button
            {
                Text = I18n.T("BtnClean"),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(53, 116, 240),
                Font = new Font("Microsoft YaHei UI", 11.5f, FontStyle.Bold),
                Location = new Point(16, 314),
                Size = new Size(358, 46),
                Cursor = Cursors.Hand
            };
            _btnClean.FlatAppearance.BorderSize = 0;

            // Lifetime stats banner
            _lblLifetimeStats = new Label
            {
                Text = string.Format(I18n.T("LifetimeStats"), "0.0 GB", 0),
                ForeColor = Color.FromArgb(46, 160, 67),
                Font = new Font("Microsoft YaHei UI", 8.5f),
                Location = new Point(12, 370),
                Size = new Size(366, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Background automation controls
            _chkAuto = new CheckBox
            {
                Text = I18n.T("AutoSchedule"),
                ForeColor = Color.FromArgb(201, 209, 217),
                Font = new Font("Microsoft YaHei UI", 8.5f),
                Location = new Point(20, 400),
                AutoSize = true,
                Checked = ConfigManager.Current.AutoCleanEnabled
            };

            _cmbInterval = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei UI", 8.5f),
                Location = new Point(220, 398),
                Size = new Size(154, 25),
                BackColor = Color.FromArgb(45, 47, 52),
                ForeColor = Color.White
            };
            UpdateIntervalItems();

            _chkAutoStart = new CheckBox
            {
                Text = I18n.T("AutoStart"),
                ForeColor = Color.FromArgb(201, 209, 217),
                Font = new Font("Microsoft YaHei UI", 8.5f),
                Location = new Point(20, 430),
                AutoSize = true,
                Checked = ConfigManager.Current.AutoStartWithWindows
            };

            // Add controls
            this.Controls.Add(_lblTitle);
            this.Controls.Add(_lblVersion);
            this.Controls.Add(_btnLang);
            this.Controls.Add(_btnPin);
            this.Controls.Add(_btnMin);
            this.Controls.Add(_btnClose);
            this.Controls.Add(_lblTotalMemory);
            this.Controls.Add(_lblSubtitle);
            this.Controls.Add(pnlDetails);
            this.Controls.Add(_chkDeepGc);
            this.Controls.Add(_chkSmartThreshold);
            this.Controls.Add(_lblStatus);
            this.Controls.Add(_btnClean);
            this.Controls.Add(_lblLifetimeStats);
            this.Controls.Add(_chkAuto);
            this.Controls.Add(_cmbInterval);
            this.Controls.Add(_chkAutoStart);

            // Drag window handling
            this.MouseDown += Form_MouseDown;
            _lblTitle.MouseDown += Form_MouseDown;
        }

        private void UpdateIntervalItems()
        {
            int oldIdx = _cmbInterval.SelectedIndex;
            _cmbInterval.Items.Clear();
            _cmbInterval.Items.AddRange(new object[]
            {
                I18n.T("Interval15m"),
                I18n.T("Interval30m"),
                I18n.T("Interval1h"),
                I18n.T("Interval2h")
            });
            _cmbInterval.SelectedIndex = oldIdx >= 0 ? oldIdx : (ConfigManager.Current.IntervalMinutes switch
            {
                15 => 0,
                60 => 2,
                120 => 3,
                _ => 1
            });
        }

        private void ApplyLocalization()
        {
            _lblTitle.Text = I18n.T("AppTitle");
            // Dynamically position version badge right after title text
            _lblVersion.Location = new Point(_lblTitle.Right + 8, 12);

            _chkDeepGc.Text = I18n.T("DeepGcOption");
            _chkSmartThreshold.Text = I18n.T("SmartThresholdOption");
            _chkAuto.Text = I18n.T("AutoSchedule");
            _chkAutoStart.Text = I18n.T("AutoStart");
            _btnClean.Text = I18n.T("BtnClean");
            _lblStatus.Text = I18n.T("StatusReady");
            _btnLang.Text = I18n.CurrentLanguage == AppLanguage.English ? "中" : "EN";
            UpdateIntervalItems();
        }

        private void SetupEvents()
        {
            _btnLang.Click += (s, e) =>
            {
                var nextLang = I18n.CurrentLanguage == AppLanguage.English ? AppLanguage.Chinese : AppLanguage.English;
                I18n.SetLanguage(nextLang);
                ConfigManager.Save();
                ApplyLocalization();
                RefreshStats();
            };

            _btnPin.Click += (s, e) =>
            {
                var cfg = ConfigManager.Current;
                cfg.IsPinned = !cfg.IsPinned;
                this.TopMost = cfg.IsPinned;
                _btnPin.ForeColor = cfg.IsPinned ? Color.FromArgb(53, 116, 240) : Color.Gray;
                ConfigManager.Save();
            };

            _btnMin.Click += (s, e) => { this.WindowState = FormWindowState.Minimized; };
            _btnClose.Click += (s, e) => { this.Hide(); };

            _btnClean.Click += async (s, e) => { await TriggerCleanAsync(true); };

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
        }

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(this.Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HT_CAPTION, 0);
            }
        }

        private void RefreshStats()
        {
            try
            {
                var stats = _engine.GetCurrentStats();
                _lblTotalMemory.Text = ByteSizeFormatter.FormatGb(stats.TotalBytes);
                _lblSubtitle.Text = string.Format(I18n.T("ProcessesCount"), stats.TotalProcesses);

                _lblIdeaMem.Text = string.Format(I18n.T("IdeaHost"), ByteSizeFormatter.FormatMb(stats.IdeaBytes));
                _lblJavaMem.Text = string.Format(I18n.T("JavaServices"), ByteSizeFormatter.FormatMb(stats.JavaBytes));
                _lblNodeMem.Text = string.Format(I18n.T("NodeServices"), ByteSizeFormatter.FormatMb(stats.NodeBytes));

                long totalSaved = ConfigManager.Current.TotalSavedBytes;
                int count = ConfigManager.Current.TotalCleanCount;
                _lblLifetimeStats.Text = string.Format(I18n.T("LifetimeStats"), ByteSizeFormatter.Format(totalSaved), count);
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