using System;
using OrbitalEscape.Events;
using OrbitalEscape.Movement;
using OrbitalEscape.Orbits.Generators;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public sealed class ImpulseTrapController : IDisposable
    {
        private readonly JumpBehaviour _jumpBehaviour;
        private readonly ImpulseTrapConfig _config;
        private readonly EventBinding<ObstacleCollidedEvent> _obstacleCollidedBinding;

        [Inject]
        public ImpulseTrapController(JumpBehaviour jumpBehaviour, ImpulseTrapConfig config)
        {
            _jumpBehaviour = jumpBehaviour;
            _config = config;
            _obstacleCollidedBinding = new EventBinding<ObstacleCollidedEvent>(OnObstacleCollided);
            EventBus<ObstacleCollidedEvent>.Register(_obstacleCollidedBinding);
        }

        public void Dispose() => EventBus<ObstacleCollidedEvent>.Deregister(_obstacleCollidedBinding);

        private void OnObstacleCollided(ObstacleCollidedEvent @event)
        {
            if (@event.ObstacleType != ArcObjectType.ImpulseTrap || !@event.Entered)
                return;

            _jumpBehaviour.JumpFromImpulseTrap(_config.JumpInputLockDuration);
        }
    }
}
