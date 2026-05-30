using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OrbitalEscape.UI
{
    public class MainMenuWindowView : BaseView
    {
        private const string PlayButtonName = "btn-play";
        private const string SettingsButtonName = "btn-settings";
        private const string ExitButtonName = "btn-exit";

        private Button _playButton;
        private Button _settingsButton;
        private Button _exitButton;

        public event Action PlayClicked;
        public event Action SettingsClicked;
        public event Action ExitClicked;

        private void Awake()
        {
            IReadOnlyDictionary<string, Button> buttons = ResolveButtons();
            _playButton = GetRequiredButton(buttons, PlayButtonName);
            _settingsButton = GetRequiredButton(buttons, SettingsButtonName);
            _exitButton = GetRequiredButton(buttons, ExitButtonName);
        }

        private void OnEnable()
        {
            _playButton.onClick.AddListener(OnPlayClicked);
            _settingsButton.onClick.AddListener(OnSettingsClicked);
            _exitButton.onClick.AddListener(OnExitClicked);
        }

        private void OnDisable()
        {
            _playButton.onClick.RemoveListener(OnPlayClicked);
            _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            _exitButton.onClick.RemoveListener(OnExitClicked);
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
                return button;

            throw new MissingComponentException($"{nameof(MainMenuWindowView)} requires child Button '{buttonName}'.");
        }

        private void OnPlayClicked() => PlayClicked?.Invoke();
        private void OnSettingsClicked() => SettingsClicked?.Invoke();
        private void OnExitClicked() => ExitClicked?.Invoke();
    }
}
