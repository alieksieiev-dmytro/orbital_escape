using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    public readonly struct PaletteSnapshot
    {
        public Color MainColor { get; }
        public Color OutlineColor { get; }
        public float Progress { get; }

        public PaletteSnapshot(Color mainColor, Color outlineColor, float progress)
        {
            MainColor = mainColor;
            OutlineColor = outlineColor;
            Progress = progress;
        }
    }
}
