using System;
using Zenject;

namespace OrbitalEscape.Visuals.Palettes
{
    public sealed class MainMenuPaletteController : IInitializable, IDisposable
    {
        private readonly IPaletteService _paletteService;
        private readonly IPaletteObjectPainter _painter;
        private readonly MainMenuPaletteSceneReferences _sceneReferences;

        [Inject]
        public MainMenuPaletteController(
            IPaletteService paletteService,
            IPaletteObjectPainter painter,
            MainMenuPaletteSceneReferences sceneReferences)
        {
            _paletteService = paletteService;
            _painter = painter;
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
            _painter.ApplyEnvironment(_sceneReferences.Background, null, palette);

            foreach (var environmentRoot in _sceneReferences.EnvironmentRoots)
            {
                if (environmentRoot != null)
                {
                    _painter.ApplyLevelObject(environmentRoot.gameObject, palette);
                }
            }
        }
    }
}
