using Microsoft.Extensions.Primitives;

/// <summary>
/// Contrato para operações de cache distribuído e em memória.
/// Fornece APIs para leitura, escrita e cache com criação on-demand.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Obtém um valor do cache distribuído.
    /// </summary>
    /// <typeparam name="T">Tipo do objeto a ser desserializado.</typeparam>
    /// <param name="key">Chave do cache.</param>
    /// <returns>Instância do tipo T quando existir, caso contrário null.</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Define um valor no cache distribuído.
    /// </summary>
    /// <typeparam name="T">Tipo do objeto a ser serializado.</typeparam>
    /// <param name="key">Chave do cache.</param>
    /// <param name="value">Valor a ser armazenado.</param>
    /// <param name="absoluteExpireTime">Tempo absoluto de expiração. Se não informado, usa padrão.</param>
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpireTime = null);

    /// <summary>
    /// Obtém um valor do cache em memória ou cria-o via factory, com expiração absoluta opcional.
    /// </summary>
    /// <typeparam name="T">Tipo do objeto.</typeparam>
    /// <param name="key">Chave do cache.</param>
    /// <param name="factory">Função assíncrona que produz o valor quando não existe em cache.</param>
    /// <param name="absoluteExpireTime">Tempo absoluto de expiração para o item de memória.</param>
    /// <returns>Instância do tipo T.</returns>
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? absoluteExpireTime = null
    );

    /// <summary>
    /// Obtém um valor do cache em memória ou cria-o via factory, com expiração por token.
    /// </summary>
    /// <typeparam name="T">Tipo do objeto.</typeparam>
    /// <param name="key">Chave do cache.</param>
    /// <param name="factory">Função assíncrona que produz o valor quando não existe em cache.</param>
    /// <param name="expirationToken">Token de expiração para invalidação.</param>
    /// <returns>Instância do tipo T.</returns>
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, IChangeToken expirationToken);

    /// <summary>
    /// Remove um valor do cache distribuído.
    /// </summary>
    /// <param name="key">Chave do cache.</param>
    Task RemoveAsync(string key);
}
