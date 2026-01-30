using System;

namespace OrbitalEscape.Orbits
{
    [Serializable]
    public struct Rift : IOrbitObject
    {
        public ArcObjectType Type => ArcObjectType.Rift;
    }
}
