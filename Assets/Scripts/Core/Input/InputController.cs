using System.Collections.Generic;
using OrbitalEscape.Events;
using Zenject;

namespace OrbitalEscape.Input
{
    public class InputController : ITickable
    {
        private readonly List<KeyBinding> _keyBindings;
        private readonly IInputGate _inputGate;

        [Inject]
        public InputController(List<KeyBinding> keyBindings, IInputGate inputGate)
        {
            _keyBindings = keyBindings;
            _inputGate = inputGate;
        }

        public void Tick()
        {
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Escape))
            {
                EventBus<EscapeRequestedEvent>.Raise(new EscapeRequestedEvent());
                _inputGate.SuppressForCurrentFrame();
                return;
            }

            if (_inputGate.IsLocked)
            {
                return;
            }

            foreach (var keyBinding in _keyBindings)
            {
                if (_inputGate.IsLocked)
                {
                    return;
                }

                if (!UnityEngine.Input.GetKeyDown(keyBinding.Key))
                    continue;
                
                EventBus<CommandCalledEvent>.Raise(new CommandCalledEvent(keyBinding.CommandId));
            }
        }
    }
}
