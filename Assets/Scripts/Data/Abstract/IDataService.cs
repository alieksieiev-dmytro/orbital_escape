using System;

namespace OrbitalEscape.Data
{
    public interface IDataService : IDisposable
    {
        TData Load<TData>() where TData : IDataSection;
        void Save<TData>(TData data) where TData : IDataSection;
    }
}
