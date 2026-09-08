using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    public sealed class MainMenuPaletteSceneReferences
    {
        public SpriteRenderer Background { get; }
        public IReadOnlyList<Transform> EnvironmentRoots { get; }

        public MainMenuPaletteSceneReferences(
            SpriteRenderer background,
            Transform[] environmentRoots)
        {
            Background = background;
            EnvironmentRoots = environmentRoots ?? Array.Empty<Transform>();
        }
    }
}
