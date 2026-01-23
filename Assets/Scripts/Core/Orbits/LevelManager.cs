using System.Collections.Generic;
using System.Linq;
using OrbitalEscape.Events;
using OrbitalEscape.Movement;
using OrbitalEscape.Orbits.Generators;
using OrbitalEscape.Utils;
using OrbitalEscape.Visuals;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private Transform _orbitsRoot;
        [SerializeField] private Transform _obstaclesRoot;
        [SerializeField] private GameObject _orbitPrefab;
        [SerializeField] private GameObject _obstaclePrefab;
        [SerializeField] private List<Orbit> _mockupData;
        
        [Inject(Id = Constants.INJECTION_ID_CENTRE)] 
        private Vector2 _center;
        private JumpBehaviour _jumpBehaviour;
        private EventBinding<PassedThroughOrbitEvent> _passedThroughOrbitBinding;
        private List<GameObject> _orbitObjects;
        
        [Inject] private IArcGenerator _riftGenerator;

        [Inject]
        public void Construct(JumpBehaviour jumpBehaviour) => _jumpBehaviour = jumpBehaviour;
        
        private void Awake()
        {
            BuildLevel();
            _passedThroughOrbitBinding = new EventBinding<PassedThroughOrbitEvent>(OnPassedThroughOrbit);
        }

        [ContextMenu("Build Level")]
        private void BuildLevel()
        {
            ClearLevel();
            GenerateRifts();
            CreateLevel(_mockupData);
        }
        
        public List<Orbit> GetOrbits() => _mockupData;

        private void CreateLevel(List<Orbit> orbits)
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
                orbitObj.AddComponent<RiftController>().Initialize(orbit.GetOrbitObjectsByType(OrbitObjectType.Rift));
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

        private void ClearLevel()
        {
            for (int i = _orbitsRoot.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(_orbitsRoot.GetChild(i).gameObject);
            }
            
            for (int i = _obstaclesRoot.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(_obstaclesRoot.GetChild(i).gameObject);
            }
        }

        private void GenerateRifts()
        {
            for (int i = 1; i < _mockupData.Count - 1; i++)
            {
                var orbit = _mockupData[i];
                var riftArcs = _riftGenerator.GenerateArcs(orbit.Radius);
                orbit.OrbitObjects.AddRange(riftArcs);
            }
        }
        
        private void OnEnable() => EventBus<PassedThroughOrbitEvent>.Register(_passedThroughOrbitBinding);
        
        private void OnDisable() => EventBus<PassedThroughOrbitEvent>.Deregister(_passedThroughOrbitBinding);
        
        private void OnPassedThroughOrbit(PassedThroughOrbitEvent @event)
        {
            var currentOrbit = _mockupData[@event.Orbit.Index];
            var oppositeOrbitIndex = (@event.IsOuterOrbit)
                ? _mockupData[@event.Orbit.Index + 1]
                : _mockupData[@event.Orbit.Index - 1];
            _jumpBehaviour.SetOrbits(currentOrbit, oppositeOrbitIndex);
        }
    }
}
