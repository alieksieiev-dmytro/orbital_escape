using System;
using System.Collections.Generic;
using OrbitalEscape.Difficulty;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OrbitalEscape.UI
{
    public sealed class SelectGameModeWindowView : BaseView
    {
        private const string PreviousButtonName = "btn-previous";
        private const string NextButtonName = "btn-next";
        private const string BackButtonName = "btn-back";
        private const string PlayButtonName = "btn-play";
        private const string TitleTextName = "txt-title";
        private const string DescriptionTextName = "txt-description";
        private const string CounterTextName = "txt-counter";
        private const string PreviewImageName = "img-preview";

        private Button _previousButton;
        private Button _nextButton;
        private Button _backButton;
        private Button _playButton;
        private TMP_Text _titleText;
        private TMP_Text _descriptionText;
        private TMP_Text _counterText;
        private Image _previewImage;

        public event Action PreviousClicked;
        public event Action NextClicked;
        public event Action BackClicked;
        public event Action PlayClicked;

        private void Awake()
        {
            IReadOnlyDictionary<string, Button> buttons = ResolveComponents<Button>();
            IReadOnlyDictionary<string, TMP_Text> texts = ResolveComponents<TMP_Text>();
            IReadOnlyDictionary<string, Image> images = ResolveComponents<Image>();

            _previousButton = GetRequired(buttons, PreviousButtonName);
            _nextButton = GetRequired(buttons, NextButtonName);
            _backButton = GetRequired(buttons, BackButtonName);
            _playButton = GetRequired(buttons, PlayButtonName);
            _titleText = GetRequired(texts, TitleTextName);
            _descriptionText = GetRequired(texts, DescriptionTextName);
            _counterText = GetOptional(texts, CounterTextName);
            _previewImage = GetOptional(images, PreviewImageName);
        }

        private void OnEnable()
        {
            _previousButton.onClick.AddListener(OnPreviousClicked);
            _nextButton.onClick.AddListener(OnNextClicked);
            _backButton.onClick.AddListener(OnBackClicked);
            _playButton.onClick.AddListener(OnPlayClicked);
        }

        private void OnDisable()
        {
            _previousButton.onClick.RemoveListener(OnPreviousClicked);
            _nextButton.onClick.RemoveListener(OnNextClicked);
            _backButton.onClick.RemoveListener(OnBackClicked);
            _playButton.onClick.RemoveListener(OnPlayClicked);
        }

        public override void Show()
        {
            base.Show();
            transform.SetAsLastSibling();
        }

        public void SetOption(DifficultySelectionOption option, int index, int count)
        {
            _titleText.text = option.Title;
            _descriptionText.text = option.Description;

            if (_counterText != null)
            {
                _counterText.text = $"{index + 1}/{count}";
            }

            if (_previewImage != null)
            {
                _previewImage.sprite = option.Image;
                _previewImage.preserveAspect = true;
                _previewImage.color = option.Image == null
                    ? new Color(1.0f, 1.0f, 1.0f, 0.08f)
                    : Color.white;
            }

            bool hasMultipleOptions = count > 1;
            _previousButton.interactable = hasMultipleOptions;
            _nextButton.interactable = hasMultipleOptions;
        }

        private IReadOnlyDictionary<string, TComponent> ResolveComponents<TComponent>() where TComponent : Component
        {
            var componentsByName = new Dictionary<string, TComponent>();
            TComponent[] components = GetComponentsInChildren<TComponent>(true);

            for (int i = 0; i < components.Length; i++)
            {
                componentsByName.TryAdd(components[i].gameObject.name, components[i]);
            }

            return componentsByName;
        }

        private static TComponent GetOptional<TComponent>(
            IReadOnlyDictionary<string, TComponent> components,
            string componentName)
            where TComponent : Component
        {
            return components.TryGetValue(componentName, out TComponent component)
                ? component
                : null;
        }

        private TComponent GetRequired<TComponent>(
            IReadOnlyDictionary<string, TComponent> components,
            string componentName)
            where TComponent : Component
        {
            if (components.TryGetValue(componentName, out TComponent component))
            {
                return component;
            }

            throw new MissingComponentException($"{nameof(SelectGameModeWindowView)} requires child {typeof(TComponent).Name} '{componentName}'.");
        }

        private void OnPreviousClicked() => PreviousClicked?.Invoke();
        private void OnNextClicked() => NextClicked?.Invoke();
        private void OnBackClicked() => BackClicked?.Invoke();
        private void OnPlayClicked() => PlayClicked?.Invoke();
    }
}
