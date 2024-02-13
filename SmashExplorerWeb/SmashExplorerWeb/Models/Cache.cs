using System;
using System.Collections.Generic;

public class Cache<K, T>
{
    private readonly Dictionary<K, (T cachedObject, DateTime expiry)> _internalCache;
    private readonly long _ttlMilliseconds;
    private readonly object _lock = new object();

    public Cache(int ttlSeconds)
    {
        _internalCache = new Dictionary<K, (T cachedObject, DateTime expiry)>();
        _ttlMilliseconds = ttlSeconds;
    }

    public bool ContainsKey(K key)
    {
        lock (_lock)
        {
            return _internalCache.ContainsKey(key);
        }
    }

    public T GetFromCache(K key)
    {
        lock (_lock)
        {
            if (!_internalCache.ContainsKey(key))
            {
                return default;
            }

            return _internalCache[key].cachedObject;
        }
    }

    public void InvalidateCache(K key)
    {
        lock (_lock)
        {
            _internalCache.Remove(key);
        }
    }

    public void CleanupCache()
    {
        lock (_lock)
        {
            foreach (var key in _internalCache.Keys)
            {
                if (_internalCache[key].expiry < DateTime.UtcNow)
                {
                    _internalCache.Remove(key);
                }
            }
        }
    }

    public void SetCacheObject(K key, T toCacheObject)
    {
        lock (_lock)
        {
            _internalCache[key] = (toCacheObject, GetExpiry());
        }
    }

    public void AddToCacheObject(K key, Action<T> mutationFunc)
    {
        lock (_lock)
        {
            if (!_internalCache.ContainsKey(key))
            {
                _internalCache[key] = ((T) Activator.CreateInstance(typeof(T)), GetExpiry());
                return;
            }

            mutationFunc.Invoke(_internalCache[key].cachedObject);
            _internalCache[key] = (_internalCache[key].cachedObject, GetExpiry());
        }
    }

    private DateTime GetExpiry()
    {
        return DateTime.UtcNow.AddSeconds(_ttlMilliseconds);
    }
}