using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using IdeaMemoryManager.Interop;

namespace IdeaMemoryManager.Core.Automation
{
    /// <summary>
    /// Detects user inactivity and process workload to ensure background optimization
    /// never causes Stop-The-World (STW) latency during active coding or compilation.
    /// </summary>
    public static class IdleDetector
    {
        public static double GetUserIdleSeconds()
        {
            var lii = new NativeMethods.LASTINPUTINFO();
            lii.cbSize = (uint)Marshal.SizeOf(lii);

            if (NativeMethods.GetLastInputInfo(ref lii))
            {
                uint currentTick = (uint)Environment.TickCount;
                uint elapsedMs = currentTick >= lii.dwTime ? (currentTick - lii.dwTime) : 0;
                return elapsedMs / 1000.0;
            }

            return 0;
        }

        public static bool IsUserIdle(int thresholdSeconds)
        {
            return GetUserIdleSeconds() >= thresholdSeconds;
        }

        public static bool IsProcessCpuBusy(Process process, double cpuLimitPercent = 25.0)
        {
            if (process == null) return false;
            try
            {
                if (process.HasExited) return false;
                
                // Sample process CPU delta across 150ms
                TimeSpan startCpu = process.TotalProcessorTime;
                DateTime startTime = DateTime.UtcNow;
                System.Threading.Thread.Sleep(150);
                TimeSpan endCpu = process.TotalProcessorTime;
                DateTime endTime = DateTime.UtcNow;

                double cpuUsedMs = (endCpu - startCpu).TotalMilliseconds;
                double totalMsPassed = (endTime - startTime).TotalMilliseconds * Environment.ProcessorCount;
                double cpuUsage = (cpuUsedMs / totalMsPassed) * 100.0;

                return cpuUsage > cpuLimitPercent;
            }
            catch
            {
                return false;
            }
        }
    }
}