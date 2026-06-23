using UnityEngine;
using Zenject;

namespace OrbitalEscape.Movement
{
    public sealed class ScoreJumpSpeedService : IJumpSpeedService
    {
        private const float MIN_DURATION = 0.01f;

        private readonly IMovementSpeedService _movementSpeedService;

        [Inject]
        public ScoreJumpSpeedService(IMovementSpeedService movementSpeedService)
        {
            _movementSpeedService = movementSpeedService;
        }

        public float ScaleDuration(float baseDuration)
        {
            float speedMultiplier = Mathf.Max(MIN_DURATION, _movementSpeedService.CurrentSpeedMultiplier);
            return Mathf.Max(MIN_DURATION, Mathf.Max(0.0f, baseDuration) / speedMultiplier);
        }
    }
}
