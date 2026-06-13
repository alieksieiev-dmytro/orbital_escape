using System;
using System.Collections.Generic;
using System.Linq;
using OrbitalEscape.Utils;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    public sealed class PatternLevelContentGenerator : ILevelContentGenerator
    {
        private const int MaxCursorStepsPerBand = 2048;

        private readonly PatternPlacementValidator _validator;
        private readonly WeightedPatternSelector _selector;

        public PatternLevelContentGenerator(
            PatternPlacementValidator validator,
            WeightedPatternSelector selector)
        {
            _validator = validator;
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

            int firstBandIndex = Math.Max(0, context.StartIndex - 1);
            for (int lowerOrbitIndex = firstBandIndex; lowerOrbitIndex < context.LastPlannedOrbitIndex; lowerOrbitIndex++)
            {
                Orbit lowerOrbit = FindOrbit(context, lowerOrbitIndex);
                Orbit upperOrbit = FindOrbit(context, lowerOrbitIndex + 1);
                if (lowerOrbit == null || upperOrbit == null)
                {
                    continue;
                }

                GenerateBand(lowerOrbit, upperOrbit, context, request);
            }
        }

        private void GenerateBand(
            Orbit lowerOrbit,
            Orbit upperOrbit,
            LevelBuildContext context,
            LevelBuildRequest request)
        {
            PatternDifficultyStageSnapshot stage = context.Settings.Patterns.GetStage(request.Score);
            float remainingBudget = stage.BudgetPerOrbitBand * Math.Max(0.0f, stage.PatternDensity);
            if (remainingBudget <= 0.0f)
            {
                return;
            }

            bool lowerMutable = lowerOrbit.Index >= context.StartIndex &&
                                context.Settings.ShouldGenerateContent(lowerOrbit);
            bool upperMutable = upperOrbit.Index >= context.StartIndex &&
                                context.Settings.ShouldGenerateContent(upperOrbit);
            if (!lowerMutable && !upperMutable)
            {
                return;
            }

            float cursorWorld = 0.0f;
            float initialBudget = remainingBudget;
            List<ObstaclePatternSnapshot> history = new List<ObstaclePatternSnapshot>();

            for (int step = 0; step < MaxCursorStepsPerBand && cursorWorld < lowerOrbit.Circumference && remainingBudget > 0.0f; step++)
            {
                PatternRejectionLog rejectionLog = new PatternRejectionLog();
                List<WeightedPatternCandidate> candidates = BuildCandidates(
                    lowerOrbit,
                    upperOrbit,
                    lowerMutable,
                    upperMutable,
                    cursorWorld,
                    remainingBudget,
                    stage,
                    history,
                    context,
                    request,
                    rejectionLog);

                PatternPlacement placement;
                if (!_selector.TrySelect(candidates, context.Random, out placement))
                {
                    if (!TryCreateFallbackPlacement(
                            lowerOrbit,
                            upperOrbit,
                            lowerMutable,
                            upperMutable,
                            cursorWorld,
                            stage,
                            context,
                            out placement,
                            out string fallbackFailureReason))
                    {
                        context.AddWarning(
                            $"Pattern generation stopped for orbit band {lowerOrbit.Index}-{upperOrbit.Index}: no valid pattern or fallback at {cursorWorld:F2} world units. Rejections: {rejectionLog.Summary}. Fallback: {fallbackFailureReason}");
                        break;
                    }

                    if (context.Settings.Patterns.DebugGizmosEnabled)
                    {
                        context.AddWarning(
                            $"Pattern fallback used for orbit band {lowerOrbit.Index}-{upperOrbit.Index} at {placement.StartWorld:F2} world units. Rejections: {rejectionLog.Summary}");
                    }
                }
                else
                {
                    placement = CreatePlacement(
                        placement.Pattern,
                        placement.Flipped,
                        false,
                        lowerOrbit,
                        upperOrbit,
                        placement.StartWorld,
                        lowerMutable,
                        upperMutable,
                        context,
                        randomizeSpikeHeight: true);
                }

                float budgetSpent = Math.Max(0.0f, placement.Pattern.Cost);
                remainingBudget = Math.Max(0.0f, remainingBudget - budgetSpent);
                ApplyPlacement(
                    placement,
                    context,
                    request,
                    budgetSpent,
                    remainingBudget,
                    initialBudget,
                    rejectionLog.Summary);
                cursorWorld = placement.StartWorld + Math.Max(placement.Pattern.Length, 0.01f);

                if (!placement.IsFallback)
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
            bool lowerMutable,
            bool upperMutable,
            float cursorWorld,
            float remainingBudget,
            PatternDifficultyStageSnapshot stage,
            IReadOnlyList<ObstaclePatternSnapshot> history,
            LevelBuildContext context,
            LevelBuildRequest request,
            PatternRejectionLog rejectionLog)
        {
            PatternGenerationSettings settings = context.Settings.Patterns;
            List<WeightedPatternCandidate> candidates = new List<WeightedPatternCandidate>();

            for (int i = 0; i < settings.Patterns.Count && candidates.Count < settings.MaxSelectionAttempts; i++)
            {
                ObstaclePatternSnapshot pattern = settings.Patterns[i];
                if (!IsEligibleByContext(pattern, remainingBudget, stage, request.Score, out string contextReason))
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
                    if (!CanPlaceOnMutableOrbits(pattern, flipped, lowerMutable, upperMutable))
                    {
                        rejectionLog.Add("boundary lane is immutable");
                        return;
                    }

                    float startWorld = cursorWorld + stage.MinimumSpacing;
                    if (startWorld >= lowerOrbit.Circumference || pattern.Length > lowerOrbit.Circumference)
                    {
                        rejectionLog.Add("pattern exceeds orbit circumference");
                        return;
                    }

                    float weight = GetEffectiveWeight(pattern, stage, history);
                    if (weight <= 0.0f)
                    {
                        rejectionLog.Add("weight reduced to zero");
                        return;
                    }

                    PatternPlacement placement = CreatePlacement(
                        pattern,
                        flipped,
                        false,
                        lowerOrbit,
                        upperOrbit,
                        startWorld,
                        lowerMutable,
                        upperMutable,
                        context,
                        randomizeSpikeHeight: false);

                    if (_validator.IsValid(placement, context, out string validationReason))
                    {
                        candidates.Add(new WeightedPatternCandidate(placement, weight));
                    }
                    else
                    {
                        rejectionLog.Add(validationReason);
                    }
                }
            }

            return candidates;
        }

        private bool TryCreateFallbackPlacement(
            Orbit lowerOrbit,
            Orbit upperOrbit,
            bool lowerMutable,
            bool upperMutable,
            float cursorWorld,
            PatternDifficultyStageSnapshot stage,
            LevelBuildContext context,
            out PatternPlacement placement,
            out string failureReason)
        {
            ObstaclePatternSnapshot fallback = context.Settings.Patterns.FallbackPattern;
            if (fallback == null)
            {
                placement = null;
                failureReason = "fallback pattern is not configured";
                return false;
            }

            float startWorld = cursorWorld + stage.MinimumSpacing;
            if (startWorld >= lowerOrbit.Circumference)
            {
                placement = null;
                failureReason = "fallback exceeds orbit circumference";
                return false;
            }

            bool flipped = fallback.AllowFlip && context.Random.Range(0, 2) == 1;
            if (!CanPlaceOnMutableOrbits(fallback, flipped, lowerMutable, upperMutable))
            {
                flipped = false;
            }

            if (!CanPlaceOnMutableOrbits(fallback, flipped, lowerMutable, upperMutable))
            {
                placement = null;
                failureReason = "fallback lane is immutable";
                return false;
            }

            placement = CreatePlacement(
                fallback,
                flipped,
                true,
                lowerOrbit,
                upperOrbit,
                startWorld,
                lowerMutable,
                upperMutable,
                context,
                randomizeSpikeHeight: false);

            bool valid = _validator.IsValid(placement, context, out failureReason);
            if (valid)
            {
                failureReason = string.Empty;
            }

            return valid;
        }

        private PatternPlacement CreatePlacement(
            ObstaclePatternSnapshot pattern,
            bool flipped,
            bool isFallback,
            Orbit lowerOrbit,
            Orbit upperOrbit,
            float startWorld,
            bool lowerMutable,
            bool upperMutable,
            LevelBuildContext context,
            bool randomizeSpikeHeight)
        {
            List<PatternPlacedObject> objects = new List<PatternPlacedObject>();

            IReadOnlyList<PatternSlotSnapshot> lowerSlots = GetLane(pattern, flipped, lowerLane: true);
            IReadOnlyList<PatternSlotSnapshot> upperSlots = GetLane(pattern, flipped, lowerLane: false);
            float lowerLaneLength = GetLaneLength(pattern, flipped, lowerLane: true);
            float upperLaneLength = GetLaneLength(pattern, flipped, lowerLane: false);

            if (lowerMutable)
            {
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
            }

            if (upperMutable)
            {
                AddLaneObjects(
                    upperSlots,
                    pattern.Length,
                    upperLaneLength,
                    upperOrbit,
                    ArcType.Inner,
                    startWorld,
                    context,
                    randomizeSpikeHeight,
                    objects);
            }

            float startAngle = WorldToDegrees(startWorld, lowerOrbit.Radius);
            float endAngle = WorldToDegrees(startWorld + Math.Max(pattern.Length, 0.01f), lowerOrbit.Radius);
            return new PatternPlacement(
                pattern,
                flipped,
                isFallback,
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
            LevelBuildRequest request,
            float budgetSpent,
            float remainingBudget,
            float initialBudget,
            string rejectionSummary)
        {
            for (int i = 0; i < placement.Objects.Count; i++)
            {
                PatternPlacedObject placedObject = placement.Objects[i];
                placedObject.Orbit.OrbitObjects.Add(placedObject.ArcObject);
            }

            if (context.Settings.Patterns.DebugGizmosEnabled)
            {
                context.AddDebugEntry(new PatternDebugEntry(
                    placement.LowerOrbit.Index,
                    placement.UpperOrbit.Index,
                    placement.LowerOrbit.Radius,
                    placement.UpperOrbit.Radius,
                    placement.StartAngleDegree,
                    placement.EndAngleDegree,
                    placement.IsFallback,
                    request.Difficulty,
                    request.Seed,
                    request.Score,
                    request.MovementSpeed,
                    budgetSpent,
                    Math.Min(remainingBudget, initialBudget),
                    rejectionSummary));
            }
        }

        private static bool IsEligibleByContext(
            ObstaclePatternSnapshot pattern,
            float remainingBudget,
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

            if (pattern.Cost > remainingBudget)
            {
                reason = "insufficient remaining budget";
                return false;
            }

            if (pattern.Cost > stage.MaxPatternCost)
            {
                reason = "pattern exceeds stage max cost";
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

        private static bool CanPlaceOnMutableOrbits(
            ObstaclePatternSnapshot pattern,
            bool flipped,
            bool lowerMutable,
            bool upperMutable)
        {
            bool lowerHasObstacle = flipped ? pattern.HasObstacleOnOrbit2() : pattern.HasObstacleOnOrbit1();
            bool upperHasObstacle = flipped ? pattern.HasObstacleOnOrbit1() : pattern.HasObstacleOnOrbit2();

            return (lowerMutable || !lowerHasObstacle) &&
                   (upperMutable || !upperHasObstacle);
        }

        private static float GetEffectiveWeight(
            ObstaclePatternSnapshot pattern,
            PatternDifficultyStageSnapshot stage,
            IReadOnlyList<ObstaclePatternSnapshot> history)
        {
            int samePatternStreak = 0;
            for (int i = history.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(history[i], pattern))
                {
                    break;
                }

                samePatternStreak++;
            }

            if (samePatternStreak >= stage.MaxRepeatedPatternCount)
            {
                return 0.0f;
            }

            float repetitionModifier = samePatternStreak > 0
                ? stage.SamePatternWeightMultiplier
                : 1.0f;

            if (history.Count > 0 &&
                (history[^1].Tags & pattern.Tags) != PatternTag.None &&
                !ReferenceEquals(history[^1], pattern))
            {
                repetitionModifier *= stage.SameTagWeightMultiplier;
            }

            return pattern.BaseWeight *
                   pattern.DifficultyWeightMultiplier *
                   repetitionModifier;
        }

        private static IReadOnlyList<PatternSlotSnapshot> GetLane(
            ObstaclePatternSnapshot pattern,
            bool flipped,
            bool lowerLane)
        {
            IReadOnlyList<PatternSlotSnapshot> source = lowerLane
                ? (flipped ? pattern.Orbit2Slots : pattern.Orbit1Slots)
                : (flipped ? pattern.Orbit1Slots : pattern.Orbit2Slots);

            return flipped ? source.Reverse().ToArray() : source;
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

        private static float WorldToDegrees(float worldDistance, float radius)
        {
            return worldDistance / Math.Max(0.01f, radius) * OrbitMath.RADIANS_TO_DEGREES;
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
