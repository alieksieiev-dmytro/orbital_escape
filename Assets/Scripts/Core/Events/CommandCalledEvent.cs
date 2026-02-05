using System;

namespace OrbitalEscape.Events
{
    public readonly struct CommandCalledEvent : IEvent
    {
        public readonly IConvertible CommandType;
        
        public CommandCalledEvent(IConvertible commandType)
        {
            CommandType = commandType;
        }
    }
}
