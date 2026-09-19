using UnityEngine;

namespace OrbitalEscape.Orbits.Generators
{
    [CreateAssetMenu(fileName = "SpikeConfig", menuName = "Scriptable Objects/Configs/Spike Config")]
    public class SpikeConfig : ScriptableObject
    {
        [field: SerializeField, Range(0.0f, 1.0f)] public float Density { get; private set; }
        [field: SerializeField, Range(0.0f, 1.0f)] public float ConsecutiveSpikeChance { get; private set; }
        [field: SerializeField] public float SpikeArcLength { get; private set; }
    }
}
