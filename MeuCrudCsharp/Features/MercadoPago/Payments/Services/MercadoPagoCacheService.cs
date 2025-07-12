using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Services
{
    /// <summary>
    /// Representa um objeto de resposta armazenado em cache.
    /// </summary>
    public record CachedResponse(object Body, int StatusCode);

    public class MercadoPagoCacheService : ICacheService
    {
        // Duração do cache para as chaves de idempotência (24 horas).
        private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);
        private readonly IMemoryCache _memoryCache;

        public MercadoPagoCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Constrói a chave de cache padronizada.
        /// </summary>
        private string BuildCacheKey(string prefix, string idempotencyKey)
        {
            return $"{prefix.ToLower()}_idempotency_{idempotencyKey}";
        }

        public Task<CachedResponse?> GetCachedResponseAsync(string prefix, string idempotencyKey)
        {
            var cacheKey = BuildCacheKey(prefix, idempotencyKey);

            // Tenta obter o valor do cache.
            _memoryCache.TryGetValue(cacheKey, out CachedResponse? cachedResponse);

            // Retorna o valor encontrado (ou null se não encontrou) dentro de uma Task.
            return Task.FromResult(cachedResponse);
        }

        public Task StoreResponseAsync(
            string prefix,
            string idempotencyKey,
            object body,
            int statusCode
        )
        {
            var cacheKey = BuildCacheKey(prefix, idempotencyKey);
            var dataToCache = new CachedResponse(body, statusCode);

            // Armazena o objeto no cache com um tempo de expiração definido.
            _memoryCache.Set(cacheKey, dataToCache, CacheTtl);

            // Esta operação é síncrona no IMemoryCache, então podemos apenas retornar uma Task completada.
            return Task.CompletedTask;
        }
    }
}
