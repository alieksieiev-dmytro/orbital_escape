using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Visuals.Palettes
{
    public sealed class PaletteFontMaterialController : IInitializable, IDisposable
    {
        private readonly IPaletteService _paletteService;
        private readonly PaletteFontMaterialConfig _config;
        private readonly Dictionary<PaletteFontMaterialBinding, Color> _originalColors = new();

        [Inject]
        public PaletteFontMaterialController(
            IPaletteService paletteService,
            PaletteFontMaterialConfig config)
        {
            _paletteService = paletteService;
            _config = config;
        }

        public void Initialize()
        {
            foreach (var binding in _config.Bindings)
            {
                if (binding != null && binding.TryGetCurrentColor(out Color color))
                {
                    _originalColors[binding] = color;
                }
            }

            _paletteService.PaletteChanged += ApplyPalette;
            ApplyPalette(_paletteService.CurrentPalette);
        }

        public void Dispose()
        {
            _paletteService.PaletteChanged -= ApplyPalette;

            foreach (var entry in _originalColors)
            {
                entry.Key.SetColor(entry.Value);
            }

            _originalColors.Clear();
        }

        private void ApplyPalette(PaletteSnapshot palette)
        {
            foreach (var binding in _config.Bindings)
            {
                binding?.ApplyPalette(palette);
            }
        }
    }
}
