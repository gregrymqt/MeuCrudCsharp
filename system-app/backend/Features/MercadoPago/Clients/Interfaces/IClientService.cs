// Local: Features/Clients/Interfaces/IClientService.cs

using MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces
{
    /// <summary>
    /// Contrato para o serviço de gerenciamento de clientes e cartões no provedor de pagamentos.
    /// </summary>
    public interface IClientService
    {
        Task RemoveCardFromWalletAsync(string userId, string cardId);
        Task<WalletCardDto> AddCardToWalletAsync(string userId, string cardToken);

        Task<List<WalletCardDto>> GetUserWalletAsync(string userId);

        Task<CustomerWithCardResponseDto> CreateCustomerWithCardAsync(
            string email,
            string name,
            string token
        );
        Task<CardInCustomerResponseDto> AddCardToCustomerAsync(string customerId, string token);
    }
}
