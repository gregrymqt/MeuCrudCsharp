using System;
using System.Threading.Tasks;

namespace MeuCrudCsharp.Features.Caching.Interfaces;

public interface ICacheService
{
    /// <summary>
    /// Busca um valor do cache. Se não existir, executa a função factory para criar o valor,
    /// o armazena no cache e o retorna.
    /// </summary>
    /// <typeparam name="T">O tipo do objeto a ser obtido ou criado.</typeparam>
    /// <param name="key">A chave do cache.</param>
    /// <param name="factory">A função que será executada para criar o valor se ele não estiver no cache.</param>
    /// <param name="absoluteExpireTime">O tempo de expiração para o novo item no cache.</param>
    /// <returns>O valor do cache ou o valor recém-criado.</returns>
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? absoluteExpireTime = null
    );

    /// <summary>
    /// Remove um valor do cache de forma assíncrona.
    /// </summary>
    /// <param name="key">A chave a ser removida.</param>
    Task RemoveAsync(string key);

    Task InvalidateCacheByKeyAsync(string cacheVersionKey);
}
