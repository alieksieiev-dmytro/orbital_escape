using System;
using OrbitalEscape.Orbits;
using Zenject;

namespace OrbitalEscape.Visuals.Palettes
{
    public sealed class PaletteSceneController : IInitializable, IDisposable
    {
        private readonly IPaletteService _paletteService;
        private readonly IPaletteObjectPainter _painter;
        private readonly ILevelObjectSpawner _levelObjectSpawner;
        private readonly PaletteSceneReferences _sceneReferences;

        [Inject]
        public PaletteSceneController(
            IPaletteService paletteService,
            IPaletteObjectPainter painter,
            ILevelObjectSpawner levelObjectSpawner,
            PaletteSceneReferences sceneReferences)
        {
            _paletteService = paletteService;
            _painter = painter;
            _levelObjectSpawner = levelObjectSpawner;
            _sceneReferences = sceneReferences;
        }

        public void Initialize()
        {
            _paletteService.PaletteChanged += ApplyPalette;
            ApplyPalette(_paletteService.CurrentPalette);
        }

        public void Dispose()
        {
            _paletteService.PaletteChanged -= ApplyPalette;
        }

        private void ApplyPalette(PaletteSnapshot palette)
        {
            _painter.ApplyEnvironment(_sceneReferences.Background, _sceneReferences.Accent, palette);
            _painter.ApplyPlayer(_sceneReferences.Player, palette);

            foreach (var orbitObject in _levelObjectSpawner.SpawnedOrbitObjects)
            {
                _painter.ApplyLevelObject(orbitObject, palette);
            }
        }
    }
}
