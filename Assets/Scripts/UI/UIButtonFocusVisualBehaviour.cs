using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OrbitalEscape.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button), typeof(Image))]
    public sealed class UIButtonFocusVisualBehaviour : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField, Range(0f, 1f)] private float _focusedAlpha = 1f;
        [SerializeField, Min(0f)] private float _fadeInDuration = 0.12f;
        [SerializeField, Min(0f)] private float _fadeOutDuration = 0.15f;
        [SerializeField] private Ease _fadeEase = Ease.OutQuad;

        private Button _button;
        private Image _image;
        private Tween _fadeTween;
        private bool _isHovered;

        private void Awake()
        {
            ResolveComponents();
            SetAlpha(0f);
        }

        private void OnEnable()
        {
            ResolveComponents();
            ResetVisual();
        }

        private void OnDisable()
        {
            ResetVisual();
        }

        private void OnDestroy()
        {
            KillTween();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            RefreshVisual();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            ResolveComponents();

            bool shouldShow = _isHovered &&
                              _button != null &&
                              _button.IsActive() &&
                              _button.IsInteractable();
            float targetAlpha = shouldShow ? _focusedAlpha : 0f;
            float duration = shouldShow ? _fadeInDuration : _fadeOutDuration;

            KillTween();

            if (_image == null)
            {
                return;
            }

            if (duration <= 0f)
            {
                SetAlpha(targetAlpha);
                return;
            }

            _fadeTween = _image
                .DOFade(targetAlpha, duration)
                .SetEase(_fadeEase)
                .SetUpdate(true);
        }

        private void ResetVisual()
        {
            KillTween();
            _isHovered = false;
            SetAlpha(0f);
        }

        private void ResolveComponents()
        {
            _button ??= GetComponent<Button>();
            _image ??= GetComponent<Image>();
        }

        private void SetAlpha(float alpha)
        {
            if (_image == null)
            {
                return;
            }

            Color color = _image.color;
            color.a = alpha;
            _image.color = color;
        }

        private void KillTween()
        {
            _fadeTween?.Kill();
            _fadeTween = null;
        }
    }
}
