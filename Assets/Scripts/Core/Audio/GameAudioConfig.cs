using OrbitalEscape.Difficulty;
using UnityEngine;
using UnityEngine.Audio;

namespace OrbitalEscape.Audio
{
    [CreateAssetMenu(fileName = "GameAudioConfig", menuName = "Scriptable Objects/Configs/Game Audio Config")]
    public sealed class GameAudioConfig : ScriptableObject
    {
        [SerializeField] private MusicProvider _mainMenuMusicProvider;
        [SerializeField] private DifficultyMusicProfile[] _gameplayMusicProfiles;
        [field: SerializeField] public AudioClip JumpSfx { get; private set; }
        [field: SerializeField] public AudioClip DeathSfx { get; private set; }
        [field: SerializeField] public AudioClip RestartSfx { get; private set; }
        [field: SerializeField] public AudioClip UIFocusSfx { get; private set; }
        [field: SerializeField] public AudioClip UIClickSfx { get; private set; }
        [field: SerializeField] public AudioMixer AudioMixer { get; private set; }
        [field: SerializeField] public AudioMixerGroup MusicMixerGroup { get; private set; }
        [field: SerializeField] public AudioMixerGroup SfxMixerGroup { get; private set; }
        [field: SerializeField, Range(0.0f, 1.0f)] public float MasterVolume { get; private set; } = 1.0f;
        [field: SerializeField, Range(0.0f, 1.0f)] public float MusicVolume { get; private set; } = 1.0f;
        [field: SerializeField, Range(0.0f, 1.0f)] public float SfxVolume { get; private set; } = 1.0f;
        [field: SerializeField, Min(0.0f)] public float MusicFadeDuration { get; private set; } = 0.25f;
        [field: SerializeField, Min(0.0f), Tooltip("Fade in from silence when music starts or restarts, in seconds. Zero disables the fade.")] public float MusicStartFadeDuration { get; private set; } = 0.6f;
        [field: SerializeField] public bool MusicLoops { get; private set; } = true;
        [field: SerializeField, Range(0.0f, 1.0f)] public float ObscuredMusicVolumeMultiplier { get; private set; } = 0.22f;
        [field: SerializeField, Min(10.0f)] public float ObscuredMusicLowPassCutoff { get; private set; } = 850.0f;
        [field: SerializeField, Range(1.0f, 10.0f)] public float ObscuredMusicLowPassResonance { get; private set; } = 1.4f;
        [field: SerializeField, Min(0.0f)] public float ObscuredMusicTransitionDuration { get; private set; } = 0.2f;

        public MusicPlaybackSelection GetMainMenuMusic()
        {
            return _mainMenuMusicProvider != null ? _mainMenuMusicProvider.GetSelection() : default;
        }

        public MusicPlaybackSelection GetGameplayMusic(DifficultyType difficulty)
        {
            if (_gameplayMusicProfiles == null)
            {
                return default;
            }

            for (int i = 0; i < _gameplayMusicProfiles.Length; i++)
            {
                DifficultyMusicProfile profile = _gameplayMusicProfiles[i];
                if (profile != null && profile.Difficulty == difficulty)
                {
                    return profile.GetSelection();
                }
            }

            return default;
        }

        public AudioClip GetSfxClip(SfxType sfxType)
        {
            return sfxType switch
            {
                SfxType.Jump => JumpSfx,
                SfxType.Death => DeathSfx,
                SfxType.Restart => RestartSfx,
                SfxType.UIFocus => UIFocusSfx,
                SfxType.UIClick => UIClickSfx,
                _ => null
            };
        }
    }
}
