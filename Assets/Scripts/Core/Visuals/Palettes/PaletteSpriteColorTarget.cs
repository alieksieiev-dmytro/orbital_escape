using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PaletteSpriteColorTarget : MonoBehaviour, IPaletteTarget
    {
        [SerializeField] private SpriteRenderer _targetRenderer;
        [SerializeField] private PaletteColorType _colorType = PaletteColorType.Accent;
        [SerializeField] private bool _preserveCurrentAlpha = true;

        protected virtual void Reset()
        {
            ResolveRenderer();
        }

        protected virtual void Awake()
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
            if (_preserveCurrentAlpha)
            {
                color.a = _targetRenderer.color.a;
            }

            _targetRenderer.color = color;
        }

        protected virtual Color GetPaletteColor(PaletteSnapshot palette)
        {
            return palette.GetColor(_colorType);
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
