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
            
            var orbitSections = OrbitMath.DivideOrbitIntoEqualArcs(orbit, _config.SpikeArcLength).ToList();
            int spikesCount = _config.BaseSpikeCount + orbit.Index * _config.AdditionalSpikeCountPerOrbit;
            List<ArcObject> resultSpikes = new List<ArcObject>();
            
            float deltaRadians = _config.SpikeArcLength / orbit.Radius;
            float deltaDegrees = deltaRadians * OrbitMath.RADIANS_TO_DEGREES;
            
            var otherObjects = orbit.GetAllObjectsExceptType(ArcObjectType.Spike, arcType);
            otherObjects.AddRange(orbit.GetAllObjectsExceptType(ArcObjectType.Spike, ArcType.TwoSide));

            while (resultSpikes.Count < spikesCount)
            {
                int sectionIndex = Random.Range(0, orbitSections.Count);
                
                float startAngle = orbitSections[sectionIndex];
                orbitSections.RemoveAt(sectionIndex);
                
                float endAngle = startAngle + deltaDegrees;
                float height = Random.Range(_config.MinSpikeHeight, _config.MaxSpikeHeight);
                
                var arc = new Arc(startAngle, endAngle, orbit.Radius);
                
                if (OverlapsWithOtherArcs(arc, otherObjects))
                    continue;
                
                var spike = new ArcObject(arc, new SpikeObstacle(height), arcType);

                if (_processedOrbits.Count > 1 && CheckOverlapFromOppositeOrbit(spike, _processedOrbits[^2].OrbitObjects))
                    continue;
                
                resultSpikes.Add(spike);
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

        private bool OverlapsWithOtherArcs(Arc spike, List<ArcObject> arcObjects)
        {
            for (int i = 0; i < arcObjects.Count; i++)
            {
                if (spike.IsOverlapping(arcObjects[i].Arc))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
