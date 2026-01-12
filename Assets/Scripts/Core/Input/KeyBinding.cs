namespace OrbitalEscape.Input
{
    public class KeyBinding
    {
        public string Action { get; }
        public ICommand Command { get; }
        
        public KeyBinding(string action, ICommand command)
        {
            Action = action;
            Command = command;
        }
    }
}
