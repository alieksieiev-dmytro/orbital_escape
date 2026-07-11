using System;
using System.Collections.Generic;

namespace OrbitalEscape.Pooling
{
    public sealed class ObjectPool<T> : IObjectPool<T> where T : class
    {
        private readonly Func<T> _create;
        private readonly Action<T> _onRent;
        private readonly Action<T> _onReturn;
        private readonly Action<T> _onDestroy;
        private readonly Stack<T> _inactiveItems = new Stack<T>();
        private readonly HashSet<T> _inactiveSet = new HashSet<T>();
        private readonly HashSet<T> _activeItems = new HashSet<T>();

        private bool _isDisposed;

        public int CountAll => CountActive + CountInactive;
        public int CountActive => _activeItems.Count;
        public int CountInactive => _inactiveItems.Count;

        public ObjectPool(
            Func<T> create,
            Action<T> onRent = null,
            Action<T> onReturn = null,
            Action<T> onDestroy = null,
            int prewarmCount = 0)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _onRent = onRent;
            _onReturn = onReturn;
            _onDestroy = onDestroy;

            Prewarm(prewarmCount);
        }

        public T Rent()
        {
            ThrowIfDisposed();

            T item;
            if (_inactiveItems.Count > 0)
            {
                item = _inactiveItems.Pop();
                _inactiveSet.Remove(item);
            }
            else
            {
                item = CreateItem();
            }

            if (!_activeItems.Add(item))
            {
                throw new InvalidOperationException("The object pool tried to rent an item that is already active.");
            }

            _onRent?.Invoke(item);
            return item;
        }

        public bool Return(T item)
        {
            if (_isDisposed || item == null || !_activeItems.Remove(item))
            {
                return false;
            }

            _onReturn?.Invoke(item);
            if (_inactiveSet.Add(item))
            {
                _inactiveItems.Push(item);
            }

            return true;
        }

        public void ReturnAll()
        {
            ThrowIfDisposed();

            if (_activeItems.Count == 0)
            {
                return;
            }

            var activeItems = new List<T>(_activeItems);
            for (int i = 0; i < activeItems.Count; i++)
            {
                Return(activeItems[i]);
            }
        }

        public void Prewarm(int count)
        {
            ThrowIfDisposed();

            int targetCount = Math.Max(0, count);
            while (CountAll < targetCount)
            {
                T item = CreateItem();
                _onReturn?.Invoke(item);
                _inactiveSet.Add(item);
                _inactiveItems.Push(item);
            }
        }

        public void Clear()
        {
            foreach (T item in _activeItems)
            {
                _onDestroy?.Invoke(item);
            }

            foreach (T item in _inactiveItems)
            {
                _onDestroy?.Invoke(item);
            }

            _activeItems.Clear();
            _inactiveItems.Clear();
            _inactiveSet.Clear();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            Clear();
            _isDisposed = true;
        }

        private T CreateItem()
        {
            T item = _create();
            if (item == null)
            {
                throw new InvalidOperationException("The object pool create function returned null.");
            }

            return item;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ObjectPool<T>));
            }
        }
    }
}
