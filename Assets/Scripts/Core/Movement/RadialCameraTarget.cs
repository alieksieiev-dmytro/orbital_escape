using UnityEngine;
using Zenject;

namespace OrbitalEscape.Movement
{
    [DefaultExecutionOrder(-100)]
    public sealed class RadialCameraTarget : MonoBehaviour
    {
        private const float MIN_SQR_MAGNITUDE = 0.00000001f;

        [SerializeField] private Transform _player;
        [SerializeField] private Transform _center;

        private CameraSettingsConfig _settings;
        private float _currentAngle;
        private float _angularVelocity;
        private float _angularDampingStrength;
        private bool _isInitialized;

        [Inject]
        private void Construct(CameraSettingsConfig settings)
        {
            _settings = settings;
        }

        private void OnEnable()
        {
            SnapToPlayer();
        }

        private void OnDisable()
        {
            _angularVelocity = 0.0f;
            _angularDampingStrength = 0.0f;
            _isInitialized = false;
        }

        private void LateUpdate()
        {
            RefreshPose(Time.deltaTime);
        }

        public void SetAngularDampingStrength(float strength)
        {
            _angularDampingStrength = Mathf.Clamp01(strength);
            if (_angularDampingStrength <= 0.0f)
            {
                SnapToPlayer();
            }
        }

        public void SnapToPlayer()
        {
            if (!TryGetTargetAngle(out float targetAngle))
            {
                return;
            }

            _currentAngle = targetAngle;
            _angularVelocity = 0.0f;
            _isInitialized = true;
            ApplyPose(_currentAngle);
        }

        private void RefreshPose(float deltaTime)
        {
            if (!TryGetTargetAngle(out float targetAngle))
            {
                return;
            }

            float effectiveSmoothTime = (_settings?.AngularSmoothTime ?? 0.0f) * _angularDampingStrength;
            if (!_isInitialized || effectiveSmoothTime <= 0.0f)
            {
                _currentAngle = targetAngle;
                _angularVelocity = 0.0f;
                _isInitialized = true;
            }
            else if (deltaTime > 0.0f)
            {
                _currentAngle = Mathf.SmoothDampAngle(
                    _currentAngle,
                    targetAngle,
                    ref _angularVelocity,
                    effectiveSmoothTime,
                    _settings.MaxAngularSpeed,
                    deltaTime);
            }

            ApplyPose(_currentAngle);
        }

        private bool TryGetTargetAngle(out float targetAngle)
        {
            targetAngle = 0.0f;
            if (_player == null || _center == null)
            {
                return false;
            }

            Vector2 radialDirection = _player.position - _center.position;
            if (radialDirection.sqrMagnitude < MIN_SQR_MAGNITUDE)
            {
                return false;
            }

            targetAngle = GetCameraAngle(radialDirection);
            return true;
        }

        private void ApplyPose(float cameraAngle)
        {
            Vector3 centerPosition = _center.position;
            transform.SetPositionAndRotation(
                new Vector3(centerPosition.x, centerPosition.y, transform.position.z),
                Quaternion.Euler(0.0f, 0.0f, cameraAngle));
        }

        private static float GetCameraAngle(Vector2 radialDirection)
        {
            return Mathf.Atan2(radialDirection.y, radialDirection.x) * Mathf.Rad2Deg - 90.0f;
        }
    }
}
