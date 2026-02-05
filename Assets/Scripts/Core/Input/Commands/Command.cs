using System;
using System.Globalization;
using OrbitalEscape.Events;

namespace OrbitalEscape.Input
{
    public abstract class Command : IDisposable
    {
        public abstract IConvertible Type { get; }
        
        public abstract void Execute();
        
        private EventBinding<CommandCalledEvent> _binding;

        protected Command()
        {
            _binding = new EventBinding<CommandCalledEvent>(OnActionKeyPressed);
            EventBus<CommandCalledEvent>.Register(_binding);
        }
        
        public void Dispose()
        {
            EventBus<CommandCalledEvent>.Deregister(_binding);
            _binding = null;
        }

        private void OnActionKeyPressed(CommandCalledEvent @event)
        {
            if (Type.ToString(CultureInfo.InvariantCulture) == @event.CommandType.ToString(CultureInfo.InvariantCulture))
            {
                Execute();
            }
        }
    }

}
