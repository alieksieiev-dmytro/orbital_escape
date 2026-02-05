namespace OrbitalEscape.Input
{
    public class KeyBinding
    {
        public string Key { get; }
        public string CommandId { get; }
        
        public KeyBinding(string key, string commandId)
        {
            Key = key;
            CommandId = commandId;
        }
    }
}
