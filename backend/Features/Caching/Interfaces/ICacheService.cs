using System;
using System.Threading.Tasks;

namespace MeuCrudCsharp.Features.Caching.Interfaces;

public interface ICacheService
{
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? absoluteExpireTime = null
    );
    Task RemoveAsync(string key);
    Task InvalidateCacheByKeyAsync(string cacheVersionKey);
    Task<T?> GetAsync<T>(string key);

    Task<long> IncrementAsync(string key, int expirationSeconds);
}
