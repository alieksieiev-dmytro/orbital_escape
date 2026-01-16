using System;
using System.Collections.Generic;
using OrbitalEscape.Events;
using OrbitalEscape.Movement;
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

        [Inject]
        public void Construct(JumpBehaviour jumpBehaviour) => _jumpBehaviour = jumpBehaviour;
        
        private void Awake()
        {
            BuildLevel();
            _passedThroughOrbitBinding = new EventBinding<PassedThroughOrbitEvent>((@event => {
                var currentOrbit = _mockupData[@event.OrbitIndex];
                var oppositeOrbitIndex = (@event.IsOuterOrbit)
                    ? _mockupData[@event.OrbitIndex + 1]
                    : _mockupData[@event.OrbitIndex - 1];
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
            foreach (var orbit in orbits)
            {
                GameObject orbitObj = Instantiate(
                    _orbitPrefab, 
                    new Vector3(_center.x, _center.y, 0f),
                    Quaternion.identity, 
                    _orbitsRoot);
                orbitObj.transform.localScale = new Vector3(orbit.Radius * 2, orbit.Radius * 2, 1f);
                orbitObj.AddComponent<RiftController>().Initialize(orbit.Rifts);
            }
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
