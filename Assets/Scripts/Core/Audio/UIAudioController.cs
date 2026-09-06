using System;
using OrbitalEscape.Events;
using Zenject;

namespace OrbitalEscape.Audio
{
    public sealed class UIAudioController : IInitializable, IDisposable
    {
        private readonly IAudioService _audioService;
        private readonly EventBinding<UIButtonSoundRequestedEvent> _soundBinding;

        public UIAudioController(IAudioService audioService)
        {
            _audioService = audioService;
            _soundBinding = new EventBinding<UIButtonSoundRequestedEvent>(OnSoundRequested);
        }

        public void Initialize() => EventBus<UIButtonSoundRequestedEvent>.Register(_soundBinding);

        public void Dispose() => EventBus<UIButtonSoundRequestedEvent>.Deregister(_soundBinding);

        private void OnSoundRequested(UIButtonSoundRequestedEvent soundEvent)
        {
            _audioService.PlaySfx(soundEvent.Sound);
        }
    }
}
