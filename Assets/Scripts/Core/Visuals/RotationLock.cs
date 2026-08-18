using OrbitalEscape.Events;
using OrbitalEscape.Movement;
using UnityEngine;

namespace OrbitalEscape.Visuals
{
    [DisallowMultipleComponent]
    public sealed class RotationLock : MonoBehaviour
    {
        private Quaternion _authoredLocalRotation;
        private EventBinding<MovementDirectionChangedEvent> _directionChangedBinding;

        private void Awake()
        {
            _authoredLocalRotation = transform.localRotation;
            _directionChangedBinding = new EventBinding<MovementDirectionChangedEvent>(OnDirectionChanged);
        }

        private void OnEnable()
        {
            EventBus<MovementDirectionChangedEvent>.Register(_directionChangedBinding);
        }

        private void OnDisable()
        {
            EventBus<MovementDirectionChangedEvent>.Deregister(_directionChangedBinding);
        }

        private void OnDirectionChanged(MovementDirectionChangedEvent directionChangedEvent)
        {
            float reversalAngle = directionChangedEvent.Direction == MovementDirection.CounterClockwise
                ? 180.0f
                : 0.0f;

            transform.localRotation = _authoredLocalRotation * Quaternion.Euler(0.0f, 0.0f, reversalAngle);
        }
    }
}
