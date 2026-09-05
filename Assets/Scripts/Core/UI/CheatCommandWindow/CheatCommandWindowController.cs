using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using OrbitalEscape.Audio;
using OrbitalEscape.Cheats;
using OrbitalEscape.Profiling;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.UI
{
    public sealed class CheatCommandWindowController : BaseController<CheatCommandWindowView>
    {
        private readonly IViewFactory _viewFactory;
        private readonly IUIManager _uiManager;
        private readonly CheatPanelConfig _config;
        private readonly ICheatCommandService _commandService;
        private readonly IResourceProfilerService _profilerService;
        private readonly IAudioService _audioService;
        private bool _isBusy;
        private CancellationTokenSource _operationCancellation;

        public override IConvertible Type => WindowType.CheatCommand;

        [Inject]
        public CheatCommandWindowController(
            IViewFactory viewFactory,
            IUIManager uiManager,
            CheatPanelConfig config,
            ICheatCommandService commandService,
            IResourceProfilerService profilerService,
            IAudioService audioService)
        {
            _viewFactory = viewFactory;
            _uiManager = uiManager;
            _config = config;
            _commandService = commandService;
            _profilerService = profilerService;
            _audioService = audioService;
        }

        public override void Initialize(ScreenData screenData)
        {
            if (!_config.ShouldShowCommandPanel)
            {
                _uiManager.OpenWindow(WindowType.Pause, null);
                return;
            }

            _audioService.SetMusicObscured(true);
            View.AddScoreClicked += OnAddScoreClicked;
            View.ClearObstaclesClicked += OnClearObstaclesClicked;
            View.GoToOrbitClicked += OnGoToOrbitClicked;
            View.ProfilingClicked += OnProfilingClicked;
            View.BackClicked += OnBackClicked;
            _profilerService.ProfilingStateChanged += OnProfilingStateChanged;
            View.SetProfilingVisible(_config.ShouldShowProfiling);
            View.SetProfilingActive(_profilerService.IsProfiling);
            View.Show();
        }

        public override void Complete()
        {
            _operationCancellation?.Cancel();
            _profilerService.ProfilingStateChanged -= OnProfilingStateChanged;
            _audioService.SetMusicObscured(false);
            if (IsNullView)
                return;

            View.AddScoreClicked -= OnAddScoreClicked;
            View.ClearObstaclesClicked -= OnClearObstaclesClicked;
            View.GoToOrbitClicked -= OnGoToOrbitClicked;
            View.ProfilingClicked -= OnProfilingClicked;
            View.BackClicked -= OnBackClicked;
            View.Hide();
        }

        protected override CheatCommandWindowView CreateView()
        {
            return _viewFactory.Create<CheatCommandWindowView>(nameof(WindowType.CheatCommand));
        }

        private void OnAddScoreClicked()
        {
            if (_isBusy)
                return;

            View.SetStatus(_commandService.AddScore().Message);
        }

        private void OnClearObstaclesClicked()
        {
            if (_isBusy)
                return;

            View.SetStatus(_commandService.ClearAllObstacles().Message);
        }

        private void OnProfilingClicked()
        {
            if (_isBusy)
                return;

            ResourceProfilerResult result = _profilerService.IsProfiling
                ? _profilerService.StopProfiling()
                : _profilerService.StartProfiling();

            View.SetProfilingActive(_profilerService.IsProfiling);
            View.SetStatus(result.Message);
        }

        private void OnProfilingStateChanged(bool isProfiling)
        {
            if (!IsNullView)
            {
                View.SetProfilingActive(isProfiling);
            }
        }

        private void OnGoToOrbitClicked(int orbitIndex)
        {
            if (_isBusy)
                return;

            GoToOrbitAsync(View, orbitIndex).Forget();
        }

        private async UniTaskVoid GoToOrbitAsync(CheatCommandWindowView view, int orbitIndex)
        {
            _isBusy = true;
            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(view.destroyCancellationToken);
            _operationCancellation = cancellation;
            view.SetBusy(true);
            view.SetStatus($"Moving to orbit {orbitIndex}...");

            try
            {
                CheatCommandResult result = await _commandService.GoToOrbitAsync(
                    orbitIndex,
                    cancellation.Token);
                if (!cancellation.IsCancellationRequested)
                    view.SetStatus(result.Message);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _operationCancellation = null;
                _isBusy = false;
                cancellation.Dispose();
                if (view != null && view.Root != null)
                {
                    view.SetBusy(false);
                }
            }
        }

        public override void RequestClose()
        {
            if (_isBusy)
                return;

            _uiManager.CloseWindow(Type);
            _uiManager.OpenWindow(WindowType.Pause, null);
        }

        private void OnBackClicked() => RequestClose();
    }
}
