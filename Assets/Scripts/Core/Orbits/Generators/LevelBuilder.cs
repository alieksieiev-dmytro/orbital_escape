using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using OrbitalEscape.Difficulty;
using OrbitalEscape.Orbits.Generators.Patterns;
using OrbitalEscape.Score;
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
        private readonly ObstaclePatternGenerationConfig _patternGenerationConfig;
        private readonly ILevelContentGenerator _contentGenerator;
        private readonly IDifficultyService _difficultyService;
        private readonly IScoreService _scoreService;

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
            ObstaclePatternGenerationConfig patternGenerationConfig,
            ILevelContentGenerator contentGenerator,
            IDifficultyService difficultyService,
            IScoreService scoreService)
        {
            _orbitGenerator = orbitGenerator;
            _orbitConfig = orbitConfig;
            _generationConfig = generationConfig;
            _riftConfig = riftConfig;
            _spikeConfig = spikeConfig;
            _miniSpikeConfig = miniSpikeConfig;
            _impulseTrapConfig = impulseTrapConfig;
            _gravityWellConfig = gravityWellConfig;
            _patternGenerationConfig = patternGenerationConfig;
            _contentGenerator = contentGenerator;
            _difficultyService = difficultyService;
            _scoreService = scoreService;
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
            float score = _scoreService.Score;

            PatternSlotLengthProvider patternSlotLengthProvider = new PatternSlotLengthProvider(
                _spikeConfig.SpikeArcLength,
                _miniSpikeConfig.SpikeArcLength,
                _impulseTrapConfig.TrapArcLength,
                _gravityWellConfig.FieldArcLength,
                _riftConfig.RiftLength);

            LevelBuildSettings settings = new LevelBuildSettings(
                new OrbitGenerationSettings(_orbitConfig.FirstOrbitRadius, _orbitConfig.OrbitRadiusStep),
                new LevelContentSettings(_generationConfig.FirstOrbitIndexWithContent),
                new RiftGenerationSettings(
                    _riftConfig.MinDistanceBetweenRifts,
                    _riftConfig.RiftLength,
                    _riftConfig.RiftCount,
                    _riftConfig.BaseRiftCountRadius,
                    _riftConfig.AdditionalRiftRadiusStep,
                    _riftConfig.RiftWeightIncreasePerNonRiftPattern,
                    _riftConfig.MaxRiftPressureMultiplier,
                    _riftConfig.NeighborRiftAvoidanceAngle,
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
                settings,
                score);
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
