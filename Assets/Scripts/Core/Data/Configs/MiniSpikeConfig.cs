using UnityEngine;

namespace OrbitalEscape.Orbits.Generators
{
    [CreateAssetMenu(fileName = "MiniSpikeConfig", menuName = "Scriptable Objects/Configs/Mini Spike Config")]
    public class MiniSpikeConfig : ScriptableObject
    {
        [field: SerializeField] public float Height { get; private set; }
        [field: SerializeField] public float SpikeArcLength { get; private set; }
    }
}
