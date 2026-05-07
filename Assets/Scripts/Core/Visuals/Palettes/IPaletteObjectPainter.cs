using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    public interface IPaletteObjectPainter
    {
        void ApplyEnvironment(SpriteRenderer background, SpriteRenderer accent, PaletteSnapshot palette);
        void ApplyPlayer(Transform player, PaletteSnapshot palette);
        void ApplyLevelObject(GameObject levelObject, PaletteSnapshot palette);
    }
}
