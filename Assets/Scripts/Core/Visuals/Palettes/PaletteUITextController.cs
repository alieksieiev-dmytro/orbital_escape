using System;
using OrbitalEscape.UI;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Visuals.Palettes
{
    public sealed class PaletteUITextController : IInitializable, IDisposable
    {
        private readonly IPaletteService _paletteService;
        private readonly IViewFactory _viewFactory;

        [Inject]
        public PaletteUITextController(IPaletteService paletteService, IViewFactory viewFactory)
        {
            _paletteService = paletteService;
            _viewFactory = viewFactory;
        }

        public void Initialize()
        {
            _paletteService.PaletteChanged += ApplyPalette;
            _viewFactory.ViewCreated += OnViewCreated;
            ApplyPalette(_paletteService.CurrentPalette);
        }

        public void Dispose()
        {
            _paletteService.PaletteChanged -= ApplyPalette;
            _viewFactory.ViewCreated -= OnViewCreated;
        }

        private void OnViewCreated(IView view)
        {
            if (view?.Root == null)
            {
                return;
            }

            ApplyPalette(view.Root.transform, _paletteService.CurrentPalette);
        }

        private void ApplyPalette(PaletteSnapshot palette)
        {
            ApplyTargets(UnityEngine.Object.FindObjectsByType<PaletteTextColorTarget>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None), palette);
            ApplyTargets(UnityEngine.Object.FindObjectsByType<PaletteTextGlowTarget>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None), palette);
        }

        private void ApplyPalette(Transform root, PaletteSnapshot palette)
        {
            ApplyTargets(root.GetComponentsInChildren<PaletteTextColorTarget>(true), palette);
            ApplyTargets(root.GetComponentsInChildren<PaletteTextGlowTarget>(true), palette);
        }

        private void ApplyTargets<TTarget>(TTarget[] targets, PaletteSnapshot palette)
            where TTarget : IPaletteTarget
        {
            foreach (var target in targets)
            {
                target.ApplyPalette(palette);
            }
        }
    }
}
