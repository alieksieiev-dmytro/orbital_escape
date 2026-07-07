using System;
using System.Collections.Generic;
using OrbitalEscape.Utils;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    public sealed class PatternLevelContentGenerator : ILevelContentGenerator
    {
        private const int MaxCursorStepsPerBand = 2048;
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
            HashSet<string> skippedMarkerWarnings = new HashSet<string>();
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
                    skippedMarkerWarnings,
                    writableOrbitIndices);
            }

            WarnForUnmetRiftMarkers(context, riftMarkerAngles);
        }

        private void GenerateBand(
            Orbit lowerOrbit,
            Orbit upperOrbit,
            LevelBuildContext context,
            Dictionary<int, List<float>> riftMarkerAngles,
            HashSet<string> skippedMarkerWarnings,
            HashSet<int> writableOrbitIndices)
        {
            PatternDifficultyStageSnapshot stage = context.Settings.Patterns.GetStage(lowerOrbit.Index);
            if (!context.Settings.ShouldGenerateContent(lowerOrbit) ||
                !context.Settings.ShouldGenerateContent(upperOrbit))
            {
                return;
            }

            float cursorWorld = 0.0f;
            List<ObstaclePatternSnapshot> history = new List<ObstaclePatternSnapshot>();
            float bandPhaseDegree = context.Settings.RandomizeBandPhase
                ? context.Random.Range(0.0f, 360.0f)
                : 0.0f;

            for (int step = 0; step < MaxCursorStepsPerBand && cursorWorld < lowerOrbit.Circumference; step++)
            {
                PatternRejectionLog rejectionLog = new PatternRejectionLog();
                List<WeightedPatternCandidate> candidates = BuildCandidates(
                    lowerOrbit,
                    upperOrbit,
                    cursorWorld,
                    bandPhaseDegree,
                    stage,
                    history,
                    context,
                    rejectionLog,
                    riftMarkerAngles,
                    writableOrbitIndices,
                    out bool rejectedByMarker,
                    out int rejectedMarkerOrbitIndex,
                    out float rejectedMarkerAngle,
                    out bool reachedOrbitEnd,
                    out float nextCursorWorld);

                if (candidates.Count == 0 &&
                    rejectedByMarker &&
                    nextCursorWorld < float.PositiveInfinity &&
                    nextCursorWorld > cursorWorld)
                {
                    cursorWorld = Math.Max(
                        nextCursorWorld,
                        cursorWorld + PatternSlot.MinimumSpaceLength);
                    continue;
                }

                int forcedMarkerOrbitIndex = -1;
                float forcedMarkerAngle = 0.0f;
                if (candidates.Count == 0 && rejectedByMarker && rejectedMarkerOrbitIndex >= 0)
                {
                    candidates = BuildForcedRiftCandidates(
                        lowerOrbit,
                        upperOrbit,
                        cursorWorld,
                        bandPhaseDegree,
                        stage,
                        history,
                        context,
                        rejectionLog,
                        writableOrbitIndices,
                        rejectedMarkerOrbitIndex);

                    if (candidates.Count > 0)
                    {
                        forcedMarkerOrbitIndex = rejectedMarkerOrbitIndex;
                        forcedMarkerAngle = rejectedMarkerAngle;
                    }
                    else
                    {
                        WarnAndSkipRiftMarker(
                            context,
                            riftMarkerAngles,
                            skippedMarkerWarnings,
                            rejectedMarkerOrbitIndex,
                            rejectedMarkerAngle,
                            rejectionLog.Summary);
                        continue;
                    }
                }

                PatternPlacement placement;
                if (!_selector.TrySelect(candidates, context.Random, out placement))
                {
                    if (nextCursorWorld < float.PositiveInfinity &&
                        nextCursorWorld > cursorWorld)
                    {
                        cursorWorld = Math.Max(
                            nextCursorWorld,
                            cursorWorld + PatternSlot.MinimumSpaceLength);
                        continue;
                    }

                    if (reachedOrbitEnd)
                    {
                        break;
                    }

                    context.AddWarning(
                        $"Pattern generation stopped for orbit band {lowerOrbit.Index}-{upperOrbit.Index}: no valid pattern at {cursorWorld:F2} world units. Rejections: {rejectionLog.Summary}.");
                    break;
                }
                else
                {
                    placement = CreatePlacement(
                        placement.Pattern,
                        placement.Flipped,
                        lowerOrbit,
                        upperOrbit,
                        placement.StartWorld,
                        bandPhaseDegree,
                        context,
                        randomizeSpikeHeight: true);
                }

                ApplyPlacement(
                    placement,
                    context,
                    writableOrbitIndices);
                FulfillRiftMarkers(placement, riftMarkerAngles, writableOrbitIndices);
                if (forcedMarkerOrbitIndex >= 0)
                {
                    FulfillSpecificRiftMarker(
                        placement,
                        riftMarkerAngles,
                        forcedMarkerOrbitIndex,
                        forcedMarkerAngle);
                }

                cursorWorld = placement.StartWorld + Math.Max(placement.Pattern.Length, 0.01f);

                if (placement.Pattern != null)
                {
                    history.Add(placement.Pattern);
                    if (history.Count > 8)
                    {
                        history.RemoveAt(0);
                    }
                }
            }
        }

        private List<WeightedPatternCandidate> BuildCandidates(
            Orbit lowerOrbit,
            Orbit upperOrbit,
            float cursorWorld,
            float bandPhaseDegree,
            PatternDifficultyStageSnapshot stage,
            IReadOnlyList<ObstaclePatternSnapshot> history,
            LevelBuildContext context,
            PatternRejectionLog rejectionLog,
            Dictionary<int, List<float>> riftMarkerAngles,
            HashSet<int> writableOrbitIndices,
            out bool rejectedByMarker,
            out int rejectedMarkerOrbitIndex,
            out float rejectedMarkerAngle,
            out bool reachedOrbitEnd,
            out float nextCursorWorld)
        {
            PatternGenerationSettings settings = context.Settings.Patterns;
            List<WeightedPatternCandidate> candidates = new List<WeightedPatternCandidate>();
            bool hasMarkerRejection = false;
            bool hasOrbitEndRejection = false;
            int firstRejectedMarkerOrbitIndex = -1;
            float firstRejectedMarkerAngle = 0.0f;
            float nearestNextCursorWorld = float.PositiveInfinity;

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
                    float startWorld = cursorWorld;
                    if (startWorld >= lowerOrbit.Circumference ||
                        startWorld + pattern.Length > lowerOrbit.Circumference + OrbitBoundaryTolerance)
                    {
                        hasOrbitEndRejection = true;
                        rejectionLog.Add("pattern exceeds remaining orbit span");
                        return;
                    }

                    PatternPlacement placement = CreatePlacement(
                        pattern,
                        flipped,
                        lowerOrbit,
                        upperOrbit,
                        startWorld,
                        bandPhaseDegree,
                        context,
                        randomizeSpikeHeight: false);

                    float weight = PatternWeightCalculator.GetEffectiveWeight(pattern, stage, history) *
                                   GetNeighborRiftWeightModifier(
                                       placement,
                                       context,
                                       writableOrbitIndices);
                    if (weight <= 0.0f)
                    {
                        rejectionLog.Add("weight reduced to zero");
                        return;
                    }

                    if (!IsPlacementCompatibleWithExistingObjects(
                            placement,
                            context,
                            writableOrbitIndices,
                            out string placementReason,
                            out float placementNextCursorWorld))
                    {
                        rejectionLog.Add(placementReason);
                        nearestNextCursorWorld = Math.Min(nearestNextCursorWorld, placementNextCursorWorld);
                        return;
                    }

                    if (!SatisfiesRiftMarkers(
                            placement,
                            lowerOrbit,
                            upperOrbit,
                            cursorWorld,
                            bandPhaseDegree,
                            riftMarkerAngles,
                            writableOrbitIndices,
                            out string markerReason,
                            out int markerOrbitIndex,
                            out float markerAngle,
                            out float markerCursorWorld))
                    {
                        hasMarkerRejection |= markerReason == "rift marker due";
                        if (firstRejectedMarkerOrbitIndex < 0 && markerOrbitIndex >= 0)
                        {
                            firstRejectedMarkerOrbitIndex = markerOrbitIndex;
                            firstRejectedMarkerAngle = markerAngle;
                        }

                        if (markerCursorWorld < float.PositiveInfinity)
                        {
                            nearestNextCursorWorld = Math.Min(nearestNextCursorWorld, markerCursorWorld);
                        }

                        rejectionLog.Add(markerReason);
                        return;
                    }

                    candidates.Add(new WeightedPatternCandidate(placement, weight));
                }
            }

            rejectedByMarker = hasMarkerRejection;
            rejectedMarkerOrbitIndex = firstRejectedMarkerOrbitIndex;
            rejectedMarkerAngle = firstRejectedMarkerAngle;
            reachedOrbitEnd = hasOrbitEndRejection;
            nextCursorWorld = nearestNextCursorWorld;
            return candidates;
        }

        private List<WeightedPatternCandidate> BuildForcedRiftCandidates(
            Orbit lowerOrbit,
            Orbit upperOrbit,
            float cursorWorld,
            float bandPhaseDegree,
            PatternDifficultyStageSnapshot stage,
            IReadOnlyList<ObstaclePatternSnapshot> history,
            LevelBuildContext context,
            PatternRejectionLog rejectionLog,
            HashSet<int> writableOrbitIndices,
            int requiredOrbitIndex)
        {
            PatternGenerationSettings settings = context.Settings.Patterns;
            Orbit requiredOrbit = requiredOrbitIndex == lowerOrbit.Index
                ? lowerOrbit
                : requiredOrbitIndex == upperOrbit.Index
                    ? upperOrbit
                    : null;
            List<WeightedPatternCandidate> candidates = new List<WeightedPatternCandidate>();
            if (requiredOrbit == null)
            {
                rejectionLog.Add("rift marker orbit is outside current band");
                return candidates;
            }

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
                    if (cursorWorld >= lowerOrbit.Circumference ||
                        cursorWorld + pattern.Length > lowerOrbit.Circumference + OrbitBoundaryTolerance)
                    {
                        rejectionLog.Add("forced rift pattern exceeds remaining orbit span");
                        return;
                    }

                    PatternPlacement placement = CreatePlacement(
                        pattern,
                        flipped,
                        lowerOrbit,
                        upperOrbit,
                        cursorWorld,
                        bandPhaseDegree,
                        context,
                        randomizeSpikeHeight: false);

                    if (CountPlacedRifts(placement, requiredOrbit) <= 0)
                    {
                        rejectionLog.Add("forced rift pattern has no rift on marker orbit");
                        return;
                    }

                    float weight = PatternWeightCalculator.GetEffectiveWeight(pattern, stage, history) *
                                   GetNeighborRiftWeightModifier(
                                       placement,
                                       context,
                                       writableOrbitIndices);
                    if (weight <= 0.0f)
                    {
                        rejectionLog.Add("forced rift weight reduced to zero");
                        return;
                    }

                    if (!IsPlacementCompatibleWithExistingObjects(
                            placement,
                            context,
                            writableOrbitIndices,
                            out string placementReason,
                            out _))
                    {
                        rejectionLog.Add($"forced rift {placementReason}");
                        return;
                    }

                    candidates.Add(new WeightedPatternCandidate(placement, weight));
                }
            }

            return candidates;
        }

        private PatternPlacement CreatePlacement(
            ObstaclePatternSnapshot pattern,
            bool flipped,
            Orbit lowerOrbit,
            Orbit upperOrbit,
            float startWorld,
            float bandPhaseDegree,
            LevelBuildContext context,
            bool randomizeSpikeHeight)
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
                randomizeSpikeHeight,
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
                randomizeSpikeHeight,
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
            bool randomizeSpikeHeight,
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
                        new ArcObject(arc, CreateOrbitObject(slot.Type, context, randomizeSpikeHeight), arcType)));
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
                    slot.Length + extraLength * share);
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
            PatternSlotType type,
            LevelBuildContext context,
            bool randomizeSpikeHeight)
        {
            return type switch
            {
                PatternSlotType.Spike => new SpikeObstacle(GetSpikeHeight(
                    context,
                    context.Settings.Spike,
                    randomizeSpikeHeight)),
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
                    context.Settings.LaserGate.ActiveTime),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported obstacle pattern slot type.")
            };
        }

        private static float GetSpikeHeight(
            LevelBuildContext context,
            SpikeGenerationSettings settings,
            bool randomizeSpikeHeight)
        {
            if (!randomizeSpikeHeight)
            {
                return (settings.MinSpikeHeight + settings.MaxSpikeHeight) * 0.5f;
            }

            return context.Random.Range(settings.MinSpikeHeight, settings.MaxSpikeHeight);
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

            if (orbitIndex < pattern.MinOrbitIndex || orbitIndex > pattern.MaxOrbitIndex)
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
            if (HasInternalBlockingConflict(blockingObjects, writableOrbitIndices))
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
            HashSet<int> writableOrbitIndices)
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
                        !BlocksPlacement(candidate.ArcObject, existing.ArcObject))
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

                if (!BlocksPlacement(candidate, existing))
                {
                    continue;
                }

                float shiftDegrees = GetForwardClearanceDegrees(candidate.Arc, existing.Arc);
                nearestShiftDegrees = Math.Min(nearestShiftDegrees, shiftDegrees);
            }

            requiredShiftDegrees = nearestShiftDegrees;
            return nearestShiftDegrees < float.PositiveInfinity;
        }

        private static bool BlocksPlacement(
            ArcObject candidate,
            ArcObject existing)
        {
            if (candidate.Type == ArcType.TwoSide || existing.Type == ArcType.TwoSide)
            {
                return HasPositiveOverlap(candidate.Arc, existing.Arc);
            }

            return candidate.Type == existing.Type &&
                   HasPositiveOverlap(candidate.Arc, existing.Arc);
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
            foreach (int orbitIndex in writableOrbitIndices)
            {
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
                    seed);
                if (angles.Count > 0)
                {
                    markers[orbit.Index] = angles;
                }
            }

            return markers;
        }

        private static List<float> CreateRiftMarkerAngles(
            LevelBuildContext context,
            Orbit orbit,
            IReadOnlyList<ArcObject> existingRifts,
            int targetCount,
            int seed)
        {
            List<float> markers = new List<float>();
            if (targetCount <= 0)
            {
                return markers;
            }

            List<float> occupiedAngles = GetExistingRiftAngles(existingRifts);
            LevelRandom random = new LevelRandom(CreateRiftMarkerSeed(seed, orbit.Index));
            int attempts = 0;
            int maxAttempts = Math.Max(context.Settings.Rift.MaxPlacementAttempts, targetCount);
            float minCenterDistance = context.Settings.Rift.GetEffectiveMinCenterDistanceBetweenRifts(orbit);

            while (markers.Count < targetCount && attempts < maxAttempts)
            {
                attempts++;
                float angle = random.Range(0.0f, 360.0f);
                if (IsTooCloseToExistingAngle(angle, occupiedAngles, orbit.Radius, minCenterDistance))
                {
                    continue;
                }

                markers.Add(angle);
                occupiedAngles.Add(angle);
            }

            markers.Sort();
            if (markers.Count < targetCount)
            {
                context.AddWarning(
                    $"Created {markers.Count}/{targetCount} random rift markers for orbit {orbit.Index} " +
                    $"after {attempts} attempts. Adjust rift count, length, spacing, or max attempts.");
            }

            return markers;
        }

        private static bool SatisfiesRiftMarkers(
            PatternPlacement placement,
            Orbit lowerOrbit,
            Orbit upperOrbit,
            float cursorWorld,
            float bandPhaseDegree,
            Dictionary<int, List<float>> riftMarkerAngles,
            HashSet<int> writableOrbitIndices,
            out string reason,
            out int markerOrbitIndex,
            out float markerAngle,
            out float markerCursorWorld)
        {
            string resultReason = string.Empty;
            int resultMarkerOrbitIndex = -1;
            float resultMarkerAngle = 0.0f;
            float resultMarkerCursorWorld = float.PositiveInfinity;

            (int OrbitIndex, int CrossedMarkers, int PlacedRifts, float FirstMarkerAngle) lowerState =
                GetOrbitMarkerState(lowerOrbit);
            (int OrbitIndex, int CrossedMarkers, int PlacedRifts, float FirstMarkerAngle) upperState =
                GetOrbitMarkerState(upperOrbit);
            bool hasCrossedMarker = lowerState.CrossedMarkers > 0 || upperState.CrossedMarkers > 0;
            bool placesRift = lowerState.PlacedRifts > 0 || upperState.PlacedRifts > 0;
            bool satisfiesCrossedMarker = (lowerState.CrossedMarkers > 0 && lowerState.PlacedRifts > 0) ||
                                          (upperState.CrossedMarkers > 0 && upperState.PlacedRifts > 0);
            bool placesRiftOutsideMarker = (lowerState.PlacedRifts > lowerState.CrossedMarkers) ||
                                           (upperState.PlacedRifts > upperState.CrossedMarkers);
            bool result = true;

            if (placesRiftOutsideMarker)
            {
                resultReason = "rift outside marker";
                TryGetNextRiftMarkerCursorWorld(
                    lowerOrbit,
                    upperOrbit,
                    cursorWorld,
                    bandPhaseDegree,
                    riftMarkerAngles,
                    writableOrbitIndices,
                    out resultMarkerCursorWorld);
                result = false;
            }
            else if (hasCrossedMarker && !satisfiesCrossedMarker)
            {
                (int OrbitIndex, int CrossedMarkers, int PlacedRifts, float FirstMarkerAngle) firstDueState =
                    lowerState.CrossedMarkers > 0 ? lowerState : upperState;
                resultReason = "rift marker due";
                resultMarkerOrbitIndex = firstDueState.OrbitIndex;
                resultMarkerAngle = firstDueState.FirstMarkerAngle;
                result = false;
            }
            else if (!hasCrossedMarker && placesRift)
            {
                resultReason = "rift outside marker";
                TryGetNextRiftMarkerCursorWorld(
                    lowerOrbit,
                    upperOrbit,
                    cursorWorld,
                    bandPhaseDegree,
                    riftMarkerAngles,
                    writableOrbitIndices,
                    out resultMarkerCursorWorld);
                result = false;
            }

            reason = resultReason;
            markerOrbitIndex = resultMarkerOrbitIndex;
            markerAngle = resultMarkerAngle;
            markerCursorWorld = resultMarkerCursorWorld;
            return result;

            (int OrbitIndex, int CrossedMarkers, int PlacedRifts, float FirstMarkerAngle) GetOrbitMarkerState(Orbit orbit)
            {
                if (!writableOrbitIndices.Contains(orbit.Index))
                {
                    return (orbit.Index, 0, 0, 0.0f);
                }

                int placedRifts = CountPlacedRifts(placement, orbit);
                int crossedMarkers = CountCrossedRiftMarkers(
                    orbit,
                    placement,
                    riftMarkerAngles,
                    out float firstMarkerAngle);

                return (
                    orbit.Index,
                    crossedMarkers,
                    placedRifts,
                    firstMarkerAngle);
            }
        }

        private static float GetNeighborRiftWeightModifier(
            PatternPlacement placement,
            LevelBuildContext context,
            HashSet<int> writableOrbitIndices)
        {
            bool containsRift = false;
            float neighborMultiplier = 1.0f;
            for (int i = 0; i < placement.Objects.Count; i++)
            {
                PatternPlacedObject placedObject = placement.Objects[i];
                if (placedObject.ArcObject.Object.Type != ArcObjectType.Rift ||
                    !writableOrbitIndices.Contains(placedObject.Orbit.Index))
                {
                    continue;
                }

                containsRift = true;
                neighborMultiplier = Math.Min(
                    neighborMultiplier,
                    GetNeighborRiftWeightMultiplier(placedObject, context));
            }

            return containsRift ? neighborMultiplier : 1.0f;
        }

        private static float GetNeighborRiftWeightMultiplier(
            PatternPlacedObject candidate,
            LevelBuildContext context)
        {
            Orbit previousOrbit = FindOrbit(context, candidate.Orbit.Index - 1);
            if (previousOrbit == null)
            {
                return 1.0f;
            }

            float candidateAngle = candidate.ArcObject.Arc.MidAngleDegree();
            float nearestDistance = float.PositiveInfinity;
            UpdateNearestDistance(previousOrbit.OrbitObjects);
            UpdateNearestDistance(context.GetStagedExistingOrbitObjects(previousOrbit));

            return nearestDistance < float.PositiveInfinity
                ? context.Settings.Rift.GetNeighborWeightMultiplier(nearestDistance)
                : 1.0f;

            void UpdateNearestDistance(IReadOnlyList<ArcObject> objects)
            {
                for (int i = 0; i < objects.Count; i++)
                {
                    ArcObject existing = objects[i];
                    if (existing.Object.Type != ArcObjectType.Rift)
                    {
                        continue;
                    }

                    float distance = CircularAngleDistance(
                        candidateAngle,
                        existing.Arc.MidAngleDegree());
                    nearestDistance = Math.Min(
                        nearestDistance,
                        DegreesToWorld(distance, candidate.ArcObject.Arc.Radius));
                }
            }
        }

        private static void FulfillRiftMarkers(
            PatternPlacement placement,
            Dictionary<int, List<float>> riftMarkerAngles,
            HashSet<int> writableOrbitIndices)
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
                if (placedRifts <= 0)
                {
                    return;
                }

                List<float> crossedAngles = GetCrossedRiftMarkerAngles(orbit, placement, riftMarkerAngles);
                for (int i = 0; i < crossedAngles.Count && i < placedRifts; i++)
                {
                    markers.Remove(crossedAngles[i]);
                }
            }
        }

        private static void FulfillSpecificRiftMarker(
            PatternPlacement placement,
            Dictionary<int, List<float>> riftMarkerAngles,
            int orbitIndex,
            float angle)
        {
            if (!riftMarkerAngles.TryGetValue(orbitIndex, out List<float> markers))
            {
                return;
            }

            Orbit orbit = placement.LowerOrbit.Index == orbitIndex
                ? placement.LowerOrbit
                : placement.UpperOrbit.Index == orbitIndex
                    ? placement.UpperOrbit
                    : null;
            if (orbit == null || CountPlacedRifts(placement, orbit) <= 0)
            {
                return;
            }

            int markerIndex = markers.IndexOf(angle);
            if (markerIndex >= 0)
            {
                markers.RemoveAt(markerIndex);
            }
        }

        private static void WarnAndSkipRiftMarker(
            LevelBuildContext context,
            Dictionary<int, List<float>> riftMarkerAngles,
            HashSet<string> skippedMarkerWarnings,
            int orbitIndex,
            float angle,
            string rejectionSummary)
        {
            if (!riftMarkerAngles.TryGetValue(orbitIndex, out List<float> markers))
            {
                return;
            }

            int markerIndex = markers.IndexOf(angle);
            if (markerIndex < 0)
            {
                return;
            }

            Orbit orbit = FindOrbit(context, orbitIndex);
            string key = $"{orbitIndex}:{angle:F3}";
            if (skippedMarkerWarnings.Add(key))
            {
                string circumference = orbit != null ? orbit.Circumference.ToString("F2") : "unknown";
                context.AddWarning(
                    $"No authored rift pattern could cover rift marker {angle:F2} on orbit {orbitIndex} " +
                    $"(circumference {circumference}). Skipping that marker. Rejections: {rejectionSummary}.");
            }

            markers.RemoveAt(markerIndex);
        }

        private static bool TryGetNextRiftMarkerCursorWorld(
            Orbit lowerOrbit,
            Orbit upperOrbit,
            float cursorWorld,
            float bandPhaseDegree,
            Dictionary<int, List<float>> riftMarkerAngles,
            HashSet<int> writableOrbitIndices,
            out float nextCursorWorld)
        {
            float next = float.PositiveInfinity;
            TryUpdateNext(lowerOrbit);
            TryUpdateNext(upperOrbit);
            nextCursorWorld = next;
            return nextCursorWorld < float.PositiveInfinity;

            void TryUpdateNext(Orbit orbit)
            {
                if (!writableOrbitIndices.Contains(orbit.Index) ||
                    !riftMarkerAngles.TryGetValue(orbit.Index, out List<float> markers))
                {
                    return;
                }

                for (int i = 0; i < markers.Count; i++)
                {
                    float cursorAtMarker = DegreesToWorld(
                        OrbitMath.NormalizeDegrees360(markers[i] - bandPhaseDegree),
                        lowerOrbit.Radius);
                    if (cursorAtMarker > cursorWorld + PatternSlot.MinimumSpaceLength)
                    {
                        next = Math.Min(next, cursorAtMarker);
                    }
                }
            }
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

        private static int CountCrossedRiftMarkers(
            Orbit orbit,
            PatternPlacement placement,
            Dictionary<int, List<float>> riftMarkerAngles,
            out float firstAngle)
        {
            firstAngle = 0.0f;
            int count = 0;
            if (!riftMarkerAngles.TryGetValue(orbit.Index, out List<float> markers))
            {
                return count;
            }

            for (int i = 0; i < markers.Count; i++)
            {
                float angle = markers[i];
                if (!IsAngleInPlacementSpan(angle, orbit, placement))
                {
                    continue;
                }

                if (count == 0)
                {
                    firstAngle = angle;
                }

                count++;
            }

            return count;
        }

        private static List<float> GetCrossedRiftMarkerAngles(
            Orbit orbit,
            PatternPlacement placement,
            Dictionary<int, List<float>> riftMarkerAngles)
        {
            List<float> crossedAngles = new List<float>();
            if (!riftMarkerAngles.TryGetValue(orbit.Index, out List<float> markers))
            {
                return crossedAngles;
            }

            for (int i = 0; i < markers.Count; i++)
            {
                float angle = markers[i];
                if (IsAngleInPlacementSpan(angle, orbit, placement))
                {
                    crossedAngles.Add(angle);
                }
            }

            return crossedAngles;
        }

        private static bool IsAngleInPlacementSpan(
            float angle,
            Orbit orbit,
            PatternPlacement placement)
        {
            float start = OrbitMath.NormalizeDegrees360(placement.StartAngleDegree);
            float delta = OrbitMath.DeltaDegrees(placement.StartAngleDegree, placement.EndAngleDegree);
            float angleDelta = OrbitMath.NormalizeDegrees360(angle - start);
            float tolerance = WorldToDegrees(PatternSlot.MinimumSpaceLength, orbit.Radius);
            return angleDelta <= delta + tolerance;
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
            Dictionary<int, List<float>> riftMarkerAngles)
        {
            foreach (KeyValuePair<int, List<float>> marker in riftMarkerAngles)
            {
                if (marker.Value.Count == 0)
                {
                    continue;
                }

                Orbit orbit = FindOrbit(context, marker.Key);
                if (orbit == null || !context.Settings.ShouldGenerateContent(orbit))
                {
                    continue;
                }

                context.AddWarning(
                    $"Rift marker mismatch for orbit {orbit.Index}: {marker.Value.Count} rift marker(s) " +
                    $"were not fulfilled " +
                    $"at radius {orbit.Radius:F2}. Add or adjust authored rift patterns to cover marker positions.");
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
