using System;
using UnityEngine;

namespace OrbitalEscape.Orbits
{
    [Serializable]
    public struct Rift
    {
        [Range(0.0f, 360.0f)]
        public float StartAngleDegrees;

        [Min(0.0f)]
        public float ArcLength;
    }
}
