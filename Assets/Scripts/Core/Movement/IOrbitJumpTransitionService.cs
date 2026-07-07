using System;
using System.Collections.Generic;

namespace OrbitalEscape.Movement
{
    public interface IOrbitJumpTransitionService
    {
        bool IsTransitioning { get; }
        void Play(OrbitJumpStep step, Action onComplete = null);
        void Play(IReadOnlyList<OrbitJumpStep> steps, Action onComplete = null);
        void Stop();
    }
}
