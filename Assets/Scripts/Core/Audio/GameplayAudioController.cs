using System;
using OrbitalEscape.Difficulty;
using OrbitalEscape.Events;
using Zenject;

namespace OrbitalEscape.Audio
{
    public sealed class GameplayAudioController : IInitializable, IDisposable
    {
        private readonly IAudioService _audioService;
        private readonly IDifficultyService _difficultyService;
        private EventBinding<PlayerJumpedEvent> _playerJumpedBinding;
        private EventBinding<PlayerDiedEvent> _playerDiedBinding;
        private EventBinding<RestartRequestedEvent> _restartRequestedBinding;

        public GameplayAudioController(IAudioService audioService, IDifficultyService difficultyService)
        {
            _audioService = audioService;
            _difficultyService = difficultyService;
        }

        public void Initialize()
        {
            _audioService.SetMusicObscured(false);
            _audioService.PlayGameplayMusic(_difficultyService.CurrentDifficulty, true);

            _playerJumpedBinding = new EventBinding<PlayerJumpedEvent>(_ => _audioService.PlaySfx(SfxType.Jump));
            _playerDiedBinding = new EventBinding<PlayerDiedEvent>(_ => _audioService.PlaySfx(SfxType.Death));
            _restartRequestedBinding = new EventBinding<RestartRequestedEvent>(_ => _audioService.PlaySfx(SfxType.Restart));

            EventBus<PlayerJumpedEvent>.Register(_playerJumpedBinding);
            EventBus<PlayerDiedEvent>.Register(_playerDiedBinding);
            EventBus<RestartRequestedEvent>.Register(_restartRequestedBinding);
        }

        public void Dispose()
        {
            if (_playerJumpedBinding != null)
            {
                EventBus<PlayerJumpedEvent>.Deregister(_playerJumpedBinding);
            }

            if (_playerDiedBinding != null)
            {
                EventBus<PlayerDiedEvent>.Deregister(_playerDiedBinding);
            }

            if (_restartRequestedBinding != null)
            {
                EventBus<RestartRequestedEvent>.Deregister(_restartRequestedBinding);
            }
        }
    }
}
