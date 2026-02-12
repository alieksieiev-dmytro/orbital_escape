using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OrbitalEscape.Events;
using OrbitalEscape.Factories;
using OrbitalEscape.Orbits.Generators;
using OrbitalEscape.Visuals;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public class LevelManager : MonoBehaviour
    {
        private const int ORBIT_MULTIPLIER = 2;
        private const int MAX_ORBIT_PER_GENERATION = 8;
        
        private IArcGenerator _riftGenerator;
        private IArcGenerator _spikeGenerator;
        private IObjectFactory<GameObject> _orbitFactory;
        private IObjectFactory<GameObject> _spikeFactory;
        private OrbitGenerator _orbitGenerator;
        private List<Orbit> _orbitsData;
        private EventBinding<PassedThroughOrbitEvent> _passedThroughOrbitBinding;

        [Inject]
        public void Construct(OrbitGenerator orbitGenerator,
            [Inject(Id = FactoryType.OrbitFactory)] IObjectFactory<GameObject> objectFactory,
            [Inject(Id = FactoryType.SpikeFactory)] IObjectFactory<GameObject> spikeFactory,
            [Inject(Id = ArcObjectType.Rift)] IArcGenerator riftGenerator,
            [Inject(Id = ArcObjectType.Spike)] IArcGenerator spikeGenerator)
        {
            _orbitGenerator = orbitGenerator;
            _orbitFactory = objectFactory;
            _spikeFactory = spikeFactory;
            _riftGenerator = riftGenerator;
            _spikeGenerator = spikeGenerator;
        }

        private void Awake()
        {
            BuildLevel();
            _passedThroughOrbitBinding = new EventBinding<PassedThroughOrbitEvent>(OnPassedThroughOrbit);
        }

        [ContextMenu("Build Level")]
        private void BuildLevel()
        {
            _orbitsData = new List<Orbit>();
            ExpandLevel(2);
            
            ClearLevel();
            CreateLevel(_orbitsData);
        }

        private void CreateLevel(List<Orbit> orbits)
        {
            CreateOrbitObjects(orbits);

            var innerOrbit = _orbitFactory.Get(0).GetComponent<AuroraFadeInOut>();
            var outerOrbit = _orbitFactory.Get(1).GetComponent<AuroraFadeInOut>();
            
            innerOrbit.InitAurora(orbits[0]);
            outerOrbit.InitAurora(orbits[1]);
            
            innerOrbit.SetActive(false);
            outerOrbit.SetActive(true);
        }

        private void ClearLevel()
        {
            _orbitFactory.Clear();
            _spikeFactory.Clear();
        }
        
        private void OnEnable() => EventBus<PassedThroughOrbitEvent>.Register(_passedThroughOrbitBinding);
        
        private void OnDisable() => EventBus<PassedThroughOrbitEvent>.Deregister(_passedThroughOrbitBinding);
        
        private void OnPassedThroughOrbit(PassedThroughOrbitEvent @event)
        {
            if (@event.Orbit.Index >= _orbitsData.Count * 0.5f)
            {
                int newOrbitsCount = _orbitsData.Count * ORBIT_MULTIPLIER - _orbitsData.Count;
                var newOrbits = ExpandLevel(newOrbitsCount);
                
                StartCoroutine(DelayedObjectsCreation(CreateOrbitObjects, newOrbits));
            }

            var currentOrbit = _orbitsData[@event.Orbit.Index];
            var oppositeOrbitIndex = @event.IsOuterOrbit
                ? _orbitsData[@event.Orbit.Index + 1]
                : _orbitsData[@event.Orbit.Index - 1];
            EventBus<OrbitsSetEvent>.Raise(new OrbitsSetEvent(currentOrbit, oppositeOrbitIndex));
        }

        private List<Orbit> ExpandLevel(int count)
        {
            var orbits = _orbitGenerator.GenerateOrbits(count);
            int selectedOrbits = Mathf.Clamp(_orbitsData.Count == 0 ? _orbitGenerator.Count : count, 0, MAX_ORBIT_PER_GENERATION);
            var newOrbits = orbits.TakeLast(selectedOrbits).ToList();

            foreach (var orbit in newOrbits.Where(orbit => orbit.Index != 0))
            {
                orbit.OrbitObjects.AddRange(_riftGenerator.GenerateArcs(orbit, ArcType.TwoSide));
                orbit.OrbitObjects.AddRange(_spikeGenerator.GenerateArcs(orbit, ArcType.Outer));
                orbit.OrbitObjects.AddRange(_spikeGenerator.GenerateArcs(orbit, ArcType.Inner));
            }
            
            _orbitsData.AddRange(newOrbits);
            return newOrbits;
        }

        private void CreateOrbitObjects(IEnumerable<Orbit> orbits)
        {
            foreach (var orbit in orbits)
            {
                var orbitGameObject = _orbitFactory.Create(orbit);

                foreach (var spike in orbit.GetOrbitObjectsByType(ArcObjectType.Spike))
                {
                    _spikeFactory.Create(orbitGameObject.transform, orbit, spike);
                }
            }
        }

        private IEnumerator DelayedObjectsCreation(Action<IEnumerable<Orbit>> action, IEnumerable<Orbit> orbits)
        {
            yield return null;
            action(orbits);
        }
    }
}
