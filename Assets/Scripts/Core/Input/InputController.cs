using System.Collections.Generic;
using OrbitalEscape.Events;
using Zenject;

namespace OrbitalEscape.Input
{
    public class InputController : ITickable
    {
        private readonly List<KeyBinding> _keyBindings;

        [Inject]
        public InputController(List<KeyBinding> keyBindings)
        {
            _keyBindings = keyBindings;
        }

        public void Tick()
        {
            foreach (var keyBinding in _keyBindings)
            {
                if (!UnityEngine.Input.GetKeyDown(keyBinding.Key))
                    continue;
                
                EventBus<CommandCalledEvent>.Raise(new CommandCalledEvent(keyBinding.CommandId));
            }
        }
    }
}
