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
        
        [Inject(Id = Constants.INJECTION_ID_CENTRE)]          private Vector2 _center;
        [Inject(Id = Constants.INJECTION_ID_RIFT_GENERATOR)]  private IArcGenerator _riftGenerator;
        [Inject(Id = Constants.INJECTION_ID_SPIKE_GENERATOR)] private IArcGenerator _spikeGenerator;
        private JumpBehaviour _jumpBehaviour;
        private EventBinding<PassedThroughOrbitEvent> _passedThroughOrbitBinding;
        private List<GameObject> _orbitObjects;

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
            GenerateSpikes();
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
                orbitObj.GetComponent<RiftController>().Initialize(orbit.GetOrbitObjectsByType(ArcObjectType.Rift));
                orbitObj.GetComponent<AuroraFadeInOut>().InitAurora(orbit);
                _orbitObjects.Add(orbitObj);

                SpawnSpikes(orbit);
            }

            var innerOrbit = _orbitObjects[0].GetComponent<AuroraFadeInOut>();
            var outerOrbit = _orbitObjects[1].GetComponent<AuroraFadeInOut>();
            
            innerOrbit.InitAurora(orbits[0]);
            outerOrbit.InitAurora(orbits[1]);
            
            innerOrbit.SetActive(false);
            outerOrbit.SetActive(true);
        }
        
        private void SpawnSpikes(Orbit orbit)
        {
            var outerSpikes = orbit.GetOrbitObjectsByType(ArcObjectType.Spike)
                .Where(x => x.Type == ArcType.Outer);
            foreach (var spike in outerSpikes)
            {
                var spikeObj = Instantiate(
                    _obstaclePrefab,
                    new Vector3(_center.x, _center.y, 0f),
                    Quaternion.identity,
                    _obstaclesRoot);
                    
                spikeObj.transform.localScale =
                    new Vector3(spike.Arc.Length(), ((SpikeObstacle)spike.Object).Height, 1f);
                
                var angleDelta = OrbitMath.DeltaDegrees(spike.Arc.StartAngleDegree, spike.Arc.EndAngleDegree) / 2f;
                var angle = spike.Arc.StartAngleDegree + angleDelta * 0.5f;
                var position = OrbitMath.GetPointOnCircle(_center, orbit.Radius - 0.1f, angle);
                Vector2 dir = _center - position;
                var rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                
                spikeObj.transform.rotation = Quaternion.Euler(0f, 0f, rotation + 90.0f);
                spikeObj.transform.localPosition = new Vector3(position.x, position.y, 0f);
            }
                
            var innerSpikes = orbit.GetOrbitObjectsByType(ArcObjectType.Spike)
                .Where(x => x.Type == ArcType.Inner);
            foreach (var spike in innerSpikes)
            {
                var spikeObj = Instantiate(
                    _obstaclePrefab,
                    new Vector3(_center.x, _center.y, 0f),
                    Quaternion.identity,
                    _obstaclesRoot);
                
                spikeObj.transform.localScale =
                    new Vector3(spike.Arc.Length(), ((SpikeObstacle)spike.Object).Height, 1f);
                
                var angleDelta = OrbitMath.DeltaDegrees(spike.Arc.StartAngleDegree, spike.Arc.EndAngleDegree) / 2f;
                var angle = spike.Arc.StartAngleDegree + angleDelta * 0.5f;
                var position = OrbitMath.GetPointOnCircle(_center, orbit.Radius, angle);
                Vector2 dir = position - _center;
                var rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                
                spikeObj.transform.rotation = Quaternion.Euler(0f, 0f, rotation + 90.0f);
                spikeObj.transform.localPosition = new Vector3(position.x, position.y, 0f);
            }
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
                var riftArcs = _riftGenerator.GenerateArcs(orbit, ArcType.TwoSide);
                orbit.OrbitObjects.AddRange(riftArcs);
            }
        }

        private void GenerateSpikes()
        {
            for (int i = 1; i < _mockupData.Count - 1; i++)
            {
                var orbit = _mockupData[i];
                var outerSpikes = _spikeGenerator.GenerateArcs(orbit, ArcType.Outer);
                var innerSpikes = _spikeGenerator.GenerateArcs(orbit, ArcType.Inner);
                orbit.OrbitObjects.AddRange(outerSpikes);
                orbit.OrbitObjects.AddRange(innerSpikes);
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
