using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OrbitalEscape.Difficulty;
using OrbitalEscape.Orbits;
using OrbitalEscape.Orbits.Generators;
using OrbitalEscape.Orbits.Generators.Patterns;

public sealed class PatternGenerationTests
{
    private const float LowerRadius = 10.0f;
    private const float UpperRadius = 20.0f;
    private const float Tolerance = 0.001f;

    [Test]
    public void UpperLaneSpaceExpandsToMatchLowerPatternEndAngle()
    {
        PatternPlacement placement = CreatePlacement(
            CreatePattern(
                new[]
                {
                    Slot(PatternSlotType.Spike, 2.0f),
                    Slot(PatternSlotType.Space, 2.0f)
                },
                new[]
                {
                    Slot(PatternSlotType.Space, 2.0f),
                    Slot(PatternSlotType.Spike, 2.0f)
                }),
            flipped: false,
            startWorld: 0.0f);

        Arc upperSpike = GetSpikesOn(placement, placement.UpperOrbit).Single();

        AssertAngle(RadiansToDegrees(0.3f), upperSpike.StartAngleDegree);
        AssertAngle(placement.EndAngleDegree, upperSpike.EndAngleDegree);
        Assert.That(upperSpike.Length(), Is.EqualTo(2.0f).Within(Tolerance));
    }

    [Test]
    public void UpperLaneMultipleSpacesSplitExtraLengthProportionally()
    {
        PatternPlacement placement = CreatePlacement(
            CreatePattern(
                new[] { Slot(PatternSlotType.Space, 20.0f) },
                new[]
                {
                    Slot(PatternSlotType.Space, 2.0f),
                    Slot(PatternSlotType.Spike, 2.0f),
                    Slot(PatternSlotType.Space, 4.0f),
                    Slot(PatternSlotType.Spike, 2.0f)
                }),
            flipped: false,
            startWorld: 0.0f);

        List<Arc> upperSpikes = GetSpikesOn(placement, placement.UpperOrbit);

        Assert.That(upperSpikes.Count, Is.EqualTo(2));
        AssertAngle(RadiansToDegrees(0.6f), upperSpikes[0].StartAngleDegree);
        AssertAngle(RadiansToDegrees(0.7f), upperSpikes[0].EndAngleDegree);
        AssertAngle(RadiansToDegrees(1.9f), upperSpikes[1].StartAngleDegree);
        AssertAngle(placement.EndAngleDegree, upperSpikes[1].EndAngleDegree);
    }

    [Test]
    public void UpperLaneWithoutSpacesIsCenteredWithoutStretchingObstacles()
    {
        PatternPlacement placement = CreatePlacement(
            CreatePattern(
                new[] { Slot(PatternSlotType.Space, 4.0f) },
                new[]
                {
                    Slot(PatternSlotType.Spike, 2.0f),
                    Slot(PatternSlotType.Spike, 2.0f)
                }),
            flipped: false,
            startWorld: 0.0f);

        List<Arc> upperSpikes = GetSpikesOn(placement, placement.UpperOrbit);

        Assert.That(upperSpikes.Count, Is.EqualTo(2));
        AssertAngle(RadiansToDegrees(0.1f), upperSpikes[0].StartAngleDegree);
        AssertAngle(RadiansToDegrees(0.3f), upperSpikes[1].EndAngleDegree);
        Assert.That(upperSpikes[0].Length(), Is.EqualTo(2.0f).Within(Tolerance));
        Assert.That(upperSpikes[1].Length(), Is.EqualTo(2.0f).Within(Tolerance));
    }

    [Test]
    public void NonZeroCursorKeepsUpperAndLowerPatternStartsAlignedByAngle()
    {
        PatternPlacement placement = CreatePlacement(
            CreatePattern(
                new[]
                {
                    Slot(PatternSlotType.Spike, 2.0f),
                    Slot(PatternSlotType.Space, 2.0f)
                },
                new[]
                {
                    Slot(PatternSlotType.Space, 2.0f),
                    Slot(PatternSlotType.Spike, 2.0f)
                }),
            flipped: false,
            startWorld: 10.0f);

        Arc upperSpike = GetSpikesOn(placement, placement.UpperOrbit).Single();

        AssertAngle(RadiansToDegrees(1.0f), placement.StartAngleDegree);
        AssertAngle(RadiansToDegrees(1.3f), upperSpike.StartAngleDegree);
        AssertAngle(placement.EndAngleDegree, upperSpike.EndAngleDegree);
    }

    [Test]
    public void FlipSwapsLaneAssignmentBeforeUpperLaneCompensation()
    {
        PatternPlacement placement = CreatePlacement(
            CreatePattern(
                new[]
                {
                    Slot(PatternSlotType.Space, 2.0f),
                    Slot(PatternSlotType.Spike, 2.0f)
                },
                new[]
                {
                    Slot(PatternSlotType.Spike, 2.0f),
                    Slot(PatternSlotType.Space, 2.0f)
                },
                allowFlip: true),
            flipped: true,
            startWorld: 0.0f);

        Arc lowerSpike = GetSpikesOn(placement, placement.LowerOrbit).Single();
        Arc upperSpike = GetSpikesOn(placement, placement.UpperOrbit).Single();

        AssertAngle(0.0f, lowerSpike.StartAngleDegree);
        AssertAngle(RadiansToDegrees(0.3f), upperSpike.StartAngleDegree);
        AssertAngle(placement.EndAngleDegree, upperSpike.EndAngleDegree);
    }

    private static PatternPlacement CreatePlacement(
        ObstaclePatternSnapshot pattern,
        bool flipped,
        float startWorld)
    {
        PatternLevelContentGenerator generator = new PatternLevelContentGenerator(new WeightedPatternSelector());
        MethodInfo method = typeof(PatternLevelContentGenerator).GetMethod(
            "CreatePlacement",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);

        Orbit lowerOrbit = new Orbit(LowerRadius, 0);
        Orbit upperOrbit = new Orbit(UpperRadius, 1);
        LevelBuildContext context = new LevelBuildContext(
            Array.Empty<Orbit>(),
            0,
            CreateSettings(pattern),
            new LevelRandom(123));

        return (PatternPlacement)method.Invoke(
            generator,
            new object[]
            {
                pattern,
                flipped,
                lowerOrbit,
                upperOrbit,
                startWorld,
                context,
                false
            });
    }

    private static ObstaclePatternSnapshot CreatePattern(
        IReadOnlyList<PatternSlotSnapshot> orbit1Slots,
        IReadOnlyList<PatternSlotSnapshot> orbit2Slots,
        bool allowFlip = false)
    {
        return new ObstaclePatternSnapshot(
            true,
            orbit1Slots,
            orbit2Slots,
            PatternTag.Spike,
            1.0f,
            0.0f,
            0.0f,
            1.0f,
            allowFlip);
    }

    private static LevelBuildSettings CreateSettings(ObstaclePatternSnapshot pattern)
    {
        return new LevelBuildSettings(
            new OrbitGenerationSettings(LowerRadius, UpperRadius - LowerRadius),
            new LevelContentSettings(0),
            new RiftGenerationSettings(0.0f, 2.0f, 0.0f, 0.0f, 1),
            new SpikeGenerationSettings(0.0f, 0.0f, 1.0f, 1.0f, 2.0f),
            new ImpulseTrapGenerationSettings(0.0f, 2.0f, 1.0f),
            new GravityWellGenerationSettings(0.0f, 2.0f, 0.333f, 2.5f, 0.4f, 0.25f),
            new PatternGenerationSettings(
                new[] { pattern },
                new[]
                {
                    new PatternDifficultyStageSnapshot(
                        0.0f,
                        0.0f,
                        1.0f,
                        1.0f,
                        PatternSlotTypeFlags.All)
                },
                false,
                false,
                0,
                1));
    }

    private static PatternSlotSnapshot Slot(PatternSlotType type, float length)
    {
        return new PatternSlotSnapshot(type, length);
    }

    private static List<Arc> GetSpikesOn(PatternPlacement placement, Orbit orbit)
    {
        return placement.Objects
            .Where(placedObject => ReferenceEquals(placedObject.Orbit, orbit) &&
                                   placedObject.ArcObject.Object.Type == ArcObjectType.Spike)
            .Select(placedObject => placedObject.ArcObject.Arc)
            .ToList();
    }

    private static void AssertAngle(float expected, float actual)
    {
        Assert.That(actual, Is.EqualTo(expected).Within(Tolerance));
    }

    private static float RadiansToDegrees(float radians)
    {
        return radians * 180.0f / (float)Math.PI;
    }
}
