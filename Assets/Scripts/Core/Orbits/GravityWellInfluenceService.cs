using System;
using DG.Tweening;
using OrbitalEscape.Movement;
using OrbitalEscape.Utils;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Orbits
{
    public sealed class GravityWellInfluenceService :
        IGravityWellInfluenceService,
        IInitializable,
        ITickable,
        IDisposable
    {
        private const float RADIUS_TOLERANCE = 0.05f;

        private readonly IMovementModel _movementModel;
        private readonly PlayerOrbitState _orbitState;
        private readonly IGravityWellZoneRegistry _zoneRegistry;
        private readonly IOrbitJumpTransitionService _jumpTransitionService;

        private GravityWellZone? _activeZone;
        private GravityWellZone? _suppressedZone;

        [Inject]
        public GravityWellInfluenceService(
            IMovementModel movementModel,
            PlayerOrbitState orbitState,
            IGravityWellZoneRegistry zoneRegistry,
            IOrbitJumpTransitionService jumpTransitionService)
        {
            _movementModel = movementModel;
            _orbitState = orbitState;
            _zoneRegistry = zoneRegistry;
            _jumpTransitionService = jumpTransitionService;
        }

        public void Initialize()
        {
            _zoneRegistry.Changed += OnZonesChanged;
        }

        public void Tick()
        {
            if (!_orbitState.TryInitialize())
            {
                return;
            }

            if (_jumpTransitionService.IsTransitioning)
            {
                return;
            }

            UpdateZoneState();

            if (_activeZone.HasValue)
            {
                PullTowardSourceOrbit(_activeZone.Value);
                return;
            }
        }

        public bool TryGetActiveZone(out GravityWellZone zone)
        {
            if (_activeZone.HasValue)
            {
                zone = _activeZone.Value;
                return true;
            }

            zone = default;
            return false;
        }

        public void SuppressActiveUntilExit()
        {
            if (!_activeZone.HasValue)
            {
                return;
            }

            _suppressedZone = _activeZone.Value;
            _activeZone = null;
        }

        public void Dispose()
        {
            _zoneRegistry.Changed -= OnZonesChanged;
            ClearState();
        }

        private void UpdateZoneState()
        {
            if (_suppressedZone.HasValue && !IsInside(_suppressedZone.Value))
            {
                _suppressedZone = null;
            }

            if (_activeZone.HasValue && !IsInside(_activeZone.Value))
            {
                GravityWellZone exitedZone = _activeZone.Value;
                _activeZone = null;
                JumpToNearestOrbit(exitedZone);
                return;
            }

            if (_activeZone.HasValue)
            {
                return;
            }

            if (TryFindBestZone(out GravityWellZone zone))
            {
                Activate(zone);
            }
        }

        private bool TryFindBestZone(out GravityWellZone bestZone)
        {
            bestZone = default;
            float bestPriority = float.MaxValue;
            bool found = false;

            foreach (GravityWellZone zone in _zoneRegistry.Zones)
            {
                if (_suppressedZone.HasValue && _suppressedZone.Value.Id == zone.Id)
                {
                    continue;
                }

                if (!IsInside(zone))
                {
                    continue;
                }

                float priority = GetPriority(zone);
                if (priority >= bestPriority)
                {
                    continue;
                }

                bestPriority = priority;
                bestZone = zone;
                found = true;
            }

            return found;
        }

        private float GetPriority(GravityWellZone zone)
        {
            bool matchesCurrentPair =
                zone.SourceOrbit == _orbitState.CurrentOrbit && zone.TargetOrbit == _orbitState.OppositeOrbit ||
                zone.SourceOrbit == _orbitState.OppositeOrbit && zone.TargetOrbit == _orbitState.CurrentOrbit;

            float sourceRadius = _orbitState.GetMovementRadius(zone.SourceOrbit, zone.TargetOrbit);
            float distance = Mathf.Abs(_movementModel.Radius - sourceRadius);
            return matchesCurrentPair ? distance : 1000.0f + distance;
        }

        private bool IsInside(GravityWellZone zone)
        {
            if (!IsAngleInside(zone.Arc))
            {
                return false;
            }

            float sourceRadius = _orbitState.GetMovementRadius(zone.SourceOrbit, zone.TargetOrbit);
            float targetRadius = _orbitState.GetMovementRadius(zone.TargetOrbit, zone.SourceOrbit);
            float minRadius = Mathf.Min(sourceRadius, targetRadius) - RADIUS_TOLERANCE;
            float maxRadius = Mathf.Max(sourceRadius, targetRadius) + RADIUS_TOLERANCE;
            return _movementModel.Radius >= minRadius && _movementModel.Radius <= maxRadius;
        }

        private bool IsAngleInside(Arc arc)
        {
            (float start, float end) = OrbitMath.NormalizeInterval(arc.StartAngleDegree, arc.EndAngleDegree);
            float angle = OrbitMath.NormalizeDegrees360(_movementModel.AngleRadians * OrbitMath.RADIANS_TO_DEGREES);
            if (angle < start)
            {
                angle += 360.0f;
            }

            return angle >= start && angle <= end;
        }

        private void Activate(GravityWellZone zone)
        {
            _activeZone = zone;
            _orbitState.SetCurrentAndOpposite(zone.SourceOrbit, zone.TargetOrbit);
        }

        private void PullTowardSourceOrbit(GravityWellZone zone)
        {
            float targetRadius = _orbitState.GetMovementRadius(zone.SourceOrbit, zone.TargetOrbit);
            _movementModel.Radius = Mathf.MoveTowards(
                _movementModel.Radius,
                targetRadius,
                zone.PullSpeed * Time.deltaTime);
        }

        private void JumpToNearestOrbit(GravityWellZone zone)
        {
            float sourceRadius = _orbitState.GetMovementRadius(zone.SourceOrbit, zone.TargetOrbit);
            float targetRadius = _orbitState.GetMovementRadius(zone.TargetOrbit, zone.SourceOrbit);
            float sourceDistance = Mathf.Abs(_movementModel.Radius - sourceRadius);
            float targetDistance = Mathf.Abs(_movementModel.Radius - targetRadius);

            Orbit returnOrbit;
            Orbit returnOppositeOrbit;
            float returnRadius;

            if (targetDistance < sourceDistance)
            {
                returnOrbit = zone.TargetOrbit;
                returnOppositeOrbit = zone.SourceOrbit;
                returnRadius = targetRadius;
            }
            else
            {
                returnOrbit = zone.SourceOrbit;
                returnOppositeOrbit = zone.TargetOrbit;
                returnRadius = sourceRadius;
            }

            _orbitState.SetCurrentAndOpposite(returnOrbit, returnOppositeOrbit);
            _jumpTransitionService.Play(new OrbitJumpStep(returnRadius, zone.ReturnDuration, Ease.OutQuad));
        }

        private void OnZonesChanged()
        {
            if (_zoneRegistry.Zones.Count == 0)
            {
                ClearState();
            }
        }

        private void ClearState()
        {
            _activeZone = null;
            _suppressedZone = null;
        }
    }
}
