using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OrbitalEscape.UI
{
    public class PauseWindowView : BaseView
    {
        private const string ResumeButtonName = "btn-resume";
        private const string RestartButtonName = "btn-restart";
        private const string ExitButtonName = "btn-exit";
        private const string CheatsButtonName = "btn-cheats";

        [SerializeField] private float _fadeDuration = 0.1f;
        [SerializeField] private int _sortingOrder = 100;

        private Button _resumeButton;
        private Button _restartButton;
        private Button _exitButton;
        private Button _cheatsButton;
        private PauseBlurBackground _blurBackground;
        private Coroutine _showCoroutine;
        private Tween _fadeTween;

        public event Action ResumeClicked;
        public event Action RestartClicked;
        public event Action ExitClicked;
        public event Action CheatsClicked;

        private void Awake()
        {
            IReadOnlyDictionary<string, Button> buttons = ResolveButtons();
            _resumeButton = GetRequiredButton(buttons, ResumeButtonName);
            _restartButton = GetRequiredButton(buttons, RestartButtonName);
            _exitButton = GetRequiredButton(buttons, ExitButtonName);
            _cheatsButton = GetOptionalButton(buttons, CheatsButtonName);
            _blurBackground = GetComponentInChildren<PauseBlurBackground>(true);
        }

        private void OnEnable()
        {
            _resumeButton.onClick.AddListener(OnResumeClicked);
            _restartButton.onClick.AddListener(OnRestartClicked);
            _exitButton.onClick.AddListener(OnExitClicked);
            _cheatsButton?.onClick.AddListener(OnCheatsClicked);
        }

        private void OnDisable()
        {
            _resumeButton.onClick.RemoveListener(OnResumeClicked);
            _restartButton.onClick.RemoveListener(OnRestartClicked);
            _exitButton.onClick.RemoveListener(OnExitClicked);
            _cheatsButton?.onClick.RemoveListener(OnCheatsClicked);
            StopShowCoroutine();
            _fadeTween?.Kill();
            _blurBackground?.Release();
        }

        public void SetCheatsVisible(bool visible)
        {
            if (_cheatsButton != null)
            {
                _cheatsButton.gameObject.SetActive(visible);
            }
        }

        public override void Show()
        {
            ConfigureRootCanvas();
            transform.SetAsLastSibling();

            _fadeTween?.Kill();
            StopShowCoroutine();

            if (Root == null)
            {
                return;
            }

            HideDuringCapture();

            if (_blurBackground == null)
            {
                ShowWithFade();
                return;
            }

            _showCoroutine = StartCoroutine(ShowAfterBlurCapture());
        }

        public override void Hide()
        {
            StopShowCoroutine();
            _fadeTween?.Kill();
            _blurBackground?.Release();
            base.Hide();
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
            {
                return;
            }

            CanvasGroup.alpha = 0f;
            _fadeTween?.Kill();
            _fadeTween = CanvasGroup
                .DOFade(1f, _fadeDuration)
                .SetUpdate(true);
        }

        private void HideDuringCapture()
        {
            Root.enabled = false;

            if (CanvasGroup == null)
            {
                return;
            }

            CanvasGroup.alpha = 0f;
            CanvasGroup.blocksRaycasts = false;
            CanvasGroup.interactable = false;
        }

        private void StopShowCoroutine()
        {
            if (_showCoroutine == null)
            {
                return;
            }

            StopCoroutine(_showCoroutine);
            _showCoroutine = null;
        }

        private IReadOnlyDictionary<string, Button> ResolveButtons()
        {
            var buttonsByName = new Dictionary<string, Button>();
            Button[] buttons = GetComponentsInChildren<Button>(true);

            foreach (Button button in buttons)
            {
                buttonsByName.TryAdd(button.gameObject.name, button);
            }

            return buttonsByName;
        }

        private Button GetRequiredButton(IReadOnlyDictionary<string, Button> buttons, string buttonName)
        {
            if (buttons.TryGetValue(buttonName, out Button button))
            {
                return button;
            }

            throw new MissingComponentException($"{nameof(PauseWindowView)} requires child Button '{buttonName}'.");
        }

        private Button GetOptionalButton(IReadOnlyDictionary<string, Button> buttons, string buttonName)
        {
            return buttons.TryGetValue(buttonName, out Button button)
                ? button
                : null;
        }

        private void ConfigureRootCanvas()
        {
            if (Root == null)
            {
                return;
            }

            Root.overrideSorting = true;
            Root.sortingOrder = _sortingOrder;
        }

        private void OnResumeClicked() => ResumeClicked?.Invoke();
        private void OnRestartClicked() => RestartClicked?.Invoke();
        private void OnExitClicked() => ExitClicked?.Invoke();
        private void OnCheatsClicked() => CheatsClicked?.Invoke();
    }
}
