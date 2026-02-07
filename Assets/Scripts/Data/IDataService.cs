using System;

namespace OrbitalEscape.Data
{
    public interface IDataService : IDisposable
    {
        void SaveData(IDataSection dataSection);
        T LoadData<T>(IConvertible key) where T : IDataSection;
    }
}
