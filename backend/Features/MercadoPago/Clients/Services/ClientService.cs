// Local: Features/Clients/Service/ClientService.cs

using System.Text.Json;
using MercadoPago.Client.Customer;
using MercadoPago.Resource;
using MercadoPago.Resource.Customer;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Clients.Services;

public class ClientService : MercadoPagoServiceBase, IClientService
{
    private readonly ICacheService _cacheService;
    private readonly ApiDbContext _dbContext;
    private readonly IUserContext _userContext;

    public ClientService(
        IHttpClientFactory httpClient,
        ILogger<ClientService> logger,
        ICacheService cacheService,
        ApiDbContext dbContext,
        IUserContext userContext
    )
        : base(httpClient, logger)
    {
        _cacheService = cacheService;
        _dbContext = dbContext;
        _userContext = userContext;
    }

    public async Task<CustomerWithCardResponseDto> CreateCustomerWithCardAsync(
        string email,
        string firstName,
        string cardToken
    )
    {
        _logger.LogInformation(
            "Iniciando processo de criação de cliente com cartão para {Email}",
            email
        );
        Customer newlyCreatedCustomer = null;

        try
        {
            // Etapa 1: Criar o cliente
            var customerClient = new CustomerClient();
            var customerRequest = new CustomerRequest { Email = email, FirstName = firstName };
            newlyCreatedCustomer = await customerClient.CreateAsync(customerRequest);
            _logger.LogInformation(
                "Cliente criado com sucesso no MP. ID: {CustomerId}",
                newlyCreatedCustomer.Id
            );

            CardInCustomerResponseDto addedCard = await AddCardToCustomerAsync(
                newlyCreatedCustomer.Id,
                cardToken
            );

            return new CustomerWithCardResponseDto(
                CustomerId: newlyCreatedCustomer.Id,
                Email: newlyCreatedCustomer.Email,
                Card: addedCard
            );
        }
        catch (Exception ex)
        {
            if (newlyCreatedCustomer != null)
            {
                _logger.LogError(
                    ex,
                    "Falha ao adicionar cartão ao cliente recém-criado {CustomerId}. O cliente foi criado no MP, mas a operação falhou. A próxima tentativa reutilizará este cliente.",
                    newlyCreatedCustomer.Id
                );
            }
            else
            {
                _logger.LogError(
                    ex,
                    "Falha durante a criação do cliente para o email {Email}",
                    email
                );
            }

            throw new AppServiceException(
                "Não foi possível processar o cadastro do seu método de pagamento. Por favor, tente novamente.",
                ex
            );
        }
    }

    public async Task<CardInCustomerResponseDto> AddCardToCustomerAsync(
        string customerId,
        string cardToken
    )
    {
        _logger.LogInformation("Adicionando cartão ao cliente MP: {CustomerId}", customerId);

        try
        {
            var customerClient = new CustomerClient();
            var cardRequest = new CustomerCardCreateRequest { Token = cardToken };
            CustomerCard addedCard = await customerClient.CreateCardAsync(customerId, cardRequest);
            _logger.LogInformation(
                "Cartão com final {LastFourDigits} adicionado com sucesso.",
                addedCard.LastFourDigits
            );

            // Invalida o cache para forçar a atualização na próxima listagem de cartões
            var cacheKey = $"customer-cards:{customerId}";
            await _cacheService.RemoveAsync(cacheKey);

            // Retorna o DTO do cartão criado
            return new CardInCustomerResponseDto(
                Id: addedCard.Id,
                LastFourDigits: addedCard.LastFourDigits,
                ExpirationMonth: addedCard.ExpirationMonth,
                ExpirationYear: addedCard.ExpirationYear
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao tentar adicionar cartão para o cliente MP: {CustomerId}",
                customerId
            );
            // Lança a exceção para que o método chamador (CreateCustomerWithCardAsync) possa tratá-la.
            throw;
        }
    }

    public async Task<CardInCustomerResponseDto> DeleteCardFromCustomerAsync(string cardId)
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

        var cardResponse =
            JsonSerializer.Deserialize<CardInCustomerResponseDto>(responseBody)
            ?? throw new AppServiceException(
                "Falha ao desserializar a resposta ao deletar cartão."
            );

        var cacheKey = $"customer-cards:{customerId}";
        await _cacheService.RemoveAsync(cacheKey);

        return cardResponse;
    }

    public async Task<List<CardInCustomerResponseDto>> ListCardsFromCustomerAsync()
    {
        var customerId = await GetCurrentUserCustomerIdAsync();
        _logger.LogInformation("Listando cartões para o cliente MP: {CustomerId}", customerId);

        var cacheKey = $"customer-cards:{customerId}";
        var expirationTime = TimeSpan.FromMinutes(15);

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                _logger.LogInformation(
                    "Cache miss para {CacheKey}. Buscando da API via SDK.",
                    cacheKey
                );

                // 1. Instancia o cliente do SDK e chama o método para listar os cartões
                var customerClient = new CustomerClient();
                ResourcesList<CustomerCard> customerCardsResponse =
                    await customerClient.ListCardsAsync(customerId);

                var jsonContent = customerCardsResponse.ApiResponse.Content;

                if (string.IsNullOrEmpty(jsonContent))
                {
                    return new List<CardInCustomerResponseDto>();
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var cards = JsonSerializer.Deserialize<List<CardInCustomerResponseDto>>(
                    jsonContent,
                    options
                );

                return cards ?? new List<CardInCustomerResponseDto>();
            },
            expirationTime
        ) ?? throw new ResourceNotFoundException("Cartões não encontrados.");
    }

    public async Task<CardInCustomerResponseDto> GetCardInCustomerAsync(
        string customerId,
        string cardId
    )
    {
        _logger.LogInformation(
            "Buscando cartão {CardId} para o cliente MP: {CustomerId}",
            cardId,
            customerId
        );
        var cacheKey = $"customer-cards:{customerId}:{cardId}";
        var expirationTime = TimeSpan.FromMinutes(15);

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                _logger.LogInformation(
                    "Cache miss para {CacheKey}. Buscando da API via SDK.",
                    cacheKey
                );

                var customerClient = new CustomerClient();
                var customerCardsResponse = await customerClient.GetCardAsync(customerId, cardId);

                var jsonContent = customerCardsResponse.ApiResponse.Content;

                if (string.IsNullOrEmpty(jsonContent))
                {
                    return new CardInCustomerResponseDto(null, null, null, null);
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var cards = JsonSerializer.Deserialize<CardInCustomerResponseDto>(
                    jsonContent,
                    options
                );

                return cards ?? new CardInCustomerResponseDto(null, null, null, null);
            },
            expirationTime
        ) ?? throw new ResourceNotFoundException("Cartão não encontrado.");
    }

    private async Task<string> GetCurrentUserCustomerIdAsync()
    {
        var userIdString = await _userContext.GetCurrentUserId();
        if (string.IsNullOrEmpty(userIdString))
        {
            throw new AppServiceException("Não foi possível identificar o usuário na sessão.");
        }

        var cacheKey = $"mp-customer-id:{userIdString}";
        var expirationTime = TimeSpan.FromHours(1); // O ID do cliente raramente muda

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                _logger.LogInformation(
                    "Cache miss para {CacheKey}. Buscando MP Customer ID do banco de dados.",
                    cacheKey
                );

                var user = await _dbContext
                    .Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userIdString);

                if (user == null)
                    throw new AppServiceException("User not found.");

                if (string.IsNullOrEmpty(user.CustomerId))
                {
                    _logger.LogWarning(
                        "User {UserId} does not have an associated payment customer profile.",
                        user.Id
                    );
                    throw new AppServiceException(
                        "Usuário não possui um cliente de pagamentos associado."
                    );
                }

                return user.CustomerId;
            },
            expirationTime
        ) ?? throw new ResourceNotFoundException("Erro ao buscar o ID do cliente MP.");
    }
}
