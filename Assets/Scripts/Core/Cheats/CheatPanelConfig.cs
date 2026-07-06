using UnityEngine;

namespace OrbitalEscape.Cheats
{
    [CreateAssetMenu(fileName = "CheatPanelConfig", menuName = "Scriptable Objects/Configs/Cheat Panel Config")]
    public sealed class CheatPanelConfig : ScriptableObject
    {
        [field: SerializeField] public bool CheatsEnabled { get; private set; } = true;
        [field: SerializeField] public bool InfoPanelEnabled { get; private set; } = true;
        [field: SerializeField] public bool CommandPanelEnabled { get; private set; } = true;

        public bool ShouldShowInfoPanel => CheatsEnabled && InfoPanelEnabled;
        public bool ShouldShowCommandPanel => CheatsEnabled && CommandPanelEnabled;
    }
}
