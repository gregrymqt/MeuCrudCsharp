namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.Services;

using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using MeuCrudCsharp.Models.Enums;
using Microsoft.Extensions.Logging;

public class UserSubscriptionService : IUserSubscriptionService
{
    private readonly IUserContext _userContext;
    private readonly ISubscriptionRepository _repository;
    private readonly IMercadoPagoSubscriptionService _mpSubscriptionService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<UserSubscriptionService> _logger;

    public UserSubscriptionService(
        IUserContext userContext,
        ISubscriptionRepository repository,
        IMercadoPagoSubscriptionService mpSubscriptionService,
        ICacheService cacheService,
        ILogger<UserSubscriptionService> logger
    )
    {
        _userContext = userContext;
        _repository = repository;
        _mpSubscriptionService = mpSubscriptionService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<SubscriptionDetailsDto?> GetMySubscriptionDetailsAsync()
    {
        var userId = await _userContext.GetCurrentUserId();

        return await _cacheService.GetOrCreateAsync(
            $"SubscriptionDetails_{userId}",
            async () =>
            {
                // 1. Busca dados locais usando a query unificada
                var subscription = await _repository.GetActiveSubscriptionByUserIdAsync(userId);

                if (subscription?.Plan == null)
                    return null;

                // 2. Busca dados externos para ter o status real e data de cobrança
                var mpSubscription = await _mpSubscriptionService.GetSubscriptionByIdAsync(
                    subscription.ExternalId
                );

                if (mpSubscription == null)
                    return null;

                // 3. CORREÇÃO DO ERRO: Uso do Construtor Positional do Record
                return new SubscriptionDetailsDto(
                    subscription.ExternalId, // subscriptionId
                    subscription.Plan.Name, // planName
                    mpSubscription.Status, // status (vem do MP)
                    (decimal)subscription.CurrentAmount, // amount (cast explícito se necessário)
                    subscription.LastFourCardDigits, // lastFourCardDigits
                    mpSubscription.NextPaymentDate // nextBillingDate
                );
            }
        );
    }

    public async Task ChangeSubscriptionStatusAsync(string newStatus)
    {
        var userId = await _userContext.GetCurrentUserId();

        // 1. Validação usando o Enum para segurança
        var statusEnum = SubscriptionStatusExtensions.FromMpString(newStatus);
        if (statusEnum == SubscriptionStatus.Unknown)
        {
            throw new AppServiceException($"Status '{newStatus}' inválido.");
        }

        var subscription =
            await _repository.GetActiveSubscriptionByUserIdAsync(userId)
            ?? throw new ResourceNotFoundException(
                "Nenhuma assinatura ativa encontrada para atualização."
            );

        // 2. Chama o Mercado Pago
        var dto = new UpdateSubscriptionStatusDto(statusEnum.ToMpString());

        var result = await _mpSubscriptionService.UpdateSubscriptionStatusAsync(
            subscription.ExternalId,
            dto
        );

        // 3. Atualiza Localmente
        // Se o MP retornou sucesso, atualizamos o banco local com o mesmo status
        if (result.Status == statusEnum.ToMpString())
        {
            subscription.Status = result.Status; // "authorized", "paused", etc.
            subscription.UpdatedAt = DateTime.UtcNow;

            await _repository.SaveChangesAsync();
            await _cacheService.RemoveAsync($"SubscriptionDetails_{userId}");

            _logger.LogInformation(
                "Assinatura {Id} do usuário {UserId} atualizada para {Status}.",
                subscription.ExternalId,
                userId,
                subscription.Status
            );
        }
    }
}
