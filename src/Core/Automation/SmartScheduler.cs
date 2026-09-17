using System;
using System.Windows.Forms;
using IdeaMemoryManager.Common;
using IdeaMemoryManager.Common.Localization;
using IdeaMemoryManager.Config;
using IdeaMemoryManager.Core.Engine;

namespace IdeaMemoryManager.Core.Automation
{
    /// <summary>
    /// Intelligent background scheduler providing periodic timer checks
    /// and adaptive high-watermark threshold self-healing.
    /// </summary>
    public class SmartScheduler
    {
        private readonly MemoryCleanEngine _engine;
        private readonly System.Windows.Forms.Timer _timer;
        private int _highWatermarkStreak = 0;

        public event Action<string> OnAutoCleanCompleted;

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

            // Polling interval: 60s for threshold tracking, or interval minutes for periodic timer
            if (config.SmartThresholdEnabled)
            {
                _timer.Interval = 60 * 1000;
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
                        _highWatermarkStreak = 0;
                        TriggerSilentClean("Smart Threshold");
                    }
                }
                else
                {
                    _highWatermarkStreak = 0;
                }
            }
            else if (config.AutoCleanEnabled)
            {
                TriggerSilentClean("Scheduled");
            }
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