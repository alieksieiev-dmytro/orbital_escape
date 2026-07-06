using System;
using System.Collections.Generic;
using System.Linq;
using OrbitalEscape.Cheats;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.UI
{
    public sealed class CheatInfoHUDController : IInitializable, ITickable, IDisposable
    {
        private const string PrefabPath = "Prefabs/UI/HUDs/CheatInfoHUD";

        private readonly CheatPanelConfig _config;
        private readonly WindowRoot _windowRoot;
        private readonly List<ICheatInfoProvider> _providers;
        private CheatInfoHUD _hud;
        private readonly List<string> _lines;

        [Inject]
        public CheatInfoHUDController(
            CheatPanelConfig config,
            WindowRoot windowRoot,
            List<ICheatInfoProvider> providers)
        {
            _config = config;
            _windowRoot = windowRoot;
            _providers = providers.OrderBy(provider => provider.Order).ToList();
            _lines = new List<string>();
        }

        public void Initialize()
        {
            if (!_config.ShouldShowInfoPanel)
            {
                return;
            }

            CheatInfoHUD prefab = Resources.Load<CheatInfoHUD>(PrefabPath);
            if (prefab == null)
            {
                _hud = CheatInfoHUDBuilder.Create(_windowRoot.transform);
            }
            else
            {
                _hud = UnityEngine.Object.Instantiate(prefab, _windowRoot.transform);
            }

            _hud.SetVisible(true);
        }

        public void Tick()
        {
            if (_hud == null || !_config.ShouldShowInfoPanel)
            {
                return;
            }

            _lines.Clear();
            for (int i = 0; i < _providers.Count; i++)
            {
                string line = _providers[i].GetLine();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    _lines.Add(line);
                }
            }

            _hud.SetLines(_lines);
        }

        public void Dispose()
        {
            if (_hud != null)
            {
                UnityEngine.Object.Destroy(_hud.gameObject);
                _hud = null;
            }
        }
    }
}
