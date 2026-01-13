using System;
using UnityEngine;

namespace OrbitalEscape.Rifts
{
    [Serializable]
    public class Rift
    {
        [Range(0.0f, 360.0f)]
        public float StartAngleDegrees = 0.0f;

        [Min(0.0f)]
        public float ArcLength = 0.0f;

        public bool Enabled = true;
    }
}
