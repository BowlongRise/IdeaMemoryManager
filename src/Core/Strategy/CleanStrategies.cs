using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using IdeaMemoryManager.Common;
using IdeaMemoryManager.Core.Models;
using IdeaMemoryManager.Interop;

namespace IdeaMemoryManager.Core.Strategy
{
    public interface ICleanStrategy
    {
        string Name { get; }
        void Execute(MemoryStats stats);
    }

    /// <summary>
    /// Triggers JVM Full GC via jcmd to collect dead heap objects and prevent memory bounce-back.
    /// </summary>
    public class JvmGcStrategy : ICleanStrategy
    {
        public string Name => "JVM Deep Garbage Collection";
        private string cachedJcmdPath;

        public void Execute(MemoryStats stats)
        {
            string jcmdPath = ResolveJcmdPath(stats.MainIdeaProcess);
            if (string.IsNullOrEmpty(jcmdPath) || !File.Exists(jcmdPath))
            {
                Logger.Warn("jcmd executable not located, skipping deep JVM GC strategy.");
                return;
            }

            foreach (var target in stats.Targets)
            {
                if (target.Category == ProcessCategory.IdeaHost || target.Category == ProcessCategory.JavaService)
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = jcmdPath,
                            Arguments = $"{target.Id} GC.run",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var proc = Process.Start(psi);
                        proc?.WaitForExit(1500);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"Failed to invoke jcmd GC on PID {target.Id}: {ex.Message}");
                    }
                }
            }
            Thread.Sleep(300);
        }

        private string ResolveJcmdPath(Process ideaProc)
        {
            if (!string.IsNullOrEmpty(cachedJcmdPath) && File.Exists(cachedJcmdPath))
                return cachedJcmdPath;

            try
            {
                // Dynamic resolution relative to the IDE runtime: <IDE_HOME>/jbr/bin/jcmd.exe
                if (ideaProc != null)
                {
                    string ideaExe = ideaProc.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(ideaExe))
                    {
                        string ideaHome = Path.GetDirectoryName(Path.GetDirectoryName(ideaExe));
                        string jcmdCandidate = Path.Combine(ideaHome, "jbr", "bin", "jcmd.exe");
                        if (File.Exists(jcmdCandidate))
                        {
                            cachedJcmdPath = jcmdCandidate;
                            return cachedJcmdPath;
                        }
                    }
                }
            }
            catch { }

            // Fallback to JAVA_HOME environment variable
            string javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrEmpty(javaHome))
            {
                string candidate = Path.Combine(javaHome, "bin", "jcmd.exe");
                if (File.Exists(candidate))
                {
                    cachedJcmdPath = candidate;
                    return cachedJcmdPath;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Safely trims resident physical memory working sets via Windows Kernel API.
    /// </summary>
    public class WorkingSetTrimStrategy : ICleanStrategy
    {
        public string Name => "Kernel WorkingSet Trim";

        public void Execute(MemoryStats stats)
        {
            foreach (var target in stats.Targets)
            {
                try
                {
                    var p = target.ProcessInstance;
                    if (p != null && !p.HasExited)
                    {
                        IntPtr handle = p.Handle;
                        NativeMethods.EmptyWorkingSet(handle);
                        NativeMethods.SetProcessWorkingSetSize(handle, (IntPtr)(-1), (IntPtr)(-1));
                    }
                }
                catch { }
            }
            Thread.Sleep(200);
        }
    }
}