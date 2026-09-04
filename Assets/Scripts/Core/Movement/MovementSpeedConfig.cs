using OrbitalEscape.Difficulty;
using UnityEngine;

namespace OrbitalEscape.Movement
{
    [CreateAssetMenu(fileName = "MovementSpeedConfig", menuName = "Scriptable Objects/Configs/Movement Speed Config")]
    public sealed class MovementSpeedConfig : ScriptableObject
    {
        [SerializeField] private DifficultyMovementSpeedProfile[] _difficultyProfiles;
        [SerializeField, Min(0.0f)] private float _directionChangeDuration = 0.08f;

        public float DirectionChangeDuration => _directionChangeDuration;

        public float Evaluate(DifficultyType difficulty, float score)
        {
            return GetProfile(difficulty).Evaluate(score);
        }

        public float EvaluateSpeedMultiplier(DifficultyType difficulty, float score)
        {
            return GetProfile(difficulty).EvaluateMultiplier(score);
        }

        private DifficultyMovementSpeedProfile GetProfile(DifficultyType difficulty)
        {
            if (_difficultyProfiles != null)
            {
                for (int i = 0; i < _difficultyProfiles.Length; i++)
                {
                    if (_difficultyProfiles[i].Difficulty == difficulty)
                    {
                        return _difficultyProfiles[i];
                    }
                }

                if (_difficultyProfiles.Length > 0)
                {
                    return _difficultyProfiles[0];
                }
            }

            return DifficultyMovementSpeedProfile.Default;
        }
    }
}
