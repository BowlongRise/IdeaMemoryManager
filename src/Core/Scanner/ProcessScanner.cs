using System;
using System.Collections.Generic;
using System.Diagnostics;
using IdeaMemoryManager.Core.Models;

namespace IdeaMemoryManager.Core.Scanner
{
    /// <summary>
    /// Accurately scans and discovers running development processes,
    /// establishing topology for the IDE host and associated microservices/tooling.
    /// </summary>
    public class ProcessScanner
    {
        private static readonly HashSet<string> IdeHostKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "idea", "idea64", "clion", "clion64", "webstorm", "webstorm64", "pycharm", "pycharm64", "goland", "rider"
        };

        private static readonly HashSet<string> JavaKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "java", "javaw"
        };

        private static readonly HashSet<string> NodeKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "node", "esbuild"
        };

        private static readonly HashSet<string> HelperKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "dart", "fsnotifier", "jcef_helper"
        };

        public MemoryStats Scan()
        {
            var stats = new MemoryStats();
            int currentProcessId = Environment.ProcessId;

            var ideaPids = new HashSet<int>();
            var allTargetPids = new HashSet<int>();

            Process[] processes = Process.GetProcesses();

            // 1. Identify primary IDE host processes
            foreach (var p in processes)
            {
                if (p.Id == currentProcessId) continue;
                try
                {
                    string name = p.ProcessName;
                    if (IsIdeHostProcess(name))
                    {
                        ideaPids.Add(p.Id);
                        allTargetPids.Add(p.Id);
                        stats.MainIdeaProcess ??= p;
                    }
                }
                catch { }
            }

            // 2. Discover related Java runtimes, Node.js tooling, and language server daemons
            foreach (var p in processes)
            {
                if (p.Id == currentProcessId) continue;
                try
                {
                    string name = p.ProcessName;
                    ProcessCategory? category = null;

                    if (allTargetPids.Contains(p.Id))
                    {
                        category = ProcessCategory.IdeaHost;
                    }
                    else if (JavaKeywords.Contains(name))
                    {
                        category = ProcessCategory.JavaService;
                    }
                    else if (NodeKeywords.Contains(name))
                    {
                        category = ProcessCategory.NodeFrontend;
                    }
                    else if (HelperKeywords.Contains(name) || name.Contains("jcef", StringComparison.OrdinalIgnoreCase))
                    {
                        category = ProcessCategory.NativeHelper;
                    }

                    if (category.HasValue)
                    {
                        long bytes = p.WorkingSet64;
                        stats.TotalBytes += bytes;

                        switch (category.Value)
                        {
                            case ProcessCategory.IdeaHost:
                                stats.IdeaBytes += bytes;
                                break;
                            case ProcessCategory.JavaService:
                                stats.JavaBytes += bytes;
                                break;
                            case ProcessCategory.NodeFrontend:
                                stats.NodeBytes += bytes;
                                break;
                        }

                        stats.Targets.Add(new ProcessTarget
                        {
                            Id = p.Id,
                            Name = p.ProcessName,
                            Category = category.Value,
                            WorkingSetBytes = bytes,
                            ProcessInstance = p
                        });
                    }
                }
                catch { }
            }

            return stats;
        }

        private static bool IsIdeHostProcess(string name)
        {
            // Exclude our own optimizer executable
            if (name.Contains("cleaner", StringComparison.OrdinalIgnoreCase) || 
                name.Contains("ideamemory", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            foreach (var kw in IdeHostKeywords)
            {
                if (name.Equals(kw, StringComparison.OrdinalIgnoreCase) || name.StartsWith(kw, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}