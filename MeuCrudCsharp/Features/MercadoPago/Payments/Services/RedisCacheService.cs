using System;
using System.Text.Json;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using StackExchange.Redis;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _redisDb;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _redisDb = redis.GetDatabase();
        }

        private string BuildCacheKey(string prefix, string idempotencyKey)
        {
            return $"{prefix.ToLower()}_idempotency_{idempotencyKey}";
        }

        public async Task<CachedResponse?> GetCachedResponseAsync(
            string prefix,
            string idempotencyKey
        )
        {
            var cacheKey = BuildCacheKey(prefix, idempotencyKey);
            RedisValue cachedValue = await _redisDb.StringGetAsync(cacheKey);

            if (cachedValue.IsNullOrEmpty)
            {
                return null;
            }

            // Desserializa a string JSON de volta para o nosso objeto
            return JsonSerializer.Deserialize<CachedResponse>(cachedValue);
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

            // Serializa o objeto para uma string JSON para poder armazenar no Redis
            var valueToStore = JsonSerializer.Serialize(dataToCache);

            return _redisDb.StringSetAsync(cacheKey, valueToStore, CacheTtl);
        }
    }
}
