using System;
using System.Collections.Generic;
using System.Globalization;

namespace IdeaMemoryManager.Common.Localization
{
    public enum AppLanguage
    {
        English,
        Chinese
    }

    public static class I18n
    {
        public static AppLanguage CurrentLanguage { get; set; } = AppLanguage.English;

        public static event Action OnLanguageChanged;

        static I18n()
        {
            CurrentLanguage = CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                ? AppLanguage.Chinese
                : AppLanguage.English;
        }

        public static void SetLanguage(AppLanguage language)
        {
            if (CurrentLanguage != language)
            {
                CurrentLanguage = language;
                OnLanguageChanged?.Invoke();
            }
        }

        public static string T(string key)
        {
            if (Translations.TryGetValue(key, out var dict))
            {
                if (dict.TryGetValue(CurrentLanguage, out var val))
                    return val;
            }
            return key;
        }

        private static readonly Dictionary<string, Dictionary<AppLanguage, string>> Translations = new()
        {
            ["AppTitle"] = new()
            {
                [AppLanguage.English] = "⚡ IdeaMemory",
                [AppLanguage.Chinese] = "⚡ IDEA内存管理"
            },
            ["SubtitleScanning"] = new()
            {
                [AppLanguage.English] = "Monitoring development environment processes...",
                [AppLanguage.Chinese] = "正在接管开发环境及子进程..."
            },
            ["ProcessesCount"] = new()
            {
                [AppLanguage.English] = "Supervising {0} active development processes",
                [AppLanguage.Chinese] = "已接管 {0} 个开发与关联子进程"
            },
            ["IdeaHost"] = new()
            {
                [AppLanguage.English] = "• IDE Host: {0}",
                [AppLanguage.Chinese] = "• IDE 宿主：{0}"
            },
            ["JavaServices"] = new()
            {
                [AppLanguage.English] = "• Java Backend Services: {0}",
                [AppLanguage.Chinese] = "• Java 后端服务：{0}"
            },
            ["NodeServices"] = new()
            {
                [AppLanguage.English] = "• Frontend & Tooling: {0}",
                [AppLanguage.Chinese] = "• Node 前端工具服务：{0}"
            },
            ["DeepGcOption"] = new()
            {
                [AppLanguage.English] = "JVM Deep GC (prevents rebound in seconds)",
                [AppLanguage.Chinese] = "JVM 真实垃圾回收 (防止数秒后反弹)"
            },
            ["SmartThresholdOption"] = new()
            {
                [AppLanguage.English] = "Smart trigger (>3.5 GB threshold auto-clean)",
                [AppLanguage.Chinese] = "智能自愈 (内存超 3.5GB 自动静默优化)"
            },
            ["StatusReady"] = new()
            {
                [AppLanguage.English] = "Ready - click below to reclaim memory instantly",
                [AppLanguage.Chinese] = "就绪 - 点击下方按钮立即释放物理内存"
            },
            ["BtnClean"] = new()
            {
                [AppLanguage.English] = "⚡ Reclaim Physical Memory",
                [AppLanguage.Chinese] = "⚡ 一键极速释放物理内存"
            },
            ["BtnCleaning"] = new()
            {
                [AppLanguage.English] = "⏳ Deep Cleaning in Progress...",
                [AppLanguage.Chinese] = "⏳ 正在深度优化中..."
            },
            ["LifetimeStats"] = new()
            {
                [AppLanguage.English] = "🏆 Lifetime Saved: {0} across {1} cleans",
                [AppLanguage.Chinese] = "🏆 累计已为你释放：{0} (共 {1} 次)"
            },
            ["AutoSchedule"] = new()
            {
                [AppLanguage.English] = "Auto-maintenance",
                [AppLanguage.Chinese] = "自动定时维护"
            },
            ["AutoStart"] = new()
            {
                [AppLanguage.English] = "Start with Windows (silent in tray)",
                [AppLanguage.Chinese] = "开机自启并在后台静默守护"
            },
            ["CleanSuccess"] = new()
            {
                [AppLanguage.English] = "✨ Successfully reclaimed {0} physical RAM!",
                [AppLanguage.Chinese] = "✨ 成功释放 {0} 物理内存！"
            },
            ["StatusCleaning"] = new()
            {
                [AppLanguage.English] = "Running JVM garbage collection & trimming working sets...",
                [AppLanguage.Chinese] = "正在触发 JVM 垃圾回收与工作集压缩..."
            },
            ["TrayCleanNow"] = new()
            {
                [AppLanguage.English] = "⚡ Clean Memory Now",
                [AppLanguage.Chinese] = "⚡ 立即释放内存"
            },
            ["TrayShowWindow"] = new()
            {
                [AppLanguage.English] = "Show Floating Window",
                [AppLanguage.Chinese] = "显示悬浮主窗"
            },
            ["TrayExit"] = new()
            {
                [AppLanguage.English] = "Exit",
                [AppLanguage.Chinese] = "退出程序"
            },
            ["Interval15m"] = new()
            {
                [AppLanguage.English] = "Every 15 min",
                [AppLanguage.Chinese] = "每 15 分钟"
            },
            ["Interval30m"] = new()
            {
                [AppLanguage.English] = "Every 30 min",
                [AppLanguage.Chinese] = "每 30 分钟"
            },
            ["Interval1h"] = new()
            {
                [AppLanguage.English] = "Every 1 hour",
                [AppLanguage.Chinese] = "每 1 小时"
            },
            ["Interval2h"] = new()
            {
                [AppLanguage.English] = "Every 2 hours",
                [AppLanguage.Chinese] = "每 2 小时"
            },
            ["IdleAwareDefer"] = new()
            {
                [AppLanguage.English] = "Active typing/compilation detected, deferred auto-clean...",
                [AppLanguage.Chinese] = "检测到活跃输入或编译，已智能顺延清理..."
            },
            ["ShowTopology"] = new()
            {
                [AppLanguage.English] = "▲ Process Topology ({0})",
                [AppLanguage.Chinese] = "▲ 进程拓扑管理 ({0})"
            },
            ["HideTopology"] = new()
            {
                [AppLanguage.English] = "▼ Collapse Details",
                [AppLanguage.Chinese] = "▼ 收起拓扑详情"
            },
            ["WhitelistAdd"] = new()
            {
                [AppLanguage.English] = "🛡️ Add to Whitelist (Skip clean)",
                [AppLanguage.Chinese] = "🛡️ 加入白名单 (永久跳过清理)"
            },
            ["WhitelistRemove"] = new()
            {
                [AppLanguage.English] = "Remove from Whitelist",
                [AppLanguage.Chinese] = "从白名单中移除"
            },
            ["WhitelistedBadge"] = new()
            {
                [AppLanguage.English] = "[Whitelisted]",
                [AppLanguage.Chinese] = "[已加白]"
            },
            ["JvmTelemetryTitle"] = new()
            {
                [AppLanguage.English] = "🔬 JVM Heap Telemetry",
                [AppLanguage.Chinese] = "🔬 JVM 堆内深度遥测"
            },
            ["Trend60s"] = new()
            {
                [AppLanguage.English] = "Memory Trend (60s)",
                [AppLanguage.Chinese] = "60秒内存走势"
            }
        };
    }
}