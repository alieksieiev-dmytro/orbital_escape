using System.Collections.Generic;
using System;
using System.Linq;
using OrbitalEscape.Utils;

namespace OrbitalEscape.Orbits.Generators
{
    public class RiftGenerator : IArcGenerator
    {
        public List<ArcObject> GenerateArcs(Orbit orbit, ArcType arcType, LevelBuildContext context)
        {
            RiftGenerationSettings config = context.Settings.Rift;
            List<ArcObject> result = new List<ArcObject>();
            List<ArcObject> blockingObjects = GetBlockingObjects(context);
            int targetCount = config.GetTargetCount(orbit);
            int maxAttempts = Math.Max(config.MaxPlacementAttempts, targetCount);
            int attempts = 0;

            while (result.Count < targetCount && attempts < maxAttempts)
            {
                attempts++;
                float startAngle = context.Random.Range(0.0f, 360.0f);
                float deltaRadians = config.RiftLength / orbit.Radius;
                float deltaDegrees = deltaRadians * OrbitMath.RADIANS_TO_DEGREES;
                float endAngle = startAngle + deltaDegrees;
                
                Arc newRiftArc = new Arc(startAngle, endAngle, orbit.Radius);
                if (OverlapsWithOtherArcs(newRiftArc, blockingObjects))
                {
                    continue;
                }

                bool arcConflict = false;

                foreach (var arc in result)
                {
                    if (newRiftArc.IsOverlapping(arc.Arc) ||
                        newRiftArc.Distance(arc.Arc) < config.MinDistanceBetweenRifts)
                    {
                        arcConflict = true;
                        break;
                    }
                }
                
                if (arcConflict)
                    continue;

                var newRift = new ArcObject(newRiftArc, new Rift(config.RiftPadding), ArcType.TwoSide);
                result.Add(newRift);
            }

            if (result.Count < targetCount)
            {
                context.AddWarning(
                    $"Generated {result.Count}/{targetCount} rifts for orbit {orbit.Index} after {attempts} attempts. " +
                    "Adjust rift count, length, spacing, or max attempts.");
            }
            
            return result;
        }

        private static List<ArcObject> GetBlockingObjects(LevelBuildContext context)
        {
            if (!context.TryGetPreviousOrbit(out Orbit previousOrbit))
            {
                return new List<ArcObject>();
            }

            return previousOrbit.OrbitObjects
                .Where(obj => obj.Object.Type == ArcObjectType.GravityWell && obj.Type == ArcType.Outer)
                .ToList();
        }

        private static bool OverlapsWithOtherArcs(Arc arc, IReadOnlyList<ArcObject> arcObjects)
        {
            for (int i = 0; i < arcObjects.Count; i++)
            {
                if (arc.IsOverlapping(arcObjects[i].Arc))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
