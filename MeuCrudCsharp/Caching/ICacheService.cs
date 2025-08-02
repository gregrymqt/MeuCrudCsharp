using Microsoft.Extensions.Primitives;
using System;
using System.Threading.Tasks;

public interface ICacheService
{
    /// <summary>
    /// Obtém um item do cache. Retorna o valor padrão (null) se não encontrado.
    /// </summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Define/sobrescreve um item no cache com um tempo de expiração.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpireTime = null);

    /// <summary>
    /// Padrão Get-Or-Create: Obtém um item do cache ou o cria usando a factory se não existir.
    /// </summary>
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpireTime = null);

    /// <summary>
    /// Remove um item do cache.
    /// </summary>
    Task RemoveAsync(string key);

    // Em ICacheService.cs
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory,
        TimeSpan? absoluteExpireTime = null,
        IChangeToken? expirationToken = null); // Adicione este parâmetro
}