using System;
using DG.Tweening;
using UnityEngine;

namespace OrbitalEscape.Visuals
{
    [DisallowMultipleComponent]
    public sealed class PlayerDeathAnimation : MonoBehaviour, IPlayerDeathAnimation
    {
        [Header("Visuals")]
        [SerializeField] private Transform _model;
        [SerializeField] private SpriteRenderer _ring;
        [SerializeField] private TrailRenderer[] _trails;
        [SerializeField] private ParticleSystem[] _ambientParticles;
        [SerializeField] private ParticleSystem _accentBurstParticles;
        [SerializeField] private ParticleSystem _coreBurstParticles;
        [SerializeField] private ParticleSystem _waveBurstParticles;

        [Header("Timing")]
        [SerializeField, Range(0.01f, 0.5f)] private float _totalDuration = 0.45f;
        [SerializeField, Min(0.0f)] private float _expandDuration = 0.08f;
        [SerializeField, Min(0.0f)] private float _collapseDuration = 0.12f;
        [SerializeField, Min(1.0f)] private float _expandedScale = 1.25f;

        [Header("Burst")]
        [SerializeField, Min(0)] private int _accentParticleCount = 24;
        [SerializeField, Min(0)] private int _coreParticleCount = 10;
        [SerializeField, Min(0)] private int _waveParticleCount = 1;

        private Sequence _sequence;
        private Vector3 _modelInitialScale;
        private Vector3 _ringInitialScale;
        private Color _ringInitialColor;
        private bool[] _trailEmissionStates;
        private bool[] _ambientPlayStates;
        private Action _onCompleted;
        private ParticleSystem[] _allParticles;
        private bool[] _initialParticlePlayStates;

        private void Awake()
        {
            CacheInitialState();
        }

        public void Play(Action onCompleted)
        {
            StopSequence();
            _onCompleted = onCompleted;

            if (_model == null)
            {
                _onCompleted?.Invoke();
                _onCompleted = null;
                return;
            }

            StopContinuousEffects();
            PrepareBurst(_accentBurstParticles);
            PrepareBurst(_coreBurstParticles);
            PrepareBurst(_waveBurstParticles);

            float expandDuration = Mathf.Min(_expandDuration, _totalDuration);
            float collapseDuration = Mathf.Min(_collapseDuration, _totalDuration - expandDuration);
            float visualDuration = expandDuration + collapseDuration;
            float remainingDuration = Mathf.Max(0.0f, _totalDuration - visualDuration);

            _sequence = DOTween.Sequence().SetUpdate(true);
            _sequence.Append(_ring.transform.DOScale(_ringInitialScale * 0.9f, expandDuration * 0.2f)
                .SetEase(Ease.OutQuart));
            _sequence.Append(_ring.transform.DOScale(_ringInitialScale * _expandedScale, expandDuration * 0.8f)
                .SetEase(Ease.OutQuart));
            _sequence.Join(_ring.DOFade(0f, expandDuration * 0.8f)
                .SetEase(Ease.OutQuart));
            _sequence.JoinCallback(EmitParticles);
            _sequence.Append(_model.DOScale(Vector3.zero, collapseDuration)
                .SetEase(Ease.InQuad));
            _sequence.AppendCallback(() => _model.gameObject.SetActive(false));

            if (remainingDuration > 0.0f)
            {
                _sequence.AppendInterval(remainingDuration);
            }

            _sequence.OnComplete(Complete);
            _sequence.OnKill(() => _sequence = null);
            _sequence.Play();
        }

        private void OnDisable()
        {
            StopSequence();
            RestoreInitialState();
        }

        private void CacheInitialState()
        {
            _allParticles = GetComponentsInChildren<ParticleSystem>(true);
            _initialParticlePlayStates = new bool[_allParticles.Length];
            for (int i = 0; i < _allParticles.Length; i++)
                _initialParticlePlayStates[i] = _allParticles[i].main.playOnAwake;
            if (_model != null)
            {
                _modelInitialScale = _model.localScale;
            }

            if (_ring != null)
            {
                _ringInitialScale = _ring.transform.localScale;
                _ringInitialColor = _ring.color;
            }

            _trailEmissionStates = new bool[_trails?.Length ?? 0];
            for (int i = 0; i < _trailEmissionStates.Length; i++)
            {
                _trailEmissionStates[i] = _trails[i] != null && _trails[i].emitting;
            }

            _ambientPlayStates = new bool[_ambientParticles?.Length ?? 0];
            for (int i = 0; i < _ambientPlayStates.Length; i++)
            {
                _ambientPlayStates[i] = _ambientParticles[i] != null && _ambientParticles[i].isPlaying;
            }
        }

        private void StopContinuousEffects()
        {
            if (_trails != null)
            {
                foreach (TrailRenderer trail in _trails)
                {
                    if (trail != null)
                    {
                        trail.emitting = false;
                    }
                }
            }

            if (_ambientParticles == null)
            {
                return;
            }

            foreach (ParticleSystem particles in _ambientParticles)
            {
                particles?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void PrepareBurst(ParticleSystem particles)
        {
            if (particles == null)
            {
                return;
            }

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void EmitParticles()
        {
            if (_accentBurstParticles != null && _accentParticleCount > 0)
            {
                _accentBurstParticles.Emit(_accentParticleCount);
            }

            if (_coreBurstParticles != null && _coreParticleCount > 0)
            {
                _coreBurstParticles.Emit(_coreParticleCount);
            }

            if (_waveBurstParticles != null && _waveParticleCount > 0)
            {
                _waveBurstParticles.Emit(_waveParticleCount);
            }
        }

        private void Complete()
        {
            _sequence = null;
            Action callback = _onCompleted;
            _onCompleted = null;
            callback?.Invoke();
        }

        private void StopSequence()
        {
            _sequence?.Kill();
            _sequence = null;
            _onCompleted = null;
        }

        private void RestoreInitialState()
        {
            if (_model != null)
            {
                _model.gameObject.SetActive(true);
                _model.localScale = _modelInitialScale;
            }

            if (_ring != null)
            {
                _ring.transform.localScale = _ringInitialScale;
                _ring.color = _ringInitialColor;
            }

            if (_trails != null)
            {
                for (int i = 0; i < _trails.Length; i++)
                {
                    if (_trails[i] != null)
                    {
                        _trails[i].emitting = i < _trailEmissionStates.Length && _trailEmissionStates[i];
                    }
                }
            }

            if (_ambientParticles != null)
            {
                for (int i = 0; i < _ambientParticles.Length; i++)
                {
                    ParticleSystem particles = _ambientParticles[i];
                    if (particles != null && i < _ambientPlayStates.Length && _ambientPlayStates[i])
                    {
                        particles.Play();
                    }
                }
            }

            PrepareBurst(_accentBurstParticles);
            PrepareBurst(_coreBurstParticles);
            PrepareBurst(_waveBurstParticles);
        }

        public void ResetForRun()
        {
            StopSequence();
            RestoreInitialState();
            if (_trails != null)
                foreach (TrailRenderer trail in _trails)
                    if (trail != null)
                        trail.Clear();
            for (int i = 0; i < _allParticles.Length; i++)
            {
                ParticleSystem particles = _allParticles[i];
                if (particles == null)
                    continue;
                particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (_initialParticlePlayStates[i] && particles.gameObject.activeInHierarchy)
                    particles.Play(false);
            }
        }
    }
}
