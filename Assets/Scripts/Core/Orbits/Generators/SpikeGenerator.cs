using System.Collections.Generic;
using System.Linq;
using OrbitalEscape.Orbits.Generators;
using OrbitalEscape.Utils;
using UnityEngine;
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
            if (!_processedOrbits.Contains(orbit))
            {
                _processedOrbits.Add(orbit);
            }

            List<float> orbitSections = OrbitMath.DivideOrbitIntoEqualArcs(orbit, _config.SpikeArcLength).ToList();
            int sectionCount = orbitSections.Count;

            int spikesCount = Mathf.CeilToInt(_config.Density * sectionCount);
            spikesCount = Mathf.Clamp(spikesCount, 0, sectionCount);

            List<int> availableSections = new List<int>(sectionCount);
            for (int i = 0; i < sectionCount; i++)
            {
                availableSections.Add(i);
            }

            List<ArcObject> resultSpikes = new List<ArcObject>(spikesCount);

            float deltaRadians = _config.SpikeArcLength / orbit.Radius;
            float deltaDegrees = deltaRadians * OrbitMath.RADIANS_TO_DEGREES;

            List<ArcObject> otherObjects = orbit.GetAllObjectsExceptType(ArcObjectType.Spike, arcType);
            otherObjects.AddRange(orbit.GetAllObjectsExceptType(ArcObjectType.Spike, ArcType.TwoSide));

            while (resultSpikes.Count < spikesCount && availableSections.Count > 0)
            {
                int pickPos = Random.Range(0, availableSections.Count);
                int sectionIndex = availableSections[pickPos];

                availableSections.RemoveAt(pickPos);

                bool allowNeighbor = Random.Range(0.0f, 1.0f) < _config.ConsecutiveSpikeChance;
                if (!allowNeighbor)
                {
                    int prev = (sectionIndex - 1 + sectionCount) % sectionCount;
                    int next = (sectionIndex + 1) % sectionCount;

                    availableSections.Remove(prev);
                    availableSections.Remove(next);
                }

                float startAngle = orbitSections[sectionIndex];
                float endAngle = startAngle + deltaDegrees;
                float height = Random.Range(_config.MinSpikeHeight, _config.MaxSpikeHeight);

                Arc arc = new Arc(startAngle, endAngle, orbit.Radius);

                if (OverlapsWithOtherArcs(arc, otherObjects))
                {
                    continue;
                }

                ArcObject spike = new ArcObject(arc, new SpikeObstacle(height), arcType);

                if (_processedOrbits.Count > 1 &&
                    CheckOverlapFromOppositeOrbit(spike, _processedOrbits[^2].OrbitObjects))
                {
                    continue;
                }

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

        private List<int> GetIndexes(int count)
        {
            var result = new List<int>(count);
            
            for (int i = 0; i < count; i++)
                result.Add(i);
            
            return result;
        }
    }
}
