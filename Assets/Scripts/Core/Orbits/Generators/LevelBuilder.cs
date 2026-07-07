using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using OrbitalEscape.Difficulty;
using OrbitalEscape.Orbits.Generators.Patterns;
using Zenject;

namespace OrbitalEscape.Orbits.Generators
{
    public sealed class LevelBuilder : ILevelBuilder
    {
        private readonly OrbitGenerator _orbitGenerator;
        private readonly OrbitConfig _orbitConfig;
        private readonly LevelGenerationConfig _generationConfig;
        private readonly RiftConfig _riftConfig;
        private readonly SpikeConfig _spikeConfig;
        private readonly MiniSpikeConfig _miniSpikeConfig;
        private readonly ImpulseTrapConfig _impulseTrapConfig;
        private readonly GravityWellConfig _gravityWellConfig;
        private readonly LaserGateConfig _laserGateConfig;
        private readonly ObstaclePatternGenerationConfig _patternGenerationConfig;
        private readonly ILevelContentGenerator _contentGenerator;
        private readonly IDifficultyService _difficultyService;

        [Inject]
        public LevelBuilder(
            OrbitGenerator orbitGenerator,
            OrbitConfig orbitConfig,
            LevelGenerationConfig generationConfig,
            RiftConfig riftConfig,
            SpikeConfig spikeConfig,
            MiniSpikeConfig miniSpikeConfig,
            ImpulseTrapConfig impulseTrapConfig,
            GravityWellConfig gravityWellConfig,
            LaserGateConfig laserGateConfig,
            ObstaclePatternGenerationConfig patternGenerationConfig,
            ILevelContentGenerator contentGenerator,
            IDifficultyService difficultyService)
        {
            _orbitGenerator = orbitGenerator;
            _orbitConfig = orbitConfig;
            _generationConfig = generationConfig;
            _riftConfig = riftConfig;
            _spikeConfig = spikeConfig;
            _miniSpikeConfig = miniSpikeConfig;
            _impulseTrapConfig = impulseTrapConfig;
            _gravityWellConfig = gravityWellConfig;
            _laserGateConfig = laserGateConfig;
            _patternGenerationConfig = patternGenerationConfig;
            _contentGenerator = contentGenerator;
            _difficultyService = difficultyService;
        }

        public async UniTask<LevelBuildResult> BuildAsync(
            IReadOnlyList<Orbit> existingOrbits,
            int count,
            CancellationToken cancellationToken)
        {
            LevelBuildRequest request = CreateRequest(existingOrbits, count);
            LevelBuildResult result = await UniTask.RunOnThreadPool(
                () => Build(request, cancellationToken),
                cancellationToken: cancellationToken);
            await UniTask.SwitchToMainThread(cancellationToken);
            return result;
        }

        private LevelBuildRequest CreateRequest(IReadOnlyList<Orbit> existingOrbits, int count)
        {
            DifficultyType difficulty = _difficultyService.CurrentDifficulty;

            PatternSlotLengthProvider patternSlotLengthProvider = new PatternSlotLengthProvider(
                _spikeConfig.SpikeArcLength,
                _miniSpikeConfig.SpikeArcLength,
                _impulseTrapConfig.TrapArcLength,
                _gravityWellConfig.FieldArcLength,
                _riftConfig.RiftLength,
                _laserGateConfig.Width);

            LevelBuildSettings settings = new LevelBuildSettings(
                new OrbitGenerationSettings(_orbitConfig.FirstOrbitRadius, _orbitConfig.OrbitRadiusStep),
                new LevelContentSettings(_generationConfig.FirstOrbitIndexWithContent),
                new RiftGenerationSettings(
                    _riftConfig.MinCenterDistanceBetweenRifts,
                    _riftConfig.RiftLength,
                    _riftConfig.RiftCount,
                    _riftConfig.BaseRiftCountRadius,
                    _riftConfig.AdditionalRiftRadiusStep,
                    _riftConfig.NeighborRiftAvoidanceDistance,
                    _riftConfig.AlignedNeighborRiftWeightMultiplier,
                    _riftConfig.RiftPaddings,
                    _riftConfig.MaxPlacementAttempts),
                new SpikeGenerationSettings(
                    _spikeConfig.Density,
                    _spikeConfig.ConsecutiveSpikeChance,
                    _spikeConfig.MinSpikeHeight,
                    _spikeConfig.MaxSpikeHeight,
                    _spikeConfig.SpikeArcLength),
                new MiniSpikeGenerationSettings(
                    _miniSpikeConfig.Height,
                    _miniSpikeConfig.SpikeArcLength),
                new ImpulseTrapGenerationSettings(
                    _impulseTrapConfig.Density,
                    _impulseTrapConfig.TrapArcLength,
                    _impulseTrapConfig.Height),
                new GravityWellGenerationSettings(
                    _gravityWellConfig.Density,
                    _gravityWellConfig.FieldArcLength,
                    _gravityWellConfig.PartialJumpStep,
                    _gravityWellConfig.PullSpeed,
                    _gravityWellConfig.ReturnDuration,
                    _gravityWellConfig.VisualAlpha),
                new LaserGateGenerationSettings(
                    _laserGateConfig.Width,
                    _laserGateConfig.PassTime,
                    _laserGateConfig.ChargingTime,
                    _laserGateConfig.ActiveTime),
                _patternGenerationConfig.CreateSettings(difficulty, patternSlotLengthProvider),
                _patternGenerationConfig.RandomizeBandPhase);

            int seedBase = _patternGenerationConfig.UseFixedSeed
                ? _patternGenerationConfig.FixedSeed
                : Environment.TickCount;
            int seed = HashCode.Combine(seedBase, existingOrbits.Count, count, difficulty);
            return new LevelBuildRequest(
                new List<Orbit>(existingOrbits),
                count,
                seed,
                settings);
        }

        private LevelBuildResult Build(LevelBuildRequest request, CancellationToken cancellationToken)
        {
            if (request.Count <= 0)
            {
                return new LevelBuildResult(new List<Orbit>(), new List<string>());
            }

            LevelBuildContext context = new LevelBuildContext(
                request.ExistingOrbits,
                request.Count,
                request.Settings,
                new LevelRandom(request.Seed));

            List<Orbit> orbits = _orbitGenerator.GenerateOrbits(
                context.StartIndex,
                request.Count,
                request.Settings.Orbit);

            foreach (Orbit orbit in orbits)
            {
                cancellationToken.ThrowIfCancellationRequested();
                context.RegisterBuiltOrbit(orbit);
            }

            cancellationToken.ThrowIfCancellationRequested();
            _contentGenerator.Populate(orbits, context, request);

            return new LevelBuildResult(
                orbits,
                context.Warnings,
                context.CreateContentDeltas());
        }
    }
}
