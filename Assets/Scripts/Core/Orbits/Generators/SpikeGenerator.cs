using System.Collections.Generic;
using System.Linq;
using OrbitalEscape.Orbits.Generators;
using OrbitalEscape.Utils;
using Zenject;
using Random = UnityEngine.Random;

namespace OrbitalEscape.Orbits
{
    public class SpikeGenerator : IArcGenerator
    {
        private readonly List<Orbit> _processedOrbits;
        private readonly SpikeConfig _config;
        
        [Inject]
        private SpikeGenerator(SpikeConfig config)
        {
            _processedOrbits = new List<Orbit>();
            _config = config;
        }
        
        public List<ArcObject> GenerateArcs(Orbit orbit, ArcType arcType)
        {
            _processedOrbits.Add(orbit);
            
            int spikesCount = _config.BaseSpikeCount + orbit.Index * _config.AdditionalSpikeCountPerOrbit;
            List<ArcObject> resultSpikes = new List<ArcObject>(spikesCount);
            List<Arc> resultArcs = new List<Arc>(spikesCount);
            
            float deltaRadians = _config.SpikeArcLength / orbit.Radius;
            float deltaDegrees = deltaRadians * OrbitMath.RADIANS_TO_DEGREES;
            
            int attempts = 0;
            int maxAttempts = spikesCount * 100;

            while (resultSpikes.Count < spikesCount && attempts < maxAttempts)
            {
                attempts++;
                float startAngle = Random.Range(0.0f, 360.0f);
                float endAngle = startAngle + deltaDegrees;
                float height = Random.Range(_config.MinSpikeHeight, _config.MaxSpikeHeight);

                var arc = new Arc(startAngle, endAngle, orbit.Radius);

                if (OverlapsWithOtherArcs(arc, resultArcs))
                    continue;
                
                if (OverlapsWithOtherArcs(arc, orbit.TwoSideArcs))
                    continue;
                
                if (arcType == ArcType.Inner && OverlapsWithOtherArcs(arc, orbit.InnerArcs))
                    continue;
                
                if (arcType == ArcType.Outer && OverlapsWithOtherArcs(arc, orbit.OuterArcs))
                    continue;
                
                var spike = new ArcObject(arc, new SpikeObstacle(height), arcType);

                if (_processedOrbits.Count > 1 && CheckOverlapFromOppositeOrbit(spike, _processedOrbits[^2].OrbitObjects))
                    continue;

                resultSpikes.Add(spike);
                resultArcs.Add(arc);
            }

            return resultSpikes;
        }

        private bool CheckOverlapFromOppositeOrbit(ArcObject spike, IEnumerable<ArcObject> arcObjects)
        {
            ArcType spikeType = spike.Type;
            ArcType oppositeType = spikeType == ArcType.Outer ? ArcType.Inner : ArcType.Outer;

            foreach (ArcObject arcObject in arcObjects)
            {
                if (arcObject.Type != oppositeType)
                {
                    continue;
                }

                if (arcObject.Object is not SpikeObstacle)
                {
                    continue;
                }

                if (arcObject.Arc.IsOverlapping(spike.Arc))
                {
                    return true;
                }
            }

            return false;
        }

        private bool OverlapsWithOtherArcs(Arc spike, List<Arc> arcObjects)
        {
            for (int i = 0; i < arcObjects.Count; i++)
            {
                if (arcObjects[i].IsOverlapping(spike))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
