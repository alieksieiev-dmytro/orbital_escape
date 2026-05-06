using System;
using System.Collections;
using System.Collections.Generic;
using OrbitalEscape.Events;
using OrbitalEscape.Orbits.Generators;
using OrbitalEscape.Visuals;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public class LevelManager : MonoBehaviour
    {
        private ILevelBuilder _levelBuilder;
        private ILevelObjectSpawner _levelObjectSpawner;
        private LevelGenerationConfig _generationConfig;
        private LevelState _levelState;
        private EventBinding<PassedThroughOrbitEvent> _passedThroughOrbitBinding;

        [Inject]
        public void Construct(
            ILevelBuilder levelBuilder,
            ILevelObjectSpawner levelObjectSpawner,
            LevelGenerationConfig generationConfig,
            LevelState levelState)
        {
            _levelBuilder = levelBuilder;
            _levelObjectSpawner = levelObjectSpawner;
            _generationConfig = generationConfig;
            _levelState = levelState;
        }

        private void Awake()
        {
            BuildLevel();
            _passedThroughOrbitBinding = new EventBinding<PassedThroughOrbitEvent>(OnPassedThroughOrbit);
        }

        [ContextMenu("Build Level")]
        private void BuildLevel()
        {
            ClearLevel();
            _levelState.Clear();
            ExpandLevel(_generationConfig.InitialOrbitCount);
            CreateLevel(_levelState.Orbits);
        }

        private void CreateLevel(IReadOnlyList<Orbit> orbits)
        {
            CreateOrbitObjects(orbits);

            var innerOrbit = _levelObjectSpawner.GetOrbitObject(0).GetComponent<AuroraFadeInOut>();
            var outerOrbit = _levelObjectSpawner.GetOrbitObject(1).GetComponent<AuroraFadeInOut>();
            
            innerOrbit.InitAurora(orbits[0]);
            outerOrbit.InitAurora(orbits[1]);
            
            innerOrbit.SetActive(false);
            outerOrbit.SetActive(true);

            EventBus<OrbitsSetEvent>.Raise(new OrbitsSetEvent(orbits[0], orbits[1]));
        }

        private void ClearLevel()
        {
            _levelObjectSpawner.Clear();
        }
        
        private void OnEnable() => EventBus<PassedThroughOrbitEvent>.Register(_passedThroughOrbitBinding);
        
        private void OnDisable() => EventBus<PassedThroughOrbitEvent>.Deregister(_passedThroughOrbitBinding);
        
        private void OnPassedThroughOrbit(PassedThroughOrbitEvent @event)
        {
            if (@event.Orbit.Index >= _levelState.Count * 0.5f)
            {
                int newOrbitsCount = _generationConfig.GetExtensionCount(_levelState.Count);
                var newOrbits = ExpandLevel(newOrbitsCount);
                
                StartCoroutine(DelayedObjectsCreation(CreateOrbitObjects, newOrbits));
            }

            var currentOrbit = _levelState.GetOrbit(@event.Orbit.Index);
            var oppositeOrbitIndex = @event.IsOuterOrbit
                ? _levelState.GetOrbit(@event.Orbit.Index + 1)
                : _levelState.GetOrbit(@event.Orbit.Index - 1);
            EventBus<OrbitsSetEvent>.Raise(new OrbitsSetEvent(currentOrbit, oppositeOrbitIndex));
        }

        private List<Orbit> ExpandLevel(int count)
        {
            List<Orbit> newOrbits = _levelBuilder.Build(_levelState.Orbits, count);
            _levelState.AddRange(newOrbits);
            return newOrbits;
        }

        private void CreateOrbitObjects(IEnumerable<Orbit> orbits)
        {
            foreach (var orbit in orbits)
            {
                _levelObjectSpawner.SpawnOrbit(orbit);
            }
        }

        private IEnumerator DelayedObjectsCreation(Action<IEnumerable<Orbit>> action, IEnumerable<Orbit> orbits)
        {
            yield return null;
            action(orbits);
        }
    }
}
