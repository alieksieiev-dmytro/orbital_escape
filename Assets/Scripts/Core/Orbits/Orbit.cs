using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrbitalEscape.Orbits
{
    [Serializable]
    public struct Orbit
    {
        [field: SerializeField] public List<Rift> Rifts { get; private set; }
        [field: SerializeField] public float Radius { get; private set; }
        [field: SerializeField] public int Index { get; private set; }
        
        public Orbit(float radius, int index)
        {
            Radius = radius;
            Index = index;
            Rifts = new List<Rift>();
        }
    }
}
