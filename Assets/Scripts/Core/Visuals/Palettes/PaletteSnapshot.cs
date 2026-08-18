using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    public readonly struct PaletteSnapshot
    {
        public Color BackgroundColor { get; }
        public Color EnvironmentColor { get; }
        public Color AccentColor { get; }
        public float Progress { get; }

        public PaletteSnapshot(Color backgroundColor, Color environmentColor, Color accentColor, float progress)
        {
            BackgroundColor = backgroundColor;
            EnvironmentColor = environmentColor;
            AccentColor = accentColor;
            Progress = progress;
        }

        public Color GetColor(PaletteColorType colorType)
        {
            return colorType switch
            {
                PaletteColorType.Background => BackgroundColor,
                PaletteColorType.Environment => EnvironmentColor,
                _ => AccentColor
            };
        }
    }
}
