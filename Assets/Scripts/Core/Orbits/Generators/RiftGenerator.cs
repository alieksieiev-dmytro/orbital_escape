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

        public List<ArcObject> GenerateArcs(float radius)
        {
            List<ArcObject> result = new List<ArcObject>();

            while (result.Count < _config.RiftCount)
            {
                float startAngle = Random.Range(0.0f, 360.0f);
                float deltaRadians = _config.RiftLength / radius;
                float deltaDegrees = deltaRadians * OrbitMath.RADIANS_TO_DEGREES;
                float endAngle = startAngle + deltaDegrees;
                
                Arc newRiftArc = new Arc(startAngle, endAngle, radius);
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

                var newRift = new ArcObject(newRiftArc, new Rift(), ArcType.TwoSide);
                result.Add(newRift);
            }
            
            return result;
        }
    }
}
