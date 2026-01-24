using System;

namespace OrbitalEscape.Orbits
{
    [Serializable]
    public struct Rift : IOrbitObject
    {
        public OrbitObjectType Type => OrbitObjectType.Rift;
    }
}
