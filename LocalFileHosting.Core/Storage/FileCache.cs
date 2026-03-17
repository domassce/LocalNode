using System;
using System.Collections.Generic;
using LocalFileHosting.Core.Interfaces;

namespace LocalFileHosting.Core.Storage
{
    /// <summary>
    /// A generic caching mechanism for file entities.
    /// Demonstrates the use of Generics and custom collections.
    /// </summary>
    /// <typeparam name="T">The type of file entity to cache.</typeparam>
    public class FileCache<T> where T : class, IFileEntity
    {
        private readonly Dictionary<Guid, CacheItem<T>> _cache = new();
        private readonly TimeSpan _expirationTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCache{T}"/> class with a specific expiration duration.
        /// </summary>
        /// <param name="expirationTime">The amount of time an item remains valid in the cache.</param>
        public FileCache(TimeSpan expirationTime)
        {
            _expirationTime = expirationTime;
        }

        /// <summary>
        /// Adds an item to the cache or updates an existing one with a new expiration timestamp.
        /// </summary>
        /// <param name="item">The item to add to the cache.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="item"/> is null.</exception>
        public void Add(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            _cache[item.Id] = new CacheItem<T>(item, DateTime.UtcNow.Add(_expirationTime));
        }

        /// <summary>
        /// Retrieves an item from the cache if it exists and has not expired.
        /// </summary>
        /// <param name="id">The unique identifier of the cached item.</param>
        /// <returns>The cached item if found and valid; otherwise, null.</returns>
        public T? Get(Guid id)
        {
            if (_cache.TryGetValue(id, out var cacheItem))
            {
                if (cacheItem.Expiration > DateTime.UtcNow)
                {
                    return cacheItem.Item;
                }

                // Item expired - cleanup on access
                _cache.Remove(id);
            }
            return default;
        }

        /// <summary>
        /// Manually removes all expired items from the cache to free up memory.
        /// </summary>
        public void ClearExpired()
        {
            var expiredKeys = new List<Guid>();
            foreach (var kvp in _cache)
            {
                if (kvp.Value.Expiration <= DateTime.UtcNow)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
            }
        }

        private class CacheItem<TItem>
        {
            public TItem Item { get; }
            public DateTime Expiration { get; }

            public CacheItem(TItem item, DateTime expiration)
            {
                Item = item;
                Expiration = expiration;
            }
        }
    }
}
