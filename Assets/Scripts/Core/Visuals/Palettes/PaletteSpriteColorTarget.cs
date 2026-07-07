using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PaletteSpriteColorTarget : MonoBehaviour, IPaletteTarget
    {
        [SerializeField] private SpriteRenderer _targetRenderer;
        [SerializeField] private PaletteSpriteColorType _colorType = PaletteSpriteColorType.Accent;

        private void Reset()
        {
            ResolveRenderer();
        }

        private void Awake()
        {
            ResolveRenderer();
        }

        public void SetAlpha(float alpha)
        {
            if (_targetRenderer == null)
            {
                ResolveRenderer();
            }

            if (_targetRenderer == null)
            {
                return;
            }

            Color color = _targetRenderer.color;
            color.a = alpha;
            _targetRenderer.color = color;
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

            Color color = GetPaletteColor(palette);
            color.a = _targetRenderer.color.a;
            _targetRenderer.color = color;
        }

        private Color GetPaletteColor(PaletteSnapshot palette)
        {
            return _colorType switch
            {
                PaletteSpriteColorType.Background => palette.BackgroundColor,
                PaletteSpriteColorType.Environment => palette.EnvironmentColor,
                _ => palette.AccentColor
            };
        }

        private void ResolveRenderer()
        {
            if (_targetRenderer == null)
            {
                _targetRenderer = GetComponent<SpriteRenderer>();
            }
        }
    }
}
