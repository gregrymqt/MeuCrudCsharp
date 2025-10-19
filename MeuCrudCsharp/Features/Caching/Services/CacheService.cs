using System;
using System.Text.Json;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Caching;
using MeuCrudCsharp.Features.Caching.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MeuCrudCsharp.Features.Caching.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(10);

    // Removi IMemoryCache para manter a classe focada e consistente.
    public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Obtém um item do cache. Se não existir, executa a função 'factory' para criar o item,
    /// armazena o resultado no cache e o retorna.
    /// </summary>
    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? absoluteExpireTime = null
    )
    {
        // 1. Tenta buscar do cache primeiro.
        var cachedValue = await GetAsync<T>(key);
        if (cachedValue != null)
        {
            _logger.LogDebug("Cache HIT para a chave {CacheKey}.", key);
            return cachedValue;
        }

        _logger.LogDebug("Cache MISS para a chave {CacheKey}. Buscando da fonte de dados.", key);

        // 2. Se não encontrar (cache miss), executa a função factory para buscar os dados.
        var freshValue = await factory();

        // 3. Se a busca retornar dados válidos, salva no cache para futuras requisições.
        if (freshValue != null)
        {
            await SetAsync(key, freshValue, absoluteExpireTime);
        }

        return freshValue;
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
            _logger.LogInformation("Chave de cache {CacheKey} removida com sucesso.", key);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError(
                ex,
                "Não foi possível conectar ao Redis para remover a chave {CacheKey}.",
                key
            );
            // Em cenários de remoção, podemos optar por não lançar a exceção para não quebrar a aplicação.
        }
    }

    public async Task InvalidateCacheByKeyAsync(string cacheVersionKey)
    {
        try
        {
            var newVersion = Guid.NewGuid().ToString();
            // Usamos SetAsync para definir explicitamente a nova versão.
            await SetAsync(cacheVersionKey, newVersion, TimeSpan.FromDays(30));

            _logger.LogInformation(
                "Cache para a chave de versão '{CacheKey}' invalidado. Nova versão: {CacheVersion}",
                cacheVersionKey,
                newVersion
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao invalidar o cache para a chave de versão {CacheKey}",
                cacheVersionKey
            );
        }
    }

    // Métodos privados para manter a lógica de Get/Set encapsulada.
    private async Task<T?> GetAsync<T>(string key)
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
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao ler ou desserializar o cache para a chave {CacheKey}.",
                key
            );
            return default; // Trata o erro como um cache miss.
        }
    }

    private async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpireTime = null)
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
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Não foi possível conectar ao Redis ou serializar para definir a chave {CacheKey}.",
                key
            );
            // Não relançamos a exceção para que a aplicação continue funcionando mesmo se o cache falhar.
        }
    }
}
