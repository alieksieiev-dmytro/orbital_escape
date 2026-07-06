using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OrbitalEscape.UI
{
    public sealed class CheatCommandWindowView : BaseView
    {
        private const string AddScoreButtonName = "btn-add-score";
        private const string ClearObstaclesButtonName = "btn-clear-obstacles";
        private const string GoToOrbitButtonName = "btn-go-to-orbit";
        private const string BackButtonName = "btn-back";
        private const string OrbitInputName = "input-orbit";
        private const string StatusTextName = "txt-status";

        private Button _addScoreButton;
        private Button _clearObstaclesButton;
        private Button _goToOrbitButton;
        private Button _backButton;
        private TMP_InputField _orbitInput;
        private TMP_Text _statusText;

        public event Action AddScoreClicked;
        public event Action ClearObstaclesClicked;
        public event Action<int> GoToOrbitClicked;
        public event Action BackClicked;

        private void Awake()
        {
            IReadOnlyDictionary<string, Button> buttons = ResolveComponents<Button>();
            IReadOnlyDictionary<string, TMP_InputField> inputs = ResolveComponents<TMP_InputField>();
            IReadOnlyDictionary<string, TMP_Text> texts = ResolveComponents<TMP_Text>();

            _addScoreButton = GetRequired(buttons, AddScoreButtonName);
            _clearObstaclesButton = GetRequired(buttons, ClearObstaclesButtonName);
            _goToOrbitButton = GetRequired(buttons, GoToOrbitButtonName);
            _backButton = GetRequired(buttons, BackButtonName);
            _orbitInput = GetRequired(inputs, OrbitInputName);
            _statusText = GetOptional(texts, StatusTextName);
        }

        private void OnEnable()
        {
            _addScoreButton.onClick.AddListener(OnAddScoreClicked);
            _clearObstaclesButton.onClick.AddListener(OnClearObstaclesClicked);
            _goToOrbitButton.onClick.AddListener(OnGoToOrbitClicked);
            _backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnDisable()
        {
            _addScoreButton.onClick.RemoveListener(OnAddScoreClicked);
            _clearObstaclesButton.onClick.RemoveListener(OnClearObstaclesClicked);
            _goToOrbitButton.onClick.RemoveListener(OnGoToOrbitClicked);
            _backButton.onClick.RemoveListener(OnBackClicked);
        }

        public override void Show()
        {
            base.Show();
            transform.SetAsLastSibling();
            SetStatus("Cheats ready.");
        }

        public void SetStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }
        }

        public void SetBusy(bool busy)
        {
            _addScoreButton.interactable = !busy;
            _clearObstaclesButton.interactable = !busy;
            _goToOrbitButton.interactable = !busy;
            _backButton.interactable = !busy;
            _orbitInput.interactable = !busy;
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

            throw new MissingComponentException($"{nameof(CheatCommandWindowView)} requires child {typeof(TComponent).Name} '{componentName}'.");
        }

        private void OnAddScoreClicked() => AddScoreClicked?.Invoke();
        private void OnClearObstaclesClicked() => ClearObstaclesClicked?.Invoke();

        private void OnGoToOrbitClicked()
        {
            if (!int.TryParse(_orbitInput.text, out int orbitIndex))
            {
                SetStatus("Enter a valid orbit index.");
                return;
            }

            GoToOrbitClicked?.Invoke(orbitIndex);
        }

        private void OnBackClicked() => BackClicked?.Invoke();
    }
}
