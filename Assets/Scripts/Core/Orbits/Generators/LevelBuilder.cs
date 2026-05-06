using System.Collections.Generic;
using System.Linq;
using Zenject;

namespace OrbitalEscape.Orbits.Generators
{
    public sealed class LevelBuilder : ILevelBuilder
    {
        private readonly OrbitGenerator _orbitGenerator;
        private readonly LevelGenerationConfig _generationConfig;
        private readonly List<IOrbitContentPass> _contentPasses;

        [Inject]
        public LevelBuilder(
            OrbitGenerator orbitGenerator,
            LevelGenerationConfig generationConfig,
            List<IOrbitContentPass> contentPasses)
        {
            _orbitGenerator = orbitGenerator;
            _generationConfig = generationConfig;
            _contentPasses = contentPasses
                .OrderBy(pass => pass.Order)
                .ToList();
        }

        public List<Orbit> Build(IReadOnlyList<Orbit> existingOrbits, int count)
        {
            if (count <= 0)
            {
                return new List<Orbit>();
            }

            LevelBuildContext context = new LevelBuildContext(existingOrbits, _generationConfig);
            List<Orbit> orbits = _orbitGenerator.GenerateOrbits(context.StartIndex, count);

            foreach (Orbit orbit in orbits)
            {
                if (_generationConfig.ShouldGenerateContent(orbit))
                {
                    foreach (IOrbitContentPass contentPass in _contentPasses)
                    {
                        contentPass.Populate(orbit, context);
                    }
                }

                context.RegisterBuiltOrbit(orbit);
            }

            return orbits;
        }
    }
}
