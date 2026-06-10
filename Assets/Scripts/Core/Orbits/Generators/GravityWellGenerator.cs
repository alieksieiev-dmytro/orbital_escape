using System;
using System.Collections.Generic;
using System.Linq;
using OrbitalEscape.Utils;

namespace OrbitalEscape.Orbits.Generators
{
    public sealed class GravityWellGenerator : IArcGenerator
    {
        public List<ArcObject> GenerateArcs(Orbit orbit, ArcType arcType, LevelBuildContext context)
        {
            GravityWellGenerationSettings config = context.Settings.GravityWell;
            if (!HasNeighbor(orbit, arcType, context) || config.FieldArcLength <= 0.0f)
            {
                return new List<ArcObject>();
            }

            List<float> orbitSections = OrbitMath.DivideOrbitIntoEqualArcs(orbit, config.FieldArcLength).ToList();
            int sectionCount = orbitSections.Count;
            int wellsCount = Clamp((int)Math.Ceiling(config.Density * sectionCount), 0, sectionCount);

            List<int> availableSections = GetIndexes(sectionCount);
            List<ArcObject> resultWells = new List<ArcObject>(wellsCount);
            List<ArcObject> blockingObjects = GetBlockingObjects(orbit, arcType, context);

            float deltaRadians = config.FieldArcLength / orbit.Radius;
            float deltaDegrees = deltaRadians * OrbitMath.RADIANS_TO_DEGREES;

            while (resultWells.Count < wellsCount && availableSections.Count > 0)
            {
                int pickPos = context.Random.Range(0, availableSections.Count);
                int sectionIndex = availableSections[pickPos];
                availableSections.RemoveAt(pickPos);

                float startAngle = orbitSections[sectionIndex];
                Arc arc = new Arc(startAngle, startAngle + deltaDegrees, orbit.Radius);
                if (OverlapsWithOtherArcs(arc, blockingObjects))
                {
                    continue;
                }

                ArcObject gravityWell = new ArcObject(
                    arc,
                    new GravityWell(
                        config.PartialJumpStep,
                        config.PullSpeed,
                        config.ReturnDuration,
                        config.VisualAlpha),
                    arcType);
                resultWells.Add(gravityWell);
                blockingObjects.Add(gravityWell);
            }

            return resultWells;
        }

        private static List<ArcObject> GetBlockingObjects(Orbit orbit, ArcType arcType, LevelBuildContext context)
        {
            List<ArcObject> blockingObjects = new List<ArcObject>();
            AddBlockingObjectsFromOrbit(blockingObjects, orbit);

            if (TryGetGeneratedTargetOrbit(orbit, arcType, context, out Orbit targetOrbit))
            {
                AddBlockingObjectsFromOrbit(blockingObjects, targetOrbit);
            }

            return blockingObjects;
        }

        private static void AddBlockingObjectsFromOrbit(List<ArcObject> blockingObjects, Orbit orbit)
        {
            blockingObjects.AddRange(orbit.OrbitObjects
                .Where(IsBlockingObstacle));
        }

        private static bool IsBlockingObstacle(ArcObject arcObject)
        {
            return arcObject.Object.Type == ArcObjectType.Rift ||
                   arcObject.Object.Type == ArcObjectType.ImpulseTrap ||
                   arcObject.Object.Type == ArcObjectType.GravityWell;
        }

        private static bool HasNeighbor(Orbit orbit, ArcType arcType, LevelBuildContext context)
        {
            return arcType switch
            {
                ArcType.Inner => orbit.Index > 0,
                ArcType.Outer => orbit.Index < context.LastPlannedOrbitIndex,
                _ => false
            };
        }

        private static bool TryGetGeneratedTargetOrbit(
            Orbit orbit,
            ArcType arcType,
            LevelBuildContext context,
            out Orbit targetOrbit)
        {
            int targetIndex = arcType switch
            {
                ArcType.Inner => orbit.Index - 1,
                ArcType.Outer => orbit.Index + 1,
                _ => -1
            };

            for (int i = 0; i < context.Orbits.Count; i++)
            {
                if (context.Orbits[i].Index == targetIndex)
                {
                    targetOrbit = context.Orbits[i];
                    return true;
                }
            }

            targetOrbit = null;
            return false;
        }

        private static bool OverlapsWithOtherArcs(Arc arc, IReadOnlyList<ArcObject> arcObjects)
        {
            for (int i = 0; i < arcObjects.Count; i++)
            {
                if (arc.IsOverlapping(arcObjects[i].Arc))
                {
                    return true;
                }
            }

            return false;
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        private static List<int> GetIndexes(int count)
        {
            List<int> result = new List<int>(count);
            for (int i = 0; i < count; i++)
            {
                result.Add(i);
            }

            return result;
        }
    }
}
