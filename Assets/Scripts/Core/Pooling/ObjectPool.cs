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
        private readonly Func<T, bool> _isValid;
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
            int prewarmCount = 0,
            Func<T, bool> isValid = null)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _onRent = onRent;
            _onReturn = onReturn;
            _onDestroy = onDestroy;
            _isValid = isValid ?? (item => item != null);

            Prewarm(prewarmCount);
        }

        public T Rent()
        {
            ThrowIfDisposed();

            T item = null;
            while (_inactiveItems.Count > 0)
            {
                T candidate = _inactiveItems.Pop();
                _inactiveSet.Remove(candidate);
                if (_isValid(candidate))
                {
                    item = candidate;
                    break;
                }
            }

            if (ReferenceEquals(item, null))
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
            if (_isDisposed || ReferenceEquals(item, null) || !_activeItems.Remove(item))
            {
                return false;
            }

            if (!_isValid(item))
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
                if (_isValid(item))
                {
                    _onDestroy?.Invoke(item);
                }
            }

            foreach (T item in _inactiveItems)
            {
                if (_isValid(item))
                {
                    _onDestroy?.Invoke(item);
                }
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
            if (ReferenceEquals(item, null) || !_isValid(item))
            {
                throw new InvalidOperationException("The object pool create function returned an invalid item.");
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
