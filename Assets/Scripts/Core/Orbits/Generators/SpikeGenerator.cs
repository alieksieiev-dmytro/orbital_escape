using System;
using System.Collections.Generic;
using System.Linq;
using OrbitalEscape.Utils;
using Zenject;
using Random = UnityEngine.Random;

namespace OrbitalEscape.Orbits.Generators
{
    public class SpikeGenerator : IArcGenerator
    {
        private SpikeConfig _config;
        
        [Inject]
        private void Construct(SpikeConfig config)
        {
            _config = config;
        }
        
        public List<ArcObject> GenerateArcs(Orbit orbit, ArcType arcType)
        {
            List<ArcObject> result = new List<ArcObject>();
            var spikesCount = _config.BaseSpikeCount + orbit.Index * _config.AdditionalSpikeCountPerOrbit;

            while (result.Count < spikesCount)
            {
                float startAngle = Random.Range(0.0f, 360.0f);
                float deltaRadians = _config.SpikeArcLength / orbit.Radius;
                float deltaDegrees = deltaRadians * OrbitMath.RADIANS_TO_DEGREES;
                float endAngle = startAngle + deltaDegrees;
                
                var newSpikeArc = new Arc(startAngle, endAngle, orbit.Radius);
                bool arcConflict = result.Any(arc => newSpikeArc.IsOverlapping(arc.Arc));

                List<Arc> arcsFromOrbit = new List<Arc>();
                arcsFromOrbit.AddRange(orbit.TwoSideArcs);
                if (arcType == ArcType.Inner)
                    arcsFromOrbit.AddRange(orbit.InnerArcs);
                else if (arcType == ArcType.Outer)
                    arcsFromOrbit.AddRange(orbit.OuterArcs);

                arcConflict |= arcsFromOrbit.Any(arc => newSpikeArc.IsOverlapping(arc));

                if (arcConflict)
                    continue;

                float spikeHeight = Random.Range(_config.MinSpikeHeight, _config.MaxSpikeHeight);
                var newSpike = new ArcObject(newSpikeArc, new SpikeObstacle(spikeHeight), arcType);
                result.Add(newSpike);
            }
            
            return result;
        }
    }
}
