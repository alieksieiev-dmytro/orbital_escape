using System.Collections.Generic;
using System.Linq;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    public sealed class PatternPlacementValidator
    {
        public bool IsValid(
            PatternPlacement placement,
            LevelBuildContext context,
            out string reason)
        {
            IReadOnlyList<PatternPlacedObject> placedObjects = placement.Objects;

            for (int i = 0; i < placedObjects.Count; i++)
            {
                PatternPlacedObject candidate = placedObjects[i];

                if (!HasValidNeighbor(candidate, context))
                {
                    reason = $"{candidate.ArcObject.Object.Type} on orbit {candidate.Orbit.Index} has no valid neighbor.";
                    return false;
                }

                if (ConflictsWithExisting(candidate, context, out reason))
                {
                    return false;
                }

                for (int j = i + 1; j < placedObjects.Count; j++)
                {
                    if (Conflicts(candidate, placedObjects[j], context, out reason))
                    {
                        return false;
                    }
                }
            }

            reason = string.Empty;
            return true;
        }

        private static bool ConflictsWithExisting(
            PatternPlacedObject candidate,
            LevelBuildContext context,
            out string reason)
        {
            foreach (Orbit orbit in context.Orbits)
            {
                for (int i = 0; i < orbit.OrbitObjects.Count; i++)
                {
                    PatternPlacedObject existing = new PatternPlacedObject(orbit, orbit.OrbitObjects[i]);
                    if (Conflicts(candidate, existing, context, out reason))
                    {
                        return true;
                    }
                }
            }

            reason = string.Empty;
            return false;
        }

        private static bool Conflicts(
            PatternPlacedObject a,
            PatternPlacedObject b,
            LevelBuildContext context,
            out string reason)
        {
            ArcObject aObject = a.ArcObject;
            ArcObject bObject = b.ArcObject;

            if (!aObject.Arc.IsOverlapping(bObject.Arc) &&
                !RiftsTooClose(a, b, context))
            {
                reason = string.Empty;
                return false;
            }

            if (a.Orbit.Index == b.Orbit.Index &&
                SideConflicts(aObject.Type, bObject.Type))
            {
                reason = $"{aObject.Object.Type} overlaps {bObject.Object.Type} on orbit {a.Orbit.Index}.";
                return true;
            }

            if (IsOppositeSpikeConflict(a, b))
            {
                reason = $"Opposite spikes overlap between orbits {a.Orbit.Index} and {b.Orbit.Index}.";
                return true;
            }

            if (IsGravityBandConflict(a, b, context))
            {
                reason = $"Gravity well overlaps incompatible obstacle between orbits {a.Orbit.Index} and {b.Orbit.Index}.";
                return true;
            }

            reason = string.Empty;
            return false;
        }

        private static bool RiftsTooClose(
            PatternPlacedObject a,
            PatternPlacedObject b,
            LevelBuildContext context)
        {
            return a.Orbit.Index == b.Orbit.Index &&
                   a.ArcObject.Object.Type == ArcObjectType.Rift &&
                   b.ArcObject.Object.Type == ArcObjectType.Rift &&
                   a.ArcObject.Arc.Distance(b.ArcObject.Arc) < context.Settings.Rift.MinDistanceBetweenRifts;
        }

        private static bool SideConflicts(ArcType a, ArcType b)
        {
            return a == b || a == ArcType.TwoSide || b == ArcType.TwoSide;
        }

        private static bool IsOppositeSpikeConflict(PatternPlacedObject a, PatternPlacedObject b)
        {
            if (a.ArcObject.Object.Type != ArcObjectType.Spike ||
                b.ArcObject.Object.Type != ArcObjectType.Spike ||
                System.Math.Abs(a.Orbit.Index - b.Orbit.Index) != 1)
            {
                return false;
            }

            return (a.ArcObject.Type == ArcType.Outer && b.ArcObject.Type == ArcType.Inner) ||
                   (a.ArcObject.Type == ArcType.Inner && b.ArcObject.Type == ArcType.Outer);
        }

        private static bool IsGravityBandConflict(
            PatternPlacedObject a,
            PatternPlacedObject b,
            LevelBuildContext context)
        {
            bool aGravity = a.ArcObject.Object.Type == ArcObjectType.GravityWell;
            bool bGravity = b.ArcObject.Object.Type == ArcObjectType.GravityWell;

            if (!aGravity && !bGravity)
            {
                return false;
            }

            if (aGravity && IsInGravityBand(a, b.Orbit.Index, context))
            {
                return IsGravityBlockingType(b.ArcObject.Object.Type);
            }

            if (bGravity && IsInGravityBand(b, a.Orbit.Index, context))
            {
                return IsGravityBlockingType(a.ArcObject.Object.Type);
            }

            return false;
        }

        private static bool IsGravityBlockingType(ArcObjectType type)
        {
            return type == ArcObjectType.Rift ||
                   type == ArcObjectType.ImpulseTrap ||
                   type == ArcObjectType.GravityWell;
        }

        private static bool IsInGravityBand(
            PatternPlacedObject gravityWell,
            int orbitIndex,
            LevelBuildContext context)
        {
            if (gravityWell.Orbit.Index == orbitIndex)
            {
                return true;
            }

            return TryGetNeighborOrbitIndex(gravityWell, context, out int neighborIndex) &&
                   neighborIndex == orbitIndex;
        }

        private static bool HasValidNeighbor(PatternPlacedObject candidate, LevelBuildContext context)
        {
            if (candidate.ArcObject.Object.Type != ArcObjectType.GravityWell)
            {
                return true;
            }

            return TryGetNeighborOrbitIndex(candidate, context, out _);
        }

        private static bool TryGetNeighborOrbitIndex(
            PatternPlacedObject candidate,
            LevelBuildContext context,
            out int neighborIndex)
        {
            neighborIndex = candidate.ArcObject.Type switch
            {
                ArcType.Inner => candidate.Orbit.Index - 1,
                ArcType.Outer => candidate.Orbit.Index + 1,
                _ => -1
            };

            if (neighborIndex < 0)
            {
                return false;
            }

            int resolvedNeighborIndex = neighborIndex;
            return context.Orbits.Any(orbit => orbit.Index == resolvedNeighborIndex);
        }
    }
}
