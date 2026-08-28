using System;
using UnityEngine;

namespace OrbitalEscape.Orbits
{
    public sealed class LaserGateController : MonoBehaviour
    {
        private const float MinimumStateDuration = 0.01f;

        [SerializeField] private LaserGateHitbox _hitbox;

        private bool _isConfigured;
        private float _elapsed;
        private float _passTime;
        private float _chargingTime;
        private float _activeTime;

        public event Action<LaserGateState> StateChanged;
        public LaserGateState State { get; private set; }
        public bool IsActive => State == LaserGateState.Active;

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
            SetState(LaserGateState.Pass);
        }

        private void Update()
        {
            if (!_isConfigured)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed < GetStateDuration(State))
            {
                return;
            }

            SetState(GetNextState(State));
        }

        public void Configure(LaserGate laserGate)
        {
            _passTime = laserGate.PassTime;
            _chargingTime = laserGate.ChargingTime;
            _activeTime = laserGate.ActiveTime;
            _isConfigured = true;
            SetState(LaserGateState.Pass);
        }

        private void SetState(LaserGateState state)
        {
            State = state;
            _elapsed = 0.0f;

            if (_hitbox != null)
            {
                _hitbox.SetDangerActive(IsActive);
            }

            StateChanged?.Invoke(State);
        }

        private float GetStateDuration(LaserGateState state)
        {
            float duration = state switch
            {
                LaserGateState.Charging => _chargingTime,
                LaserGateState.Active => _activeTime,
                _ => _passTime
            };

            return Mathf.Max(MinimumStateDuration, duration);
        }

        private static LaserGateState GetNextState(LaserGateState state)
        {
            return state switch
            {
                LaserGateState.Pass => LaserGateState.Charging,
                LaserGateState.Charging => LaserGateState.Active,
                _ => LaserGateState.Pass
            };
        }

        private void ResolveReferences()
        {
            if (_hitbox == null)
            {
                _hitbox = GetComponentInChildren<LaserGateHitbox>(true);
            }
        }
    }
}
