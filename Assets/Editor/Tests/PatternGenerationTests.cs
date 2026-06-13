using System.Collections.Generic;
using NUnit.Framework;
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
    }
}
