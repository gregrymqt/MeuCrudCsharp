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
    public class ClientService : MercadoPagoServiceBase, IClientService
    {
        // O construtor agora é mais simples. Ele apenas passa as dependências
        // para a classe base, que gerencia o HttpClient e o Access Token.
        public ClientService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<ClientService> logger
        )
            : base(httpClient, configuration, logger) { }

        public async Task<CustomerResponseDto> CreateCustomerAsync(string email, string firstName)
        {
            const string endpoint = "/v1/customers";
            var payload = new CustomerRequestDto { Email = email, FirstName = firstName };

            // Usa o método genérico da classe base para fazer a requisição
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

            // Usa o método genérico da classe base para fazer a requisição
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

            // O endpoint para deletar um cartão específico
            var endpoint = $"/v1/customers/{customerId}/cards/{cardId}";

            // Para uma requisição DELETE, não há corpo (payload é nulo)
            var responseBody = await SendMercadoPagoRequestAsync(
                HttpMethod.Delete,
                endpoint,
                (object?)null
            );

            // A API de exclusão do MP retorna o objeto do cartão que foi deletado.
            return JsonSerializer.Deserialize<CardResponseDto>(responseBody)
                ?? throw new AppServiceException(
                    "Falha ao desserializar a resposta ao deletar cartão."
                );
        }

        public async Task<List<CardResponseDto>> ListCardsFromCustomerAsync(string customerId)
        {
            _logger.LogInformation("Listando cartões para o cliente MP: {CustomerId}", customerId);

            var endpoint = $"/v1/customers/{customerId}/cards";

            // Para uma requisição GET, não há corpo (payload é nulo)
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
