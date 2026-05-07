using System.Collections.Generic;
using System;
using OrbitalEscape.Utils;

namespace OrbitalEscape.Orbits.Generators
{
    public class RiftGenerator : IArcGenerator
    {
        public List<ArcObject> GenerateArcs(Orbit orbit, ArcType arcType, LevelBuildContext context)
        {
            RiftGenerationSettings config = context.Settings.Rift;
            List<ArcObject> result = new List<ArcObject>();
            int targetCount = Math.Max(0, (int)Math.Ceiling(config.RiftCount));
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
    }
}
