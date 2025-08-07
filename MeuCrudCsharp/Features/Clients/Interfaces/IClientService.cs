// Local: Features/Clients/Interfaces/IClientService.cs

using MeuCrudCsharp.Features.Clients.DTOs;

namespace MeuCrudCsharp.Features.Clients.Interfaces
{
    public interface IClientService
    {
        /// <summary>
        /// Adiciona um novo cartão a um cliente (CREATE).
        /// </summary>
        Task<CardResponseDto> AddCardToCustomerAsync(string customerId, string cardToken);

        /// <summary>
        /// Cria um novo cliente no Mercado Pago.
        /// </summary>
        Task<CustomerResponseDto> CreateCustomerAsync(string email, string firstName);

        /// <summary>
        /// Cria uma lista dos cartões do cliente que estão no Mercado Pago.
        /// </summary>
        Task<List<CardResponseDto>> ListCardsFromCustomerAsync(string customerId);

        /// <summary>
        /// deleta o cartão do cliente no Mercado Pago.
        /// </summary>
        Task<CardResponseDto> DeleteCardFromCustomerAsync(string customerId, string cardId);
    }
}
