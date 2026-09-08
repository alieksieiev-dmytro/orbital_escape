using System;
using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    [Serializable]
    public struct MainMenuPalettePreset
    {
        [SerializeField] private Color _backgroundColor;
        [SerializeField] private Color _environmentColor;
        [SerializeField] private Color _accentColor;

        public PaletteSnapshot ToSnapshot()
        {
            return new PaletteSnapshot(_backgroundColor, _environmentColor, _accentColor, 0.0f);
        }
    }
}
