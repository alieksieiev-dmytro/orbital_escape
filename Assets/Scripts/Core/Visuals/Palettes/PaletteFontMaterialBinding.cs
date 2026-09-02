using System;
using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    [Serializable]
    public sealed class PaletteFontMaterialBinding
    {
        [SerializeField] private Material _material;
        [SerializeField] private string _colorProperty = "_FaceColor";
        [SerializeField] private PaletteColorType _colorType = PaletteColorType.Environment;
        [SerializeField] private bool _preserveCurrentAlpha = true;
        [SerializeField] private Color _mergeColour = Color.white;
        [SerializeField, Range(0.0f, 1.0f)] private float _mergePercent;

        public bool TryGetCurrentColor(out Color color)
        {
            color = default;

            if (!TryGetColorPropertyId(out int colorPropertyId))
            {
                return false;
            }

            color = _material.GetColor(colorPropertyId);
            return true;
        }

        public void ApplyPalette(PaletteSnapshot palette)
        {
            if (!TryGetColorPropertyId(out int colorPropertyId))
            {
                return;
            }

            Color color = Color.Lerp(
                palette.GetColor(_colorType),
                _mergeColour,
                _mergePercent);

            if (_preserveCurrentAlpha)
            {
                color.a = _material.GetColor(colorPropertyId).a;
            }

            _material.SetColor(colorPropertyId, color);
        }

        public void SetColor(Color color)
        {
            if (TryGetColorPropertyId(out int colorPropertyId))
            {
                _material.SetColor(colorPropertyId, color);
            }
        }

        private bool TryGetColorPropertyId(out int colorPropertyId)
        {
            colorPropertyId = 0;

            if (_material == null || string.IsNullOrWhiteSpace(_colorProperty))
            {
                return false;
            }

            colorPropertyId = Shader.PropertyToID(_colorProperty);
            return _material.HasProperty(colorPropertyId);
        }
    }
}
