using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PaletteSpriteColorTarget : MonoBehaviour, IPaletteTarget
    {
        [SerializeField] private SpriteRenderer _targetRenderer;

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

            Color color = palette.OutlineColor;
            _targetRenderer.color = color;
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
