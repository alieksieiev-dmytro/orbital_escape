using System.Collections.Generic;
using OrbitalEscape.Orbits;
using OrbitalEscape.Visuals;
using UnityEngine;

namespace OrbitalEscape.Factories
{
    public class OrbitFactory : IOrbitFactory
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
        
        public GameObject GetOrCreate(Orbit orbit)
        {
            while (_orbitObjects.Count <= orbit.Index)
                _orbitObjects.Add(null);

            var orbitObj = _orbitObjects[orbit.Index];
            if (orbitObj == null)
            {
                orbitObj = Object.Instantiate(_orbitPrefab,
                new Vector3(Constants.Center.x, Constants.Center.y, 0f),
                Quaternion.identity,
                _root);
                _orbitObjects[orbit.Index] = orbitObj;
            }

            orbitObj.transform.localScale = new Vector3(orbit.Radius * 2, orbit.Radius * 2, 1f);
            orbitObj.GetComponent<AuroraFadeInOut>().ResetForRun(orbit);
            orbitObj.SetActive(true);
            return orbitObj;
        }
        
        public GameObject Get(int index) => _orbitObjects[index];
        
        public void DeactivateAll()
        {
            foreach (GameObject orbit in _orbitObjects)
                if (orbit != null)
                    orbit.SetActive(false);
        }
    }
}
