using DG.Tweening;
using UnityEngine;

namespace OrbitalEscape.Visuals
{
    [DisallowMultipleComponent]
    public sealed class SpikeAlphaController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _outlineRenderer;

        private float _authoredAlpha;
        private float _alphaMultiplier = 1f;
        private Tween _alphaTween;

        private void Awake()
        {
            if (_outlineRenderer != null)
            {
                _authoredAlpha = _outlineRenderer.color.a;
            }
        }

        private void OnDisable()
        {
            ResetImmediately();
        }

        private void OnDestroy()
        {
            KillTween();
        }

        public void SetDimmed(bool dimmed, SpikePresentationConfig config, bool animate)
        {
            float targetMultiplier = dimmed ? config.UnreachableAlphaMultiplier : 1f;
            float duration = animate ? config.TransitionDuration : 0f;

            KillTween();
            if (duration <= 0f || Mathf.Approximately(_alphaMultiplier, targetMultiplier))
            {
                ApplyAlphaMultiplier(targetMultiplier);
                return;
            }

            _alphaTween = DOTween
                .To(() => _alphaMultiplier, ApplyAlphaMultiplier, targetMultiplier, duration)
                .SetEase(config.TransitionEase);
        }

        private void ResetImmediately()
        {
            KillTween();
            ApplyAlphaMultiplier(1f);
        }

        private void ApplyAlphaMultiplier(float multiplier)
        {
            _alphaMultiplier = Mathf.Clamp01(multiplier);
            if (_outlineRenderer == null)
            {
                return;
            }

            Color color = _outlineRenderer.color;
            color.a = _authoredAlpha * _alphaMultiplier;
            _outlineRenderer.color = color;
        }

        private void KillTween()
        {
            _alphaTween?.Kill();
            _alphaTween = null;
        }
    }
}
