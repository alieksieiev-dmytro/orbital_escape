using System;
using System.Collections.Generic;

namespace OrbitalEscape.Orbits
{
    public interface IGravityWellZoneRegistry
    {
        event Action Changed;
        IReadOnlyList<GravityWellZone> Zones { get; }
        void Register(GravityWellZone zone);
        void Clear();
    }
}
