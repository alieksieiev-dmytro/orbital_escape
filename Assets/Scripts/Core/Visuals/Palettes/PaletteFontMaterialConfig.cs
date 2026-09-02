using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    [CreateAssetMenu(
        fileName = "PaletteFontMaterialConfig",
        menuName = "Scriptable Objects/Configs/Palette Font Material Config")]
    public sealed class PaletteFontMaterialConfig : ScriptableObject
    {
        [SerializeField] private PaletteFontMaterialBinding[] _bindings;

        public IReadOnlyList<PaletteFontMaterialBinding> Bindings =>
            _bindings ?? Array.Empty<PaletteFontMaterialBinding>();
    }
}
