using System;
using System.Text.Json;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Exceptions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using StackExchange.Redis;

/// <summary>
/// Implementação de <see cref="ICacheService"/> que combina cache distribuído
/// para persistência e MemoryCache para caching em memória de curta duração.
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(10);
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// Cria uma nova instância do serviço de cache.
    /// </summary>
    /// <param name="cache">Implementação de cache distribuído (Redis, Memory, etc.).</param>
    /// <param name="logger">Logger para rastreamento e diagnóstico.</param>
    /// <param name="memoryCache">Cache em memória para resultados quentes.</param>
    public CacheService(
        IDistributedCache cache,
        ILogger<CacheService> logger,
        IMemoryCache memoryCache
    )
    {
        _cache = cache;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var cachedValue = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(cachedValue))
            {
                return default;
            }
            return JsonSerializer.Deserialize<T>(cachedValue);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError(
                ex,
                "Não foi possível conectar ao Redis para buscar a chave {CacheKey}.",
                key
            );
            return default;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Dado corrompido na chave de cache {CacheKey}. O item será tratado como expirado.",
                key
            );
            await RemoveAsync(key);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpireTime = null)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpireTime ?? DefaultExpiration,
            };
            var serializedValue = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, serializedValue, options);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError(
                ex,
                "Não foi possível conectar ao Redis para definir a chave {CacheKey}.",
                key
            );
            throw new AppServiceException("Não foi possível salvar os dados no cache.", ex);
        }
    }

    /// <inheritdoc />
    public Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? absoluteExpireTime = null
    )
    {
        var options = new MemoryCacheEntryOptions();
        if (absoluteExpireTime.HasValue)
        {
            options.SetAbsoluteExpiration(absoluteExpireTime.Value);
        }

        return _memoryCache.GetOrCreateAsync(
            key,
            entry =>
            {
                entry.SetOptions(options);
                return factory();
            }
        );
    }

    /// <inheritdoc />
    public Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        IChangeToken expirationToken
    )
    {
        var options = new MemoryCacheEntryOptions();
        options.AddExpirationToken(expirationToken);

        return _memoryCache.GetOrCreateAsync(
            key,
            entry =>
            {
                entry.SetOptions(options);
                return factory();
            }
        );
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError(
                ex,
                "Não foi possível conectar ao Redis para remover a chave {CacheKey}.",
                key
            );
            throw new AppServiceException("Não foi possível invalidar o cache.", ex);
        }
    }
}
