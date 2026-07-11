using UnityEngine;

namespace OrbitalEscape.Pooling
{
    [CreateAssetMenu(fileName = "ObjectPoolConfig", menuName = "Scriptable Objects/Configs/Object Pool Config")]
    public sealed class ObjectPoolConfig : ScriptableObject
    {
        [field: SerializeField, Min(0)] public int SpikePrewarmCount { get; private set; } = 64;
    }
}
