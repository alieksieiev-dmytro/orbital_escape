using System;
using System.Collections.Generic;
using OrbitalEscape.Utils;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    public sealed class PatternLevelContentGenerator : ILevelContentGenerator
    {
        private const int MaxCursorStepsPerBand = 2048;
        private const int MaxRiftMarkerLayoutCandidates = 16;
        private const float OrbitBoundaryTolerance = 0.0001f;
        private const float ArcOverlapToleranceDegrees = 0.0001f;

        private readonly WeightedPatternSelector _selector;

        public PatternLevelContentGenerator(WeightedPatternSelector selector)
        {
            _selector = selector;
        }

        public void Populate(
            IReadOnlyList<Orbit> generatedOrbits,
            LevelBuildContext context,
            LevelBuildRequest request)
        {
            if (generatedOrbits == null || generatedOrbits.Count == 0)
            {
                return;
            }

            PatternGenerationSettings settings = context.Settings.Patterns;
            if (settings.Patterns.Count == 0)
            {
                context.AddWarning("Pattern generation skipped: no obstacle patterns are configured.");
                return;
            }

            HashSet<int> writableOrbitIndices = GetWritableOrbitIndices(generatedOrbits, context.StartIndex);
            Dictionary<int, List<float>> riftMarkerAngles = CreateRiftMarkerAngles(
                context,
                writableOrbitIndices,
                request.Seed);
            int firstGeneratedBandIndex = context.StartIndex > 0 ? context.StartIndex - 1 : context.StartIndex;
            int firstBandIndex = Math.Max(firstGeneratedBandIndex, context.Settings.Content.FirstOrbitIndexWithContent);
            for (int lowerOrbitIndex = firstBandIndex; lowerOrbitIndex < context.LastPlannedOrbitIndex; lowerOrbitIndex++)
            {
                Orbit lowerOrbit = FindOrbit(context, lowerOrbitIndex);
                Orbit upperOrbit = FindOrbit(context, lowerOrbitIndex + 1);
                if (lowerOrbit == null || upperOrbit == null)
                {
                    continue;
                }

                GenerateBand(
                    lowerOrbit,
                    upperOrbit,
                    context,
                    riftMarkerAngles,
                    writableOrbitIndices);
            }

            WarnForUnmetRiftMarkers(context, riftMarkerAngles, writableOrbitIndices);
        }

        private void GenerateBand(
            Orbit lowerOrbit,
            Orbit upperOrbit,
            LevelBuildContext context,
            Dictionary<int, List<float>> riftMarkerAngles,
            HashSet<int> writableOrbitIndices)
        {
            PatternDifficultyStageSnapshot stage = context.Settings.Patterns.GetStage(lowerOrbit.Index);
            if (!context.Settings.ShouldGenerateContent(lowerOrbit) ||
                !context.Settings.ShouldGenerateContent(upperOrbit))
            {
                return;
            }

            float cursorWorld = 0.0f;
            PatternPlacement previousPlacement = null;
            PatternPlacement firstPlacement = null;
            List<ObstaclePatternSnapshot> history = new List<ObstaclePatternSnapshot>();
            float bandPhaseDegree = context.Settings.RandomizeBandPhase
                ? context.Random.Range(0.0f, 360.0f)
                : 0.0f;
            int step = 0;

            for (; step < MaxCursorStepsPerBand && cursorWorld < lowerOrbit.Circumference; step++)
            {
                PatternRejectionLog rejectionLog = new PatternRejectionLog();
                List<WeightedPatternCandidate> candidates = BuildCandidates(
                    lowerOrbit,
                    upperOrbit,
                    cursorWorld,
                    previousPlacement,
                    firstPlacement,
                    bandPhaseDegree,
                    stage,
                    history,
                    context,
                    rejectionLog,
                    riftMarkerAngles,
                    writableOrbitIndices,
                    out float nextCursorWorld);

                if (!_selector.TrySelect(candidates, context.Random, out PatternPlacement placement))
                {
                    if (nextCursorWorld > cursorWorld + OrbitBoundaryTolerance &&
                        nextCursorWorld < lowerOrbit.Circumference)
                    {
                        // Crossed markers remain due after a clearance jump.
                        cursorWorld = nextCursorWorld;
                        continue;
                    }

                    if (HasPendingRiftMarkers(lowerOrbit, upperOrbit, riftMarkerAngles))
                    {
                        context.AddWarning(
                            $"Rift placement exhausted for orbit band {lowerOrbit.Index}-{upperOrbit.Index} " +
                            $"at {cursorWorld:F2} world units. Markers remain pending. Rejections: {rejectionLog.Summary}.");
                    }

                    break;
                }

                placement = CreatePlacement(
                    placement.Pattern,
                    placement.Flipped,
                    lowerOrbit,
                    upperOrbit,
                    placement.StartWorld,
                    bandPhaseDegree,
                    context,
                    sampleRandomSpikeHeight: true);

                ApplyPlacement(placement, context, writableOrbitIndices);
                FulfillRiftMarkers(placement, riftMarkerAngles, writableOrbitIndices, bandPhaseDegree);

                firstPlacement ??= placement;
                previousPlacement = placement;
                cursorWorld = placement.StartWorld + placement.Pattern.Length;
                history.Add(placement.Pattern);
                if (history.Count > 8)
                {
                    history.RemoveAt(0);
                }
            }

            if (step == MaxCursorStepsPerBand && cursorWorld < lowerOrbit.Circumference)
            {
                context.AddWarning(
                    $"Pattern generation reached its {MaxCursorStepsPerBand} step limit for orbit band " +
                    $"{lowerOrbit.Index}-{upperOrbit.Index}. Unfulfilled rift markers remain pending.");
            }
        }

        private List<WeightedPatternCandidate> BuildCandidates(
            Orbit lowerOrbit,
            Orbit upperOrbit,
            float cursorWorld,
            PatternPlacement previousPlacement,
            PatternPlacement firstPlacement,
            float bandPhaseDegree,
            PatternDifficultyStageSnapshot stage,
            IReadOnlyList<ObstaclePatternSnapshot> history,
            LevelBuildContext context,
            PatternRejectionLog rejectionLog,
            Dictionary<int, List<float>> riftMarkerAngles,
            HashSet<int> writableOrbitIndices,
            out float nextCursorWorld)
        {
            PatternGenerationSettings settings = context.Settings.Patterns;
            List<WeightedPatternCandidate> candidates = new List<WeightedPatternCandidate>();
            List<WeightedPatternCandidate> partialRiftCandidates = new List<WeightedPatternCandidate>();
            float nearestNextCursorWorld = GetNextRiftMarkerCursorWorld(
                lowerOrbit, upperOrbit, cursorWorld, bandPhaseDegree, riftMarkerAngles);

            for (int i = 0; i < settings.Patterns.Count && candidates.Count < settings.MaxSelectionAttempts; i++)
            {
                ObstaclePatternSnapshot pattern = settings.Patterns[i];
                if (!IsEligibleByContext(pattern, stage, lowerOrbit.Index, out string contextReason))
                {
                    rejectionLog.Add(contextReason);
                    continue;
                }

                TryAddCandidate(false);
                if (pattern.AllowFlip)
                {
                    TryAddCandidate(true);
                }

                void TryAddCandidate(bool flipped)
                {
                    float previousEnd = previousPlacement != null
                        ? previousPlacement.StartWorld + previousPlacement.Pattern.Length
                        : 0.0f;
                    float requiredPadding = previousPlacement != null && RequiresSafePadding(
                        previousPlacement.Pattern, previousPlacement.Flipped, pattern, flipped)
                        ? settings.SafePadding
                        : 0.0f;
                    float startWorld = Math.Max(cursorWorld, previousEnd + requiredPadding);
                    float bandEndWorld = lowerOrbit.Circumference + (firstPlacement?.StartWorld ?? 0.0f);
                    if (pattern.Length >= lowerOrbit.Circumference ||
                        startWorld >= lowerOrbit.Circumference ||
                        startWorld + pattern.Length > bandEndWorld + OrbitBoundaryTolerance)
                    {
                        rejectionLog.Add("pattern exceeds remaining orbit span");
                        return;
                    }

                    ObstaclePatternSnapshot firstPattern = firstPlacement?.Pattern ?? pattern;
                    bool firstFlipped = firstPlacement?.Flipped ?? flipped;
                    float firstStartWorld = firstPlacement?.StartWorld ?? startWorld;
                    float seamGap = lowerOrbit.Circumference + firstStartWorld -
                                    (startWorld + pattern.Length);
                    if (RequiresSafePadding(pattern, flipped, firstPattern, firstFlipped) &&
                        seamGap + OrbitBoundaryTolerance < settings.SafePadding)
                    {
                        rejectionLog.Add("pattern violates safe padding at orbit seam");
                        return;
                    }

                    PatternPlacement placement = CreatePlacement(
                        pattern, flipped, lowerOrbit, upperOrbit, startWorld,
                        bandPhaseDegree, context, sampleRandomSpikeHeight: false);

                    float weight = PatternWeightCalculator.GetEffectiveWeight(pattern, stage, history);
                    if (weight <= 0.0f)
                    {
                        rejectionLog.Add("weight reduced to zero");
                        return;
                    }

                    if (!IsPlacementCompatibleWithExistingObjects(
                            placement, context, writableOrbitIndices,
                            out string placementReason, out float placementNextCursorWorld))
                    {
                        rejectionLog.Add(placementReason);
                        if (placementNextCursorWorld > cursorWorld + OrbitBoundaryTolerance)
                        {
                            nearestNextCursorWorld = Math.Min(nearestNextCursorWorld, placementNextCursorWorld);
                        }
                        return;
                    }

                    if (!SatisfiesRiftMarkers(
                            placement, bandPhaseDegree, riftMarkerAngles, writableOrbitIndices,
                            out bool satisfiesAllDueMarkers, out string markerReason))
                    {
                        rejectionLog.Add(markerReason);
                        return;
                    }

                    WeightedPatternCandidate candidate = new WeightedPatternCandidate(placement, weight);
                    if (satisfiesAllDueMarkers)
                    {
                        candidates.Add(candidate);
                    }
                    else if (partialRiftCandidates.Count < settings.MaxSelectionAttempts)
                    {
                        // Serve one lane now; the other lane's markers stay due at the next cursor.
                        partialRiftCandidates.Add(candidate);
                    }
                }
            }

            nextCursorWorld = nearestNextCursorWorld;
            return candidates.Count > 0 ? candidates : partialRiftCandidates;
        }

        private static bool RequiresSafePadding(
            ObstaclePatternSnapshot previous,
            bool previousFlipped,
            ObstaclePatternSnapshot next,
            bool nextFlipped)
        {
            IReadOnlyList<PatternSlotSnapshot> previousLower = GetLane(previous, previousFlipped, lowerLane: true);
            IReadOnlyList<PatternSlotSnapshot> previousUpper = GetLane(previous, previousFlipped, lowerLane: false);
            IReadOnlyList<PatternSlotSnapshot> nextLower = GetLane(next, nextFlipped, lowerLane: true);
            IReadOnlyList<PatternSlotSnapshot> nextUpper = GetLane(next, nextFlipped, lowerLane: false);

            return EndsWithObstacle(previousLower) && StartsWithObstacle(nextUpper) ||
                   EndsWithObstacle(previousUpper) && StartsWithObstacle(nextLower);
        }

        private static bool EndsWithObstacle(IReadOnlyList<PatternSlotSnapshot> slots)
        {
            return slots.Count > 0 && slots[^1].IsObstacle;
        }

        private static bool StartsWithObstacle(IReadOnlyList<PatternSlotSnapshot> slots)
        {
            return slots.Count > 0 && slots[0].IsObstacle;
        }

        private PatternPlacement CreatePlacement(
            ObstaclePatternSnapshot pattern,
            bool flipped,
            Orbit lowerOrbit,
            Orbit upperOrbit,
            float startWorld,
            float bandPhaseDegree,
            LevelBuildContext context,
            bool sampleRandomSpikeHeight)
        {
            List<PatternPlacedObject> objects = new List<PatternPlacedObject>();

            IReadOnlyList<PatternSlotSnapshot> lowerSlots = GetLane(pattern, flipped, lowerLane: true);
            IReadOnlyList<PatternSlotSnapshot> upperSlots = GetLane(pattern, flipped, lowerLane: false);
            float lowerLaneLength = GetLaneLength(pattern, flipped, lowerLane: true);
            float upperLaneLength = GetLaneLength(pattern, flipped, lowerLane: false);
            float patternAngle = pattern.Length / Math.Max(0.01f, lowerOrbit.Radius);
            float upperPatternLength = patternAngle * upperOrbit.Radius;
            float upperStartWorld = startWorld / Math.Max(0.01f, lowerOrbit.Radius) * upperOrbit.Radius;
            IReadOnlyList<PatternSlotSnapshot> adjustedUpperSlots = CreateUpperLaneSlots(
                upperSlots,
                upperLaneLength,
                upperPatternLength);
            float adjustedUpperLaneLength = SumLaneLength(adjustedUpperSlots);

            AddLaneObjects(
                lowerSlots,
                pattern.Length,
                lowerLaneLength,
                lowerOrbit,
                ArcType.Outer,
                startWorld,
                bandPhaseDegree,
                context,
                sampleRandomSpikeHeight,
                objects);

            AddLaneObjects(
                adjustedUpperSlots,
                upperPatternLength,
                adjustedUpperLaneLength,
                upperOrbit,
                ArcType.Inner,
                upperStartWorld,
                bandPhaseDegree,
                context,
                sampleRandomSpikeHeight,
                objects);

            float startAngle = bandPhaseDegree + WorldToDegrees(startWorld, lowerOrbit.Radius);
            float endAngle = bandPhaseDegree +
                             WorldToDegrees(startWorld + Math.Max(pattern.Length, 0.01f), lowerOrbit.Radius);
            return new PatternPlacement(
                pattern,
                flipped,
                lowerOrbit,
                upperOrbit,
                startWorld,
                startAngle,
                endAngle,
                objects);
        }

        private void AddLaneObjects(
            IReadOnlyList<PatternSlotSnapshot> slots,
            float patternLength,
            float laneLength,
            Orbit orbit,
            ArcType side,
            float patternStartWorld,
            float bandPhaseDegree,
            LevelBuildContext context,
            bool sampleRandomSpikeHeight,
            List<PatternPlacedObject> objects)
        {
            float localWorld = Math.Max(0.0f, (patternLength - laneLength) * 0.5f);

            for (int i = 0; i < slots.Count; i++)
            {
                PatternSlotSnapshot slot = slots[i];
                if (slot.IsObstacle)
                {
                    float objectStartWorld = patternStartWorld + localWorld;
                    float startAngle = bandPhaseDegree + WorldToDegrees(objectStartWorld, orbit.Radius);
                    float endAngle = bandPhaseDegree + WorldToDegrees(objectStartWorld + slot.Length, orbit.Radius);
                    Arc arc = new Arc(startAngle, endAngle, orbit.Radius);
                    ArcType arcType = slot.Type == PatternSlotType.Rift ? ArcType.TwoSide : side;
                    objects.Add(new PatternPlacedObject(
                        orbit,
                        new ArcObject(arc, CreateOrbitObject(slot, context, sampleRandomSpikeHeight), arcType)));
                }

                localWorld += slot.Length;
            }
        }

        private static IReadOnlyList<PatternSlotSnapshot> CreateUpperLaneSlots(
            IReadOnlyList<PatternSlotSnapshot> slots,
            float laneLength,
            float targetLength)
        {
            float extraLength = targetLength - laneLength;
            if (extraLength <= 0.0f || slots.Count == 0)
            {
                return slots;
            }

            int spaceCount = 0;
            float totalSpaceLength = 0.0f;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Type != PatternSlotType.Space)
                {
                    continue;
                }

                spaceCount++;
                totalSpaceLength += slots[i].Length;
            }

            if (spaceCount == 0)
            {
                return slots;
            }

            PatternSlotSnapshot[] adjustedSlots = new PatternSlotSnapshot[slots.Count];
            for (int i = 0; i < slots.Count; i++)
            {
                PatternSlotSnapshot slot = slots[i];
                if (slot.Type != PatternSlotType.Space)
                {
                    adjustedSlots[i] = slot;
                    continue;
                }

                float share = totalSpaceLength > 0.0f
                    ? slot.Length / totalSpaceLength
                    : 1.0f / spaceCount;
                adjustedSlots[i] = new PatternSlotSnapshot(
                    PatternSlotType.Space,
                    slot.Length + extraLength * share,
                    0.0f,
                    0.0f,
                    0.0f);
            }

            return adjustedSlots;
        }

        private static float SumLaneLength(IReadOnlyList<PatternSlotSnapshot> slots)
        {
            float length = 0.0f;
            for (int i = 0; i < slots.Count; i++)
            {
                length += slots[i].Length;
            }

            return length;
        }

        private static IOrbitObject CreateOrbitObject(
            PatternSlotSnapshot slot,
            LevelBuildContext context,
            bool sampleRandomSpikeHeight)
        {
            return slot.Type switch
            {
                PatternSlotType.Spike => new SpikeObstacle(slot.Height),
                PatternSlotType.RandomizedSpike => new SpikeObstacle(
                    sampleRandomSpikeHeight && slot.MaxHeight > slot.MinHeight
                        ? context.Random.Range(slot.MinHeight, slot.MaxHeight)
                        : slot.MinHeight + (slot.MaxHeight - slot.MinHeight) * 0.5f),
                PatternSlotType.MiniSpike => new SpikeObstacle(context.Settings.MiniSpike.Height),
                PatternSlotType.ImpulseTrap => new ImpulseTrap(context.Settings.ImpulseTrap.Height),
                PatternSlotType.GravityWell => new GravityWell(
                    context.Settings.GravityWell.PartialJumpStep,
                    context.Settings.GravityWell.PullSpeed,
                    context.Settings.GravityWell.ReturnDuration,
                    context.Settings.GravityWell.VisualAlpha),
                PatternSlotType.Rift => new Rift(context.Settings.Rift.RiftPadding),
                PatternSlotType.LaserGate => new LaserGate(
                    context.Settings.LaserGate.PassTime,
                    context.Settings.LaserGate.ChargingTime,
                    context.Settings.LaserGate.ActiveTime,
                    context.Settings.LaserGate.LowerBaseYOffset,
                    context.Settings.LaserGate.UpperBaseYOffset),
                _ => throw new ArgumentOutOfRangeException(nameof(slot), slot.Type, "Unsupported obstacle pattern slot type.")
            };
        }

        private void ApplyPlacement(
            PatternPlacement placement,
            LevelBuildContext context,
            HashSet<int> writableOrbitIndices)
        {
            for (int i = 0; i < placement.Objects.Count; i++)
            {
                PatternPlacedObject placedObject = placement.Objects[i];
                if (!writableOrbitIndices.Contains(placedObject.Orbit.Index))
                {
                    continue;
                }

                if (placedObject.Orbit.Index >= context.StartIndex)
                {
                    placedObject.Orbit.OrbitObjects.Add(placedObject.ArcObject);
                }
                else
                {
                    context.StageExistingOrbitObject(placedObject.Orbit, placedObject.ArcObject);
                }
            }

        }

        private static bool IsEligibleByContext(
            ObstaclePatternSnapshot pattern,
            PatternDifficultyStageSnapshot stage,
            int orbitIndex,
            out string reason)
        {
            if (!pattern.Enabled)
            {
                reason = "pattern disabled";
                return false;
            }

            if (pattern.Length <= 0.0f)
            {
                reason = "pattern has no length";
                return false;
            }

            if (pattern.BaseWeight <= 0.0f)
            {
                reason = "base weight is zero";
                return false;
            }

            if (!stage.IgnorePatternOrbitRange &&
                (orbitIndex < pattern.MinOrbitIndex || orbitIndex > pattern.MaxOrbitIndex))
            {
                reason = "orbit index outside pattern range";
                return false;
            }

            if (pattern.ContainsLockedSlot(stage.UnlockedSlots))
            {
                reason = "pattern contains locked slot";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static bool IsPlacementCompatibleWithExistingObjects(
            PatternPlacement placement,
            LevelBuildContext context,
            HashSet<int> writableOrbitIndices,
            out string reason,
            out float nextCursorWorld)
        {
            List<PatternPlacedObject> blockingObjects = CreateBlockingObjects(placement, context);
            if (HasInternalBlockingConflict(
                    blockingObjects,
                    writableOrbitIndices,
                    context.Settings.Rift))
            {
                reason = "placement has internal obstacle overlap";
                nextCursorWorld = float.PositiveInfinity;
                return false;
            }

            float nearestNextCursorWorld = float.PositiveInfinity;
            for (int i = 0; i < blockingObjects.Count; i++)
            {
                PatternPlacedObject placedObject = blockingObjects[i];
                if (!writableOrbitIndices.Contains(placedObject.Orbit.Index))
                {
                    continue;
                }

                List<ArcObject> existingObjects = GetExistingBlockingObjects(
                    placedObject.Orbit,
                    context);

                if (IsBlockedBy(
                        placedObject.Orbit,
                        placedObject.ArcObject,
                        existingObjects,
                        context.Settings.Rift,
                        out float shiftDegrees))
                {
                    float shiftedCursorWorld = placement.StartWorld +
                                               DegreesToWorld(shiftDegrees, placement.LowerOrbit.Radius);
                    nearestNextCursorWorld = Math.Min(nearestNextCursorWorld, shiftedCursorWorld);
                }
            }

            if (nearestNextCursorWorld < float.PositiveInfinity)
            {
                reason = "placement overlaps existing orbit object";
                nextCursorWorld = nearestNextCursorWorld;
                return false;
            }

            reason = string.Empty;
            nextCursorWorld = float.PositiveInfinity;
            return true;
        }

        private static List<PatternPlacedObject> CreateBlockingObjects(
            PatternPlacement placement,
            LevelBuildContext context)
        {
            List<PatternPlacedObject> blockingObjects = new List<PatternPlacedObject>(placement.Objects.Count);
            for (int i = 0; i < placement.Objects.Count; i++)
            {
                PatternPlacedObject placedObject = placement.Objects[i];
                blockingObjects.Add(placedObject);

                if (TryCreateLaserGateTargetOccupancy(
                        placedObject.Orbit,
                        placedObject.ArcObject,
                        context,
                        out PatternPlacedObject targetOccupancy))
                {
                    blockingObjects.Add(targetOccupancy);
                }
            }

            return blockingObjects;
        }

        private static bool HasInternalBlockingConflict(
            IReadOnlyList<PatternPlacedObject> objects,
            HashSet<int> writableOrbitIndices,
            RiftGenerationSettings riftSettings)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                PatternPlacedObject candidate = objects[i];
                if (!writableOrbitIndices.Contains(candidate.Orbit.Index))
                {
                    continue;
                }

                for (int j = 0; j < i; j++)
                {
                    PatternPlacedObject existing = objects[j];
                    if (!ReferenceEquals(candidate.Orbit, existing.Orbit) ||
                        !BlocksPlacement(candidate.ArcObject, existing.ArcObject, riftSettings))
                    {
                        continue;
                    }

                    return true;
                }
            }

            return false;
        }

        private static List<ArcObject> GetExistingBlockingObjects(
            Orbit orbit,
            LevelBuildContext context)
        {
            List<ArcObject> objects = new List<ArcObject>();
            objects.AddRange(orbit.OrbitObjects);
            objects.AddRange(context.GetStagedExistingOrbitObjects(orbit));
            AddAdjacentLaserGateOccupancies(orbit, context, objects);
            return objects;
        }

        private static void AddAdjacentLaserGateOccupancies(
            Orbit targetOrbit,
            LevelBuildContext context,
            List<ArcObject> objects)
        {
            TryAddFromSource(targetOrbit.Index - 1, ArcType.Outer, ArcType.Inner);
            TryAddFromSource(targetOrbit.Index + 1, ArcType.Inner, ArcType.Outer);

            void TryAddFromSource(int sourceIndex, ArcType sourceSide, ArcType targetSide)
            {
                Orbit sourceOrbit = FindOrbit(context, sourceIndex);
                if (sourceOrbit == null)
                {
                    return;
                }

                AddSourceObjects(sourceOrbit.OrbitObjects);
                AddSourceObjects(context.GetStagedExistingOrbitObjects(sourceOrbit));

                void AddSourceObjects(IReadOnlyList<ArcObject> sourceObjects)
                {
                    for (int i = 0; i < sourceObjects.Count; i++)
                    {
                        ArcObject sourceObject = sourceObjects[i];
                        if (sourceObject.Object.Type != ArcObjectType.LaserGate ||
                            sourceObject.Type != sourceSide)
                        {
                            continue;
                        }

                        objects.Add(CreateLaserGateTargetArcObject(targetOrbit, sourceObject, targetSide));
                    }
                }
            }
        }

        private static bool TryCreateLaserGateTargetOccupancy(
            Orbit sourceOrbit,
            ArcObject sourceObject,
            LevelBuildContext context,
            out PatternPlacedObject targetOccupancy)
        {
            targetOccupancy = default;
            if (sourceObject.Object.Type != ArcObjectType.LaserGate)
            {
                return false;
            }

            int targetIndex;
            ArcType targetSide;
            switch (sourceObject.Type)
            {
                case ArcType.Outer:
                    targetIndex = sourceOrbit.Index + 1;
                    targetSide = ArcType.Inner;
                    break;
                case ArcType.Inner:
                    targetIndex = sourceOrbit.Index - 1;
                    targetSide = ArcType.Outer;
                    break;
                default:
                    return false;
            }

            Orbit targetOrbit = FindOrbit(context, targetIndex);
            if (targetOrbit == null)
            {
                return false;
            }

            targetOccupancy = new PatternPlacedObject(
                targetOrbit,
                CreateLaserGateTargetArcObject(targetOrbit, sourceObject, targetSide));
            return true;
        }

        private static ArcObject CreateLaserGateTargetArcObject(
            Orbit targetOrbit,
            ArcObject sourceObject,
            ArcType targetSide)
        {
            Arc targetArc = new Arc(
                sourceObject.Arc.StartAngleDegree,
                sourceObject.Arc.EndAngleDegree,
                targetOrbit.Radius);
            return new ArcObject(targetArc, sourceObject.Object, targetSide);
        }

        private static bool IsBlockedBy(
            Orbit orbit,
            ArcObject candidate,
            IReadOnlyList<ArcObject> existingObjects,
            RiftGenerationSettings riftSettings,
            out float requiredShiftDegrees)
        {
            float nearestShiftDegrees = float.PositiveInfinity;
            for (int i = 0; i < existingObjects.Count; i++)
            {
                ArcObject existing = existingObjects[i];
                bool candidateIsRift = candidate.Object.Type == ArcObjectType.Rift;
                bool existingIsRift = existing.Object.Type == ArcObjectType.Rift;

                if (candidateIsRift && existingIsRift)
                {
                    float requiredCenterDistance = GetRequiredRiftCenterDistance(
                        orbit,
                        candidate.Arc,
                        existing.Arc,
                        riftSettings);
                    if (GetCenterDistance(candidate.Arc, existing.Arc) >= requiredCenterDistance)
                    {
                        continue;
                    }

                    float centerShiftDegrees = GetForwardCenterClearanceDegrees(
                        candidate.Arc,
                        existing.Arc,
                        requiredCenterDistance);
                    nearestShiftDegrees = Math.Min(nearestShiftDegrees, centerShiftDegrees);
                    continue;
                }

                if (!BlocksPlacement(candidate, existing, riftSettings))
                {
                    continue;
                }

                Arc candidateBlockingArc = GetObstacleBlockingArc(candidate, riftSettings);
                Arc existingBlockingArc = GetObstacleBlockingArc(existing, riftSettings);
                float shiftDegrees = GetForwardClearanceDegrees(candidateBlockingArc, existingBlockingArc);
                nearestShiftDegrees = Math.Min(nearestShiftDegrees, shiftDegrees);
            }

            requiredShiftDegrees = nearestShiftDegrees;
            return nearestShiftDegrees < float.PositiveInfinity;
        }

        private static bool BlocksPlacement(
            ArcObject candidate,
            ArcObject existing,
            RiftGenerationSettings riftSettings)
        {
            Arc candidateArc = GetObstacleBlockingArc(candidate, riftSettings);
            Arc existingArc = GetObstacleBlockingArc(existing, riftSettings);
            if (candidate.Type == ArcType.TwoSide || existing.Type == ArcType.TwoSide)
            {
                return HasPositiveOverlap(candidateArc, existingArc);
            }

            return candidate.Type == existing.Type &&
                   HasPositiveOverlap(candidateArc, existingArc);
        }

        private static Arc GetObstacleBlockingArc(
            ArcObject arcObject,
            RiftGenerationSettings riftSettings)
        {
            if (arcObject.Object.Type != ArcObjectType.Rift ||
                riftSettings.ObstacleClearance <= 0.0f)
            {
                return arcObject.Arc;
            }

            float radius = Math.Max(0.01f, arcObject.Arc.Radius);
            float clearanceDegrees = WorldToDegrees(riftSettings.ObstacleClearance, radius);
            return new Arc(
                arcObject.Arc.StartAngleDegree - clearanceDegrees,
                arcObject.Arc.EndAngleDegree + clearanceDegrees,
                arcObject.Arc.Radius);
        }

        private static bool HasPositiveOverlap(Arc first, Arc second)
        {
            (float firstStart, float firstEnd) = OrbitMath.NormalizeInterval(
                first.StartAngleDegree,
                first.EndAngleDegree);
            (float secondStart, float secondEnd) = OrbitMath.NormalizeInterval(
                second.StartAngleDegree,
                second.EndAngleDegree);

            for (int turn = -1; turn <= 1; turn++)
            {
                float shiftedSecondStart = secondStart + turn * 360.0f;
                float shiftedSecondEnd = secondEnd + turn * 360.0f;
                float overlapStart = Math.Max(firstStart, shiftedSecondStart);
                float overlapEnd = Math.Min(firstEnd, shiftedSecondEnd);
                if (overlapEnd - overlapStart > ArcOverlapToleranceDegrees)
                {
                    return true;
                }
            }

            return false;
        }

        private static float GetRequiredRiftCenterDistance(
            Orbit orbit,
            Arc candidate,
            Arc existing,
            RiftGenerationSettings riftSettings)
        {
            float nonOverlapDistance = Math.Max(0.01f, (candidate.Length() + existing.Length()) * 0.5f);
            return Math.Max(
                nonOverlapDistance,
                riftSettings.GetEffectiveMinCenterDistanceBetweenRifts(orbit));
        }

        private static float GetCenterDistance(Arc first, Arc second)
        {
            float centerDistanceDegrees = CircularAngleDistance(first.MidAngleDegree(), second.MidAngleDegree());
            return DegreesToWorld(centerDistanceDegrees, first.Radius);
        }

        private static float GetForwardCenterClearanceDegrees(
            Arc candidate,
            Arc existing,
            float requiredCenterDistance)
        {
            float candidateStart = OrbitMath.NormalizeDegrees360(candidate.StartAngleDegree);
            float candidateCenter = candidateStart +
                                    OrbitMath.DeltaDegrees(candidate.StartAngleDegree, candidate.EndAngleDegree) * 0.5f;
            float existingStart = OrbitMath.NormalizeDegrees360(existing.StartAngleDegree);
            float existingCenter = existingStart +
                                   OrbitMath.DeltaDegrees(existing.StartAngleDegree, existing.EndAngleDegree) * 0.5f;
            float requiredCenterDegrees = WorldToDegrees(requiredCenterDistance, candidate.Radius);
            float nearestShift = float.PositiveInfinity;

            for (int turn = -1; turn <= 1; turn++)
            {
                float targetCenter = existingCenter + turn * 360.0f + requiredCenterDegrees;
                float shift = targetCenter - candidateCenter;
                if (shift > 0.0f)
                {
                    nearestShift = Math.Min(nearestShift, shift);
                }
            }

            return nearestShift;
        }

        private static float GetForwardClearanceDegrees(Arc candidate, Arc existing)
        {
            float candidateStart = OrbitMath.NormalizeDegrees360(candidate.StartAngleDegree);
            float candidateEnd = candidateStart + OrbitMath.DeltaDegrees(
                candidate.StartAngleDegree,
                candidate.EndAngleDegree);
            float existingStart = OrbitMath.NormalizeDegrees360(existing.StartAngleDegree);
            float existingLength = OrbitMath.DeltaDegrees(existing.StartAngleDegree, existing.EndAngleDegree);
            float nearestShift = float.PositiveInfinity;

            for (int turn = -1; turn <= 1; turn++)
            {
                float intervalStart = existingStart + turn * 360.0f;
                float intervalEnd = existingStart + turn * 360.0f + existingLength;
                if (candidateEnd < intervalStart || candidateStart > intervalEnd)
                {
                    continue;
                }

                float epsilonDegrees = WorldToDegrees(PatternSlot.MinimumSpaceLength, candidate.Radius);
                float shift = intervalEnd - candidateStart + epsilonDegrees;
                if (shift > 0.0f)
                {
                    nearestShift = Math.Min(nearestShift, shift);
                }
            }

            return nearestShift;
        }

        private static Dictionary<int, List<float>> CreateRiftMarkerAngles(
            LevelBuildContext context,
            HashSet<int> writableOrbitIndices,
            int seed)
        {
            Dictionary<int, List<float>> markers = new Dictionary<int, List<float>>();
            List<int> orderedOrbitIndices = new List<int>(writableOrbitIndices);
            orderedOrbitIndices.Sort();
            for (int i = 0; i < orderedOrbitIndices.Count; i++)
            {
                int orbitIndex = orderedOrbitIndices[i];
                Orbit orbit = FindOrbit(context, orbitIndex);
                if (orbit == null || !context.Settings.ShouldGenerateContent(orbit))
                {
                    continue;
                }

                IReadOnlyList<ArcObject> existingRifts = orbit.GetOrbitObjectsByType(ArcObjectType.Rift);
                int targetCount = context.Settings.Rift.GetTargetCount(orbit);
                int markerCount = Math.Max(0, targetCount - existingRifts.Count);
                List<float> angles = CreateRiftMarkerAngles(
                    context,
                    orbit,
                    existingRifts,
                    markerCount,
                    seed,
                    markers);
                markers[orbit.Index] = angles;
            }

            return markers;
        }

        private static List<float> CreateRiftMarkerAngles(
            LevelBuildContext context,
            Orbit orbit,
            IReadOnlyList<ArcObject> existingRifts,
            int targetCount,
            int seed,
            IReadOnlyDictionary<int, List<float>> plannedMarkerAngles)
        {
            if (targetCount <= 0)
            {
                return new List<float>();
            }

            List<float> existingAngles = GetExistingRiftAngles(existingRifts);
            LevelRandom random = new LevelRandom(CreateRiftMarkerSeed(seed, orbit.Index));
            int maxLayoutAttempts = Math.Max(context.Settings.Rift.MaxPlacementAttempts, targetCount);
            float minCenterDistance = context.Settings.Rift.GetEffectiveMinCenterDistanceBetweenRifts(orbit);
            List<List<float>> completeLayouts = new List<List<float>>(MaxRiftMarkerLayoutCandidates);
            List<double> layoutWeights = new List<double>(MaxRiftMarkerLayoutCandidates);
            List<float> fullestLayout = new List<float>();

            for (int attempt = 0;
                 attempt < maxLayoutAttempts && completeLayouts.Count < MaxRiftMarkerLayoutCandidates;
                 attempt++)
            {
                List<float> layout = CreateRandomRiftMarkerLayout(
                    orbit,
                    existingAngles,
                    targetCount,
                    minCenterDistance,
                    context.Settings.Rift.MaxPlacementAttempts,
                    random);

                if (layout.Count > fullestLayout.Count)
                {
                    fullestLayout = layout;
                }

                if (layout.Count != targetCount)
                {
                    continue;
                }

                completeLayouts.Add(layout);
                layoutWeights.Add(GetRiftMarkerLayoutWeight(
                    layout,
                    orbit,
                    context,
                    plannedMarkerAngles));
            }

            if (completeLayouts.Count > 0)
            {
                List<float> selectedLayout = SelectRiftMarkerLayout(
                    completeLayouts,
                    layoutWeights,
                    random);
                selectedLayout.Sort();
                return selectedLayout;
            }

            fullestLayout.Sort();
            if (fullestLayout.Count < targetCount)
            {
                context.AddWarning(
                    $"Created {fullestLayout.Count}/{targetCount} random rift markers for orbit {orbit.Index} " +
                    $"after {maxLayoutAttempts} layout attempts. Adjust rift count, length, spacing, or max attempts.");
            }

            return fullestLayout;
        }

        private static List<float> CreateRandomRiftMarkerLayout(
            Orbit orbit,
            IReadOnlyList<float> existingAngles,
            int targetCount,
            float minCenterDistance,
            int maxPlacementAttempts,
            LevelRandom random)
        {
            List<float> layout = new List<float>(targetCount);
            List<float> occupiedAngles = new List<float>(existingAngles);
            int attempts = 0;
            int attemptLimit = Math.Max(maxPlacementAttempts, targetCount);

            while (layout.Count < targetCount && attempts < attemptLimit)
            {
                attempts++;
                float angle = random.Range(0.0f, 360.0f);
                if (IsTooCloseToExistingAngle(angle, occupiedAngles, orbit.Radius, minCenterDistance))
                {
                    continue;
                }

                layout.Add(angle);
                occupiedAngles.Add(angle);
            }

            return layout;
        }

        private static double GetRiftMarkerLayoutWeight(
            IReadOnlyList<float> layout,
            Orbit orbit,
            LevelBuildContext context,
            IReadOnlyDictionary<int, List<float>> plannedMarkerAngles)
        {
            double weight = 1.0;
            for (int markerIndex = 0; markerIndex < layout.Count; markerIndex++)
            {
                float markerAngle = layout[markerIndex];
                for (int lookback = 1; lookback <= context.Settings.Rift.CrossOrbitLookback; lookback++)
                {
                    int previousOrbitIndex = orbit.Index - lookback;
                    float nearestDistance = GetNearestCrossOrbitRiftDistance(
                        markerAngle,
                        orbit.Radius,
                        previousOrbitIndex,
                        context,
                        plannedMarkerAngles);
                    if (nearestDistance < float.PositiveInfinity)
                    {
                        weight *= context.Settings.Rift.GetCrossOrbitWeightMultiplier(nearestDistance);
                    }
                }
            }

            return weight;
        }

        private static float GetNearestCrossOrbitRiftDistance(
            float candidateAngle,
            float candidateRadius,
            int previousOrbitIndex,
            LevelBuildContext context,
            IReadOnlyDictionary<int, List<float>> plannedMarkerAngles)
        {
            float nearestDistance = float.PositiveInfinity;
            if (plannedMarkerAngles.TryGetValue(previousOrbitIndex, out List<float> plannedAngles))
            {
                for (int i = 0; i < plannedAngles.Count; i++)
                {
                    UpdateNearestDistanceFromAngle(plannedAngles[i]);
                }
            }

            Orbit previousOrbit = FindOrbit(context, previousOrbitIndex);
            if (previousOrbit != null)
            {
                UpdateNearestDistanceFromObjects(previousOrbit.OrbitObjects);
                UpdateNearestDistanceFromObjects(context.GetStagedExistingOrbitObjects(previousOrbit));
            }

            return nearestDistance;

            void UpdateNearestDistanceFromObjects(IReadOnlyList<ArcObject> objects)
            {
                for (int i = 0; i < objects.Count; i++)
                {
                    if (objects[i].Object.Type == ArcObjectType.Rift)
                    {
                        UpdateNearestDistanceFromAngle(objects[i].Arc.MidAngleDegree());
                    }
                }
            }

            void UpdateNearestDistanceFromAngle(float angle)
            {
                float distance = DegreesToWorld(
                    CircularAngleDistance(candidateAngle, angle),
                    candidateRadius);
                nearestDistance = Math.Min(nearestDistance, distance);
            }
        }

        private static List<float> SelectRiftMarkerLayout(
            IReadOnlyList<List<float>> layouts,
            IReadOnlyList<double> weights,
            LevelRandom random)
        {
            double totalWeight = 0.0;
            for (int i = 0; i < weights.Count; i++)
            {
                totalWeight += Math.Max(0.0, weights[i]);
            }

            if (totalWeight <= 0.0)
            {
                return layouts[0];
            }

            double pick = random.Range(0.0f, 1.0f) * totalWeight;
            double cumulativeWeight = 0.0;
            for (int i = 0; i < layouts.Count; i++)
            {
                cumulativeWeight += Math.Max(0.0, weights[i]);
                if (pick <= cumulativeWeight)
                {
                    return layouts[i];
                }
            }

            return layouts[^1];
        }

        private static bool SatisfiesRiftMarkers(
            PatternPlacement placement,
            float bandPhaseDegree,
            Dictionary<int, List<float>> riftMarkerAngles,
            HashSet<int> writableOrbitIndices,
            out bool satisfiesAllDueMarkers,
            out string reason)
        {
            int lowerDue = CountDueRiftMarkers(placement.LowerOrbit, placement, bandPhaseDegree, riftMarkerAngles);
            int upperDue = CountDueRiftMarkers(placement.UpperOrbit, placement, bandPhaseDegree, riftMarkerAngles);
            int lowerPlaced = CountPlacedRifts(placement, placement.LowerOrbit);
            int upperPlaced = CountPlacedRifts(placement, placement.UpperOrbit);
            satisfiesAllDueMarkers = lowerDue == lowerPlaced && upperDue == upperPlaced;

            if ((lowerPlaced > 0 && !writableOrbitIndices.Contains(placement.LowerOrbit.Index)) ||
                (upperPlaced > 0 && !writableOrbitIndices.Contains(placement.UpperOrbit.Index)) ||
                lowerPlaced > lowerDue || upperPlaced > upperDue)
            {
                reason = "rift outside marker";
                return false;
            }

            if (lowerDue + upperDue > 0 && lowerPlaced + upperPlaced == 0)
            {
                reason = "rift marker due";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static void FulfillRiftMarkers(
            PatternPlacement placement,
            Dictionary<int, List<float>> riftMarkerAngles,
            HashSet<int> writableOrbitIndices,
            float bandPhaseDegree)
        {
            FulfillOrbit(placement.LowerOrbit);
            FulfillOrbit(placement.UpperOrbit);

            void FulfillOrbit(Orbit orbit)
            {
                if (!writableOrbitIndices.Contains(orbit.Index) ||
                    !riftMarkerAngles.TryGetValue(orbit.Index, out List<float> markers))
                {
                    return;
                }

                int placedRifts = CountPlacedRifts(placement, orbit);
                List<float> dueAngles = GetDueRiftMarkerAngles(orbit, placement, bandPhaseDegree, riftMarkerAngles);
                for (int i = 0; i < placedRifts && i < dueAngles.Count; i++)
                {
                    markers.Remove(dueAngles[i]);
                }
            }
        }

        private static float GetNextRiftMarkerCursorWorld(
            Orbit lowerOrbit,
            Orbit upperOrbit,
            float cursorWorld,
            float bandPhaseDegree,
            Dictionary<int, List<float>> riftMarkerAngles)
        {
            float next = float.PositiveInfinity;
            UpdateNext(lowerOrbit);
            UpdateNext(upperOrbit);
            return next;

            void UpdateNext(Orbit orbit)
            {
                if (!riftMarkerAngles.TryGetValue(orbit.Index, out List<float> markers))
                {
                    return;
                }

                for (int i = 0; i < markers.Count; i++)
                {
                    float markerWorld = GetRiftMarkerWorld(markers[i], bandPhaseDegree, lowerOrbit.Radius);
                    if (markerWorld > cursorWorld + PatternSlot.MinimumSpaceLength)
                    {
                        next = Math.Min(next, markerWorld);
                    }
                }
            }
        }

        private static bool HasPendingRiftMarkers(
            Orbit lowerOrbit,
            Orbit upperOrbit,
            Dictionary<int, List<float>> riftMarkerAngles)
        {
            return (riftMarkerAngles.TryGetValue(lowerOrbit.Index, out List<float> lower) && lower.Count > 0) ||
                   (riftMarkerAngles.TryGetValue(upperOrbit.Index, out List<float> upper) && upper.Count > 0);
        }

        private static int CountPlacedRifts(PatternPlacement placement, Orbit orbit)
        {
            int count = 0;
            for (int i = 0; i < placement.Objects.Count; i++)
            {
                PatternPlacedObject placedObject = placement.Objects[i];
                if (ReferenceEquals(placedObject.Orbit, orbit) &&
                    placedObject.ArcObject.Object.Type == ArcObjectType.Rift)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountDueRiftMarkers(
            Orbit orbit,
            PatternPlacement placement,
            float bandPhaseDegree,
            Dictionary<int, List<float>> riftMarkerAngles)
        {
            if (!riftMarkerAngles.TryGetValue(orbit.Index, out List<float> markers))
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < markers.Count; i++)
            {
                if (IsRiftMarkerDue(markers[i], placement, bandPhaseDegree))
                {
                    count++;
                }
            }

            return count;
        }

        private static List<float> GetDueRiftMarkerAngles(
            Orbit orbit,
            PatternPlacement placement,
            float bandPhaseDegree,
            Dictionary<int, List<float>> riftMarkerAngles)
        {
            List<float> dueAngles = new List<float>();
            if (!riftMarkerAngles.TryGetValue(orbit.Index, out List<float> markers))
            {
                return dueAngles;
            }

            for (int i = 0; i < markers.Count; i++)
            {
                if (IsRiftMarkerDue(markers[i], placement, bandPhaseDegree))
                {
                    dueAngles.Add(markers[i]);
                }
            }

            dueAngles.Sort((first, second) =>
                GetRiftMarkerWorld(first, bandPhaseDegree, placement.LowerOrbit.Radius).CompareTo(
                    GetRiftMarkerWorld(second, bandPhaseDegree, placement.LowerOrbit.Radius)));
            return dueAngles;
        }

        private static bool IsRiftMarkerDue(float angle, PatternPlacement placement, float bandPhaseDegree)
        {
            // Use the same unwrapped world cursor as both lanes. Crossed markers stay due
            // until a real rift consumes them, even after a clearance jump or at the seam.
            float markerWorld = GetRiftMarkerWorld(angle, bandPhaseDegree, placement.LowerOrbit.Radius);
            return markerWorld <= placement.StartWorld + placement.Pattern.Length + PatternSlot.MinimumSpaceLength;
        }

        private static float GetRiftMarkerWorld(float angle, float bandPhaseDegree, float lowerRadius)
        {
            return DegreesToWorld(OrbitMath.NormalizeDegrees360(angle - bandPhaseDegree), lowerRadius);
        }

        private static List<float> GetExistingRiftAngles(IReadOnlyList<ArcObject> existingRifts)
        {
            List<float> angles = new List<float>(existingRifts.Count);
            for (int i = 0; i < existingRifts.Count; i++)
            {
                angles.Add(OrbitMath.NormalizeDegrees360(existingRifts[i].Arc.MidAngleDegree()));
            }

            return angles;
        }

        private static bool IsTooCloseToExistingAngle(
            float candidateAngle,
            IReadOnlyList<float> existingAngles,
            float radius,
            float minDistance)
        {
            for (int i = 0; i < existingAngles.Count; i++)
            {
                float distance = DegreesToWorld(
                    CircularAngleDistance(candidateAngle, existingAngles[i]),
                    radius);
                if (distance < minDistance)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CreateRiftMarkerSeed(int seed, int orbitIndex)
        {
            unchecked
            {
                int hash = seed;
                hash = (hash * 397) ^ orbitIndex;
                hash = (hash * 397) ^ 0x52F74A3D;
                return hash;
            }
        }

        private static IReadOnlyList<PatternSlotSnapshot> GetLane(
            ObstaclePatternSnapshot pattern,
            bool flipped,
            bool lowerLane)
        {
            IReadOnlyList<PatternSlotSnapshot> source = lowerLane
                ? (flipped ? pattern.Orbit2Slots : pattern.Orbit1Slots)
                : (flipped ? pattern.Orbit1Slots : pattern.Orbit2Slots);

            return source;
        }

        private static float GetLaneLength(
            ObstaclePatternSnapshot pattern,
            bool flipped,
            bool lowerLane)
        {
            return lowerLane
                ? (flipped ? pattern.Orbit2Length : pattern.Orbit1Length)
                : (flipped ? pattern.Orbit1Length : pattern.Orbit2Length);
        }

        private static Orbit FindOrbit(LevelBuildContext context, int index)
        {
            for (int i = 0; i < context.Orbits.Count; i++)
            {
                if (context.Orbits[i].Index == index)
                {
                    return context.Orbits[i];
                }
            }

            return null;
        }

        private static HashSet<int> GetWritableOrbitIndices(
            IReadOnlyList<Orbit> generatedOrbits,
            int startIndex)
        {
            HashSet<int> indices = new HashSet<int>();
            for (int i = 0; i < generatedOrbits.Count; i++)
            {
                indices.Add(generatedOrbits[i].Index);
            }

            if (startIndex > 0)
            {
                indices.Add(startIndex - 1);
            }

            return indices;
        }

        private static void WarnForUnmetRiftMarkers(
            LevelBuildContext context,
            Dictionary<int, List<float>> riftMarkerAngles,
            HashSet<int> writableOrbitIndices)
        {
            foreach (int orbitIndex in writableOrbitIndices)
            {
                Orbit orbit = FindOrbit(context, orbitIndex);
                if (orbit == null || !context.Settings.ShouldGenerateContent(orbit))
                {
                    continue;
                }

                int target = context.Settings.Rift.GetTargetCount(orbit);
                int actual = orbit.GetOrbitObjectsByType(ArcObjectType.Rift).Count;
                IReadOnlyList<ArcObject> staged = context.GetStagedExistingOrbitObjects(orbit);
                for (int i = 0; i < staged.Count; i++)
                {
                    if (staged[i].Object.Type == ArcObjectType.Rift)
                    {
                        actual++;
                    }
                }

                int pending = riftMarkerAngles.TryGetValue(orbitIndex, out List<float> markers) ? markers.Count : 0;
                if (actual == target && pending == 0)
                {
                    continue;
                }

                context.AddWarning(
                    $"Rift quota mismatch for orbit {orbit.Index} at radius {orbit.Radius:F2}: " +
                    $"placed {actual}/{target}, pending markers {pending}. " +
                    "See marker allocation or band placement warnings for spacing, eligibility, or remaining-span constraints.");
            }
        }

        private static float WorldToDegrees(float worldDistance, float radius)
        {
            return worldDistance / Math.Max(0.01f, radius) * OrbitMath.RADIANS_TO_DEGREES;
        }

        private static float DegreesToWorld(float angleDegrees, float radius)
        {
            return angleDegrees * OrbitMath.DEGREES_TO_RADIANS * Math.Max(0.01f, radius);
        }

        private static float CircularAngleDistance(float first, float second)
        {
            float delta = Math.Abs(
                OrbitMath.NormalizeDegrees360(first) -
                OrbitMath.NormalizeDegrees360(second));
            return Math.Min(delta, 360.0f - delta);
        }

    }
}
