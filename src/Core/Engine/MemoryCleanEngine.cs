using System;
using System.Diagnostics;
using IdeaMemoryManager.Common;
using IdeaMemoryManager.Config;
using IdeaMemoryManager.Core.Models;
using IdeaMemoryManager.Core.Scanner;
using IdeaMemoryManager.Core.Strategy;
using IdeaMemoryManager.Core.Telemetry;
using IdeaMemoryManager.Interop;

namespace IdeaMemoryManager.Core.Engine
{
    /// <summary>
    /// Unified orchestrator managing process discovery, strategy invocation, and metrics telemetry.
    /// </summary>
    public class MemoryCleanEngine
    {
        private readonly ProcessScanner _scanner = new();
        private readonly JvmGcStrategy _jvmGcStrategy = new();
        private readonly WorkingSetTrimStrategy _trimStrategy = new();

        public MemoryStats GetCurrentStats() => _scanner.Scan();

        public CleanReport ExecuteClean(bool enableDeepGc)
        {
            var sw = Stopwatch.StartNew();
            var report = new CleanReport();

            try
            {
                var beforeStats = _scanner.Scan();
                report.BeforeBytes = beforeStats.TotalBytes;
                report.AffectedProcesses = beforeStats.TotalProcesses;

                // 1. JVM Deep Garbage Collection (optional, prevents rebound)
                if (enableDeepGc)
                {
                    report.JvmGcTriggered = true;
                    _jvmGcStrategy.Execute(beforeStats);
                }

                // 2. Kernel working set page trimming
                _trimStrategy.Execute(beforeStats);

                // 3. Telemetry and metrics aggregation
                var afterStats = _scanner.Scan();
                report.AfterBytes = afterStats.TotalBytes;
                sw.Stop();
                report.ElapsedMilliseconds = sw.ElapsedMilliseconds;

                if (report.SavedBytes > 0)
                {
                    ConfigManager.RecordSavedBytes(report.SavedBytes);
                }

                // 4. Zero Self-Footprint: Trim own working set immediately after optimization
                TriggerSelfTrim();

                Logger.Info($"Optimized {report.AffectedProcesses} processes. Saved: {ByteSizeFormatter.Format(report.SavedBytes)} in {report.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                report.Success = false;
                report.ErrorMessage = ex.Message;
                Logger.Error("Execution failure during memory optimization", ex);
            }

            return report;
        }

        public JvmHeapInfo QueryMainJvmHeap(Process ideaProc)
        {
            if (ideaProc == null) return new JvmHeapInfo();
            string jcmd = _jvmGcStrategy.GetResolvedJcmdPath(ideaProc);
            return JvmTelemetryService.CollectHeapInfo(ideaProc.Id, jcmd);
        }

        public void TriggerSelfTrim()
        {
            try
            {
                NativeMethods.EmptyWorkingSet(Process.GetCurrentProcess().Handle);
            }
            catch { }
        }
    }
}