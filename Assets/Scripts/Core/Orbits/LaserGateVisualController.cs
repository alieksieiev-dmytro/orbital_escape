using UnityEngine;

namespace OrbitalEscape.Orbits
{
    [DisallowMultipleComponent]
    public sealed class LaserGateVisualController : MonoBehaviour
    {
        [SerializeField] private LaserGateController _controller;
        [SerializeField] private SpriteRenderer _laserRenderer;
        [SerializeField, Range(0.0f, 1.0f)] private float _passAlpha;
        [SerializeField, Range(0.0f, 1.0f)] private float _chargingAlpha = 0.25f;
        [SerializeField, Range(0.0f, 1.0f)] private float _activeAlpha = 0.75f;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (_controller != null)
            {
                _controller.StateChanged += OnStateChanged;
                ApplyState(_controller.State);
            }
        }

        private void OnDisable()
        {
            if (_controller != null)
            {
                _controller.StateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(LaserGateState state)
        {
            ApplyState(state);
        }

        private void ApplyState(LaserGateState state)
        {
            if (_laserRenderer == null)
            {
                ResolveReferences();
            }

            if (_laserRenderer == null)
            {
                return;
            }

            Color color = _laserRenderer.color;
            color.a = GetAlpha(state);
            _laserRenderer.color = color;
        }

        private float GetAlpha(LaserGateState state)
        {
            return state switch
            {
                LaserGateState.Charging => _chargingAlpha,
                LaserGateState.Active => _activeAlpha,
                _ => _passAlpha
            };
        }

        private void ResolveReferences()
        {
            if (_controller == null)
            {
                _controller = GetComponent<LaserGateController>();
            }

            if (_laserRenderer == null)
            {
                Transform laser = transform.Find("laser");
                if (laser != null)
                {
                    _laserRenderer = laser.GetComponent<SpriteRenderer>();
                }
            }
        }
    }
}
