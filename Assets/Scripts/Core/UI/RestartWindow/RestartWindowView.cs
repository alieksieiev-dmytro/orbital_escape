using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OrbitalEscape.UI
{
    public class RestartWindowView : BaseView
    {
        [SerializeField] private Button _restartButton;
        [SerializeField] private float _fadeDuration = 0.1f;
        [SerializeField] private int _sortingOrder = 100;

        private Tween _fadeTween;
        private PauseBlurBackground _blurBackground;
        private Coroutine _showCoroutine;
        
        public event Action RestartClicked;

        private void Awake() => _blurBackground = GetComponentInChildren<PauseBlurBackground>(true);

        private void OnEnable() => _restartButton.onClick.AddListener(OnRestartClicked);
        
        private void OnDisable()
        {
            _restartButton.onClick.RemoveListener(OnRestartClicked);
            StopPresentation();
        }

        public override void Show()
        {
            StopPresentation();
            base.Hide();
            if (Root == null)
                return;

            Root.overrideSorting = true;
            Root.sortingOrder = _sortingOrder;
            transform.SetAsLastSibling();

            if (_blurBackground != null && _blurBackground.isActiveAndEnabled)
                _showCoroutine = StartCoroutine(ShowAfterBlurCapture());
            else
                ShowWithFade();
        }

        public override void Hide()
        {
            base.Hide();
            StopPresentation();
        }

        private IEnumerator ShowAfterBlurCapture()
        {
            yield return _blurBackground.CaptureAsync();
            _showCoroutine = null;
            ShowWithFade();
        }

        private void ShowWithFade()
        {
            base.Show();
            if (CanvasGroup == null)
                return;

            CanvasGroup.alpha = 0;
            _fadeTween = CanvasGroup.DOFade(1, _fadeDuration).SetUpdate(true);
        }

        private void StopPresentation()
        {
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }

            _fadeTween?.Kill();
            _fadeTween = null;
            _blurBackground?.Release();
        }
        
        private void OnRestartClicked() => RestartClicked?.Invoke();
    }
}
