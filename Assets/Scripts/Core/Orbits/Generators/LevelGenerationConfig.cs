using UnityEngine;

namespace OrbitalEscape.Orbits.Generators
{
    [CreateAssetMenu(fileName = "LevelGenerationConfig", menuName = "Scriptable Objects/Configs/Level Generation Config")]
    public class LevelGenerationConfig : ScriptableObject
    {
        [field: SerializeField, Min(2)] public int InitialOrbitCount { get; private set; } = 2;
        [field: SerializeField, Min(1)] public int OrbitMultiplier { get; private set; } = 2;
        [field: SerializeField, Min(1)] public int MaxOrbitsPerGeneration { get; private set; } = 8;
        [field: SerializeField, Min(0)] public int FirstOrbitIndexWithContent { get; private set; } = 1;

        public bool ShouldGenerateContent(Orbit orbit)
        {
            return orbit.Index >= FirstOrbitIndexWithContent;
        }

        public int GetExtensionCount(int existingOrbitCount)
        {
            int requestedCount = existingOrbitCount * OrbitMultiplier - existingOrbitCount;
            return Mathf.Clamp(requestedCount, 0, MaxOrbitsPerGeneration);
        }
    }
}
