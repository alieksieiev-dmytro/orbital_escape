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
        
        public List<Arc> InnerArcs => OrbitObjects
            .Where(obj => obj.Type == ArcType.Inner)
            .Select(obj => obj.Arc)
            .ToList();
        
        public List<Arc> OuterArcs => OrbitObjects
            .Where(obj => obj.Type == ArcType.Outer)
            .Select(obj => obj.Arc)
            .ToList();
        
        public List<Arc> TwoSideArcs => OrbitObjects
            .Where(obj => obj.Type == ArcType.TwoSide)
            .Select(obj => obj.Arc)
            .ToList();
        
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
