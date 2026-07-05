using System;
using System.Collections.Generic;
using OrbitalEscape.Difficulty;
using OrbitalEscape.Input;
using Zenject;

namespace OrbitalEscape.UI
{
    public sealed class SelectGameModeWindowController : BaseController<SelectGameModeWindowView>
    {
        private readonly IViewFactory _viewFactory;
        private readonly IUIManager _uiManager;
        private readonly ISelectedDifficultyService _difficultyService;
        private readonly DifficultySelectionConfig _selectionConfig;
        private readonly Command _playCommand;

        private IReadOnlyList<DifficultySelectionOption> _options;
        private int _currentIndex;

        public override IConvertible Type => WindowType.SelectGameMode;

        [Inject]
        public SelectGameModeWindowController(
            IViewFactory viewFactory,
            IUIManager uiManager,
            ISelectedDifficultyService difficultyService,
            DifficultySelectionConfig selectionConfig,
            [Inject(Id = nameof(CommandType.Play))] Command playCommand)
        {
            _viewFactory = viewFactory;
            _uiManager = uiManager;
            _difficultyService = difficultyService;
            _selectionConfig = selectionConfig;
            _playCommand = playCommand;
        }

        public override void Initialize(ScreenData screenData)
        {
            _options = _selectionConfig.CreateOptions();
            _currentIndex = FindSelectedOptionIndex();

            View.PreviousClicked += OnPreviousClicked;
            View.NextClicked += OnNextClicked;
            View.BackClicked += OnBackClicked;
            View.PlayClicked += OnPlayClicked;
            View.Show();
            RefreshView();
        }

        public override void Complete()
        {
            View.PreviousClicked -= OnPreviousClicked;
            View.NextClicked -= OnNextClicked;
            View.BackClicked -= OnBackClicked;
            View.PlayClicked -= OnPlayClicked;
            View.Hide();
        }

        protected override SelectGameModeWindowView CreateView()
        {
            return _viewFactory.Create<SelectGameModeWindowView>(nameof(WindowType.SelectGameMode));
        }

        private int FindSelectedOptionIndex()
        {
            if (_options == null || _options.Count == 0)
            {
                return 0;
            }

            DifficultyType currentDifficulty = _difficultyService.CurrentDifficulty;
            for (int i = 0; i < _options.Count; i++)
            {
                if (_options[i].Difficulty == currentDifficulty)
                {
                    return i;
                }
            }

            return 0;
        }

        private void RefreshView()
        {
            if (_options == null || _options.Count == 0)
            {
                return;
            }

            View.SetOption(_options[_currentIndex], _currentIndex, _options.Count);
        }

        private void OnPreviousClicked()
        {
            MoveSelection(-1);
        }

        private void OnNextClicked()
        {
            MoveSelection(1);
        }

        private void OnBackClicked()
        {
            Complete();
            _uiManager.OpenWindow(WindowType.MainMenu, null);
        }

        private void OnPlayClicked()
        {
            if (_options != null && _options.Count > 0)
            {
                _difficultyService.SetDifficulty(_options[_currentIndex].Difficulty);
            }

            Complete();
            _playCommand.Execute();
        }

        private void MoveSelection(int direction)
        {
            if (_options == null || _options.Count <= 1)
            {
                return;
            }

            _currentIndex = (_currentIndex + direction + _options.Count) % _options.Count;
            RefreshView();
        }
    }
}
