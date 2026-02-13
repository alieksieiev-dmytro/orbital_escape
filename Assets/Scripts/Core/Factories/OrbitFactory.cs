using System.Collections.Generic;
using OrbitalEscape.Orbits;
using OrbitalEscape.Visuals;
using UnityEngine;

namespace OrbitalEscape.Factories
{
    public class OrbitFactory : IObjectFactory<GameObject>
    {
        private readonly List<GameObject> _orbitObjects;
        private readonly GameObject _orbitPrefab;
        private readonly Transform _root;
        
        public OrbitFactory(GameObject orbitPrefab, Transform root)
        {
            _orbitObjects = new List<GameObject>();
            _orbitPrefab = orbitPrefab;
            _root = root;
        }
        
        public GameObject Create(params object[] args)
        {
            var orbit = (Orbit)args[0];                
            
            var orbitObj = Object.Instantiate(_orbitPrefab,
                new Vector3(Constants.Center.x, Constants.Center.y, 0f),
                Quaternion.identity,
                _root);
            
            orbitObj.transform.localScale = new Vector3(orbit.Radius * 2, orbit.Radius * 2, 1f);
            orbitObj.GetComponent<RiftController>().Initialize(orbit.GetOrbitObjectsByType(ArcObjectType.Rift));
            orbitObj.GetComponent<AuroraFadeInOut>().InitAurora(orbit);
            
            _orbitObjects.Add(orbitObj);
            return orbitObj;
        }
        
        public GameObject Get(int index) => _orbitObjects[index];
        
        public void Clear()
        {
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(_root.GetChild(i).gameObject);
            }
        }
    }
}
