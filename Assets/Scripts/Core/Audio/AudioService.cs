using System;
using DG.Tweening;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace OrbitalEscape.Audio
{
    public sealed class AudioService : IAudioService, IInitializable, IDisposable
    {
        private const string RootName = "AudioService";
        private const string MusicSourceName = "Music";
        private const string SfxSourceName = "SFX";
        private const float NormalLowPassCutoff = 22000.0f;

        private readonly GameAudioConfig _config;

        private GameObject _root;
        private AudioSource _musicSource;
        private AudioLowPassFilter _musicLowPassFilter;
        private AudioSource _sfxSource;
        private Tween _musicVolumeTween;
        private Tween _musicLowPassTween;
        private float _masterVolume;
        private float _musicVolume;
        private float _sfxVolume;
        private bool _musicObscured;

        public AudioService(GameAudioConfig config)
        {
            _config = config;
        }

        public void Initialize()
        {
            _masterVolume = ClampVolume(_config != null ? _config.MasterVolume : 1.0f);
            _musicVolume = ClampVolume(_config != null ? _config.MusicVolume : 1.0f);
            _sfxVolume = ClampVolume(_config != null ? _config.SfxVolume : 1.0f);
            EnsureRuntime();
            ApplySourceVolumes();
        }

        public void PlayMusic(MusicTrackType trackType, bool restart)
        {
            EnsureRuntime();

            AudioClip clip = _config != null ? _config.GetMusicClip(trackType) : null;
            if (clip == null)
            {
                StopMusic();
                return;
            }

            bool sameClip = _musicSource.clip == clip;
            if (sameClip && _musicSource.isPlaying && !restart)
            {
                return;
            }

            _musicVolumeTween?.Kill();
            _musicSource.clip = clip;
            _musicSource.loop = _config == null || _config.MusicLoops;
            _musicSource.time = 0.0f;
            _musicSource.volume = 0.0f;
            ApplyLowPassInstantly();
            _musicSource.Play();
            FadeMusicTo(TargetMusicVolume);
        }

        public void StopMusic()
        {
            if (_musicSource == null || !_musicSource.isPlaying)
            {
                return;
            }

            _musicVolumeTween?.Kill();
            float fadeDuration = GetMusicFadeDuration();
            if (fadeDuration <= 0.0f)
            {
                StopMusicImmediately();
                return;
            }

            _musicVolumeTween = _musicSource
                .DOFade(0.0f, fadeDuration)
                .SetUpdate(true)
                .OnComplete(StopMusicImmediately);
        }

        public void PlaySfx(SfxType sfxType)
        {
            EnsureRuntime();

            AudioClip clip = _config != null ? _config.GetSfxClip(sfxType) : null;
            if (clip == null)
            {
                return;
            }

            _sfxSource.PlayOneShot(clip, EffectiveSfxVolume);
        }

        public void SetMusicObscured(bool obscured)
        {
            if (_musicObscured == obscured)
            {
                return;
            }

            _musicObscured = obscured;
            EnsureRuntime();
            FadeMusicTo(TargetMusicVolume);
            FadeLowPassTo(TargetLowPassCutoff);
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = ClampVolume(volume);
            ApplySourceVolumes();
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = ClampVolume(volume);
            ApplySourceVolumes();
        }

        public void SetSfxVolume(float volume)
        {
            _sfxVolume = ClampVolume(volume);
            ApplySourceVolumes();
        }

        public void Dispose()
        {
            _musicVolumeTween?.Kill();
            _musicLowPassTween?.Kill();

            if (_root != null)
            {
                Object.Destroy(_root);
            }

            _root = null;
            _musicSource = null;
            _musicLowPassFilter = null;
            _sfxSource = null;
        }

        private void EnsureRuntime()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject(RootName);
            Object.DontDestroyOnLoad(_root);

            _musicSource = CreateAudioSource(MusicSourceName);
            _musicSource.playOnAwake = false;
            _musicSource.loop = _config == null || _config.MusicLoops;
            _musicSource.outputAudioMixerGroup = _config != null ? _config.MusicMixerGroup : null;

            _musicLowPassFilter = _musicSource.gameObject.AddComponent<AudioLowPassFilter>();
            _musicLowPassFilter.cutoffFrequency = TargetLowPassCutoff;
            _musicLowPassFilter.lowpassResonanceQ = TargetLowPassResonance;

            _sfxSource = CreateAudioSource(SfxSourceName);
            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;
            _sfxSource.volume = 1.0f;
            _sfxSource.outputAudioMixerGroup = _config != null ? _config.SfxMixerGroup : null;
        }

        private AudioSource CreateAudioSource(string sourceName)
        {
            var sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(_root.transform, false);
            return sourceObject.AddComponent<AudioSource>();
        }

        private void ApplySourceVolumes()
        {
            if (_musicSource != null && _musicVolumeTween == null)
            {
                _musicSource.volume = TargetMusicVolume;
            }

            if (_sfxSource != null)
            {
                _sfxSource.volume = 1.0f;
            }
        }

        private void FadeMusicTo(float volume)
        {
            float fadeDuration = GetMusicFadeDuration();
            if (fadeDuration <= 0.0f)
            {
                _musicSource.volume = volume;
                _musicVolumeTween = null;
                return;
            }

            _musicVolumeTween?.Kill();
            _musicVolumeTween = _musicSource
                .DOFade(volume, fadeDuration)
                .SetUpdate(true)
                .OnComplete(() => _musicVolumeTween = null);
        }

        private void FadeLowPassTo(float cutoffFrequency)
        {
            if (_musicLowPassFilter == null)
            {
                return;
            }

            float duration = GetObscuredMusicTransitionDuration();
            _musicLowPassFilter.lowpassResonanceQ = TargetLowPassResonance;
            if (duration <= 0.0f)
            {
                _musicLowPassFilter.cutoffFrequency = cutoffFrequency;
                _musicLowPassTween = null;
                return;
            }

            _musicLowPassTween?.Kill();
            _musicLowPassTween = DOTween
                .To(
                    () => _musicLowPassFilter.cutoffFrequency,
                    value => _musicLowPassFilter.cutoffFrequency = value,
                    cutoffFrequency,
                    duration)
                .SetUpdate(true)
                .OnComplete(() => _musicLowPassTween = null);
        }

        private void ApplyLowPassInstantly()
        {
            if (_musicLowPassFilter == null)
            {
                return;
            }

            _musicLowPassTween?.Kill();
            _musicLowPassFilter.cutoffFrequency = TargetLowPassCutoff;
            _musicLowPassFilter.lowpassResonanceQ = TargetLowPassResonance;
            _musicLowPassTween = null;
        }

        private void StopMusicImmediately()
        {
            if (_musicSource == null)
            {
                return;
            }

            _musicSource.Stop();
            _musicSource.clip = null;
            _musicVolumeTween = null;
        }

        private float GetMusicFadeDuration()
        {
            return _config != null ? Mathf.Max(0.0f, _config.MusicFadeDuration) : 0.0f;
        }

        private float GetObscuredMusicTransitionDuration()
        {
            return _config != null ? Mathf.Max(0.0f, _config.ObscuredMusicTransitionDuration) : 0.0f;
        }

        private static float ClampVolume(float volume)
        {
            return Mathf.Clamp01(volume);
        }

        private float TargetMusicVolume => EffectiveMusicVolume * ObscuredMusicVolumeMultiplier;
        private float EffectiveMusicVolume => _masterVolume * _musicVolume;
        private float EffectiveSfxVolume => _masterVolume * _sfxVolume;
        private float ObscuredMusicVolumeMultiplier => _musicObscured
            ? ClampVolume(_config != null ? _config.ObscuredMusicVolumeMultiplier : 0.22f)
            : 1.0f;
        private float TargetLowPassCutoff => _musicObscured
            ? Mathf.Clamp(_config != null ? _config.ObscuredMusicLowPassCutoff : 850.0f, 10.0f, NormalLowPassCutoff)
            : NormalLowPassCutoff;
        private float TargetLowPassResonance => Mathf.Clamp(
            _config != null ? _config.ObscuredMusicLowPassResonance : 1.4f,
            1.0f,
            10.0f);
    }
}
