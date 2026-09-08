using System;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Visuals.Palettes
{
    public sealed class MainMenuPaletteService : IPaletteService, IInitializable
    {
        public PaletteSnapshot CurrentPalette { get; }
        public event Action<PaletteSnapshot> PaletteChanged;

        [Inject]
        public MainMenuPaletteService(MainMenuPaletteConfig config)
        {
            int presetIndex = config.PresetCount > 0
                ? UnityEngine.Random.Range(0, config.PresetCount)
                : 0;

            CurrentPalette = config.GetPalette(presetIndex);
        }

        public void Initialize()
        {
            PaletteChanged?.Invoke(CurrentPalette);
        }
    }
}
