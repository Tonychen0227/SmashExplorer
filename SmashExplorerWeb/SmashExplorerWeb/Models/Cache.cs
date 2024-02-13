using System;
using System.Collections.Generic;
using System.Linq;

public class Cache<T>
{
    private readonly Dictionary<string, (T cachedObject, DateTime expiry)> _internalCache;
    private readonly long _ttlMilliseconds;
    private readonly object _lock = new object();

    private DateTime _useShorterTTLExpiry;

    public Cache(int ttlSeconds)
    {
        _internalCache = new Dictionary<string, (T cachedObject, DateTime expiry)>();
        _ttlMilliseconds = ttlSeconds;
        _useShorterTTLExpiry = DateTime.MinValue;
    }

    public bool ContainsKey(string key)
    {
        lock (_lock)
        {
            var containsKey = _internalCache.ContainsKey(key);

            if (containsKey)
            {
                var (cachedObject, expiry) = _internalCache[key];

                if (expiry < DateTime.UtcNow)
                {
                    _internalCache.Remove(key);
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public T GetFromCache(string key)
    {
        if (!ContainsKey(key))
        {
            return default;
        }

        lock (_lock)
        {
            return _internalCache[key].cachedObject;
        }
    }

    public void InvalidateCache(string key)
    {
        if (!ContainsKey(key))
        {
            return;
        }

        lock (_lock)
        {
            _internalCache.Remove(key);
        }
    }

    public void InvalidateCacheAndOverrideTTL(string key, long shortenTTLDurationSeconds)
    {
        InvalidateCache(key);
        
        lock (_lock)
        {
            _useShorterTTLExpiry = DateTime.UtcNow.AddSeconds(shortenTTLDurationSeconds);
        }
    }

    public void CleanupCache()
    {
        lock (_lock)
        {
            foreach (var key in _internalCache.Keys.ToList())
            {
                if (_internalCache[key].expiry < DateTime.UtcNow)
                {
                    _internalCache.Remove(key);
                }
            }
        }
    }

    public void SetCacheObject(string key, T toCacheObject, long? overrideTTLSeconds = null)
    {
        lock (_lock)
        {
            _internalCache[key] = (toCacheObject, GetExpiry(overrideTTLSeconds));
        }
    }

    public void AddToCacheObject(string key, Action<T> mutationFunc)
    {
        lock (_lock)
        {
            if (!_internalCache.ContainsKey(key))
            {
                _internalCache[key] = ((T) Activator.CreateInstance(typeof(T)), GetExpiry());
            }

            mutationFunc.Invoke(_internalCache[key].cachedObject);
            _internalCache[key] = (_internalCache[key].cachedObject, GetExpiry());
        }
    }

    private DateTime GetExpiry(long? overrideTTLSeconds = null)
    {
        return DateTime.UtcNow.AddSeconds(overrideTTLSeconds ?? (DateTime.UtcNow < _useShorterTTLExpiry ? _ttlMilliseconds / 3 : _ttlMilliseconds));
    }
}