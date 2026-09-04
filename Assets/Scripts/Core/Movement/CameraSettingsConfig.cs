using UnityEngine;

namespace OrbitalEscape.Movement
{
    [CreateAssetMenu(fileName = "CameraSettingsConfig", menuName = "Scriptable Objects/Configs/Camera Settings Config")]
    public sealed class CameraSettingsConfig : ScriptableObject
    {
        [SerializeField] private bool _dampingEnabled = true;
        [SerializeField, Min(0.0f)] private float _directionChangeWindow = 0.5f;
        [SerializeField, Range(0.0f, 1.0f)] private float _softZoneWidth = 0.08f;
        [SerializeField, Min(0.0f)] private float _angularSmoothTime = 0.12f;
        [SerializeField, Min(0.0f)] private float _maxAngularSpeed = 720.0f;
        [SerializeField, Min(0.0f)] private float _recenterDuration = 0.2f;

        public bool DampingEnabled => _dampingEnabled;
        public float DirectionChangeWindow => _directionChangeWindow;
        public float SoftZoneWidth => _softZoneWidth;
        public float AngularSmoothTime => _angularSmoothTime;
        public float MaxAngularSpeed => _maxAngularSpeed;
        public float RecenterDuration => _recenterDuration;
    }
}
