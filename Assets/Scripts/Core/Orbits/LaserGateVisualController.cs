using UnityEngine;

namespace OrbitalEscape.Orbits
{
    [DisallowMultipleComponent]
    public sealed class LaserGateVisualController : MonoBehaviour
    {
        [SerializeField] private LaserGateController _controller;
        [SerializeField] private LaserGateBaseView _baseView;
        [SerializeField] private LaserGateParticleController _particleController;
        [SerializeField] private LaserGateStateView[] _stateViews;

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

        public void Fit(float width, float height, float centerYOffset)
        {
            ResolveReferences();
            if (_baseView != null)
            {
                _baseView.Fit(width, height, centerYOffset);
            }

            if (_particleController != null)
            {
                _particleController.Fit(centerYOffset);
            }

            foreach (LaserGateStateView stateView in _stateViews)
            {
                if (stateView != null)
                {
                    stateView.Fit(width, height, centerYOffset);
                }
            }
        }

        private void ApplyState(LaserGateState state)
        {
            if (_particleController != null)
            {
                _particleController.ApplyState(state);
            }

            bool stateActivated = false;
            foreach (LaserGateStateView stateView in _stateViews)
            {
                if (stateView == null)
                {
                    continue;
                }

                bool shouldActivate = !stateActivated && stateView.State == state;
                stateView.SetVisible(shouldActivate);
                stateActivated |= shouldActivate;
            }
        }

        private void ResolveReferences()
        {
            if (_controller == null)
            {
                _controller = GetComponent<LaserGateController>();
            }

            if (_stateViews == null || _stateViews.Length == 0)
            {
                _stateViews = GetComponentsInChildren<LaserGateStateView>(true);
            }

            if (_baseView == null)
            {
                _baseView = GetComponentInChildren<LaserGateBaseView>(true);
            }

            if (_particleController == null)
            {
                _particleController = GetComponent<LaserGateParticleController>();
            }
        }
    }
}
