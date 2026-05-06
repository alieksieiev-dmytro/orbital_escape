using UnityEngine;

namespace OrbitalEscape.Orbits.Generators
{
    [CreateAssetMenu(fileName = "ImpulseTrapConfig", menuName = "Scriptable Objects/Configs/Impulse Trap Config")]
    public class ImpulseTrapConfig : ScriptableObject
    {
        [field: SerializeField, Range(0.0f, 1.0f)] public float Density { get; private set; }
        [field: SerializeField] public float TrapArcLength { get; private set; }
        [field: SerializeField] public float Height { get; private set; }
    }
}
