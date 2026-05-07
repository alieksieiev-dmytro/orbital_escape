using System.Collections.Generic;
using System;
using System.Linq;
using OrbitalEscape.Orbits.Generators;
using OrbitalEscape.Utils;

namespace OrbitalEscape.Orbits
{
    public class SpikeGenerator : IArcGenerator
    {
        public List<ArcObject> GenerateArcs(Orbit orbit, ArcType arcType, LevelBuildContext context)
        {
            SpikeGenerationSettings config = context.Settings.Spike;
            List<float> orbitSections = OrbitMath.DivideOrbitIntoEqualArcs(orbit, config.SpikeArcLength).ToList();
            int sectionCount = orbitSections.Count;

            int spikesCount = (int)Math.Ceiling(config.Density * sectionCount);
            spikesCount = Clamp(spikesCount, 0, sectionCount);

            List<int> availableSections = new List<int>(sectionCount);
            for (int i = 0; i < sectionCount; i++)
            {
                availableSections.Add(i);
            }

            List<ArcObject> resultSpikes = new List<ArcObject>(spikesCount);

            float deltaRadians = config.SpikeArcLength / orbit.Radius;
            float deltaDegrees = deltaRadians * OrbitMath.RADIANS_TO_DEGREES;

            List<ArcObject> otherObjects = orbit.GetAllObjectsExceptType(ArcObjectType.Spike, arcType);
            otherObjects.AddRange(orbit.GetAllObjectsExceptType(ArcObjectType.Spike, ArcType.TwoSide));

            while (resultSpikes.Count < spikesCount && availableSections.Count > 0)
            {
                int pickPos = context.Random.Range(0, availableSections.Count);
                int sectionIndex = availableSections[pickPos];

                availableSections.RemoveAt(pickPos);

                bool allowNeighbor = context.Random.Range(0.0f, 1.0f) < config.ConsecutiveSpikeChance;
                if (!allowNeighbor)
                {
                    int prev = (sectionIndex - 1 + sectionCount) % sectionCount;
                    int next = (sectionIndex + 1) % sectionCount;

                    availableSections.Remove(prev);
                    availableSections.Remove(next);
                }

                float startAngle = orbitSections[sectionIndex];
                float endAngle = startAngle + deltaDegrees;
                float height = context.Random.Range(config.MinSpikeHeight, config.MaxSpikeHeight);

                Arc arc = new Arc(startAngle, endAngle, orbit.Radius);

                if (OverlapsWithOtherArcs(arc, otherObjects))
                {
                    continue;
                }

                ArcObject spike = new ArcObject(arc, new SpikeObstacle(height), arcType);

                if (context.TryGetPreviousOrbit(out Orbit previousOrbit) &&
                    CheckOverlapFromOppositeOrbit(spike, previousOrbit.OrbitObjects))
                {
                    continue;
                }

                resultSpikes.Add(spike);
            }

            return resultSpikes;
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
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
