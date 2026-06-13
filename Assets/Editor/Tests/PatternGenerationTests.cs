using System.Collections.Generic;
using NUnit.Framework;
using OrbitalEscape.Difficulty;
using OrbitalEscape.Orbits;
using OrbitalEscape.Orbits.Generators;
using OrbitalEscape.Orbits.Generators.Patterns;

namespace OrbitalEscape.Tests.Editor
{
    public sealed class PatternGenerationTests
    {
        [Test]
        public void SlotLengthProvider_DerivesLengthsFromObstacleConfigs()
        {
            PatternSlotLengthProvider provider = new PatternSlotLengthProvider(
                spikeLength: 1.25f,
                impulseTrapLength: 2.5f,
                gravityWellLength: 3.75f,
                riftLength: 4.5f);

            Assert.That(provider.GetLength(PatternSlotType.Space), Is.EqualTo(1.25f));
            Assert.That(provider.GetLength(PatternSlotType.Spike), Is.EqualTo(1.25f));
            Assert.That(provider.GetLength(PatternSlotType.ImpulseTrap), Is.EqualTo(2.5f));
            Assert.That(provider.GetLength(PatternSlotType.GravityWell), Is.EqualTo(3.75f));
            Assert.That(provider.GetLength(PatternSlotType.Rift), Is.EqualTo(4.5f));
        }

        [Test]
        public void WeightedSelector_IgnoresZeroWeightCandidates()
        {
            WeightedPatternSelector selector = new WeightedPatternSelector();
            PatternPlacement zeroWeightPlacement = CreatePlacement();
            PatternPlacement selectedPlacement = CreatePlacement();

            bool selected = selector.TrySelect(
                new[]
                {
                    new WeightedPatternCandidate(zeroWeightPlacement, 0.0f),
                    new WeightedPatternCandidate(selectedPlacement, 1.0f)
                },
                new LevelRandom(42),
                out PatternPlacement result);

            Assert.That(selected, Is.True);
            Assert.That(result, Is.SameAs(selectedPlacement));
        }

        [Test]
        public void PatternSnapshot_DetectsLockedSlotTypes()
        {
            ObstaclePatternSnapshot pattern = new ObstaclePatternSnapshot(
                enabled: true,
                new[] { new PatternSlotSnapshot(PatternSlotType.GravityWell, 3.0f) },
                new[] { new PatternSlotSnapshot(PatternSlotType.Space, 1.0f) },
                PatternTag.Gravity,
                cost: 1.0f,
                baseWeight: 1.0f,
                minScore: 0.0f,
                maxScore: 0.0f,
                difficultyWeightMultiplier: 1.0f,
                allowFlip: true);

            Assert.That(pattern.ContainsLockedSlot(PatternSlotTypeFlags.Space | PatternSlotTypeFlags.Spike), Is.True);
            Assert.That(pattern.ContainsLockedSlot(PatternSlotTypeFlags.All), Is.False);
        }

        [Test]
        public void Validator_BlocksGravityWellOverlapWithRiftAcrossOrbitBand()
        {
            Orbit lowerOrbit = new Orbit(5.0f, 0);
            Orbit upperOrbit = new Orbit(7.0f, 1);
            PatternPlacement placement = new PatternPlacement(
                CreatePattern(),
                flipped: false,
                isFallback: false,
                lowerOrbit,
                upperOrbit,
                startWorld: 0.0f,
                startAngleDegree: 10.0f,
                endAngleDegree: 30.0f,
                new List<PatternPlacedObject>
                {
                    new PatternPlacedObject(
                        lowerOrbit,
                        new ArcObject(
                            new Arc(10.0f, 30.0f, lowerOrbit.Radius),
                            new GravityWell(0.333f, 2.5f, 0.4f, 0.25f),
                            ArcType.Outer)),
                    new PatternPlacedObject(
                        upperOrbit,
                        new ArcObject(
                            new Arc(15.0f, 25.0f, upperOrbit.Radius),
                            new Rift(0.25f),
                            ArcType.TwoSide))
                });

            bool valid = new PatternPlacementValidator().IsValid(
                placement,
                CreateContext(lowerOrbit, upperOrbit),
                out string reason);

            Assert.That(valid, Is.False);
            Assert.That(reason, Does.Contain("Gravity well overlaps"));
        }

        [Test]
        public void Validator_AllowsRiftSpacingChecksOnDifferentOrbits()
        {
            Orbit lowerOrbit = new Orbit(5.0f, 0);
            Orbit upperOrbit = new Orbit(7.0f, 1);
            PatternPlacement placement = new PatternPlacement(
                CreatePattern(),
                flipped: false,
                isFallback: false,
                lowerOrbit,
                upperOrbit,
                startWorld: 0.0f,
                startAngleDegree: 10.0f,
                endAngleDegree: 30.0f,
                new List<PatternPlacedObject>
                {
                    new PatternPlacedObject(
                        lowerOrbit,
                        new ArcObject(new Arc(10.0f, 30.0f, lowerOrbit.Radius), new Rift(0.25f), ArcType.TwoSide)),
                    new PatternPlacedObject(
                        upperOrbit,
                        new ArcObject(new Arc(10.0f, 30.0f, upperOrbit.Radius), new Rift(0.25f), ArcType.TwoSide))
                });

            bool valid = new PatternPlacementValidator().IsValid(
                placement,
                CreateContext(lowerOrbit, upperOrbit),
                out string reason);

            Assert.That(valid, Is.True, reason);
        }

        private static PatternPlacement CreatePlacement()
        {
            Orbit lowerOrbit = new Orbit(5.0f, 0);
            Orbit upperOrbit = new Orbit(7.0f, 1);
            return new PatternPlacement(
                CreatePattern(),
                flipped: false,
                isFallback: false,
                lowerOrbit,
                upperOrbit,
                startWorld: 0.0f,
                startAngleDegree: 0.0f,
                endAngleDegree: 1.0f,
                new List<PatternPlacedObject>());
        }

        private static ObstaclePatternSnapshot CreatePattern()
        {
            return new ObstaclePatternSnapshot(
                enabled: true,
                new[] { new PatternSlotSnapshot(PatternSlotType.Space, 1.0f) },
                new[] { new PatternSlotSnapshot(PatternSlotType.Space, 1.0f) },
                PatternTag.Recovery,
                cost: 1.0f,
                baseWeight: 1.0f,
                minScore: 0.0f,
                maxScore: 0.0f,
                difficultyWeightMultiplier: 1.0f,
                allowFlip: true);
        }

        private static LevelBuildContext CreateContext(params Orbit[] orbits)
        {
            return new LevelBuildContext(
                orbits,
                buildCount: 0,
                CreateSettings(),
                new LevelRandom(123));
        }

        private static LevelBuildSettings CreateSettings()
        {
            return new LevelBuildSettings(
                new OrbitGenerationSettings(5.0f, 2.0f),
                new LevelContentSettings(0),
                new RiftGenerationSettings(20.0f, 2.0f, 0.0f, 0.25f, 8),
                new SpikeGenerationSettings(0.0f, 0.0f, 1.0f, 2.0f, 1.25f),
                new ImpulseTrapGenerationSettings(0.0f, 2.5f, 1.0f),
                new GravityWellGenerationSettings(0.0f, 3.75f, 0.333f, 2.5f, 0.4f, 0.25f),
                new PatternGenerationSettings(
                    new List<ObstaclePatternSnapshot>(),
                    fallbackPattern: null,
                    new[]
                    {
                        new PatternDifficultyStageSnapshot(
                            minScore: 0.0f,
                            maxScore: 0.0f,
                            budgetPerOrbitBand: 6.0f,
                            maxPatternCost: 6.0f,
                            maxRepeatedPatternCount: 2,
                            samePatternWeightMultiplier: 0.35f,
                            sameTagWeightMultiplier: 0.75f,
                            unlockedSlots: PatternSlotTypeFlags.All)
                    },
                    debugGizmosEnabled: false,
                    useFixedSeed: true,
                    fixedSeed: 123,
                    maxSelectionAttempts: 64));
        }
    }
}
