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
            [SerializeField, Min(0.0f)] private float _scoreToProgressMultiplier;
            [SerializeField] private AnimationCurve _speedMultiplierCurve;

            public DifficultyType Difficulty => _difficulty;

            public static DifficultyMovementSpeedProfile Default => new DifficultyMovementSpeedProfile
            {
                _difficulty = DifficultyType.Basic,
                _baseLinearSpeed = 5.0f,
                _scoreToProgressMultiplier = 0.02f,
                _speedMultiplierCurve = DefaultMultiplierCurve()
            };

            public float Evaluate(float score)
            {
                float progress = Mathf.Clamp01(Mathf.Max(0.0f, score) * _scoreToProgressMultiplier);
                float multiplier = Mathf.Max(0.0f, SpeedMultiplierCurve.Evaluate(progress));
                return BaseLinearSpeed * multiplier;
            }

            private float BaseLinearSpeed => _baseLinearSpeed > 0.0f
                ? _baseLinearSpeed
                : Default._baseLinearSpeed;

            private AnimationCurve SpeedMultiplierCurve => _speedMultiplierCurve ?? DefaultMultiplierCurve();

            private static AnimationCurve DefaultMultiplierCurve()
            {
                return AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.4f);
            }
        }
    }
}
