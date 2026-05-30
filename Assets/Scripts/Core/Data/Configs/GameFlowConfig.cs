using UnityEngine;

namespace OrbitalEscape
{
    [CreateAssetMenu(fileName = "GameFlowConfig", menuName = "Scriptable Objects/Configs/Game Flow Config")]
    public class GameFlowConfig : ScriptableObject
    {
        [field: SerializeField] public bool SkipMainMenu { get; private set; }
    }
}
