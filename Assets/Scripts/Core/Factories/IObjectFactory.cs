namespace OrbitalEscape.Factories
{
    public interface IObjectFactory<out TObject>
    {
        TObject Create(params object[] args);
        TObject Get(int index);
        void Clear();
    }
}
