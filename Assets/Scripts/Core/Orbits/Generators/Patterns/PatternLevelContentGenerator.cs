using System;
using System.Collections.Generic;
using System.Linq;
using OrbitalEscape.Utils;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    public sealed class PatternLevelContentGenerator : ILevelContentGenerator
    {
        private const int MaxCursorStepsPerBand = 2048;

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

            RiftQuotaTracker riftQuotas = new RiftQuotaTracker(context.Orbits, context.Settings.Rift);
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

                GenerateBand(lowerOrbit, upperOrbit, context, request, riftQuotas, writableOrbitIndices);
            }

            WarnForUnmetRiftQuotas(generatedOrbits, context, riftQuotas);
        }

        private void GenerateBand(
            Orbit lowerOrbit,
            Orbit upperOrbit,
            LevelBuildContext context,
            LevelBuildRequest request,
            RiftQuotaTracker riftQuotas,
            HashSet<int> writableOrbitIndices)
        {
            PatternDifficultyStageSnapshot stage = context.Settings.Patterns.GetStage(request.Score);
            if (!context.Settings.ShouldGenerateContent(lowerOrbit) ||
                !context.Settings.ShouldGenerateContent(upperOrbit))
            {
                return;
            }

            float cursorWorld = 0.0f;
            List<ObstaclePatternSnapshot> history = new List<ObstaclePatternSnapshot>();

            for (int step = 0; step < MaxCursorStepsPerBand && cursorWorld < lowerOrbit.Circumference; step++)
            {
                PatternRejectionLog rejectionLog = new PatternRejectionLog();
                List<WeightedPatternCandidate> candidates = BuildCandidates(
                    lowerOrbit,
                    upperOrbit,
                    cursorWorld,
                    stage,
                    history,
                    context,
                    request,
                    rejectionLog,
                    riftQuotas,
                    writableOrbitIndices,
                    enforceDueRifts: true,
                    out bool rejectedByDueRift,
                    out List<RiftQuotaDemand> dueRiftDemands,
                    out float nextCursorWorld);

                if (candidates.Count == 0 && rejectedByDueRift)
                {
                    riftQuotas.WarnMissingDuePatterns(context, dueRiftDemands);
                    rejectionLog.Add("relaxed due rift quota");
                    candidates = BuildCandidates(
                        lowerOrbit,
                        upperOrbit,
                        cursorWorld,
                        stage,
                        history,
                        context,
                        request,
                        rejectionLog,
                        riftQuotas,
                        writableOrbitIndices,
                        enforceDueRifts: false,
                        out _,
                        out _,
                        out float relaxedNextCursorWorld);
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
                        context,
                        randomizeSpikeHeight: true);
                }

                ApplyPlacement(
                    placement,
                    context,
                    writableOrbitIndices);
                riftQuotas.Record(placement, writableOrbitIndices);
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
            PatternDifficultyStageSnapshot stage,
            IReadOnlyList<ObstaclePatternSnapshot> history,
            LevelBuildContext context,
            LevelBuildRequest request,
            PatternRejectionLog rejectionLog,
            RiftQuotaTracker riftQuotas,
            HashSet<int> writableOrbitIndices,
            bool enforceDueRifts,
            out bool rejectedByDueRift,
            out List<RiftQuotaDemand> dueRiftDemands,
            out float nextCursorWorld)
        {
            PatternGenerationSettings settings = context.Settings.Patterns;
            List<WeightedPatternCandidate> candidates = new List<WeightedPatternCandidate>();
            bool hasDueRiftRejection = false;
            List<RiftQuotaDemand> dueDemandLog = new List<RiftQuotaDemand>();
            float nearestNextCursorWorld = float.PositiveInfinity;

            for (int i = 0; i < settings.Patterns.Count && candidates.Count < settings.MaxSelectionAttempts; i++)
            {
                ObstaclePatternSnapshot pattern = settings.Patterns[i];
                if (!IsEligibleByContext(pattern, stage, request.Score, out string contextReason))
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
                    if (startWorld >= lowerOrbit.Circumference || pattern.Length > lowerOrbit.Circumference)
                    {
                        rejectionLog.Add("pattern exceeds orbit circumference");
                        return;
                    }

                    float weight = PatternWeightCalculator.GetEffectiveWeight(pattern, stage, history);
                    if (weight <= 0.0f)
                    {
                        rejectionLog.Add("weight reduced to zero");
                        return;
                    }

                    PatternPlacement placement = CreatePlacement(
                        pattern,
                        flipped,
                        lowerOrbit,
                        upperOrbit,
                        startWorld,
                        context,
                        randomizeSpikeHeight: false);

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

                    if (riftQuotas.WouldExceed(placement, writableOrbitIndices, out string quotaReason))
                    {
                        rejectionLog.Add(quotaReason);
                        return;
                    }

                    if (enforceDueRifts &&
                        !riftQuotas.SatisfiesDueRiftRequirement(
                            placement,
                            writableOrbitIndices,
                            out string dueReason,
                            dueDemandLog))
                    {
                        hasDueRiftRejection = true;
                        rejectionLog.Add(dueReason);
                        return;
                    }

                    candidates.Add(new WeightedPatternCandidate(placement, weight));
                }
            }

            rejectedByDueRift = hasDueRiftRejection;
            dueRiftDemands = dueDemandLog;
            nextCursorWorld = nearestNextCursorWorld;
            return candidates;
        }

        private PatternPlacement CreatePlacement(
            ObstaclePatternSnapshot pattern,
            bool flipped,
            Orbit lowerOrbit,
            Orbit upperOrbit,
            float startWorld,
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
                context,
                randomizeSpikeHeight,
                objects);

            float startAngle = WorldToDegrees(startWorld, lowerOrbit.Radius);
            float endAngle = WorldToDegrees(startWorld + Math.Max(pattern.Length, 0.01f), lowerOrbit.Radius);
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
                    float startAngle = WorldToDegrees(objectStartWorld, orbit.Radius);
                    float endAngle = WorldToDegrees(objectStartWorld + slot.Length, orbit.Radius);
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
                PatternSlotType.Spike => new SpikeObstacle(GetSpikeHeight(context, randomizeSpikeHeight)),
                PatternSlotType.ImpulseTrap => new ImpulseTrap(context.Settings.ImpulseTrap.Height),
                PatternSlotType.GravityWell => new GravityWell(
                    context.Settings.GravityWell.PartialJumpStep,
                    context.Settings.GravityWell.PullSpeed,
                    context.Settings.GravityWell.ReturnDuration,
                    context.Settings.GravityWell.VisualAlpha),
                PatternSlotType.Rift => new Rift(context.Settings.Rift.RiftPadding),
                _ => new SpikeObstacle(GetSpikeHeight(context, randomizeSpikeHeight))
            };
        }

        private static float GetSpikeHeight(LevelBuildContext context, bool randomizeSpikeHeight)
        {
            SpikeGenerationSettings settings = context.Settings.Spike;
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
            float score,
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

            if (score < pattern.MinScore || score > pattern.MaxScore)
            {
                reason = "score outside pattern range";
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
                    placedObject.ArcObject,
                    placedObject.Orbit.OrbitObjects,
                    context.Settings.Rift.MinDistanceBetweenRifts,
                    out float orbitShiftDegrees);
                bool blockedByStaged = IsBlockedBy(
                    placedObject.ArcObject,
                    context.GetStagedExistingOrbitObjects(placedObject.Orbit),
                    context.Settings.Rift.MinDistanceBetweenRifts,
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
            ArcObject candidate,
            IReadOnlyList<ArcObject> existingObjects,
            float minRiftDistance,
            out float requiredShiftDegrees)
        {
            float nearestShiftDegrees = float.PositiveInfinity;
            for (int i = 0; i < existingObjects.Count; i++)
            {
                ArcObject existing = existingObjects[i];
                if (!BlocksPlacement(candidate, existing, minRiftDistance))
                {
                    continue;
                }

                float clearanceDegrees = candidate.Object.Type == ArcObjectType.Rift &&
                                         existing.Object.Type == ArcObjectType.Rift
                    ? minRiftDistance
                    : 0.0f;
                float shiftDegrees = GetForwardClearanceDegrees(candidate.Arc, existing.Arc, clearanceDegrees);
                nearestShiftDegrees = Math.Min(nearestShiftDegrees, shiftDegrees);
            }

            requiredShiftDegrees = nearestShiftDegrees;
            return nearestShiftDegrees < float.PositiveInfinity;
        }

        private static bool BlocksPlacement(
            ArcObject candidate,
            ArcObject existing,
            float minRiftDistance)
        {
            bool candidateIsRift = candidate.Object.Type == ArcObjectType.Rift;
            bool existingIsRift = existing.Object.Type == ArcObjectType.Rift;

            if (candidateIsRift && existingIsRift)
            {
                return candidate.Arc.IsOverlapping(existing.Arc) ||
                       candidate.Arc.Distance(existing.Arc) < minRiftDistance;
            }

            if (candidate.Type == ArcType.TwoSide || existing.Type == ArcType.TwoSide)
            {
                return candidate.Arc.IsOverlapping(existing.Arc);
            }

            return candidate.Type == existing.Type &&
                   candidate.Arc.IsOverlapping(existing.Arc);
        }

        private static float GetForwardClearanceDegrees(Arc candidate, Arc existing, float clearanceDegrees)
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
                float intervalStart = existingStart + turn * 360.0f - clearanceDegrees;
                float intervalEnd = existingStart + turn * 360.0f + existingLength + clearanceDegrees;
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

        private static void WarnForUnmetRiftQuotas(
            IReadOnlyList<Orbit> generatedOrbits,
            LevelBuildContext context,
            RiftQuotaTracker riftQuotas)
        {
            for (int i = 0; i < generatedOrbits.Count; i++)
            {
                Orbit orbit = generatedOrbits[i];
                if (!context.Settings.ShouldGenerateContent(orbit))
                {
                    continue;
                }

                int target = riftQuotas.GetTarget(orbit);
                int count = riftQuotas.GetCount(orbit);
                if (count == target)
                {
                    continue;
                }

                context.AddWarning(
                    $"Rift quota mismatch for orbit {orbit.Index}: generated {count}/{target} rifts " +
                    $"at radius {orbit.Radius:F2}. Add or adjust authored rift patterns to hit the exact quota.");
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

        private readonly struct RiftQuotaDemand
        {
            public Orbit Orbit { get; }
            public string Lane { get; }
            public int Target { get; }
            public int Count { get; }

            public RiftQuotaDemand(Orbit orbit, string lane, int target, int count)
            {
                Orbit = orbit;
                Lane = lane;
                Target = target;
                Count = count;
            }
        }

        private sealed class RiftQuotaTracker
        {
            private readonly RiftGenerationSettings _settings;
            private readonly Dictionary<int, Orbit> _orbits = new Dictionary<int, Orbit>();
            private readonly Dictionary<int, int> _targets = new Dictionary<int, int>();
            private readonly Dictionary<int, int> _counts = new Dictionary<int, int>();
            private readonly HashSet<string> _missingDueWarnings = new HashSet<string>();

            public RiftQuotaTracker(IReadOnlyList<Orbit> orbits, RiftGenerationSettings settings)
            {
                _settings = settings;
                for (int i = 0; i < orbits.Count; i++)
                {
                    Orbit orbit = orbits[i];
                    _orbits[orbit.Index] = orbit;
                    _targets[orbit.Index] = settings.GetTargetCount(orbit);
                    _counts[orbit.Index] = orbit.GetOrbitObjectsByType(ArcObjectType.Rift).Count;
                }
            }

            public int GetTarget(Orbit orbit)
            {
                EnsureOrbit(orbit);
                return _targets[orbit.Index];
            }

            public int GetCount(Orbit orbit)
            {
                EnsureOrbit(orbit);
                return _counts[orbit.Index];
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

            public bool SatisfiesDueRiftRequirement(
                PatternPlacement placement,
                HashSet<int> writableOrbitIndices,
                out string reason,
                List<RiftQuotaDemand> dueDemandLog)
            {
                List<RiftQuotaDemand> dueDemands = GetDueDemands(placement, writableOrbitIndices);
                if (dueDemands.Count == 0)
                {
                    reason = string.Empty;
                    return true;
                }

                int lowerRifts = CountPlacedRifts(placement, placement.LowerOrbit, writableOrbitIndices);
                int upperRifts = CountPlacedRifts(placement, placement.UpperOrbit, writableOrbitIndices);
                for (int i = 0; i < dueDemands.Count; i++)
                {
                    RiftQuotaDemand demand = dueDemands[i];
                    if ((ReferenceEquals(demand.Orbit, placement.LowerOrbit) && lowerRifts > 0) ||
                        (ReferenceEquals(demand.Orbit, placement.UpperOrbit) && upperRifts > 0))
                    {
                        reason = string.Empty;
                        return true;
                    }
                }

                dueDemandLog.AddRange(dueDemands);
                reason = "rift quota due";
                return false;
            }

            public void Record(PatternPlacement placement, HashSet<int> writableOrbitIndices)
            {
                AddCount(placement.LowerOrbit, CountPlacedRifts(placement, placement.LowerOrbit, writableOrbitIndices));
                AddCount(placement.UpperOrbit, CountPlacedRifts(placement, placement.UpperOrbit, writableOrbitIndices));
            }

            public void WarnMissingDuePatterns(
                LevelBuildContext context,
                IReadOnlyList<RiftQuotaDemand> demands)
            {
                for (int i = 0; i < demands.Count; i++)
                {
                    RiftQuotaDemand demand = demands[i];
                    string key = $"{demand.Orbit.Index}:{demand.Lane}";
                    if (!_missingDueWarnings.Add(key))
                    {
                        continue;
                    }

                    context.AddWarning(
                        $"No authored rift pattern could satisfy due rift quota for orbit {demand.Orbit.Index} " +
                        $"on the {demand.Lane} lane. Generated {demand.Count}/{demand.Target}; continuing without exceeding quota.");
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
                    reason = $"rift quota exceeded for orbit {orbit.Index}";
                    return true;
                }

                reason = string.Empty;
                return false;
            }

            private List<RiftQuotaDemand> GetDueDemands(
                PatternPlacement placement,
                HashSet<int> writableOrbitIndices)
            {
                float progressAfter = Math.Min(
                    1.0f,
                    (placement.StartWorld + Math.Max(placement.Pattern.Length, 0.01f)) /
                    Math.Max(0.01f, placement.LowerOrbit.Circumference));

                List<RiftQuotaDemand> demands = new List<RiftQuotaDemand>();
                if (writableOrbitIndices.Contains(placement.LowerOrbit.Index))
                {
                    AddDueDemand(placement.LowerOrbit, "lower", progressAfter, demands);
                }

                if (writableOrbitIndices.Contains(placement.UpperOrbit.Index))
                {
                    AddDueDemand(placement.UpperOrbit, "upper", progressAfter, demands);
                }

                return demands;
            }

            private void AddDueDemand(
                Orbit orbit,
                string lane,
                float progressAfter,
                List<RiftQuotaDemand> demands)
            {
                int target = GetTarget(orbit);
                int count = GetCount(orbit);
                int expectedCount = Math.Min(
                    target,
                    (int)Math.Floor(target * progressAfter + 0.0001f));

                if (count < expectedCount)
                {
                    demands.Add(new RiftQuotaDemand(orbit, lane, target, count));
                }
            }

            private void AddCount(Orbit orbit, int addedRifts)
            {
                if (addedRifts <= 0)
                {
                    return;
                }

                EnsureOrbit(orbit);
                _counts[orbit.Index] += addedRifts;
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
                if (_orbits.ContainsKey(orbit.Index))
                {
                    return;
                }

                _orbits[orbit.Index] = orbit;
                _targets[orbit.Index] = _settings.GetTargetCount(orbit);
                _counts[orbit.Index] = orbit.GetOrbitObjectsByType(ArcObjectType.Rift).Count;
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
