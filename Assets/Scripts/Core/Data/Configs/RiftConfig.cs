using UnityEngine;

namespace OrbitalEscape
{
    [CreateAssetMenu(fileName = "RiftConfig", menuName = "Scriptable Objects/Configs/Rift Config")]
    public class RiftConfig : ScriptableObject
    {
        [field: SerializeField] public float MinDistanceBetweenRifts { get; private set; }
        [field: SerializeField] public float RiftLength { get; private set; }
        [field: SerializeField] public float RiftCount { get; private set; }
        [field: SerializeField] public float RiftPaddings { get; private set; }
        [field: SerializeField, Range(0.0f, 1.0f)] public float AuroraRiftAlphaMultiplier { get; private set; } = 0.25f;
        [field: SerializeField, Min(1)] public int MaxPlacementAttempts { get; private set; } = 128;
    }
}
