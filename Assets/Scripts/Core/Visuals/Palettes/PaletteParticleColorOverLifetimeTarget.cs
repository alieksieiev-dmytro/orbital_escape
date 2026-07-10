using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    [DisallowMultipleComponent]
    public sealed class PaletteParticleColorOverLifetimeTarget : MonoBehaviour, IPaletteTarget
    {
        [SerializeField] private ParticleSystem _targetParticles;

        private GradientColorKey[] _colorKeys;
        private GradientAlphaKey[] _alphaKeys;

        private void Reset()
        {
            ResolveParticles();
        }

        private void Awake()
        {
            ResolveParticles();
            CacheAlphaKeys();
        }

        public void ApplyPalette(PaletteSnapshot palette)
        {
            if (_targetParticles == null)
            {
                return;
            }

            if (_alphaKeys == null || _alphaKeys.Length == 0)
            {
                CacheAlphaKeys();
            }

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = _targetParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(BuildGradient(palette.AccentColor));
        }

        private void CacheAlphaKeys()
        {
            if (_targetParticles == null)
            {
                return;
            }

            Gradient sourceGradient = GetSourceGradient(_targetParticles.colorOverLifetime.color);
            _colorKeys = CloneColorKeys(sourceGradient != null ? sourceGradient.colorKeys : null);
            _alphaKeys = CloneAlphaKeys(sourceGradient != null ? sourceGradient.alphaKeys : null);

            if (_colorKeys.Length == 0)
            {
                _colorKeys = new[]
                {
                    new GradientColorKey(Color.white, 0.0f),
                    new GradientColorKey(Color.white, 1.0f)
                };
            }

            if (_alphaKeys.Length == 0)
            {
                _alphaKeys = new[]
                {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.0f, 1.0f)
                };
            }
        }

        private Gradient BuildGradient(Color color)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                BuildColorKeys(color),
                CloneAlphaKeys(_alphaKeys));
            return gradient;
        }

        private GradientColorKey[] BuildColorKeys(Color color)
        {
            GradientColorKey[] keys = CloneColorKeys(_colorKeys);
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i].color = color;
            }

            return keys;
        }

        private Gradient GetSourceGradient(ParticleSystem.MinMaxGradient color)
        {
            if (color.mode == ParticleSystemGradientMode.Gradient)
            {
                return color.gradient;
            }

            if (color.mode == ParticleSystemGradientMode.TwoGradients)
            {
                return color.gradientMax;
            }

            return null;
        }

        private GradientColorKey[] CloneColorKeys(GradientColorKey[] keys)
        {
            if (keys == null)
            {
                return new GradientColorKey[0];
            }

            GradientColorKey[] clone = new GradientColorKey[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                clone[i] = keys[i];
            }

            return clone;
        }

        private GradientAlphaKey[] CloneAlphaKeys(GradientAlphaKey[] keys)
        {
            if (keys == null)
            {
                return new GradientAlphaKey[0];
            }

            GradientAlphaKey[] clone = new GradientAlphaKey[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                clone[i] = keys[i];
            }

            return clone;
        }

        private void ResolveParticles()
        {
            if (_targetParticles == null)
            {
                _targetParticles = GetComponent<ParticleSystem>();
            }
        }
    }
}
