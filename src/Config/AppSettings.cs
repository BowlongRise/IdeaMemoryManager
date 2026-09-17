using System;
using System.Collections.Generic;
using IdeaMemoryManager.Common.Localization;

namespace IdeaMemoryManager.Config
{
    /// <summary>
    /// Strongly-typed application settings and user preferences.
    /// </summary>
    public class AppSettings
    {
        public bool AutoCleanEnabled { get; set; } = false;
        public int IntervalMinutes { get; set; } = 30;
        public bool DeepGcEnabled { get; set; } = true;
        public bool SmartThresholdEnabled { get; set; } = false;
        public double ThresholdGb { get; set; } = 3.5;
        public bool AutoStartWithWindows { get; set; } = false;
        public bool StartMinimized { get; set; } = false;
        public bool IsPinned { get; set; } = true;
        public long TotalSavedBytes { get; set; } = 0;
        public int TotalCleanCount { get; set; } = 0;
        public AppLanguage Language { get; set; } = AppLanguage.English;

        public bool IdleAwareEnabled { get; set; } = true;
        public int IdleThresholdSeconds { get; set; } = 25;
        public HashSet<string> Whitelist { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}