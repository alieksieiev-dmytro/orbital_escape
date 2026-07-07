using System;
using System.Collections.Generic;
using DG.Tweening;
using Zenject;

namespace OrbitalEscape.Movement
{
    public sealed class OrbitJumpTransitionService : IOrbitJumpTransitionService, IDisposable
    {
        private readonly IMovementModel _movementModel;
        private Sequence _sequence;

        public bool IsTransitioning => _sequence != null;

        [Inject]
        public OrbitJumpTransitionService(IMovementModel movementModel)
        {
            _movementModel = movementModel;
        }

        public void Play(OrbitJumpStep step, Action onComplete = null)
        {
            Play(new[] { step }, onComplete);
        }

        public void Play(IReadOnlyList<OrbitJumpStep> steps, Action onComplete = null)
        {
            Stop();

            if (steps == null || steps.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            _sequence = DOTween.Sequence();
            bool hasTween = false;

            for (int i = 0; i < steps.Count; i++)
            {
                OrbitJumpStep step = steps[i];
                if (step.Duration <= 0.0f)
                {
                    _movementModel.Radius = step.TargetRadius;
                    continue;
                }

                _sequence.Append(DOTween.To(
                        () => _movementModel.Radius,
                        radius => _movementModel.Radius = radius,
                        step.TargetRadius,
                        step.Duration)
                    .SetEase(step.Ease));
                hasTween = true;
            }

            if (!hasTween)
            {
                _sequence.Kill();
                _sequence = null;
                onComplete?.Invoke();
                return;
            }

            _sequence.OnComplete(() =>
            {
                _sequence = null;
                onComplete?.Invoke();
            });
            _sequence.OnKill(() => _sequence = null);
            _sequence.Play();
        }

        public void Stop()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
