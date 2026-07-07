using System;
using OrbitalEscape.Difficulty;
using UnityEngine;

namespace OrbitalEscape.Movement
{
    [Serializable]
    public struct DifficultyMovementSpeedProfile
    {
        [SerializeField] private DifficultyType _difficulty;
        [SerializeField, Min(0.0f)] private float _baseLinearSpeed;
        [SerializeField, Min(0.0f)] private float _speedIncreasePerScore;

        public DifficultyType Difficulty => _difficulty;

        public static DifficultyMovementSpeedProfile Default => new DifficultyMovementSpeedProfile
        {
            _difficulty = DifficultyType.Easy,
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
