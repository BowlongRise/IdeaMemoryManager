using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using IdeaMemoryManager.Core.Models;

namespace IdeaMemoryManager.Core.Telemetry
{
    /// <summary>
    /// Telemetry collector retrieving JVM internal heap allocations and metaspace sizes via jcmd.
    /// </summary>
    public static class JvmTelemetryService
    {
        private static readonly Regex TotalUsedRegex = new(@"total\s+(\d+)([KMGkmg]),\s+used\s+(\d+)([KMGkmg])", RegexOptions.Compiled);
        private static readonly Regex MetaspaceRegex = new(@"Metaspace\s+used\s+(\d+)([KMGkmg])", RegexOptions.Compiled);

        public static JvmHeapInfo CollectHeapInfo(int pid, string jcmdPath)
        {
            var info = new JvmHeapInfo { Pid = pid };
            if (string.IsNullOrEmpty(jcmdPath) || !System.IO.File.Exists(jcmdPath))
                return info;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = jcmdPath,
                    Arguments = $"{pid} GC.heap_info",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return info;

                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(1000);

                if (string.IsNullOrEmpty(output)) return info;

                var matchHeap = TotalUsedRegex.Match(output);
                if (matchHeap.Success)
                {
                    info.EdenCapacityBytes = ConvertToBytes(matchHeap.Groups[1].Value, matchHeap.Groups[2].Value);
                    info.EdenUsedBytes = ConvertToBytes(matchHeap.Groups[3].Value, matchHeap.Groups[4].Value);
                    info.Available = true;
                }

                var matchMeta = MetaspaceRegex.Match(output);
                if (matchMeta.Success)
                {
                    info.MetaspaceUsedBytes = ConvertToBytes(matchMeta.Groups[1].Value, matchMeta.Groups[2].Value);
                    info.Available = true;
                }
            }
            catch
            {
                // Process may have exited or insufficient permissions
            }

            return info;
        }

        private static long ConvertToBytes(string numStr, string unit)
        {
            if (!long.TryParse(numStr, out long value)) return 0;
            return unit.ToUpperInvariant() switch
            {
                "G" => value * 1024L * 1024L * 1024L,
                "M" => value * 1024L * 1024L,
                "K" => value * 1024L,
                _ => value
            };
        }
    }
}