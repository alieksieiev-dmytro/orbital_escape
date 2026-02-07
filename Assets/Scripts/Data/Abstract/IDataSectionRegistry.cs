using System;

namespace OrbitalEscape.Data
{
    public interface IDataSectionRegistry<out TKey>
    {
        TKey GetKey(Type dataSection);
    }
}