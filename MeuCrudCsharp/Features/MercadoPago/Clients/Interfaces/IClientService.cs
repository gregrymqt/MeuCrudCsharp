// Local: Features/Clients/Interfaces/IClientService.cs

using MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces
{
    /// <summary>
    /// Contrato para o serviço de gerenciamento de clientes e cartões no provedor de pagamentos.
    /// </summary>
    public interface IClientService
    {
        /// <summary>
        /// Adiciona um novo cartão a um cliente existente.
        /// </summary>
        /// <param name="cardToken">O token do cartão a ser adicionado.</param>
        /// <returns>Um <see cref="CardResponseDto"/> representando o cartão adicionado.</returns>
        Task<CardResponseDto> AddCardToCustomerAsync(string cardToken);

        /// <summary>
        /// Cria um novo cliente no provedor de pagamentos.
        /// </summary>
        /// <param name="email">O endereço de e-mail do novo cliente.</param>
        /// <param name="firstName">O primeiro nome do novo cliente.</param>
        /// <returns>Um <see cref="CustomerResponseDto"/> representando o cliente criado.</returns>
        Task<CustomerResponseDto> CreateCustomerAsync(string email, string firstName);

        /// <summary>
        /// Lista os cartões de um cliente específico.
        /// </summary>
        /// <returns>Uma lista de <see cref="CardResponseDto"/> representando os cartões do cliente.</returns>
        Task<List<CardResponseDto>> ListCardsFromCustomerAsync();

        /// <summary>
        /// Deleta um cartão específico de um cliente.
        /// </summary>
        /// <param name="cardId">O ID do cartão a ser deletado.</param>
        /// <returns>Um <see cref="CardResponseDto"/> representando o cartão deletado.</returns>
        Task<CardResponseDto> DeleteCardFromCustomerAsync(string cardId);
    }
}
