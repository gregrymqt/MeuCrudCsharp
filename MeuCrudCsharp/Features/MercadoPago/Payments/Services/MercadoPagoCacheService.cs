using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Services
{
    // O record pode ser definido aqui ou em um arquivo separado
    public record CachedResponse(object Body, int StatusCode);

    public class MemoryCacheService : ICacheService
    {
        private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);
        private readonly IMemoryCache _memoryCache;

        public MemoryCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        private string BuildCacheKey(string prefix, string idempotencyKey)
        {
            return $"{prefix.ToLower()}_idempotency_{idempotencyKey}";
        }

        public Task<CachedResponse?> GetCachedResponseAsync(string prefix, string idempotencyKey)
        {
            var cacheKey = BuildCacheKey(prefix, idempotencyKey);
            _memoryCache.TryGetValue(cacheKey, out CachedResponse? cachedResponse);
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
            _memoryCache.Set(cacheKey, dataToCache, CacheTtl);
            return Task.CompletedTask;
        }
    }
}
