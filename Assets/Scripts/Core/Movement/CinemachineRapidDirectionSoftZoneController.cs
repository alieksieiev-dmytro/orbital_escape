using OrbitalEscape.Events;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Movement
{
    public sealed class CinemachineRapidDirectionSoftZoneController : MonoBehaviour
    {
        [SerializeField] private RadialCameraTarget _radialCameraTarget;
        [SerializeField] private CinemachineHorizontalPlayerFraming _horizontalFraming;

        private CameraSettingsConfig _settings;
        private EventBinding<MovementDirectionChangedEvent> _directionChangedEventBinding;
        private EventBinding<OrbitFocusRequestedEvent> _orbitFocusRequestedEventBinding;
        private float _lastDirectionChangeTime;
        private float _recenterElapsed;
        private bool _hasPendingDirectionChange;
        private bool _isSoftZoneActive;
        private bool _isRecentering;

        [Inject]
        private void Construct(CameraSettingsConfig settings)
        {
            _settings = settings;
        }

        private void Awake()
        {
            _directionChangedEventBinding = new EventBinding<MovementDirectionChangedEvent>(OnDirectionChanged);
            _orbitFocusRequestedEventBinding = new EventBinding<OrbitFocusRequestedEvent>(OnOrbitFocusRequested);
        }

        private void OnEnable()
        {
            EventBus<MovementDirectionChangedEvent>.Register(_directionChangedEventBinding);
            EventBus<OrbitFocusRequestedEvent>.Register(_orbitFocusRequestedEventBinding);
            ResetImmediately();
        }

        private void OnDisable()
        {
            EventBus<MovementDirectionChangedEvent>.Deregister(_directionChangedEventBinding);
            EventBus<OrbitFocusRequestedEvent>.Deregister(_orbitFocusRequestedEventBinding);
            ResetState();
        }

        private void Update()
        {
            if (_settings == null || !_settings.DampingEnabled)
            {
                if (_hasPendingDirectionChange || _isSoftZoneActive || _isRecentering)
                {
                    ResetImmediately();
                }

                return;
            }

            if (_isSoftZoneActive)
            {
                if (_horizontalFraming != null)
                {
                    _horizontalFraming.SetSoftZoneWidth(_settings.SoftZoneWidth);
                }
                if (Time.time - _lastDirectionChangeTime >= _settings.DirectionChangeWindow)
                {
                    BeginRecentering();
                }

                return;
            }

            if (_isRecentering)
            {
                UpdateRecentering(Time.deltaTime);
                return;
            }

            if (_hasPendingDirectionChange &&
                Time.time - _lastDirectionChangeTime > _settings.DirectionChangeWindow)
            {
                _hasPendingDirectionChange = false;
            }
        }

        private void OnDirectionChanged(MovementDirectionChangedEvent _)
        {
            if (_settings == null || !_settings.DampingEnabled)
            {
                return;
            }

            float currentTime = Time.time;
            if (_isSoftZoneActive)
            {
                _lastDirectionChangeTime = currentTime;
                return;
            }

            bool isRapidChange = _hasPendingDirectionChange &&
                                 currentTime - _lastDirectionChangeTime <= _settings.DirectionChangeWindow;

            _lastDirectionChangeTime = currentTime;
            if (isRapidChange)
            {
                ActivateSoftZone();
                return;
            }

            _hasPendingDirectionChange = true;
        }

        private void ActivateSoftZone()
        {
            _hasPendingDirectionChange = false;
            _isRecentering = false;
            _isSoftZoneActive = true;

            if (_radialCameraTarget != null)
            {
                _radialCameraTarget.SetAngularDampingStrength(1.0f);
            }

            if (_horizontalFraming != null)
            {
                _horizontalFraming.SetSoftZoneWidth(_settings.SoftZoneWidth);
            }
        }

        private void BeginRecentering()
        {
            _hasPendingDirectionChange = false;
            _isSoftZoneActive = false;
            _isRecentering = true;
            _recenterElapsed = 0.0f;
            _lastDirectionChangeTime = float.NegativeInfinity;
        }

        private void UpdateRecentering(float deltaTime)
        {
            if (deltaTime <= 0.0f)
            {
                return;
            }

            _recenterElapsed += deltaTime;
            float progress = _settings.RecenterDuration <= 0.0f
                ? 1.0f
                : Mathf.Clamp01(_recenterElapsed / _settings.RecenterDuration);
            if (_radialCameraTarget != null)
            {
                _radialCameraTarget.SetAngularDampingStrength(1.0f - progress);
            }

            if (progress < 1.0f)
            {
                return;
            }

            _isRecentering = false;
            if (_horizontalFraming != null)
            {
                _horizontalFraming.SetSoftZoneWidth(0.0f);
            }
        }

        private void OnOrbitFocusRequested(OrbitFocusRequestedEvent _)
        {
            ResetImmediately();
        }

        private void ResetImmediately()
        {
            ResetState();

            if (_horizontalFraming != null)
            {
                _horizontalFraming.SetSoftZoneWidth(0.0f);
            }

            if (_radialCameraTarget != null)
            {
                _radialCameraTarget.SetAngularDampingStrength(0.0f);
            }
        }

        private void ResetState()
        {
            _lastDirectionChangeTime = float.NegativeInfinity;
            _recenterElapsed = 0.0f;
            _hasPendingDirectionChange = false;
            _isSoftZoneActive = false;
            _isRecentering = false;
        }
    }
}
