using System;
using System.Collections.Generic;
using System.Linq;
using OrbitalEscape.Utils;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    public sealed class PatternLevelContentGenerator : ILevelContentGenerator
    {
        private const int MaxCursorStepsPerBand = 2048;
        private const float OrbitBoundaryTolerance = 0.0001f;

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

            RiftReservationTracker riftReservations = new RiftReservationTracker(
                context,
                context.Orbits,
                context.Settings.Rift,
                request.Seed);
            HashSet<int> writableOrbitIndices = GetWritableOrbitIndices(generatedOrbits, context.StartIndex);
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

                GenerateBand(lowerOrbit, upperOrbit, context, riftReservations, writableOrbitIndices);
            }

            WarnForUnmetRiftReservations(generatedOrbits, context, riftReservations);
        }

        private void GenerateBand(
            Orbit lowerOrbit,
            Orbit upperOrbit,
            LevelBuildContext context,
            RiftReservationTracker riftReservations,
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
                    riftReservations,
                    writableOrbitIndices,
                    enforceReservations: true,
                    out bool rejectedByReservation,
                    out List<RiftReservationDemand> reservationDemands,
                    out bool reachedOrbitEnd,
                    out float nextCursorWorld);

                if (candidates.Count == 0 &&
                    rejectedByReservation &&
                    nextCursorWorld < float.PositiveInfinity &&
                    nextCursorWorld > cursorWorld)
                {
                    cursorWorld = Math.Max(
                        nextCursorWorld,
                        cursorWorld + PatternSlot.MinimumSpaceLength);
                    continue;
                }

                if (candidates.Count == 0 && rejectedByReservation)
                {
                    riftReservations.WarnMissingReservationPatterns(context, reservationDemands);
                    rejectionLog.Add("relaxed rift reservation");
                    candidates = BuildCandidates(
                        lowerOrbit,
                        upperOrbit,
                        cursorWorld,
                        bandPhaseDegree,
                        stage,
                        history,
                        context,
                        rejectionLog,
                        riftReservations,
                        writableOrbitIndices,
                        enforceReservations: false,
                        out _,
                        out _,
                        out bool relaxedReachedOrbitEnd,
                        out float relaxedNextCursorWorld);
                    reachedOrbitEnd |= relaxedReachedOrbitEnd;
                    nextCursorWorld = Math.Min(nextCursorWorld, relaxedNextCursorWorld);
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
                riftReservations.Record(placement, writableOrbitIndices);
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
            RiftReservationTracker riftReservations,
            HashSet<int> writableOrbitIndices,
            bool enforceReservations,
            out bool rejectedByReservation,
            out List<RiftReservationDemand> reservationDemands,
            out bool reachedOrbitEnd,
            out float nextCursorWorld)
        {
            PatternGenerationSettings settings = context.Settings.Patterns;
            List<WeightedPatternCandidate> candidates = new List<WeightedPatternCandidate>();
            bool hasReservationRejection = false;
            bool hasOrbitEndRejection = false;
            List<RiftReservationDemand> reservationDemandLog = new List<RiftReservationDemand>();
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
                                   riftReservations.GetWeightModifier(
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

                    if (riftReservations.WouldExceed(placement, writableOrbitIndices, out string reservationLimitReason))
                    {
                        rejectionLog.Add(reservationLimitReason);
                        return;
                    }

                    if (!riftReservations.SatisfiesReservations(
                            placement,
                            writableOrbitIndices,
                            enforceReservations,
                            out string reservationReason,
                            reservationDemandLog))
                    {
                        hasReservationRejection |= reservationReason == "rift reservation due";
                        if (reservationReason == "rift outside reservation" &&
                            riftReservations.TryGetNextReservationCursorWorld(
                                lowerOrbit,
                                upperOrbit,
                                cursorWorld,
                                bandPhaseDegree,
                                writableOrbitIndices,
                                out float reservationCursorWorld))
                        {
                            nearestNextCursorWorld = Math.Min(nearestNextCursorWorld, reservationCursorWorld);
                        }

                        rejectionLog.Add(reservationReason);
                        return;
                    }

                    candidates.Add(new WeightedPatternCandidate(placement, weight));
                }
            }

            rejectedByReservation = hasReservationRejection;
            reservationDemands = reservationDemandLog;
            reachedOrbitEnd = hasOrbitEndRejection;
            nextCursorWorld = nearestNextCursorWorld;
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
            float nearestNextCursorWorld = float.PositiveInfinity;
            for (int i = 0; i < placement.Objects.Count; i++)
            {
                PatternPlacedObject placedObject = placement.Objects[i];
                if (!writableOrbitIndices.Contains(placedObject.Orbit.Index))
                {
                    continue;
                }

                bool blockedByOrbit = IsBlockedBy(
                    placedObject.Orbit,
                    placedObject.ArcObject,
                    placedObject.Orbit.OrbitObjects,
                    context.Settings.Rift,
                    out float orbitShiftDegrees);
                bool blockedByStaged = IsBlockedBy(
                    placedObject.Orbit,
                    placedObject.ArcObject,
                    context.GetStagedExistingOrbitObjects(placedObject.Orbit),
                    context.Settings.Rift,
                    out float stagedShiftDegrees);

                if (blockedByOrbit || blockedByStaged)
                {
                    float shiftDegrees = Math.Min(orbitShiftDegrees, stagedShiftDegrees);
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
                return candidate.Arc.IsOverlapping(existing.Arc);
            }

            return candidate.Type == existing.Type &&
                   candidate.Arc.IsOverlapping(existing.Arc);
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

        private static void WarnForUnmetRiftReservations(
            IReadOnlyList<Orbit> generatedOrbits,
            LevelBuildContext context,
            RiftReservationTracker riftReservations)
        {
            for (int i = 0; i < generatedOrbits.Count; i++)
            {
                Orbit orbit = generatedOrbits[i];
                if (!context.Settings.ShouldGenerateContent(orbit))
                {
                    continue;
                }

                int target = riftReservations.GetTarget(orbit);
                int count = riftReservations.GetCount(orbit);
                if (count == target)
                {
                    continue;
                }

                context.AddWarning(
                    $"Rift reservation mismatch for orbit {orbit.Index}: generated {count}/{target} rifts " +
                    $"at radius {orbit.Radius:F2}. Add or adjust authored rift patterns to cover reserved rift positions.");
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

        private sealed class RiftReservationDemand
        {
            public Orbit Orbit { get; }
            public string Lane { get; }
            public int PlacedRifts { get; }
            public IReadOnlyList<RiftReservation> Reservations { get; }
            public bool HasMissingRifts => PlacedRifts < Reservations.Count;

            public RiftReservationDemand(
                Orbit orbit,
                string lane,
                int placedRifts,
                IReadOnlyList<RiftReservation> reservations)
            {
                Orbit = orbit;
                Lane = lane;
                PlacedRifts = placedRifts;
                Reservations = reservations;
            }
        }

        private sealed class RiftReservation
        {
            public float PositionWorld { get; }
            public bool Fulfilled { get; private set; }
            public bool Skipped { get; private set; }
            public bool Active => !Fulfilled && !Skipped;

            public RiftReservation(float positionWorld)
            {
                PositionWorld = positionWorld;
            }

            public void Fulfill()
            {
                Fulfilled = true;
            }

            public void Skip()
            {
                Skipped = true;
            }
        }

        private sealed class RiftReservationTracker
        {
            private readonly LevelBuildContext _context;
            private readonly RiftGenerationSettings _settings;
            private readonly Dictionary<int, Orbit> _orbits = new Dictionary<int, Orbit>();
            private readonly Dictionary<int, int> _initialCounts = new Dictionary<int, int>();
            private readonly Dictionary<int, int> _counts = new Dictionary<int, int>();
            private readonly Dictionary<int, List<RiftReservation>> _reservations =
                new Dictionary<int, List<RiftReservation>>();
            private readonly HashSet<string> _missingReservationWarnings = new HashSet<string>();
            private readonly int _seed;

            public RiftReservationTracker(
                LevelBuildContext context,
                IReadOnlyList<Orbit> orbits,
                RiftGenerationSettings settings,
                int seed)
            {
                _context = context;
                _settings = settings;
                _seed = seed;
                for (int i = 0; i < orbits.Count; i++)
                {
                    _orbits[orbits[i].Index] = orbits[i];
                }
            }

            public int GetTarget(Orbit orbit)
            {
                EnsureOrbit(orbit);
                return Math.Max(_settings.GetTargetCount(orbit), _initialCounts[orbit.Index]);
            }

            public int GetCount(Orbit orbit)
            {
                EnsureOrbit(orbit);
                return _counts[orbit.Index];
            }

            public float GetWeightModifier(
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
                    EnsureOrbit(placedObject.Orbit);
                    neighborMultiplier = Math.Min(
                        neighborMultiplier,
                        GetNeighborWeightMultiplier(placedObject, context));
                }

                return containsRift
                    ? neighborMultiplier
                    : 1.0f;
            }

            public bool WouldExceed(
                PatternPlacement placement,
                HashSet<int> writableOrbitIndices,
                out string reason)
            {
                if (WouldExceed(
                        placement.LowerOrbit,
                        CountPlacedRifts(placement, placement.LowerOrbit, writableOrbitIndices),
                        out reason) ||
                    WouldExceed(
                        placement.UpperOrbit,
                        CountPlacedRifts(placement, placement.UpperOrbit, writableOrbitIndices),
                        out reason))
                {
                    return true;
                }

                reason = string.Empty;
                return false;
            }

            public bool SatisfiesReservations(
                PatternPlacement placement,
                HashSet<int> writableOrbitIndices,
                bool requireCoveredReservations,
                out string reason,
                List<RiftReservationDemand> reservationDemandLog)
            {
                List<RiftReservationDemand> reservationDemands = GetReservationDemands(
                    placement,
                    writableOrbitIndices,
                    requireCoveredReservations);
                if (reservationDemands.Count == 0)
                {
                    reason = string.Empty;
                    return true;
                }

                bool hasExcessRifts = HasExcessRifts(reservationDemands);
                bool satisfiesAnyReservation = SatisfiesAnyReservation(reservationDemands);
                if (!hasExcessRifts &&
                    (!requireCoveredReservations || satisfiesAnyReservation))
                {
                    reason = string.Empty;
                    return true;
                }

                reservationDemandLog.AddRange(reservationDemands);
                reason = hasExcessRifts ? "rift outside reservation" : "rift reservation due";
                return false;
            }

            public void Record(PatternPlacement placement, HashSet<int> writableOrbitIndices)
            {
                RecordOrbit(placement, placement.LowerOrbit, writableOrbitIndices);
                RecordOrbit(placement, placement.UpperOrbit, writableOrbitIndices);
            }

            public void WarnMissingReservationPatterns(
                LevelBuildContext context,
                IReadOnlyList<RiftReservationDemand> demands)
            {
                for (int i = 0; i < demands.Count; i++)
                {
                    RiftReservationDemand demand = demands[i];
                    if (!demand.HasMissingRifts)
                    {
                        continue;
                    }

                    for (int j = 0; j < demand.Reservations.Count; j++)
                    {
                        RiftReservation reservation = demand.Reservations[j];
                        if (!reservation.Active)
                        {
                            continue;
                        }

                        string key = $"{demand.Orbit.Index}:{demand.Lane}:{reservation.PositionWorld:F3}";
                        if (_missingReservationWarnings.Add(key))
                        {
                            context.AddWarning(
                                $"No authored rift pattern could cover reserved rift position " +
                                $"{reservation.PositionWorld:F2}/{demand.Orbit.Circumference:F2} on orbit " +
                                $"{demand.Orbit.Index} ({demand.Lane} lane). Skipping that reservation.");
                        }

                        reservation.Skip();
                    }
                }
            }

            public bool TryGetNextReservationCursorWorld(
                Orbit lowerOrbit,
                Orbit upperOrbit,
                float cursorWorld,
                float bandPhaseDegree,
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
                    if (!writableOrbitIndices.Contains(orbit.Index))
                    {
                        return;
                    }

                    EnsureOrbit(orbit);
                    List<RiftReservation> reservations = _reservations[orbit.Index];
                    for (int i = 0; i < reservations.Count; i++)
                    {
                        RiftReservation reservation = reservations[i];
                        if (!reservation.Active)
                        {
                            continue;
                        }

                        float markerAngle = WorldToDegrees(
                            NormalizeWorldPosition(reservation.PositionWorld, orbit.Circumference),
                            orbit.Radius);
                        float cursorAtMarker = DegreesToWorld(
                            OrbitMath.NormalizeDegrees360(markerAngle - bandPhaseDegree),
                            lowerOrbit.Radius);
                        if (cursorAtMarker > cursorWorld + PatternSlot.MinimumSpaceLength)
                        {
                            next = Math.Min(next, cursorAtMarker);
                        }
                    }
                }
            }

            private bool WouldExceed(Orbit orbit, int addedRifts, out string reason)
            {
                if (addedRifts <= 0)
                {
                    reason = string.Empty;
                    return false;
                }

                int target = GetTarget(orbit);
                int count = GetCount(orbit);
                if (count + addedRifts > target)
                {
                    reason = $"rift reservation exceeded for orbit {orbit.Index}";
                    return true;
                }

                reason = string.Empty;
                return false;
            }

            private List<RiftReservationDemand> GetReservationDemands(
                PatternPlacement placement,
                HashSet<int> writableOrbitIndices,
                bool requireCoveredReservations)
            {
                List<RiftReservationDemand> demands = new List<RiftReservationDemand>();
                if (writableOrbitIndices.Contains(placement.LowerOrbit.Index))
                {
                    AddReservationDemand(placement, placement.LowerOrbit, "lower", requireCoveredReservations, demands);
                }

                if (writableOrbitIndices.Contains(placement.UpperOrbit.Index))
                {
                    AddReservationDemand(placement, placement.UpperOrbit, "upper", requireCoveredReservations, demands);
                }

                return demands;
            }

            private void AddReservationDemand(
                PatternPlacement placement,
                Orbit orbit,
                string lane,
                bool requireCoveredReservations,
                List<RiftReservationDemand> demands)
            {
                List<RiftReservation> reservations = GetActiveReservationsInSpan(orbit, placement);
                int placedRifts = CountPlacedRifts(placement, orbit, new HashSet<int> { orbit.Index });

                if (placedRifts > 0 ||
                    (requireCoveredReservations && reservations.Count > 0))
                {
                    demands.Add(new RiftReservationDemand(orbit, lane, placedRifts, reservations));
                }
            }

            private List<RiftReservation> GetActiveReservationsInSpan(Orbit orbit, PatternPlacement placement)
            {
                EnsureOrbit(orbit);
                List<RiftReservation> reservations = _reservations[orbit.Index];
                List<RiftReservation> result = new List<RiftReservation>();
                for (int i = 0; i < reservations.Count; i++)
                {
                    RiftReservation reservation = reservations[i];
                    if (reservation.Active &&
                        IsWorldPositionInPlacementSpan(reservation.PositionWorld, orbit, placement))
                    {
                        result.Add(reservation);
                    }
                }

                return result;
            }

            private float GetNeighborWeightMultiplier(
                PatternPlacedObject candidate,
                LevelBuildContext context)
            {
                if (!_orbits.TryGetValue(candidate.Orbit.Index - 1, out Orbit previousOrbit))
                {
                    return 1.0f;
                }

                float candidateAngle = candidate.ArcObject.Arc.MidAngleDegree();
                float nearestDistance = float.PositiveInfinity;
                UpdateNearestDistance(previousOrbit.OrbitObjects);
                UpdateNearestDistance(context.GetStagedExistingOrbitObjects(previousOrbit));

                return nearestDistance < float.PositiveInfinity
                    ? _settings.GetNeighborWeightMultiplier(nearestDistance)
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

            private void RecordOrbit(
                PatternPlacement placement,
                Orbit orbit,
                HashSet<int> writableOrbitIndices)
            {
                if (!writableOrbitIndices.Contains(orbit.Index))
                {
                    return;
                }

                EnsureOrbit(orbit);
                int addedRifts = CountPlacedRifts(placement, orbit, writableOrbitIndices);
                if (addedRifts > 0)
                {
                    _counts[orbit.Index] += addedRifts;
                    FulfillReservationsInSpan(orbit, placement, addedRifts);
                }
            }

            private static int CountPlacedRifts(
                PatternPlacement placement,
                Orbit orbit,
                HashSet<int> writableOrbitIndices)
            {
                if (!writableOrbitIndices.Contains(orbit.Index))
                {
                    return 0;
                }

                int count = 0;
                for (int i = 0; i < placement.Objects.Count; i++)
                {
                    PatternPlacedObject placedObject = placement.Objects[i];
                    if (!ReferenceEquals(placedObject.Orbit, orbit) ||
                        placedObject.ArcObject.Object.Type != ArcObjectType.Rift)
                    {
                        continue;
                    }

                    count++;
                }

                return count;
            }

            private void EnsureOrbit(Orbit orbit)
            {
                if (_reservations.ContainsKey(orbit.Index))
                {
                    return;
                }

                RegisterOrbit(orbit);
            }

            private void RegisterOrbit(Orbit orbit)
            {
                _orbits[orbit.Index] = orbit;
                IReadOnlyList<ArcObject> existingRifts = orbit.GetOrbitObjectsByType(ArcObjectType.Rift);
                int existingCount = existingRifts.Count;
                int configuredTarget = _settings.GetTargetCount(orbit);
                int reservationTarget = Math.Max(0, configuredTarget - existingCount);

                _initialCounts[orbit.Index] = existingCount;
                _counts[orbit.Index] = existingCount;
                _reservations[orbit.Index] = CreateReservations(orbit, existingRifts, reservationTarget);
            }

            private List<RiftReservation> CreateReservations(
                Orbit orbit,
                IReadOnlyList<ArcObject> existingRifts,
                int target)
            {
                List<RiftReservation> reservations = new List<RiftReservation>();
                if (target <= 0)
                {
                    return reservations;
                }

                List<float> occupiedPositions = GetExistingRiftPositions(orbit, existingRifts);
                LevelRandom random = new LevelRandom(CreateReservationSeed(orbit.Index));
                int attempts = 0;
                int maxAttempts = Math.Max(_settings.MaxPlacementAttempts, target);
                float minCenterDistance = _settings.GetEffectiveMinCenterDistanceBetweenRifts(orbit);

                while (reservations.Count < target && attempts < maxAttempts)
                {
                    attempts++;
                    float positionWorld = random.Range(0.0f, orbit.Circumference);
                    if (IsTooCloseToExistingPosition(
                            positionWorld,
                            occupiedPositions,
                            orbit.Circumference,
                            minCenterDistance))
                    {
                        continue;
                    }

                    reservations.Add(new RiftReservation(positionWorld));
                    occupiedPositions.Add(positionWorld);
                }

                reservations.Sort((first, second) => first.PositionWorld.CompareTo(second.PositionWorld));
                if (reservations.Count < target)
                {
                    _context.AddWarning(
                        $"Reserved {reservations.Count}/{target} random rift positions for orbit {orbit.Index} " +
                        $"after {attempts} attempts. Adjust rift count, length, spacing, or max attempts.");
                }

                return reservations;
            }

            private void FulfillReservationsInSpan(Orbit orbit, PatternPlacement placement, int maxCount)
            {
                List<RiftReservation> reservations = GetActiveReservationsInSpan(orbit, placement);
                for (int i = 0; i < reservations.Count && i < maxCount; i++)
                {
                    reservations[i].Fulfill();
                }
            }

            private int CreateReservationSeed(int orbitIndex)
            {
                unchecked
                {
                    int hash = _seed;
                    hash = (hash * 397) ^ orbitIndex;
                    hash = (hash * 397) ^ 0x52F74A3D;
                    return hash;
                }
            }

            private static List<float> GetExistingRiftPositions(Orbit orbit, IReadOnlyList<ArcObject> existingRifts)
            {
                List<float> positions = new List<float>(existingRifts.Count);
                for (int i = 0; i < existingRifts.Count; i++)
                {
                    positions.Add(NormalizeWorldPosition(
                        DegreesToWorld(existingRifts[i].Arc.MidAngleDegree(), orbit.Radius),
                        orbit.Circumference));
                }

                return positions;
            }

            private static bool IsTooCloseToExistingPosition(
                float candidate,
                IReadOnlyList<float> positions,
                float circumference,
                float minDistance)
            {
                for (int i = 0; i < positions.Count; i++)
                {
                    if (CircularWorldDistance(candidate, positions[i], circumference) < minDistance)
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool HasExcessRifts(IReadOnlyList<RiftReservationDemand> demands)
            {
                for (int i = 0; i < demands.Count; i++)
                {
                    if (demands[i].PlacedRifts > demands[i].Reservations.Count)
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool SatisfiesAnyReservation(IReadOnlyList<RiftReservationDemand> demands)
            {
                for (int i = 0; i < demands.Count; i++)
                {
                    if (demands[i].PlacedRifts > 0 &&
                        demands[i].Reservations.Count > 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool IsWorldPositionInPlacementSpan(
                float positionWorld,
                Orbit orbit,
                PatternPlacement placement)
            {
                float markerAngle = WorldToDegrees(NormalizeWorldPosition(positionWorld, orbit.Circumference), orbit.Radius);
                float start = OrbitMath.NormalizeDegrees360(placement.StartAngleDegree);
                float delta = OrbitMath.DeltaDegrees(placement.StartAngleDegree, placement.EndAngleDegree);
                float markerDelta = OrbitMath.NormalizeDegrees360(markerAngle - start);
                float tolerance = WorldToDegrees(PatternSlot.MinimumSpaceLength, orbit.Radius);
                return markerDelta <= delta + tolerance;
            }

            private static float CircularWorldDistance(float first, float second, float circumference)
            {
                float delta = Math.Abs(NormalizeWorldPosition(first, circumference) - NormalizeWorldPosition(second, circumference));
                return Math.Min(delta, Math.Max(0.0f, circumference - delta));
            }

            private static float NormalizeWorldPosition(float position, float circumference)
            {
                if (circumference <= 0.0f)
                {
                    return 0.0f;
                }

                float normalized = position % circumference;
                return normalized < 0.0f ? normalized + circumference : normalized;
            }
        }

        private sealed class PatternRejectionLog
        {
            private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();
            private readonly List<string> _orderedReasons = new List<string>();

            public string Summary
            {
                get
                {
                    if (_orderedReasons.Count == 0)
                    {
                        return "none";
                    }

                    return string.Join("; ", _orderedReasons.Select(FormatReason));
                }
            }

            public void Add(string reason)
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return;
                }

                if (!_counts.ContainsKey(reason))
                {
                    _counts.Add(reason, 0);
                    _orderedReasons.Add(reason);
                }

                _counts[reason]++;
            }

            private string FormatReason(string reason)
            {
                return $"{reason} x{_counts[reason]}";
            }
        }
    }
}
