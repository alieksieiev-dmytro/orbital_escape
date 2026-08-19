using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class PreserveParticleColorTarget : MonoBehaviour, IPaletteExclusion
    {
    }
}
