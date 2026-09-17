using System;
using System.Windows.Forms;
using IdeaMemoryManager.Common;
using IdeaMemoryManager.Common.Localization;
using IdeaMemoryManager.Config;
using IdeaMemoryManager.Core.Engine;

namespace IdeaMemoryManager.Core.Automation
{
    /// <summary>
    /// Intelligent background scheduler providing periodic timer checks,
    /// adaptive high-watermark threshold self-healing, and Idle-Aware delay mechanisms.
    /// </summary>
    public class SmartScheduler
    {
        private readonly MemoryCleanEngine _engine;
        private readonly System.Windows.Forms.Timer _timer;
        private int _highWatermarkStreak = 0;

        public event Action<string> OnAutoCleanCompleted;
        public event Action<string> OnStatusDeferred;

        public SmartScheduler(MemoryCleanEngine engine)
        {
            _engine = engine;
            _timer = new System.Windows.Forms.Timer();
            _timer.Tick += Timer_Tick;
            ApplySettings();
        }

        public void ApplySettings()
        {
            var config = ConfigManager.Current;
            if (!config.AutoCleanEnabled && !config.SmartThresholdEnabled)
            {
                _timer.Stop();
                return;
            }

            // Polling interval: 30s for agile watermark tracking, or interval minutes for periodic timer
            if (config.SmartThresholdEnabled)
            {
                _timer.Interval = 30 * 1000;
            }
            else
            {
                _timer.Interval = Math.Max(1, config.IntervalMinutes) * 60 * 1000;
            }
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var config = ConfigManager.Current;

            // Adaptive watermark detection
            if (config.SmartThresholdEnabled)
            {
                var stats = _engine.GetCurrentStats();
                double currentGb = stats.TotalBytes / (1024.0 * 1024.0 * 1024.0);

                if (currentGb >= config.ThresholdGb)
                {
                    _highWatermarkStreak++;
                    // Debounce: trigger only when watermark is breached consecutively across 2 pollings
                    if (_highWatermarkStreak >= 2)
                    {
                        if (CanSafelyTrigger(stats))
                        {
                            _highWatermarkStreak = 0;
                            TriggerSilentClean("Smart Threshold");
                        }
                        else
                        {
                            OnStatusDeferred?.Invoke(I18n.T("IdleAwareDefer"));
                        }
                    }
                }
                else
                {
                    _highWatermarkStreak = 0;
                }
            }
            else if (config.AutoCleanEnabled)
            {
                var stats = _engine.GetCurrentStats();
                if (CanSafelyTrigger(stats))
                {
                    TriggerSilentClean("Scheduled");
                }
                else
                {
                    OnStatusDeferred?.Invoke(I18n.T("IdleAwareDefer"));
                }
            }
        }

        private bool CanSafelyTrigger(Models.MemoryStats stats)
        {
            var config = ConfigManager.Current;
            if (!config.IdleAwareEnabled) return true;

            // 1. Check user input idle time (default 25 seconds)
            if (!IdleDetector.IsUserIdle(config.IdleThresholdSeconds))
            {
                return false;
            }

            // 2. Check if main IDE is heavily compiling or indexing
            if (stats.MainIdeaProcess != null && IdleDetector.IsProcessCpuBusy(stats.MainIdeaProcess, 25.0))
            {
                return false;
            }

            return true;
        }

        private void TriggerSilentClean(string triggerSource)
        {
            var report = _engine.ExecuteClean(ConfigManager.Current.DeepGcEnabled);
            if (report.Success)
            {
                string msg = $"[{triggerSource}] Reclaimed {ByteSizeFormatter.Format(report.SavedBytes)} physical RAM";
                OnAutoCleanCompleted?.Invoke(msg);
            }
        }
    }
}