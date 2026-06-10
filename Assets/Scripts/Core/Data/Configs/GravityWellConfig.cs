using UnityEngine;

namespace OrbitalEscape.Orbits.Generators
{
    [CreateAssetMenu(fileName = "GravityWellConfig", menuName = "Scriptable Objects/Configs/Gravity Well Config")]
    public class GravityWellConfig : ScriptableObject
    {
        [field: SerializeField, Range(0.0f, 1.0f)] public float Density { get; private set; } = 0.03f;
        [field: SerializeField, Min(0.01f)] public float FieldArcLength { get; private set; } = 3.0f;
        [field: SerializeField, Range(0.0f, 1.0f)] public float PartialJumpStep { get; private set; } = 0.333f;
        [field: SerializeField, Min(0.0f)] public float PullSpeed { get; private set; } = 2.5f;
        [field: SerializeField, Min(0.01f)] public float ReturnDuration { get; private set; } = 0.4f;
        [field: SerializeField, Range(0.0f, 1.0f)] public float VisualAlpha { get; private set; } = 0.25f;
    }
}
