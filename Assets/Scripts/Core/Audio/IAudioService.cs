namespace OrbitalEscape.Audio
{
    public interface IAudioService
    {
        void PlayMusic(MusicTrackType trackType, bool restart);
        void StopMusic();
        void PlaySfx(SfxType sfxType);
        void SetMusicObscured(bool obscured);
        void SetMasterVolume(float volume);
        void SetMusicVolume(float volume);
        void SetSfxVolume(float volume);
    }
}
