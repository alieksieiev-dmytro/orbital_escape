using System;
using OrbitalEscape.Events;
using OrbitalEscape.UI;
using OrbitalEscape.Visuals;
using Zenject;

namespace OrbitalEscape.Movement
{
    public sealed class PlayerDeathSequenceController : IInitializable, IDisposable
    {
        private readonly IPlayerDeathAnimation _deathAnimation;
        private readonly IUIManager _uiManager;
        private EventBinding<PlayerDiedEvent> _playerDiedBinding;
        private bool _isSequenceRunning;
        private bool _isDisposed;
        private int _sequenceVersion;

        [Inject]
        public PlayerDeathSequenceController(IPlayerDeathAnimation deathAnimation, IUIManager uiManager)
        {
            _deathAnimation = deathAnimation;
            _uiManager = uiManager;
        }

        public void Initialize()
        {
            _playerDiedBinding = new EventBinding<PlayerDiedEvent>(OnPlayerDied);
            EventBus<PlayerDiedEvent>.Register(_playerDiedBinding);
        }

        public void Dispose()
        {
            _isDisposed = true;

            if (_playerDiedBinding != null)
            {
                EventBus<PlayerDiedEvent>.Deregister(_playerDiedBinding);
            }
        }

        private void OnPlayerDied()
        {
            if (_isSequenceRunning)
            {
                return;
            }

            _isSequenceRunning = true;
            int version = ++_sequenceVersion;
            _deathAnimation.Play(() =>
            {
                if (version == _sequenceVersion)
                    OnAnimationCompleted();
            });
        }

        public void ResetForRun()
        {
            _sequenceVersion++;
            _isSequenceRunning = false;
            _deathAnimation.ResetForRun();
        }

        private void OnAnimationCompleted()
        {
            if (_isDisposed)
            {
                return;
            }

            _uiManager.OpenWindow(WindowType.Restart, null);
        }
    }
}
