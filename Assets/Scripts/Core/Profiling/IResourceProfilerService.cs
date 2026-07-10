using System;

namespace OrbitalEscape.Profiling
{
    public interface IResourceProfilerService
    {
        bool IsProfiling { get; }
        string LastReportPath { get; }
        event Action<bool> ProfilingStateChanged;

        ResourceProfilerResult StartProfiling();
        ResourceProfilerResult StopProfiling();
    }
}
