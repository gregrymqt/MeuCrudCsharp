// Local: Features/Clients/Interfaces/IClientService.cs

using MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces
{
    /// <summary>
    /// Contrato para o serviço de gerenciamento de clientes e cartões no provedor de pagamentos.
    /// </summary>
    public interface IClientService
    {
        Task<CustomerWithCardResponseDto> CreateCustomerWithCardAsync(
            string email,
            string firstName,
            string cardToken
        );

        Task<CardInCustomerResponseDto> AddCardToCustomerAsync(string customerId, string cardToken);

        Task<List<CardInCustomerResponseDto>> ListCardsFromCustomerAsync();

        Task<CardInCustomerResponseDto> DeleteCardFromCustomerAsync(string cardId);

        Task<CardInCustomerResponseDto> GetCardInCustomerAsync(string customerId, string cardId);
    }
}
