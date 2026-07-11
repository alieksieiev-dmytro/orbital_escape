using System;

namespace OrbitalEscape.Pooling
{
    public interface IObjectPool<T> : IDisposable where T : class
    {
        int CountAll { get; }
        int CountActive { get; }
        int CountInactive { get; }

        T Rent();
        bool Return(T item);
        void ReturnAll();
        void Prewarm(int count);
        void Clear();
    }
}
