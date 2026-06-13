using System;
using System.Collections.Generic;
using System.Reflection;
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
                baseWeight: 1.0f,
                minScore: 0.0f,
                maxScore: 0.0f,
                difficultyWeightMultiplier: 1.0f,
                allowFlip: true);

            Assert.That(pattern.ContainsLockedSlot(PatternSlotTypeFlags.Space | PatternSlotTypeFlags.Spike), Is.True);
            Assert.That(pattern.ContainsLockedSlot(PatternSlotTypeFlags.All), Is.False);
        }

        [Test]
        public void PatternGenerator_PlacesOrbit1OnLowerAndOrbit2OnUpper()
        {
            Orbit orbitBeforeContent = new Orbit(3.0f, 0);
            Orbit lowerOrbit = new Orbit(5.0f, 1);
            Orbit upperOrbit = new Orbit(7.0f, 2);
            ObstaclePatternSnapshot pattern = new ObstaclePatternSnapshot(
                enabled: true,
                new[] { new PatternSlotSnapshot(PatternSlotType.Spike, 1.0f) },
                new[] { new PatternSlotSnapshot(PatternSlotType.ImpulseTrap, 1.0f) },
                PatternTag.Compound,
                baseWeight: 1.0f,
                minScore: 0.0f,
                maxScore: 0.0f,
                difficultyWeightMultiplier: 1.0f,
                allowFlip: false);
            LevelBuildSettings settings = CreateSettings(new[] { pattern });
            LevelBuildContext context = new LevelBuildContext(
                Array.Empty<Orbit>(),
                buildCount: 3,
                settings,
                new LevelRandom(123));
            RegisterBuiltOrbit(context, orbitBeforeContent);
            RegisterBuiltOrbit(context, lowerOrbit);
            RegisterBuiltOrbit(context, upperOrbit);
            LevelBuildRequest request = new LevelBuildRequest(
                Array.Empty<Orbit>(),
                count: 3,
                seed: 123,
                settings,
                DifficultyType.Basic,
                score: 0.0f,
                movementSpeed: 5.0f);

            new PatternLevelContentGenerator(new WeightedPatternSelector()).Populate(
                new[] { orbitBeforeContent, lowerOrbit, upperOrbit },
                context,
                request);

            AssertObjects(lowerOrbit, ArcObjectType.Spike, ArcType.Outer);
            AssertObjects(upperOrbit, ArcObjectType.ImpulseTrap, ArcType.Inner);
        }

        [Test]
        public void PatternGenerator_CanFlipOrbitLanes()
        {
            Orbit orbitBeforeContent = new Orbit(0.5f, 0);
            Orbit lowerOrbit = new Orbit(1.0f, 1);
            Orbit upperOrbit = new Orbit(2.0f, 2);
            float laneLength = lowerOrbit.Circumference;
            ObstaclePatternSnapshot pattern = new ObstaclePatternSnapshot(
                enabled: true,
                new[] { new PatternSlotSnapshot(PatternSlotType.Spike, laneLength) },
                new[] { new PatternSlotSnapshot(PatternSlotType.ImpulseTrap, laneLength) },
                PatternTag.Compound,
                baseWeight: 1.0f,
                minScore: 0.0f,
                maxScore: 0.0f,
                difficultyWeightMultiplier: 1.0f,
                allowFlip: true);
            LevelBuildSettings settings = CreateSettings(new[] { pattern });
            LevelBuildContext context = new LevelBuildContext(
                Array.Empty<Orbit>(),
                buildCount: 3,
                settings,
                new LevelRandom(123));
            RegisterBuiltOrbit(context, orbitBeforeContent);
            RegisterBuiltOrbit(context, lowerOrbit);
            RegisterBuiltOrbit(context, upperOrbit);
            LevelBuildRequest request = new LevelBuildRequest(
                Array.Empty<Orbit>(),
                count: 3,
                seed: 123,
                settings,
                DifficultyType.Basic,
                score: 0.0f,
                movementSpeed: 5.0f);

            new PatternLevelContentGenerator(new WeightedPatternSelector()).Populate(
                new[] { orbitBeforeContent, lowerOrbit, upperOrbit },
                context,
                request);

            AssertObjects(lowerOrbit, ArcObjectType.ImpulseTrap, ArcType.Outer);
            AssertObjects(upperOrbit, ArcObjectType.Spike, ArcType.Inner);
        }

        private static PatternPlacement CreatePlacement()
        {
            Orbit lowerOrbit = new Orbit(5.0f, 0);
            Orbit upperOrbit = new Orbit(7.0f, 1);
            return new PatternPlacement(
                CreatePattern(),
                flipped: false,
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
                baseWeight: 1.0f,
                minScore: 0.0f,
                maxScore: 0.0f,
                difficultyWeightMultiplier: 1.0f,
                allowFlip: true);
        }

        private static LevelBuildSettings CreateSettings(IReadOnlyList<ObstaclePatternSnapshot> patterns)
        {
            return new LevelBuildSettings(
                new OrbitGenerationSettings(5.0f, 2.0f),
                new LevelContentSettings(1),
                new RiftGenerationSettings(20.0f, 2.0f, 0.0f, 0.25f, 8),
                new SpikeGenerationSettings(0.0f, 0.0f, 1.0f, 2.0f, 1.25f),
                new ImpulseTrapGenerationSettings(0.0f, 2.5f, 1.0f),
                new GravityWellGenerationSettings(0.0f, 3.75f, 0.333f, 2.5f, 0.4f, 0.25f),
                new PatternGenerationSettings(
                    patterns,
                    new[]
                    {
                        new PatternDifficultyStageSnapshot(
                            minScore: 0.0f,
                            maxScore: 0.0f,
                            samePatternWeightMultiplier: 0.35f,
                            sameTagWeightMultiplier: 0.75f,
                            unlockedSlots: PatternSlotTypeFlags.All)
                    },
                    debugGizmosEnabled: false,
                    useFixedSeed: true,
                    fixedSeed: 123,
                    maxSelectionAttempts: 64));
        }

        private static void RegisterBuiltOrbit(LevelBuildContext context, Orbit orbit)
        {
            MethodInfo method = typeof(LevelBuildContext).GetMethod(
                "RegisterBuiltOrbit",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(context, new object[] { orbit });
        }

        private static void AssertObjects(Orbit orbit, ArcObjectType expectedObjectType, ArcType expectedArcType)
        {
            Assert.That(orbit.OrbitObjects, Is.Not.Empty);
            for (int i = 0; i < orbit.OrbitObjects.Count; i++)
            {
                Assert.That(orbit.OrbitObjects[i].Object.Type, Is.EqualTo(expectedObjectType));
                Assert.That(orbit.OrbitObjects[i].Type, Is.EqualTo(expectedArcType));
            }
        }
    }
}
