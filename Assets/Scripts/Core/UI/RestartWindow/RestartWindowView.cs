using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OrbitalEscape.UI
{
    public class RestartWindowView : BaseView
    {
        [SerializeField] private Button _restartButton;
        [SerializeField] private float _fadeDuration = 0.1f;

        private Tween _fadeTween;
        
        public event Action RestartClicked;

        private void OnEnable() => _restartButton.onClick.AddListener(OnRestartClicked);
        
        private void OnDisable() => _restartButton.onClick.RemoveListener(OnRestartClicked);

        public override void Show()
        {
            base.Show();
            CanvasGroup.alpha = 0;
            _fadeTween?.Kill();
            _fadeTween = CanvasGroup.DOFade(1, _fadeDuration);
        }

        public override void Hide()
        {
            base.Hide();
            _fadeTween?.Kill();
        }
        
        private void OnRestartClicked() => RestartClicked?.Invoke();
    }
}
