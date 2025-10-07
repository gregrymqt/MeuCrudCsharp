// Local: Features/Clients/Service/ClientService.cs

using System.Text.Json;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using Microsoft.EntityFrameworkCore;


namespace MeuCrudCsharp.Features.MercadoPago.Clients.Services;

    /// <summary>
    /// Serviço responsável por interagir com a API de Clientes do Mercado Pago,
    /// gerenciando clientes e seus cartões.
    /// Herda de <see cref="MercadoPagoServiceBase"/> para funcionalidades HTTP e autenticação.
    /// </summary>
    public class ClientService : MercadoPagoServiceBase, IClientService
    {
        private readonly ICacheService _cacheService;
        private readonly ApiDbContext  _dbContext;
        private readonly IUserContext _userContext;

        /// <summary>
        /// Inicializa uma nova instância do serviço de cliente.
        /// </summary>
        /// <param name="httpClient">Cliente HTTP para fazer requisições à API do Mercado Pago.</param>
        /// <param name="logger">Logger para rastreamento e diagnóstico.</param>
        public ClientService(IHttpClientFactory httpClient,
            ILogger<ClientService> logger,
            ICacheService cacheService,
            ApiDbContext dbContext,
            IUserContext userContext)
            : base(httpClient, logger)
        {
            _cacheService = cacheService;
            _dbContext = dbContext;
            _userContext = userContext;
        }

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
            var payload = new CustomerRequestDto(email,firstName);

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
            string cardToken
        )
        {
            var customerId = await GetCurrentUserCustomerIdAsync();
            _logger.LogInformation(
                "Adicionando novo cartão para o cliente MP: {CustomerId}",
                customerId
            );

            var endpoint = $"/v1/customers/{customerId}/cards";
            var payload = new CardRequestDto(cardToken);

            var responseBody = await SendMercadoPagoRequestAsync(
                HttpMethod.Post,
                endpoint,
                payload
            );

            var cardResponse = JsonSerializer.Deserialize<CardResponseDto>(responseBody)
                               ?? throw new AppServiceException(
                                   "Falha ao desserializar a resposta ao adicionar cartão."
                               );

            // Invalida o cache após a operação ser bem-sucedida
            var cacheKey = $"customer-cards:{customerId}";
            await _cacheService.RemoveAsync(cacheKey);

            return cardResponse;
        }

        /// <summary>
        /// Remove um cartão específico de um cliente no Mercado Pago.
        /// </summary>
        /// <param name="customerId">ID do cliente no Mercado Pago.</param>
        /// <param name="cardId">ID do cartão a ser removido.</param>
        /// <returns>Dados do cartão que foi removido.</returns>
        /// <exception cref="AppServiceException">Lançada se a desserialização da resposta falhar.</exception>
        public async Task<CardResponseDto> DeleteCardFromCustomerAsync(
            string cardId
        )
        {
            var customerId = await GetCurrentUserCustomerIdAsync();

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

            var cardResponse = JsonSerializer.Deserialize<CardResponseDto>(responseBody)
                               ?? throw new AppServiceException(
                                   "Falha ao desserializar a resposta ao deletar cartão."
                               );

            // Invalida o cache após a operação ser bem-sucedida
            var cacheKey = $"customer-cards:{customerId}";
            await _cacheService.RemoveAsync(cacheKey);

            return cardResponse;
        }

        /// <summary>
        /// Lista todos os cartões associados a um cliente no Mercado Pago.
        /// </summary>
        /// <param name="customerId">ID do cliente no Mercado Pago.</param>
        /// <returns>Lista de cartões do cliente.</returns>
        /// <exception cref="AppServiceException">Lançada se a desserialização da resposta falhar.</exception>
        public async Task<List<CardResponseDto>> ListCardsFromCustomerAsync()
        {
            var customerId = await GetCurrentUserCustomerIdAsync();

            _logger.LogInformation("Listando cartões para o cliente MP: {CustomerId}", customerId);

            // 1. Define uma chave única para o cache
            var cacheKey = $"customer-cards:{customerId}";
            var expirationTime = TimeSpan.FromMinutes(15); // Exemplo: cache por 15 minutos

            // 2. Usa GetOrCreateAsync
            //    - Tenta buscar do cache.
            //    - Se não encontrar, executa a função "factory", busca na API e salva o resultado no cache.
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                _logger.LogInformation("Cache miss para {CacheKey}. Buscando da API.", cacheKey);
                var endpoint = $"/v1/customers/{customerId}/cards";

                var responseBody = await SendMercadoPagoRequestAsync(
                    HttpMethod.Get,
                    endpoint,
                    (object?)null
                );

                return JsonSerializer.Deserialize<List<CardResponseDto>>(responseBody)
                       ?? new List<CardResponseDto>();
            }, expirationTime);
        }
        
        // MÉTODO GetCurrentUserCustomerIdAsync OTIMIZADO

        private async Task<string> GetCurrentUserCustomerIdAsync()
        {
            var userIdString = await _userContext.GetCurrentUserId();
            if (string.IsNullOrEmpty(userIdString))
            {
                throw new AppServiceException("Não foi possível identificar o usuário na sessão.");
            }

            // ✅ MELHORIA: Usa o cache para buscar o ID do cliente do MP
            var cacheKey = $"mp-customer-id:{userIdString}";
            var expirationTime = TimeSpan.FromHours(1); // O ID do cliente raramente muda

            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                _logger.LogInformation(
                    "Cache miss para {CacheKey}. Buscando MP Customer ID do banco de dados.",
                    cacheKey
                );
        
                var user = await _dbContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userIdString);

                if (user == null)
                    throw new AppServiceException("User not found.");

                if (string.IsNullOrEmpty(user.MercadoPagoCustomerId))
                {
                    _logger.LogWarning(
                        "User {UserId} does not have an associated payment customer profile.",
                        user.Id
                    );
                    throw new AppServiceException("Usuário não possui um cliente de pagamentos associado.");
                }

                return user.MercadoPagoCustomerId;

            }, expirationTime);
        }
    }

