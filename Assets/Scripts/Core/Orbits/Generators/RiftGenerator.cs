using System.Collections.Generic;
using OrbitalEscape.Utils;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Orbits.Generators
{
    public class RiftGenerator : IArcGenerator
    {
        private RiftConfig _config;

        [Inject]
        public void Construct(RiftConfig config)
        {
            _config = config;
        }

        public List<ArcObject> GenerateArcs(Orbit orbit, ArcType arcType, LevelBuildContext context)
        {
            List<ArcObject> result = new List<ArcObject>();
            int targetCount = Mathf.Max(0, Mathf.CeilToInt(_config.RiftCount));
            int maxAttempts = Mathf.Max(_config.MaxPlacementAttempts, targetCount);
            int attempts = 0;

            while (result.Count < targetCount && attempts < maxAttempts)
            {
                attempts++;
                float startAngle = Random.Range(0.0f, 360.0f);
                float deltaRadians = _config.RiftLength / orbit.Radius;
                float deltaDegrees = deltaRadians * OrbitMath.RADIANS_TO_DEGREES;
                float endAngle = startAngle + deltaDegrees;
                
                Arc newRiftArc = new Arc(startAngle, endAngle, orbit.Radius);
                bool arcConflict = false;

                foreach (var arc in result)
                {
                    if (newRiftArc.IsOverlapping(arc.Arc) ||
                        newRiftArc.Distance(arc.Arc) < _config.MinDistanceBetweenRifts)
                    {
                        arcConflict = true;
                        break;
                    }
                }
                
                if (arcConflict)
                    continue;

                var newRift = new ArcObject(newRiftArc, new Rift(_config.RiftPaddings), ArcType.TwoSide);
                result.Add(newRift);
            }

            if (result.Count < targetCount)
            {
                Debug.LogWarning(
                    $"Generated {result.Count}/{targetCount} rifts for orbit {orbit.Index} after {attempts} attempts. " +
                    "Adjust rift count, length, spacing, or max attempts.");
            }
            
            return result;
        }
    }
}
