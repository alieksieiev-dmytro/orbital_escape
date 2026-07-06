namespace OrbitalEscape.Cheats
{
    public interface ICheatInfoProvider
    {
        int Order { get; }
        string GetLine();
    }
}
