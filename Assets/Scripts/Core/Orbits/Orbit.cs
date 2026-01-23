using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OrbitalEscape.Orbits
{
    [Serializable]
    public class Orbit
    {
        private List<ArcObject> _orbitObjects;
        
        [field: SerializeField] public float Radius { get; private set; }
        [field: SerializeField] public int Index { get; private set; }
        public List<Arc> InnerArcs { get; private set; }
        public List<Arc> OuterArcs { get; private set; }
        public List<Arc> TwoSideArcs { get; private set; }
        public List<ArcObject> OrbitObjects
        {
            get
            {
                _orbitObjects ??= new List<ArcObject>();
                return _orbitObjects;
            }
            private set => _orbitObjects = value;
        }
        
        public Orbit(float radius, int index)
        {
            Radius = radius;
            Index = index;
            
            InnerArcs = new List<Arc>();
            OuterArcs = new List<Arc>();
            TwoSideArcs = new List<Arc>();
            _orbitObjects = new List<ArcObject>();
        }
        
        public List<ArcObject> GetOrbitObjectsByType(OrbitObjectType type)
        {
            return OrbitObjects.Count != 0
                ? _orbitObjects.Where(obj => obj.Object.Type == type).ToList()
                : new List<ArcObject>();
        }
    }
}
