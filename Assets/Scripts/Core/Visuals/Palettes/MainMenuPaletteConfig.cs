using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    [CreateAssetMenu(
        fileName = "MainMenuPaletteConfig",
        menuName = "Scriptable Objects/Configs/Main Menu Palette Config")]
    public sealed class MainMenuPaletteConfig : ScriptableObject
    {
        [SerializeField] private MainMenuPalettePreset[] _presets;

        public int PresetCount => _presets?.Length ?? 0;

        public PaletteSnapshot GetPalette(int index)
        {
            if (PresetCount == 0)
            {
                return new PaletteSnapshot(
                    new Color(0.02f, 0.0f, 0.08f, 1.0f),
                    new Color(0.05f, 0.9f, 1.0f, 1.0f),
                    Color.white,
                    0.0f);
            }

            return _presets[Mathf.Clamp(index, 0, PresetCount - 1)].ToSnapshot();
        }
    }
}
