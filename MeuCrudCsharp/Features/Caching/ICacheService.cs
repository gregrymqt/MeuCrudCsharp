using Microsoft.Extensions.Primitives;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpireTime = null);
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpireTime = null);

    // NOVO: Adicione esta sobrecarga para suportar o MemoryCache avançado
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, IChangeToken expirationToken);

    Task RemoveAsync(string key);
}