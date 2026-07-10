using UnityEngine;

namespace OrbitalEscape.Cheats
{
    [CreateAssetMenu(fileName = "CheatPanelConfig", menuName = "Scriptable Objects/Configs/Cheat Panel Config")]
    public sealed class CheatPanelConfig : ScriptableObject
    {
        [field: SerializeField] public bool CheatsEnabled { get; private set; } = true;
        [field: SerializeField] public bool InfoPanelEnabled { get; private set; } = true;
        [field: SerializeField] public bool CommandPanelEnabled { get; private set; } = true;
        [field: SerializeField] public bool ProfilingEnabled { get; private set; } = true;
        [field: SerializeField, Min(0.1f)] public float ProfilerMemorySampleIntervalSeconds { get; private set; } = 0.5f;
        [field: SerializeField] public string ProfilerReportsFolderName { get; private set; } = "ProfilingReports";

        public bool ShouldShowInfoPanel => CheatsEnabled && InfoPanelEnabled;
        public bool ShouldShowCommandPanel => CheatsEnabled && CommandPanelEnabled;
        public bool ShouldShowProfiling => CheatsEnabled && CommandPanelEnabled && ProfilingEnabled;
    }
}
