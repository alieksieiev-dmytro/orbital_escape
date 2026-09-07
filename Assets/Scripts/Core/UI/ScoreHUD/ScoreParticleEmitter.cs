using System;
using DG.Tweening;
using OrbitalEscape.Score;
using TMPro;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.UI
{
    public class ScoreParticleEmitter : MonoBehaviour
    {
        [SerializeField] private TMP_Text[] _particles;
        [SerializeField] private Vector2 _spawnMargins = new(100f, 40f);
        [SerializeField] private float _floatDistanceY = 80f;
        [SerializeField, Min(0f)] private float _fadeDelay = 0.4f;
        [SerializeField, Min(0.01f)] private float _animationDuration = 1f;
        [SerializeField] private Ease _movementEase = Ease.OutQuad;

        private IScoreService _scoreService;
        private CanvasGroup[] _canvasGroups;
        private RectTransform[] _particleTransforms;
        private Sequence[] _sequences;
        private ulong[] _activationOrders;
        private ulong _nextActivationOrder;
        private float _previousScore;
        private bool _isSubscribed;

        [Inject]
        public void Construct(IScoreService scoreService)
        {
            _scoreService = scoreService;
            TrySubscribe();
        }

        private void Awake()
        {
            int particleCount = _particles?.Length ?? 0;
            _canvasGroups = new CanvasGroup[particleCount];
            _particleTransforms = new RectTransform[particleCount];
            _sequences = new Sequence[particleCount];
            _activationOrders = new ulong[particleCount];

            for (int i = 0; i < particleCount; i++)
            {
                TMP_Text particle = _particles[i];
                if (particle == null)
                {
                    throw new MissingReferenceException(
                        $"{nameof(ScoreParticleEmitter)} has an empty particle reference at index {i}.");
                }

                if (!particle.TryGetComponent(out CanvasGroup canvasGroup))
                {
                    throw new MissingComponentException(
                        $"Score particle '{particle.name}' requires a {nameof(CanvasGroup)} component.");
                }

                _canvasGroups[i] = canvasGroup;
                _particleTransforms[i] = particle.rectTransform;
                ResetParticle(i);
            }
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ResetParticles();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            KillSequences();
        }

        private void OnValidate()
        {
            _spawnMargins.x = Mathf.Abs(_spawnMargins.x);
            _spawnMargins.y = Mathf.Abs(_spawnMargins.y);
            _animationDuration = Mathf.Max(0.01f, _animationDuration);
            _fadeDelay = Mathf.Clamp(_fadeDelay, 0f, _animationDuration);
        }

        private void TrySubscribe()
        {
            if (_isSubscribed || _scoreService == null || !isActiveAndEnabled)
            {
                return;
            }

            _previousScore = _scoreService.Score;
            _scoreService.ScoreChanged += OnScoreChanged;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || _scoreService == null)
            {
                return;
            }

            _scoreService.ScoreChanged -= OnScoreChanged;
            _isSubscribed = false;
        }

        private void OnScoreChanged(float score)
        {
            float scoreDelta = score - _previousScore;
            _previousScore = score;

            if (score <= 0f)
                ResetParticles();

            if (scoreDelta <= 0f || _particles.Length == 0)
            {
                return;
            }

            Emit(scoreDelta);
        }

        private void Emit(float scoreDelta)
        {
            int particleIndex = GetAvailableParticleIndex();
            TMP_Text particle = _particles[particleIndex];
            RectTransform particleTransform = _particleTransforms[particleIndex];
            CanvasGroup canvasGroup = _canvasGroups[particleIndex];

            _sequences[particleIndex]?.Kill();

            Vector2 spawnPosition = new(
                UnityEngine.Random.Range(-_spawnMargins.x, _spawnMargins.x),
                UnityEngine.Random.Range(-_spawnMargins.y, _spawnMargins.y));

            particle.SetText($"+{scoreDelta:0.#}");
            particleTransform.anchoredPosition = spawnPosition;
            canvasGroup.alpha = 1f;
            particle.gameObject.SetActive(true);
            _activationOrders[particleIndex] = ++_nextActivationOrder;

            float fadeDuration = Mathf.Max(0.01f, _animationDuration - _fadeDelay);
            Sequence sequence = DOTween.Sequence();
            sequence.Join(particleTransform
                .DOAnchorPosY(spawnPosition.y + _floatDistanceY, _animationDuration)
                .SetEase(_movementEase));
            sequence.Insert(_fadeDelay, canvasGroup.DOFade(0f, fadeDuration));
            sequence.OnComplete(() => ResetParticle(particleIndex));
            _sequences[particleIndex] = sequence;
        }

        private int GetAvailableParticleIndex()
        {
            int oldestIndex = 0;
            ulong oldestOrder = ulong.MaxValue;

            for (int i = 0; i < _particles.Length; i++)
            {
                if (!_particles[i].gameObject.activeSelf)
                {
                    return i;
                }

                if (_activationOrders[i] < oldestOrder)
                {
                    oldestOrder = _activationOrders[i];
                    oldestIndex = i;
                }
            }

            return oldestIndex;
        }

        private void ResetParticles()
        {
            if (_particles == null || _sequences == null)
            {
                return;
            }

            for (int i = 0; i < _particles.Length; i++)
            {
                ResetParticle(i);
            }

            _nextActivationOrder = 0;
        }

        private void ResetParticle(int index)
        {
            _sequences[index]?.Kill();
            _sequences[index] = null;
            _activationOrders[index] = 0;
            _canvasGroups[index].alpha = 1f;
            _particleTransforms[index].anchoredPosition = Vector2.zero;
            _particles[index].gameObject.SetActive(false);
        }

        private void KillSequences()
        {
            if (_sequences == null)
            {
                return;
            }

            foreach (Sequence sequence in _sequences)
            {
                sequence?.Kill();
            }
        }
    }
}
