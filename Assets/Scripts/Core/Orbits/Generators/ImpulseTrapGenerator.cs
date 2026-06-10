using System.Collections.Generic;
using System;
using System.Linq;
using OrbitalEscape.Utils;

namespace OrbitalEscape.Orbits.Generators
{
    public class ImpulseTrapGenerator : IArcGenerator
    {
        public List<ArcObject> GenerateArcs(Orbit orbit, ArcType arcType, LevelBuildContext context)
        {
            ImpulseTrapGenerationSettings config = context.Settings.ImpulseTrap;
            List<float> orbitSections = OrbitMath.DivideOrbitIntoEqualArcs(orbit, config.TrapArcLength).ToList();
            int sectionCount = orbitSections.Count;

            int trapsCount = (int)Math.Ceiling(config.Density * sectionCount);
            trapsCount = Clamp(trapsCount, 0, sectionCount);

            List<int> availableSections = GetIndexes(sectionCount);
            List<ArcObject> resultTraps = new List<ArcObject>(trapsCount);
            List<ArcObject> blockingObjects = GetBlockingObjects(orbit, arcType, context);

            float deltaRadians = config.TrapArcLength / orbit.Radius;
            float deltaDegrees = deltaRadians * OrbitMath.RADIANS_TO_DEGREES;

            while (resultTraps.Count < trapsCount && availableSections.Count > 0)
            {
                int pickPos = context.Random.Range(0, availableSections.Count);
                int sectionIndex = availableSections[pickPos];

                availableSections.RemoveAt(pickPos);

                float startAngle = orbitSections[sectionIndex];
                Arc arc = new Arc(startAngle, startAngle + deltaDegrees, orbit.Radius);

                if (OverlapsWithOtherArcs(arc, blockingObjects))
                    continue;

                ArcObject trap = new ArcObject(arc, new ImpulseTrap(config.Height), arcType);
                resultTraps.Add(trap);
                blockingObjects.Add(trap);
            }

            return resultTraps;
        }

        private List<ArcObject> GetBlockingObjects(Orbit orbit, ArcType arcType, LevelBuildContext context)
        {
            List<ArcObject> blockingObjects = orbit.OrbitObjects
                .Where(obj => obj.Type == arcType)
                .ToList();

            blockingObjects.AddRange(orbit.GetAllObjectsExceptType(ArcObjectType.ImpulseTrap, ArcType.TwoSide));
            blockingObjects.AddRange(orbit.GetOrbitObjectsByType(ArcObjectType.GravityWell));

            if (context.TryGetPreviousOrbit(out Orbit previousOrbit))
            {
                blockingObjects.AddRange(previousOrbit.OrbitObjects
                    .Where(obj => obj.Object.Type == ArcObjectType.GravityWell && obj.Type == ArcType.Outer));
            }

            return blockingObjects;
        }

        private bool OverlapsWithOtherArcs(Arc trap, List<ArcObject> arcObjects)
        {
            for (int i = 0; i < arcObjects.Count; i++)
            {
                if (trap.IsOverlapping(arcObjects[i].Arc))
                    return true;
            }

            return false;
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
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
