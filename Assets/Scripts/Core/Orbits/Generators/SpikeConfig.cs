using UnityEngine;

namespace OrbitalEscape.Orbits.Generators
{
    [CreateAssetMenu(fileName = "SpikeConfig", menuName = "Scriptable Objects/Configs/Spike Config")]
    public class SpikeConfig : ScriptableObject
    {
        [field: SerializeField] public int BaseSpikeCount { get; private set; }
        [field: SerializeField] public int AdditionalSpikeCountPerOrbit { get; private set; }
        [field: SerializeField] public float MinSpikeHeight { get; private set; }
        [field: SerializeField] public float MaxSpikeHeight { get; private set; }
        [field: SerializeField] public float SpikeArcLength { get; private set; }
    }
}
