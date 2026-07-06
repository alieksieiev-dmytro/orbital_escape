using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        private IRiftClosureState _riftClosureState;
        private EventBinding<PassedThroughOrbitEvent> _passedThroughOrbitBinding;
        private CancellationToken _destroyCancellationToken;
        private bool _isBuildingLevel;
        private bool _isExtendingLevel;

        [Inject]
        public void Construct(
            ILevelBuilder levelBuilder,
            ILevelObjectSpawner levelObjectSpawner,
            LevelGenerationConfig generationConfig,
            LevelState levelState,
            IRiftClosureState riftClosureState)
        {
            _levelBuilder = levelBuilder;
            _levelObjectSpawner = levelObjectSpawner;
            _generationConfig = generationConfig;
            _levelState = levelState;
            _riftClosureState = riftClosureState;
        }

        private void Awake()
        {
            _destroyCancellationToken = this.GetCancellationTokenOnDestroy();
            _passedThroughOrbitBinding = new EventBinding<PassedThroughOrbitEvent>(OnPassedThroughOrbit);
        }

        private void Start()
        {
            BuildLevelAsync(_destroyCancellationToken).Forget();
        }

        [ContextMenu("Build Level")]
        private void BuildLevel()
        {
            BuildLevelAsync(_destroyCancellationToken).Forget();
        }

        private async UniTask BuildLevelAsync(CancellationToken cancellationToken)
        {
            if (_isBuildingLevel)
            {
                return;
            }

            _isBuildingLevel = true;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                ClearLevel();
                _levelState.Clear();
                _riftClosureState.Clear();

                List<Orbit> newOrbits = await ExpandLevelAsync(_generationConfig.InitialOrbitCount, cancellationToken);
                CreateLevel(newOrbits);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _isBuildingLevel = false;
            }
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

        public void ClearObstacleContent()
        {
            if (_levelState.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _levelState.Count; i++)
            {
                _levelState.GetOrbit(i).OrbitObjects.Clear();
            }

            _riftClosureState.Clear();
            _levelObjectSpawner.RefreshOrbitContent(_levelState.Orbits);
        }

        public async UniTask EnsureOrbitAvailableAsync(int orbitIndex, CancellationToken cancellationToken)
        {
            if (orbitIndex < 0)
            {
                return;
            }

            await WaitForIdleAsync(cancellationToken);

            while (_levelState.Count <= orbitIndex)
            {
                int missingCount = orbitIndex - _levelState.Count + 1;
                int configuredCount = _generationConfig.GetExtensionCount(_levelState.Count);
                int extensionCount = Mathf.Max(missingCount, configuredCount);
                int previousCount = _levelState.Count;

                await ExtendLevelAsync(extensionCount, cancellationToken);
                await WaitForIdleAsync(cancellationToken);

                if (_levelState.Count <= previousCount)
                {
                    break;
                }
            }
        }
        
        private void OnEnable()
        {
            if (_passedThroughOrbitBinding != null)
            {
                EventBus<PassedThroughOrbitEvent>.Register(_passedThroughOrbitBinding);
            }
        }
        
        private void OnDisable()
        {
            if (_passedThroughOrbitBinding != null)
            {
                EventBus<PassedThroughOrbitEvent>.Deregister(_passedThroughOrbitBinding);
            }
        }
        
        private void OnPassedThroughOrbit(PassedThroughOrbitEvent @event)
        {
            if (_isBuildingLevel || _levelState.Count == 0)
            {
                return;
            }

            if (@event.Orbit.Index >= _levelState.Count * 0.5f && !_isExtendingLevel)
            {
                int newOrbitsCount = _generationConfig.GetExtensionCount(_levelState.Count);
                ExtendLevelAsync(newOrbitsCount, _destroyCancellationToken).Forget();
            }

            if (@event.Orbit.Index < 0 || @event.Orbit.Index >= _levelState.Count)
            {
                return;
            }

            int oppositeIndex = @event.IsOuterOrbit
                ? @event.Orbit.Index + 1
                : @event.Orbit.Index - 1;
            if (oppositeIndex < 0 || oppositeIndex >= _levelState.Count)
            {
                return;
            }

            var currentOrbit = _levelState.GetOrbit(@event.Orbit.Index);
            var oppositeOrbitIndex = _levelState.GetOrbit(oppositeIndex);
            EventBus<OrbitsSetEvent>.Raise(new OrbitsSetEvent(currentOrbit, oppositeOrbitIndex));
        }

        private async UniTask ExtendLevelAsync(int count, CancellationToken cancellationToken)
        {
            if (count <= 0)
            {
                return;
            }

            _isExtendingLevel = true;

            try
            {
                List<Orbit> newOrbits = await ExpandLevelAsync(count, cancellationToken);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                await CreateOrbitObjectsAsync(newOrbits, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _isExtendingLevel = false;
            }
        }

        private async UniTask WaitForIdleAsync(CancellationToken cancellationToken)
        {
            while (_isBuildingLevel || _isExtendingLevel)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        private async UniTask<List<Orbit>> ExpandLevelAsync(int count, CancellationToken cancellationToken)
        {
            LevelBuildResult result = await _levelBuilder.BuildAsync(_levelState.Orbits, count, cancellationToken);
            LogWarnings(result.Warnings);

            List<Orbit> newOrbits = new List<Orbit>(result.Orbits);
            _levelState.AddRange(newOrbits);
            ApplyContentDeltas(result.ContentDeltas);
            return newOrbits;
        }

        private void ApplyContentDeltas(IReadOnlyList<OrbitContentDelta> contentDeltas)
        {
            for (int i = 0; i < contentDeltas.Count; i++)
            {
                OrbitContentDelta delta = contentDeltas[i];
                if (delta.OrbitIndex < 0 || delta.OrbitIndex >= _levelState.Count)
                {
                    Debug.LogWarning($"Skipped content delta for missing orbit {delta.OrbitIndex}.");
                    continue;
                }

                if (delta.AddedObjects.Count == 0)
                {
                    continue;
                }

                Orbit orbit = _levelState.GetOrbit(delta.OrbitIndex);
                orbit.OrbitObjects.AddRange(delta.AddedObjects);
                _levelObjectSpawner.AppendOrbitContent(orbit, delta.AddedObjects);
            }
        }

        private void CreateOrbitObjects(IEnumerable<Orbit> orbits)
        {
            foreach (var orbit in orbits)
            {
                _levelObjectSpawner.SpawnOrbit(orbit);
            }
        }

        private async UniTask CreateOrbitObjectsAsync(IEnumerable<Orbit> orbits, CancellationToken cancellationToken)
        {
            foreach (var orbit in orbits)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _levelObjectSpawner.SpawnOrbit(orbit);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        private void LogWarnings(IReadOnlyList<string> warnings)
        {
            for (int i = 0; i < warnings.Count; i++)
            {
                Debug.LogWarning(warnings[i]);
            }
        }

    }
}
