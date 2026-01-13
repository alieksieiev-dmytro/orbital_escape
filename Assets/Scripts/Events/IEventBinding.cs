using System;

namespace OrbitalEscape.Events
{
    internal interface IEventBinding<T>
    {
        public Action<T> OnEvent { get; set; }
        public Action OnEventNoArgs { get; set; }
    }
}