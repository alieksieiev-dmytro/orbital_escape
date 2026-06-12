using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using OrbitalEscape.Events;
using OrbitalEscape.Orbits.Generators;
using OrbitalEscape.Orbits.Generators.Patterns;
using OrbitalEscape.Utils;
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
        private CancellationToken _destroyCancellationToken;
        private readonly List<PatternDebugEntry> _patternDebugEntries = new List<PatternDebugEntry>();
        private bool _isBuildingLevel;
        private bool _isExtendingLevel;
        private bool _showPatternDebugGizmos;

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
            _patternDebugEntries.Clear();
            _showPatternDebugGizmos = false;
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

        private async UniTask<List<Orbit>> ExpandLevelAsync(int count, CancellationToken cancellationToken)
        {
            LevelBuildResult result = await _levelBuilder.BuildAsync(_levelState.Orbits, count, cancellationToken);
            LogWarnings(result.Warnings);
            _showPatternDebugGizmos = result.PatternDebugGizmosEnabled;
            _patternDebugEntries.AddRange(result.PatternDebugEntries);

            List<Orbit> newOrbits = new List<Orbit>(result.Orbits);
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

        private void OnDrawGizmos()
        {
            if (!_showPatternDebugGizmos || _patternDebugEntries == null)
            {
                return;
            }

            for (int i = 0; i < _patternDebugEntries.Count; i++)
            {
                DrawPatternDebugEntry(_patternDebugEntries[i]);
            }
        }

        private static void DrawPatternDebugEntry(PatternDebugEntry entry)
        {
            Gizmos.color = entry.IsFallback
                ? new Color(0.2f, 0.8f, 1.0f, 0.25f)
                : new Color(1.0f, 0.85f, 0.15f, 0.5f);

            DrawArc(entry.LowerRadius, entry.StartAngleDegree, entry.EndAngleDegree);
            DrawArc(entry.UpperRadius, entry.StartAngleDegree, entry.EndAngleDegree);
            DrawRadial(entry.LowerRadius, entry.UpperRadius, entry.StartAngleDegree);
            DrawRadial(entry.LowerRadius, entry.UpperRadius, entry.EndAngleDegree);
        }

        private static void DrawArc(float radius, float startAngle, float endAngle)
        {
            float delta = OrbitMath.DeltaDegrees(startAngle, endAngle);
            int steps = Mathf.Max(4, Mathf.CeilToInt(delta / 8.0f));
            Vector3 previous = OrbitMath.GetPointOnCircle(Constants.Center, radius, startAngle);

            for (int i = 1; i <= steps; i++)
            {
                float angle = startAngle + delta * i / steps;
                Vector3 next = OrbitMath.GetPointOnCircle(Constants.Center, radius, angle);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }

        private static void DrawRadial(float lowerRadius, float upperRadius, float angle)
        {
            Vector3 lower = OrbitMath.GetPointOnCircle(Constants.Center, lowerRadius, angle);
            Vector3 upper = OrbitMath.GetPointOnCircle(Constants.Center, upperRadius, angle);
            Gizmos.DrawLine(lower, upper);
        }
    }
}
