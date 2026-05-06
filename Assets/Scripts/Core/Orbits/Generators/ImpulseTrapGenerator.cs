using System.Collections.Generic;
using System.Linq;
using OrbitalEscape.Utils;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace OrbitalEscape.Orbits.Generators
{
    public class ImpulseTrapGenerator : IArcGenerator
    {
        private readonly ImpulseTrapConfig _config;

        [Inject]
        public ImpulseTrapGenerator(ImpulseTrapConfig config)
        {
            _config = config;
        }

        public List<ArcObject> GenerateArcs(Orbit orbit, ArcType arcType, LevelBuildContext context)
        {
            List<float> orbitSections = OrbitMath.DivideOrbitIntoEqualArcs(orbit, _config.TrapArcLength).ToList();
            int sectionCount = orbitSections.Count;

            int trapsCount = Mathf.CeilToInt(_config.Density * sectionCount);
            trapsCount = Mathf.Clamp(trapsCount, 0, sectionCount);

            List<int> availableSections = GetIndexes(sectionCount);
            List<ArcObject> resultTraps = new List<ArcObject>(trapsCount);
            List<ArcObject> blockingObjects = GetBlockingObjects(orbit, arcType);

            float deltaRadians = _config.TrapArcLength / orbit.Radius;
            float deltaDegrees = deltaRadians * OrbitMath.RADIANS_TO_DEGREES;

            while (resultTraps.Count < trapsCount && availableSections.Count > 0)
            {
                int pickPos = Random.Range(0, availableSections.Count);
                int sectionIndex = availableSections[pickPos];

                availableSections.RemoveAt(pickPos);

                float startAngle = orbitSections[sectionIndex];
                Arc arc = new Arc(startAngle, startAngle + deltaDegrees, orbit.Radius);

                if (OverlapsWithOtherArcs(arc, blockingObjects))
                    continue;

                ArcObject trap = new ArcObject(arc, new ImpulseTrap(_config.Height), arcType);
                resultTraps.Add(trap);
                blockingObjects.Add(trap);
            }

            return resultTraps;
        }

        private List<ArcObject> GetBlockingObjects(Orbit orbit, ArcType arcType)
        {
            List<ArcObject> blockingObjects = orbit.OrbitObjects
                .Where(obj => obj.Type == arcType)
                .ToList();

            blockingObjects.AddRange(orbit.GetAllObjectsExceptType(ArcObjectType.ImpulseTrap, ArcType.TwoSide));

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

        private List<int> GetIndexes(int count)
        {
            var result = new List<int>(count);

            for (int i = 0; i < count; i++)
                result.Add(i);

            return result;
        }
    }
}
