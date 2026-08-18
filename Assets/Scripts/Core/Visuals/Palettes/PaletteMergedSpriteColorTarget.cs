using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    public sealed class PaletteMergedSpriteColorTarget : PaletteSpriteColorTarget
    {
        [SerializeField] private Color _mergeColour = Color.white;
        [SerializeField, Range(0.0f, 1.0f)] private float _mergePercent = 0.5f;

        protected override Color GetPaletteColor(PaletteSnapshot palette)
        {
            return Color.Lerp(base.GetPaletteColor(palette), _mergeColour, _mergePercent);
        }
    }
}
