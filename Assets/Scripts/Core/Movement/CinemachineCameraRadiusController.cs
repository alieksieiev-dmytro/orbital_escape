using DG.Tweening;
using OrbitalEscape.Events;
using Unity.Cinemachine;
using UnityEngine;

namespace OrbitalEscape.Movement
{
    public sealed class CinemachineCameraRadiusController : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private CinemachineFollow _follow;
        [SerializeField, Min(0.1f)] private float _radius = 16.65f;
        [SerializeField] private float _innerOffset = 3.35f;
        [SerializeField] private float _outerOffset = 1.65f;
        [SerializeField, Min(0.0f)] private float _transitionDuration = 0.5f;

        private EventBinding<PassedThroughOrbitEvent> _passedThroughOrbitEventBinding;
        private EventBinding<OrbitFocusRequestedEvent> _orbitFocusRequestedEventBinding;
        private Tween _radiusTween;
        private float _initialRadius;
        private EventBinding<GameplayRunRestartedEvent> _runRestartedBinding;

        private void Awake()
        {
            _initialRadius = _radius;
            _runRestartedBinding = new EventBinding<GameplayRunRestartedEvent>(() =>
                EventBus<OrbitFocusRequestedEvent>.Raise(new OrbitFocusRequestedEvent(_initialRadius)));
            _passedThroughOrbitEventBinding = new EventBinding<PassedThroughOrbitEvent>(OnPassedThroughOrbit);
            _orbitFocusRequestedEventBinding = new EventBinding<OrbitFocusRequestedEvent>(OnOrbitFocusRequested);
            ApplyRadius(_radius);
        }

        private void OnEnable()
        {
            EventBus<GameplayRunRestartedEvent>.Register(_runRestartedBinding);
            EventBus<PassedThroughOrbitEvent>.Register(_passedThroughOrbitEventBinding);
            EventBus<OrbitFocusRequestedEvent>.Register(_orbitFocusRequestedEventBinding);
        }

        private void OnDisable()
        {
            EventBus<GameplayRunRestartedEvent>.Deregister(_runRestartedBinding);
            EventBus<PassedThroughOrbitEvent>.Deregister(_passedThroughOrbitEventBinding);
            EventBus<OrbitFocusRequestedEvent>.Deregister(_orbitFocusRequestedEventBinding);
            StopRadiusTween();
        }

        private void OnPassedThroughOrbit(PassedThroughOrbitEvent @event)
        {
            float offset = @event.IsOuterOrbit ? _outerOffset : -_innerOffset;
            float targetRadius = @event.Orbit.Radius + offset;

            StopRadiusTween();
            _radiusTween = DOTween.To(
                    () => _radius,
                    ApplyRadius,
                    targetRadius,
                    _transitionDuration)
                .SetEase(Ease.InOutSine);
        }

        private void OnOrbitFocusRequested(OrbitFocusRequestedEvent @event)
        {
            StopRadiusTween();
            ApplyRadius(Mathf.Max(0.1f, @event.CameraRadius));

            if (_cinemachineCamera != null)
            {
                _cinemachineCamera.PreviousStateIsValid = false;
            }
        }

        private void ApplyRadius(float radius)
        {
            _radius = Mathf.Max(0.1f, radius);
            if (_follow == null)
            {
                return;
            }

            Vector3 offset = _follow.FollowOffset;
            offset.y = _radius;
            _follow.FollowOffset = offset;
        }

        private void StopRadiusTween()
        {
            _radiusTween?.Kill();
            _radiusTween = null;
        }
    }
}
