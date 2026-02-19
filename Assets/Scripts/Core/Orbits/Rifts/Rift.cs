using System;

namespace OrbitalEscape.Orbits
{
    [Serializable]
    public struct Rift : IOrbitObject
    {
        public readonly float Padding;
        
        public ArcObjectType Type => ArcObjectType.Rift;
        
        public Rift(float padding)
        {
            Padding = padding;
        }
    }
}
