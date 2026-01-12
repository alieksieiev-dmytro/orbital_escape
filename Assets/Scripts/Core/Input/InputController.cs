using System.Collections.Generic;
using Zenject;

namespace OrbitalEscape.Input
{
    public class InputController : ITickable
    {
        private List<KeyBinding> _keyBindings = new List<KeyBinding>();
        
        [Inject]
        public void Construct(List<KeyBinding> keyBindings) => _keyBindings = keyBindings;

        public void Tick()
        {
            foreach (var keyBinding in _keyBindings)
            {
                if (UnityEngine.Input.GetKeyDown(keyBinding.Action))
                {
                    keyBinding.Command.Execute();
                }
            }
        }
    }
}
