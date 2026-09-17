using System.Collections.Generic;
using System.Diagnostics;

namespace IdeaMemoryManager.Core.Models
{
    public enum ProcessCategory
    {
        IdeaHost,
        JavaService,
        NodeFrontend,
        NativeHelper
    }

    public class ProcessTarget
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ProcessCategory Category { get; set; }
        public long WorkingSetBytes { get; set; }
        public Process ProcessInstance { get; set; }
        public bool IsWhitelisted { get; set; }
        public bool IsSelected { get; set; } = true;
    }

    public class JvmHeapInfo
    {
        public int Pid { get; set; }
        public long EdenUsedBytes { get; set; }
        public long EdenCapacityBytes { get; set; }
        public long OldGenUsedBytes { get; set; }
        public long OldGenCapacityBytes { get; set; }
        public long MetaspaceUsedBytes { get; set; }
        public long MetaspaceCapacityBytes { get; set; }
        public bool Available { get; set; }
    }

    public class MemoryStats
    {
        public long TotalBytes { get; set; }
        public long IdeaBytes { get; set; }
        public long JavaBytes { get; set; }
        public long NodeBytes { get; set; }
        public int TotalProcesses => Targets.Count;
        public List<ProcessTarget> Targets { get; set; } = new();
        public List<JvmHeapInfo> JvmHeaps { get; set; } = new();
        public Process MainIdeaProcess { get; set; }
    }

    public class CleanReport
    {
        public long BeforeBytes { get; set; }
        public long AfterBytes { get; set; }
        public long SavedBytes => BeforeBytes > AfterBytes ? BeforeBytes - AfterBytes : 0;
        public long ElapsedMilliseconds { get; set; }
        public int AffectedProcesses { get; set; }
        public bool JvmGcTriggered { get; set; }
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; }
    }
}