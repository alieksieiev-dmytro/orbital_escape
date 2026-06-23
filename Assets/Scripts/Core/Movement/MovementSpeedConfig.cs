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

        [Serializable]
        public struct DifficultyMovementSpeedProfile
        {
            [SerializeField] private DifficultyType _difficulty;
            [SerializeField, Min(0.0f)] private float _baseLinearSpeed;
            [SerializeField, Min(0.0f)] private float _speedIncreasePerScore;

            public DifficultyType Difficulty => _difficulty;

            public static DifficultyMovementSpeedProfile Default => new DifficultyMovementSpeedProfile
            {
                _difficulty = DifficultyType.Basic,
                _baseLinearSpeed = 5.0f,
                _speedIncreasePerScore = 0.03f
            };

            public float Evaluate(float score)
            {
                return BaseLinearSpeed + SpeedIncreasePerScore * Mathf.Max(0.0f, score);
            }

            public float EvaluateMultiplier(float score)
            {
                return Evaluate(score) / Mathf.Max(0.01f, BaseLinearSpeed);
            }

            private float BaseLinearSpeed => _baseLinearSpeed > 0.0f
                ? _baseLinearSpeed
                : Default._baseLinearSpeed;

            private float SpeedIncreasePerScore => _speedIncreasePerScore > 0.0f
                ? _speedIncreasePerScore
                : Default._speedIncreasePerScore;
        }
    }
}
