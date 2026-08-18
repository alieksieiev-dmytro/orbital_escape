using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TrailRenderer))]
    public sealed class PaletteTrailColorTarget : MonoBehaviour, IPaletteTarget
    {
        [SerializeField] private TrailRenderer _targetRenderer;
        [SerializeField] private PaletteColorType _colorType = PaletteColorType.Accent;
        [SerializeField] private bool _preserveCurrentAlpha = true;

        private GradientColorKey[] _colorKeys;
        private GradientAlphaKey[] _alphaKeys;

        private void Reset()
        {
            ResolveRenderer();
        }

        private void Awake()
        {
            ResolveRenderer();
            CacheGradientKeys();
        }

        public void ApplyPalette(PaletteSnapshot palette)
        {
            if (_targetRenderer == null)
            {
                ResolveRenderer();
            }

            if (_targetRenderer == null)
            {
                return;
            }

            if (_colorKeys == null || _alphaKeys == null)
            {
                CacheGradientKeys();
            }

            Color color = palette.GetColor(_colorType);
            Gradient gradient = new Gradient();
            gradient.SetKeys(BuildColorKeys(color), BuildAlphaKeys(color.a));
            _targetRenderer.colorGradient = gradient;
        }

        private void CacheGradientKeys()
        {
            if (_targetRenderer == null)
            {
                return;
            }

            Gradient gradient = _targetRenderer.colorGradient;
            _colorKeys = gradient.colorKeys;
            _alphaKeys = gradient.alphaKeys;
        }

        private GradientColorKey[] BuildColorKeys(Color color)
        {
            GradientColorKey[] keys = (GradientColorKey[])_colorKeys.Clone();
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i].color = color;
            }

            return keys;
        }

        private GradientAlphaKey[] BuildAlphaKeys(float paletteAlpha)
        {
            GradientAlphaKey[] keys = (GradientAlphaKey[])_alphaKeys.Clone();
            if (_preserveCurrentAlpha)
            {
                return keys;
            }

            for (int i = 0; i < keys.Length; i++)
            {
                keys[i].alpha *= paletteAlpha;
            }

            return keys;
        }

        private void ResolveRenderer()
        {
            if (_targetRenderer == null)
            {
                _targetRenderer = GetComponent<TrailRenderer>();
            }
        }
    }
}
