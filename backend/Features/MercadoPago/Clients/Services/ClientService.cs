using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.MercadoPago.Clients.Services;

public class ClientService : IClientService
{
    private readonly IClientMercadoPagoService _mpService;
    private readonly ICacheService _cacheService;
    private readonly IUserRepository _userRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ILogger<ClientService> _logger;

    public ClientService(
        IClientMercadoPagoService mpService,
        ICacheService cacheService,
        IUserRepository userRepository,
        ISubscriptionRepository subscriptionRepository,
        ILogger<ClientService> logger
    )
    {
        _mpService = mpService;
        _cacheService = cacheService;
        _userRepository = userRepository;
        _subscriptionRepository = subscriptionRepository;
        _logger = logger;
    }

    // --- READ ---
    public async Task<List<WalletCardDto>> GetUserWalletAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new ResourceNotFoundException("Usuário não encontrado.");

        if (string.IsNullOrEmpty(user.CustomerId))
            return new List<WalletCardDto>();

        // 1. Busca cartões (MP + Cache)
        var mpCards = await ListCardsFromCustomerAsync(user.CustomerId);

        // 2. Busca Assinatura Ativa
        var activeSubscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId);

        // 3. Mapeia Record -> Class
        return mpCards
            .Select(card => new WalletCardDto
            {
                Id = card.Id ?? "",
                LastFourDigits = card.LastFourDigits ?? "****",
                ExpirationMonth = card.ExpirationMonth ?? 0,
                ExpirationYear = card.ExpirationYear ?? 0,
                // Pega o ID de dentro do objeto PaymentMethod ou define unknown
                PaymentMethodId = card.PaymentMethod?.Id ?? "unknown",
                IsSubscriptionActiveCard =
                    activeSubscription != null && activeSubscription.CardTokenId == card.Id,
            })
            .ToList();
    }

    // --- CREATE ---
    public async Task<WalletCardDto> AddCardToWalletAsync(string userId, string cardToken)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new ResourceNotFoundException("Usuário não encontrado.");

        CardInCustomerResponseDto resultCard;

        if (string.IsNullOrEmpty(user.CustomerId))
        {
            // Cria Cliente + Cartão
            var newCustomer = await _mpService.CreateCustomerAsync(user.Email!, user.Name!);
            user.CustomerId = newCustomer.Id;
            await _userRepository.SaveChangesAsync();

            resultCard = await AddCardToCustomerAsync(newCustomer.Id!, cardToken);
        }
        else
        {
            // Adiciona Cartão
            resultCard = await AddCardToCustomerAsync(user.CustomerId, cardToken);
        }

        return new WalletCardDto
        {
            Id = resultCard.Id ?? "",
            LastFourDigits = resultCard.LastFourDigits ?? "****",
            ExpirationMonth = resultCard.ExpirationMonth ?? 0,
            ExpirationYear = resultCard.ExpirationYear ?? 0,
            PaymentMethodId = resultCard.PaymentMethod?.Id ?? "unknown",
            IsSubscriptionActiveCard = false,
        };
    }

    // --- DELETE ---
    public async Task RemoveCardFromWalletAsync(string userId, string cardId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.CustomerId))
            throw new ResourceNotFoundException("Carteira não encontrada.");

        var activeSubscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId);
        if (activeSubscription != null && activeSubscription.CardTokenId == cardId)
        {
            throw new InvalidOperationException(
                "Este cartão está vinculado à sua assinatura ativa."
            );
        }

        await DeleteCardFromCustomerAsync(user.CustomerId, cardId);
    }

    // --- MÉTODOS PRIVADOS ---

    private async Task<CardInCustomerResponseDto> AddCardToCustomerAsync(
        string customerId,
        string cardToken
    )
    {
        // 1. O método AddCardAsync retorna um objeto do SDK (MercadoPago.Resource.Customer.CustomerCard)
        var mpCard = await _mpService.AddCardAsync(customerId, cardToken);

        // 2. Limpa o cache para garantir consistência
        await _cacheService.RemoveAsync($"customer-cards:{customerId}");
        var paymentMethodDto = new PaymentMethodDto(
            mpCard.PaymentMethod?.Id,
            mpCard.PaymentMethod?.Name
        );

        return new CardInCustomerResponseDto(
            mpCard.Id,
            mpCard.LastFourDigits,
            mpCard.ExpirationMonth,
            mpCard.ExpirationYear,
            paymentMethodDto
        );
    }

    private async Task DeleteCardFromCustomerAsync(string customerId, string cardId)
    {
        await _mpService.DeleteCardAsync(customerId, cardId);
        await _cacheService.RemoveAsync($"customer-cards:{customerId}");
    }

    private async Task<List<CardInCustomerResponseDto>> ListCardsFromCustomerAsync(
        string customerId
    )
    {
        var cacheKey = $"customer-cards:{customerId}";
        return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    return await _mpService.ListCardsAsync(customerId);
                },
                TimeSpan.FromMinutes(15)
            ) ?? new List<CardInCustomerResponseDto>();
    }
}
