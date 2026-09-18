using UnityEngine;
using UnityEngine.Serialization;

namespace OrbitalEscape
{
    [CreateAssetMenu(fileName = "RiftConfig", menuName = "Scriptable Objects/Configs/Rift Config")]
    public class RiftConfig : ScriptableObject
    {
        [field: SerializeField] public float RiftLength { get; private set; }
        [field: SerializeField] public float RiftPaddings { get; private set; }
        [field: Tooltip("Minimum empty distance kept from each rift edge when placing obstacles on either side of its orbit.")]
        [field: SerializeField, Min(0.0f)] public float ObstacleClearance { get; private set; } = 0.5f;
        [field: SerializeField] public float RiftCount { get; private set; }
        [field: SerializeField, Min(0.01f)] public float BaseRiftCountRadius { get; private set; } = 20.0f;
        [field: SerializeField, Min(0.01f)] public float MinCenterDistanceBetweenRifts { get; private set; } = 26.18f;
        [field: SerializeField, Min(0.01f)] public float AdditionalRiftRadiusStep { get; private set; } = 15.0f;
        [field: FormerlySerializedAs("<NeighborRiftAvoidanceDistance>k__BackingField")]
        [field: SerializeField, Min(0.01f)] public float CrossOrbitSafeDistance { get; private set; } = 12.0f;
        [field: SerializeField, Min(1)] public int CrossOrbitLookback { get; private set; } = 3;
        [field: FormerlySerializedAs("<AlignedNeighborRiftWeightMultiplier>k__BackingField")]
        [field: SerializeField, Range(0.01f, 1.0f)] public float ConsecutiveAlignmentWeightMultiplier { get; private set; } = 0.25f;
        [field: SerializeField, Range(0.0f, 1.0f)] public float AuroraRiftAlphaMultiplier { get; private set; } = 0.25f;
        [field: SerializeField, Min(1)] public int MaxPlacementAttempts { get; private set; } = 128;
    }
}
