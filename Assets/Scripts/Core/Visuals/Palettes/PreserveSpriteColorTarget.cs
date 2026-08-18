using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PreserveSpriteColorTarget : MonoBehaviour, IPaletteExclusion
    {
    }
}
