// Local: Features/Clients/Service/ClientService.cs
using System.Text.Json;
using MeuCrudCsharp.Features.Clients.DTOs;
using MeuCrudCsharp.Features.Clients.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.Clients.Service
{
    /// <summary>
    /// Serviço responsável por interagir com a API de Clientes do Mercado Pago,
    /// gerenciando clientes e seus cartões.
    /// Herda de <see cref="MercadoPagoServiceBase"/> para funcionalidades HTTP e autenticação.
    /// </summary>
    public class ClientService : MercadoPagoServiceBase, IClientService
    {
        /// <summary>
        /// Inicializa uma nova instância do serviço de cliente.
        /// </summary>
        /// <param name="httpClient">Cliente HTTP para fazer requisições à API do Mercado Pago.</param>
        /// <param name="logger">Logger para rastreamento e diagnóstico.</param>
        public ClientService(HttpClient httpClient, ILogger<ClientService> logger)
            : base(httpClient, logger) { }

        /// <summary>
        /// Cria um novo cliente na plataforma Mercado Pago.
        /// </summary>
        /// <param name="email">E-mail do cliente.</param>
        /// <param name="firstName">Primeiro nome do cliente.</param>
        /// <returns>Dados do cliente recém-criado.</returns>
        /// <exception cref="AppServiceException">Lançada se a desserialização da resposta falhar.</exception>
        public async Task<CustomerResponseDto> CreateCustomerAsync(string email, string firstName)
        {
            const string endpoint = "/v1/customers";
            var payload = new CustomerRequestDto { Email = email, FirstName = firstName };

            var responseBody = await SendMercadoPagoRequestAsync(
                HttpMethod.Post,
                endpoint,
                payload
            );

            return JsonSerializer.Deserialize<CustomerResponseDto>(responseBody)
                ?? throw new AppServiceException(
                    "Falha ao desserializar a resposta da criação do cliente."
                );
        }

        /// <summary>
        /// Adiciona um novo cartão a um cliente existente no Mercado Pago.
        /// </summary>
        /// <param name="customerId">ID do cliente no Mercado Pago.</param>
        /// <param name="cardToken">Token do cartão gerado pelo frontend.</param>
        /// <returns>Dados do cartão adicionado.</returns>
        /// <exception cref="AppServiceException">Lançada se a desserialização da resposta falhar.</exception>
        public async Task<CardResponseDto> AddCardToCustomerAsync(
            string customerId,
            string cardToken
        )
        {
            _logger.LogInformation(
                "Adicionando novo cartão para o cliente MP: {CustomerId}",
                customerId
            );

            var endpoint = $"/v1/customers/{customerId}/cards";
            var payload = new CardRequestDto { Token = cardToken };

            var responseBody = await SendMercadoPagoRequestAsync(
                HttpMethod.Post,
                endpoint,
                payload
            );

            return JsonSerializer.Deserialize<CardResponseDto>(responseBody)
                ?? throw new AppServiceException(
                    "Falha ao desserializar a resposta ao adicionar cartão."
                );
        }

        /// <summary>
        /// Remove um cartão específico de um cliente no Mercado Pago.
        /// </summary>
        /// <param name="customerId">ID do cliente no Mercado Pago.</param>
        /// <param name="cardId">ID do cartão a ser removido.</param>
        /// <returns>Dados do cartão que foi removido.</returns>
        /// <exception cref="AppServiceException">Lançada se a desserialização da resposta falhar.</exception>
        public async Task<CardResponseDto> DeleteCardFromCustomerAsync(
            string customerId,
            string cardId
        )
        {
            _logger.LogInformation(
                "Deletando o cartão {CardId} do cliente MP: {CustomerId}",
                cardId,
                customerId
            );

            var endpoint = $"/v1/customers/{customerId}/cards/{cardId}";

            var responseBody = await SendMercadoPagoRequestAsync(
                HttpMethod.Delete,
                endpoint,
                (object?)null
            );

            return JsonSerializer.Deserialize<CardResponseDto>(responseBody)
                ?? throw new AppServiceException(
                    "Falha ao desserializar a resposta ao deletar cartão."
                );
        }

        /// <summary>
        /// Lista todos os cartões associados a um cliente no Mercado Pago.
        /// </summary>
        /// <param name="customerId">ID do cliente no Mercado Pago.</param>
        /// <returns>Lista de cartões do cliente.</returns>
        /// <exception cref="AppServiceException">Lançada se a desserialização da resposta falhar.</exception>
        public async Task<List<CardResponseDto>> ListCardsFromCustomerAsync(string customerId)
        {
            _logger.LogInformation("Listando cartões para o cliente MP: {CustomerId}", customerId);

            var endpoint = $"/v1/customers/{customerId}/cards";

            var responseBody = await SendMercadoPagoRequestAsync(
                HttpMethod.Get,
                endpoint,
                (object?)null
            );

            return JsonSerializer.Deserialize<List<CardResponseDto>>(responseBody)
                ?? new List<CardResponseDto>();
        }
    }
}
