using UnityEngine;

namespace OrbitalEscape
{
    [CreateAssetMenu(fileName = "RiftConfig", menuName = "Scriptable Objects/Configs/Rift Config")]
    public class RiftConfig : ScriptableObject
    {
        [field: SerializeField] public float MinDistanceBetweenRifts { get; private set; }
        [field: SerializeField] public float RiftLength { get; private set; }
        [field: SerializeField] public float RiftCount { get; private set; }
        [field: SerializeField, Min(0.01f)] public float BaseRiftCountRadius { get; private set; } = 20.0f;
        [field: SerializeField, Min(0.01f)] public float AdditionalRiftRadiusStep { get; private set; } = 15.0f;
        [field: SerializeField, Min(0.0f)] public float RiftWeightIncreasePerNonRiftPattern { get; private set; } = 0.5f;
        [field: SerializeField, Min(1.0f)] public float MaxRiftPressureMultiplier { get; private set; } = 4.0f;
        [field: SerializeField, Range(0.01f, 180.0f)] public float NeighborRiftAvoidanceAngle { get; private set; } = 75.0f;
        [field: SerializeField, Range(0.01f, 1.0f)] public float AlignedNeighborRiftWeightMultiplier { get; private set; } = 0.25f;
        [field: SerializeField] public float RiftPaddings { get; private set; }
        [field: SerializeField, Range(0.0f, 1.0f)] public float AuroraRiftAlphaMultiplier { get; private set; } = 0.25f;
        [field: SerializeField, Min(1)] public int MaxPlacementAttempts { get; private set; } = 128;
    }
}
