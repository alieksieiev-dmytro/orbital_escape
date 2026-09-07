using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using OrbitalEscape.Events;
using OrbitalEscape.Input;
using OrbitalEscape.Movement;
using OrbitalEscape.Orbits;
using OrbitalEscape.Orbits.Generators;
using OrbitalEscape.Pause;
using OrbitalEscape.Profiling;
using OrbitalEscape.Score;
using OrbitalEscape.UI;
using OrbitalEscape.Visuals.Palettes;
using UnityEngine;

namespace OrbitalEscape.Restart
{
    public sealed class GameRestartService : IGameRestartService, IDisposable
    {
        private readonly NextRunPreparationService _preparation;
        private readonly LevelManager _level;
        private readonly PlayerController _player;
        private readonly PlayerMovementController _movement;
        private readonly PlayerOrbitState _orbits;
        private readonly JumpBehaviour _jump;
        private readonly PlayerDeathSequenceController _death;
        private readonly IScoreService _score;
        private readonly PaletteSceneController _palette;
        private readonly IGamePauseService _pause;
        private readonly IInputGate _input;
        private readonly IUIManager _ui;
        private readonly IResourceProfilerService _profiler;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private bool _ownsInputLock;
        private bool _needsRecovery;
        private bool _disposed;
        private int _lastRestartFrame = -1;

        public bool IsRestarting { get; private set; }

        public GameRestartService(NextRunPreparationService preparation, LevelManager level,
            PlayerController player, PlayerMovementController movement, PlayerOrbitState orbits,
            JumpBehaviour jump, PlayerDeathSequenceController death, IScoreService score,
            PaletteSceneController palette, IGamePauseService pause, IInputGate input,
            IUIManager ui, IResourceProfilerService profiler)
        {
            _preparation = preparation;
            _level = level;
            _player = player;
            _movement = movement;
            _orbits = orbits;
            _jump = jump;
            _death = death;
            _score = score;
            _palette = palette;
            _pause = pause;
            _input = input;
            _ui = ui;
            _profiler = profiler;
        }

        public async UniTask RestartAsync()
        {
            if (IsRestarting || _disposed || _lastRestartFrame == Time.frameCount)
                return;

            _lastRestartFrame = Time.frameCount;
            IsRestarting = true;
            bool wasPaused = _pause.IsPaused;
            IDisposable interaction = null;
            CancellationToken token = _lifetime.Token;
            try
            {
                if (!_ownsInputLock)
                {
                    _input.Lock();
                    _ownsInputLock = true;
                }
                _input.SuppressForCurrentFrame();
                interaction = _ui.SuspendInteraction();
                _pause.Pause();
                EventBus<RestartRequestedEvent>.Raise(new RestartRequestedEvent());

                LevelBuildResult opening = await _preparation.TakeAsync(token);
                token.ThrowIfCancellationRequested();
                if (opening.Orbits.Count < 2)
                    throw new InvalidOperationException("A fresh run requires at least two orbits.");

                if (_profiler.IsProfiling)
                    _profiler.StopProfiling();

                // Do not yield between clearing the old content and exposing the fresh run.
                _needsRecovery = true;
                _player.BeginReset();
                _jump.ResetForRun();
                _level.ApplyPreparedRun(opening);
                float radius = _orbits.GetMovementRadius(_orbits.CurrentOrbit, _orbits.OppositeOrbit);
                _movement.ResetForRun(radius);
                _death.ResetForRun();
                _score.ResetForRun();
                _palette.Refresh();
                _player.CompleteReset();
                _ui.CloseActiveWindow();
                EventBus<GameplayRunRestartedEvent>.Raise(new GameplayRunRestartedEvent());
                _pause.Resume();
                _needsRecovery = false;
                _preparation.Prepare();
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (_needsRecovery && !_disposed)
                {
                    _movement.StopMovement();
                    _pause.Pause();
                    _ui.OpenWindow(WindowType.Restart, null);
                }
                else if (!wasPaused && !_disposed)
                {
                    _pause.Resume();
                }
            }
            finally
            {
                interaction?.Dispose();
                if (!_needsRecovery || _disposed)
                    ReleaseInputLock();
                IsRestarting = false;
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _lifetime.Cancel();
            _lifetime.Dispose();
            ReleaseInputLock();
        }

        private void ReleaseInputLock()
        {
            if (!_ownsInputLock)
                return;
            _input.SuppressForCurrentFrame();
            _input.Unlock();
            _ownsInputLock = false;
        }
    }
}
