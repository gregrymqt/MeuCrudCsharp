using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System;
using System.Text.Json;
using System.Threading.Tasks;

// O nome agora é genérico, pois a classe não sabe se está usando Redis ou Memória.
public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(10);
    private readonly IMemoryCache _memoryCache;

    // A dependência é da abstração, não de uma implementação específica.
    public CacheService(IDistributedCache cache, IMemoryCache memoryCache)
    {
        _cache = cache;
        _memoryCache = memoryCache;
    }

    // O resto do código da classe permanece exatamente o mesmo...
    public async Task<T?> GetAsync<T>(string key)
    {
        var cachedValue = await _cache.GetStringAsync(key);
        return string.IsNullOrEmpty(cachedValue)
            ? default
            : JsonSerializer.Deserialize<T>(cachedValue);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpireTime = null)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpireTime ?? DefaultExpiration
        };
        var serializedValue = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, serializedValue, options);
    }

    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpireTime = null)
    {
        var cachedValue = await GetAsync<T>(key);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        var newValue = await factory();
        if (newValue != null)
        {
            await SetAsync(key, newValue, absoluteExpireTime);
        }

        return newValue;
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }

    // Em seu CacheService.cs (se estiver usando IMemoryCache por baixo)
    public Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpireTime = null, IChangeToken? expirationToken = null)
    {
        var options = new MemoryCacheEntryOptions();
        if (absoluteExpireTime.HasValue) options.SetAbsoluteExpiration(absoluteExpireTime.Value);
        if (expirationToken != null) options.AddExpirationToken(expirationToken); // Use o token aqui

        return _memoryCache.GetOrCreateAsync(key, entry =>
        {
            entry.SetOptions(options);
            return factory();
        });
    }
}