using OrbitalEscape.Orbits;
using UnityEngine;

namespace OrbitalEscape.Factories
{
    public interface ISpikeFactory
    {
        int CountAll { get; }
        int CountActive { get; }
        int CountInactive { get; }

        GameObject Create(Transform root, Orbit orbit, ArcObject spike);
        bool Release(GameObject spike);
        void ReleaseAll();
    }
}
