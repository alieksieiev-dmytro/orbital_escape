namespace OrbitalEscape.Data
{
    public interface IDataSection
    {
        string Key { get; }
        int Order { get; }

        void Save();
        IDataSection Load();
    }
}
