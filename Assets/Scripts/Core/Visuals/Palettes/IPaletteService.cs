using System;

namespace OrbitalEscape.Visuals.Palettes
{
    public interface IPaletteService
    {
        PaletteSnapshot CurrentPalette { get; }
        event Action<PaletteSnapshot> PaletteChanged;
    }
}
