using OrbitalEscape.Difficulty;
using UnityEngine;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    [CreateAssetMenu(
        fileName = "ObstaclePatternGenerationCatalog",
        menuName = "Scriptable Objects/Configs/Obstacle Pattern Generation Catalog")]
    public sealed class ObstaclePatternGenerationCatalog : ScriptableObject
    {
        [SerializeField] private ObstaclePatternGenerationConfig _easy;
        [SerializeField] private ObstaclePatternGenerationConfig _medium;
        [SerializeField] private ObstaclePatternGenerationConfig _hard;
        [SerializeField] private ObstaclePatternGenerationConfig _impossible;
        [SerializeField] private ObstaclePatternGenerationConfig _nightmare;

        public ObstaclePatternGenerationConfig GetConfig(DifficultyType difficulty)
        {
            ObstaclePatternGenerationConfig config = difficulty switch
            {
                DifficultyType.Medium => _medium,
                DifficultyType.Hard => _hard,
                DifficultyType.Impossible => _impossible,
                DifficultyType.Nightmare => _nightmare,
                _ => _easy
            };

            if (config == null)
            {
                throw new System.InvalidOperationException(
                    $"No obstacle pattern generation config is assigned for {difficulty}.");
            }

            return config;
        }
    }
}
