using System;
using System.Collections.Generic;
using OrbitalEscape.UI;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Visuals.Palettes
{
    public sealed class PaletteUIViewTargetController : IInitializable, IDisposable
    {
        private readonly IPaletteService _paletteService;
        private readonly IViewFactory _viewFactory;
        private readonly Dictionary<IView, IPaletteTarget[]> _viewTargets = new();

        [Inject]
        public PaletteUIViewTargetController(
            IPaletteService paletteService,
            IViewFactory viewFactory)
        {
            _paletteService = paletteService;
            _viewFactory = viewFactory;
        }

        public void Initialize()
        {
            _paletteService.PaletteChanged += ApplyPalette;
            _viewFactory.ViewCreated += OnViewCreated;
        }

        public void Dispose()
        {
            _paletteService.PaletteChanged -= ApplyPalette;
            _viewFactory.ViewCreated -= OnViewCreated;
            _viewTargets.Clear();
        }

        private void OnViewCreated(IView view)
        {
            if (view?.Root == null)
            {
                return;
            }

            var targets = new List<IPaletteTarget>();
            foreach (MonoBehaviour behaviour in view.Root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is IPaletteTarget target)
                {
                    targets.Add(target);
                    target.ApplyPalette(_paletteService.CurrentPalette);
                }
            }

            _viewTargets[view] = targets.ToArray();
        }

        private void ApplyPalette(PaletteSnapshot palette)
        {
            foreach (IPaletteTarget[] targets in _viewTargets.Values)
            {
                foreach (IPaletteTarget target in targets)
                {
                    if (target is UnityEngine.Object unityObject && unityObject == null)
                    {
                        continue;
                    }

                    target.ApplyPalette(palette);
                }
            }
        }
    }
}
