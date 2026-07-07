using System;
using UnityEngine;

namespace OrbitalEscape.Orbits
{
    public sealed class LaserGateController : MonoBehaviour
    {
        private const float MinimumStateDuration = 0.01f;

        [SerializeField] private LaserGateHitbox _hitbox;
        [SerializeField] private Transform _laser;
        [SerializeField] private Transform _lowerCharge;
        [SerializeField] private Transform _upperCharge;

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

        public void Fit(float width, float height)
        {
            ResolveReferences();

            if (_laser != null)
            {
                _laser.localPosition = Vector3.zero;
                _laser.localScale = new Vector3(Mathf.Max(0.01f, width), Mathf.Max(0.01f, height), 1.0f);
            }

            float chargeOffset = Mathf.Max(0.01f, height) * 0.5f;
            if (_lowerCharge != null)
            {
                _lowerCharge.localPosition = new Vector3(0.0f, -chargeOffset, 0.0f);
                _lowerCharge.localScale = Vector3.one;
            }

            if (_upperCharge != null)
            {
                _upperCharge.localPosition = new Vector3(0.0f, chargeOffset, 0.0f);
                _upperCharge.localScale = Vector3.one;
            }
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

            if (_laser == null)
            {
                _laser = transform.Find("laser");
            }

            if (_lowerCharge == null)
            {
                _lowerCharge = transform.Find("charger_lower");
            }

            if (_upperCharge == null)
            {
                _upperCharge = transform.Find("charger_top");
            }
        }
    }
}
