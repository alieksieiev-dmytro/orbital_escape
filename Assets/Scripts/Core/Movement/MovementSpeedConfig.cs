using System;
using OrbitalEscape.Difficulty;
using UnityEngine;

namespace OrbitalEscape.Movement
{
    [CreateAssetMenu(fileName = "MovementSpeedConfig", menuName = "Scriptable Objects/Configs/Movement Speed Config")]
    public sealed class MovementSpeedConfig : ScriptableObject
    {
        [SerializeField] private DifficultyMovementSpeedProfile[] _difficultyProfiles;

        public float Evaluate(DifficultyType difficulty, float score)
        {
            return GetProfile(difficulty).Evaluate(score);
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

        [Serializable]
        public struct DifficultyMovementSpeedProfile
        {
            [SerializeField] private DifficultyType _difficulty;
            [SerializeField, Min(0.0f)] private float _baseLinearSpeed;
            [SerializeField, Min(0.0f)] private float _speedIncreasePerScore;
            [SerializeField, Min(1.0f)] private float _growthModifier;

            public DifficultyType Difficulty => _difficulty;

            public static DifficultyMovementSpeedProfile Default => new DifficultyMovementSpeedProfile
            {
                _difficulty = DifficultyType.Basic,
                _baseLinearSpeed = 5.0f,
                _speedIncreasePerScore = 0.03f,
                _growthModifier = 1.02f
            };

            public float Evaluate(float score)
            {
                return BaseLinearSpeed + SpeedIncreasePerScore * EvaluateGeometricProgression(score);
            }

            private float BaseLinearSpeed => _baseLinearSpeed > 0.0f
                ? _baseLinearSpeed
                : Default._baseLinearSpeed;

            private float SpeedIncreasePerScore => _speedIncreasePerScore > 0.0f
                ? _speedIncreasePerScore
                : Default._speedIncreasePerScore;

            private float GrowthModifier => _growthModifier >= 1.0f
                ? _growthModifier
                : Default._growthModifier;

            private float EvaluateGeometricProgression(float score)
            {
                float scoreSteps = Mathf.Max(0.0f, score);
                if (Mathf.Approximately(GrowthModifier, 1.0f))
                {
                    return scoreSteps;
                }

                return (Mathf.Pow(GrowthModifier, scoreSteps) - 1.0f) / (GrowthModifier - 1.0f);
            }
        }
    }
}
