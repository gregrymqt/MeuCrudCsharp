// 1. Crie e importe suas exceções customizadas
using System;
using System.Text.Json;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Exceptions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using StackExchange.Redis; // Para capturar exceções específicas do Redis

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger; // 2. Injetando o Logger
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(10);
    private readonly IMemoryCache _memoryCache; // 1. Adicionada a dependência do IMemoryCache

    // 3. Removida a dependência do IMemoryCache para manter a responsabilidade única
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

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var cachedValue = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(cachedValue))
            {
                return default; // Item não encontrado no cache
            }
            return JsonSerializer.Deserialize<T>(cachedValue);
        }
        catch (RedisConnectionException ex) // Erro comum: Redis está fora do ar
        {
            _logger.LogError(
                ex,
                "Não foi possível conectar ao Redis para buscar a chave {CacheKey}.",
                key
            );
            return default; // Trata como um cache miss para não quebrar a aplicação
        }
        catch (JsonException ex) // Erro de desserialização (dado corrompido no cache)
        {
            _logger.LogWarning(
                ex,
                "Dado corrompido na chave de cache {CacheKey}. O item será tratado como expirado.",
                key
            );
            await RemoveAsync(key); // Remove o dado corrompido
            return default;
        }
    }

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
            // Lança uma exceção de serviço para que o chamador saiba que a operação falhou
            throw new AppServiceException("Não foi possível salvar os dados no cache.", ex);
        }
    }

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
