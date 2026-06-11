using Zenject;

namespace OrbitalEscape.Audio
{
    public sealed class MainMenuAudioController : IInitializable
    {
        private readonly IAudioService _audioService;

        public MainMenuAudioController(IAudioService audioService)
        {
            _audioService = audioService;
        }

        public void Initialize()
        {
            _audioService.SetMusicObscured(false);
            _audioService.PlayMusic(MusicTrackType.MainMenu, true);
        }
    }
}
