// Em Services/ICacheService.cs (Exemplo de implementação)

using MeuCrudCsharp.Features.MercadoPago.Payments.Services;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces
{
    // ====================================================================================
    // INTERFACE (O Contrato)
    // ====================================================================================

    /// <summary>
    /// Define o contrato para um serviço de cache focado em idempotência.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Busca uma resposta previamente armazenada no cache.
        /// </summary>
        /// <param name="prefix">O prefixo para o tipo de requisição (ex: 'Credit_Card').</param>
        /// <param name="idempotencyKey">A chave de idempotência recebida do cliente.</param>
        /// <returns>Um objeto CachedResponse se a chave for encontrada, ou null caso contrário.</returns>
        Task<CachedResponse?> GetCachedResponseAsync(string prefix, string idempotencyKey);

        /// <summary>
        /// Armazena a resposta de uma requisição bem-sucedida no cache.
        /// </summary>
        /// <param name="prefix">O prefixo para o tipo de requisição.</param>
        /// <param name="idempotencyKey">A chave de idempotência.</param>
        /// <param name="body">O corpo da resposta a ser armazenado.</param>
        /// <param name="statusCode">O código de status HTTP da resposta.</param>
        Task StoreResponseAsync(string prefix, string idempotencyKey, object body, int statusCode);
    }
}
