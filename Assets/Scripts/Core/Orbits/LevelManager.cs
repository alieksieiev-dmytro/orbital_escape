using System;
using System.Collections.Generic;
using OrbitalEscape.Events;
using OrbitalEscape.Movement;
using OrbitalEscape.Visuals;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private Transform _orbitsRoot;
        [SerializeField] private GameObject _orbitPrefab;
        [SerializeField] private List<Orbit> _mockupData;
        
        [Inject(Id = Constants.INJECTION_ID_CENTRE)] 
        private Vector2 _center;
        private JumpBehaviour _jumpBehaviour;
        private EventBinding<PassedThroughOrbitEvent> _passedThroughOrbitBinding;
        private List<GameObject> _orbitObjects;

        [Inject]
        public void Construct(JumpBehaviour jumpBehaviour) => _jumpBehaviour = jumpBehaviour;
        
        private void Awake()
        {
            BuildLevel();
            _passedThroughOrbitBinding = new EventBinding<PassedThroughOrbitEvent>((@event => {
                var currentOrbit = _mockupData[@event.Orbit.Index];
                var oppositeOrbitIndex = (@event.IsOuterOrbit)
                    ? _mockupData[@event.Orbit.Index + 1]
                    : _mockupData[@event.Orbit.Index - 1];
                _jumpBehaviour.SetOrbits(currentOrbit, oppositeOrbitIndex);
            }));
        }

        [ContextMenu("Build Level")]
        private void BuildLevel()
        {
            ClearLevel();
            CreateLevel(_mockupData);
        }
        
        public List<Orbit> GetOrbits() => _mockupData;

        public void CreateLevel(List<Orbit> orbits)
        {
            _orbitObjects = new List<GameObject>();
            
            foreach (var orbit in orbits)
            {
                var orbitObj = Instantiate(
                    _orbitPrefab, 
                    new Vector3(_center.x, _center.y, 0f),
                    Quaternion.identity, 
                    _orbitsRoot);
                orbitObj.transform.localScale = new Vector3(orbit.Radius * 2, orbit.Radius * 2, 1f);
                orbitObj.AddComponent<RiftController>().Initialize(orbit.Rifts);
                orbitObj.GetComponent<AuroraFadeInOut>().InitAurora(orbit);
                _orbitObjects.Add(orbitObj);
            }

            var innerOrbit = _orbitObjects[0].GetComponent<AuroraFadeInOut>();
            var outerOrbit = _orbitObjects[1].GetComponent<AuroraFadeInOut>();
            
            innerOrbit.InitAurora(orbits[0]);
            outerOrbit.InitAurora(orbits[1]);
            
            innerOrbit.SetActive(false);
            outerOrbit.SetActive(true);
        }
        
        public void ClearLevel()
        {
            for (int i = _orbitsRoot.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(_orbitsRoot.GetChild(i).gameObject);
            }
        }

        private void OnEnable()
        {
            EventBus<PassedThroughOrbitEvent>.Register(_passedThroughOrbitBinding);
        }
        
        private void OnDisable()
        {
            EventBus<PassedThroughOrbitEvent>.Deregister(_passedThroughOrbitBinding);
        }
    }
}
