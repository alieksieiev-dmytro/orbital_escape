using System;
using System.Collections.Generic;

namespace OrbitalEscape.Orbits
{
    public sealed class GravityWellZoneRegistry : IGravityWellZoneRegistry
    {
        private readonly List<GravityWellZone> _zones = new List<GravityWellZone>();

        public event Action Changed;
        public IReadOnlyList<GravityWellZone> Zones => _zones;

        public void Register(GravityWellZone zone)
        {
            _zones.Add(zone);
            Changed?.Invoke();
        }

        public void Clear()
        {
            if (_zones.Count == 0)
            {
                return;
            }

            _zones.Clear();
            Changed?.Invoke();
        }
    }
}
