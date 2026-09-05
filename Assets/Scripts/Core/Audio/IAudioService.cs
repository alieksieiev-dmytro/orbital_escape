using OrbitalEscape.Difficulty;

namespace OrbitalEscape.Audio
{
    public interface IAudioService
    {
        void PlayMainMenuMusic(bool restart);
        void PlayGameplayMusic(DifficultyType difficulty, bool restart);
        void StopMusic();
        void PlaySfx(SfxType sfxType);
        void SetMusicObscured(bool obscured);
        void SetMasterVolume(float volume);
        void SetMusicVolume(float volume);
        void SetSfxVolume(float volume);
    }
}
