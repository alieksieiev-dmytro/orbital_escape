using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    public sealed class PaletteSceneReferences
    {
        public Transform Player { get; }
        public SpriteRenderer Background { get; }
        public SpriteRenderer Accent { get; }

        public PaletteSceneReferences(Transform player, SpriteRenderer background, SpriteRenderer accent)
        {
            Player = player;
            Background = background;
            Accent = accent;
        }
    }
}
