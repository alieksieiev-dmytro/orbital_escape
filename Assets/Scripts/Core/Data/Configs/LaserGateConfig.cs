using UnityEngine;

namespace OrbitalEscape.Orbits.Generators
{
    [CreateAssetMenu(fileName = "LaserGateConfig", menuName = "Scriptable Objects/Configs/Laser Gate Config")]
    public sealed class LaserGateConfig : ScriptableObject
    {
        [field: SerializeField, Min(0.01f)] public float Width { get; private set; } = 0.7f;
        [field: SerializeField, Min(0.0f)] public float PassTime { get; private set; } = 1.25f;
        [field: SerializeField, Min(0.0f)] public float ChargingTime { get; private set; } = 0.5f;
        [field: SerializeField, Min(0.0f)] public float ActiveTime { get; private set; } = 0.75f;
        [field: SerializeField] public float LowerBaseYOffset { get; private set; } = -0.2285f;
        [field: SerializeField] public float UpperBaseYOffset { get; private set; } = 0.126f;
    }
}
