using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        private readonly ImpulseTrapConfig _impulseTrapConfig;
        private readonly GravityWellConfig _gravityWellConfig;
        private readonly List<IOrbitContentPass> _contentPasses;

        [Inject]
        public LevelBuilder(
            OrbitGenerator orbitGenerator,
            OrbitConfig orbitConfig,
            LevelGenerationConfig generationConfig,
            RiftConfig riftConfig,
            SpikeConfig spikeConfig,
            ImpulseTrapConfig impulseTrapConfig,
            GravityWellConfig gravityWellConfig,
            List<IOrbitContentPass> contentPasses)
        {
            _orbitGenerator = orbitGenerator;
            _orbitConfig = orbitConfig;
            _generationConfig = generationConfig;
            _riftConfig = riftConfig;
            _spikeConfig = spikeConfig;
            _impulseTrapConfig = impulseTrapConfig;
            _gravityWellConfig = gravityWellConfig;
            _contentPasses = contentPasses
                .OrderBy(pass => pass.Order)
                .ToList();
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
            LevelBuildSettings settings = new LevelBuildSettings(
                new OrbitGenerationSettings(_orbitConfig.FirstOrbitRadius, _orbitConfig.OrbitRadiusStep),
                new LevelContentSettings(_generationConfig.FirstOrbitIndexWithContent),
                new RiftGenerationSettings(
                    _riftConfig.MinDistanceBetweenRifts,
                    _riftConfig.RiftLength,
                    _riftConfig.RiftCount,
                    _riftConfig.RiftPaddings,
                    _riftConfig.MaxPlacementAttempts),
                new SpikeGenerationSettings(
                    _spikeConfig.Density,
                    _spikeConfig.ConsecutiveSpikeChance,
                    _spikeConfig.MinSpikeHeight,
                    _spikeConfig.MaxSpikeHeight,
                    _spikeConfig.SpikeArcLength),
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
                    _gravityWellConfig.VisualAlpha));

            int seed = HashCode.Combine(Environment.TickCount, existingOrbits.Count, count);
            return new LevelBuildRequest(existingOrbits.ToList(), count, seed, settings);
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

                if (request.Settings.ShouldGenerateContent(orbit))
                {
                    foreach (IOrbitContentPass contentPass in _contentPasses)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        contentPass.Populate(orbit, context);
                    }
                }

                context.RegisterBuiltOrbit(orbit);
            }

            return new LevelBuildResult(orbits, context.Warnings.ToList());
        }
    }
}
