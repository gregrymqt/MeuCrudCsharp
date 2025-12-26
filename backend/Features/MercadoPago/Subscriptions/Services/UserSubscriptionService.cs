using System;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using MeuCrudCsharp.Features.Profiles.UserAccount.DTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.Services;

public class UserSubscriptionService : IUserSubscriptionService
{
    private readonly IUserContext _userContext;
    private readonly ISubscriptionRepository _repository; // Ajuste para sua interface real de repositório
    private readonly IMercadoPagoSubscriptionService _mpSubscriptionService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<UserSubscriptionService> _logger;

    public UserSubscriptionService(
        IUserContext userContext,
        ISubscriptionRepository repository,
        IMercadoPagoSubscriptionService mpSubscriptionService,
        ICacheService cacheService,
        ILogger<UserSubscriptionService> logger)
    {
        _userContext = userContext;
        _repository = repository;
        _mpSubscriptionService = mpSubscriptionService;
        _cacheService = cacheService;
        _logger = logger;
    }

    // Lógica extraída de [cite: 17-24]
    public async Task<SubscriptionDetailsDto?> GetMySubscriptionDetailsAsync()
    {
        var userId = await _userContext.GetCurrentUserId();

        return await _cacheService.GetOrCreateAsync(
            $"SubscriptionDetails_{userId}",
            async () =>
            {
                // 1. Busca dados locais (Banco)
                var subscription = await _repository.GetActiveSubscriptionByUserIdAsync(userId);
                // Verifica se existe assinatura e plano associado [cite: 18]
                if (subscription?.Plan == null)
                    return null;

                // 2. Busca dados externos (Mercado Pago) [cite: 19]
                var mpSubscription = await _mpSubscriptionService.GetSubscriptionByIdAsync(
                    subscription.ExternalId
                );

                if (mpSubscription == null)
                    return null;

                // 3. Combina os dados para o DTO [cite: 20-22]
                return new SubscriptionDetailsDto
                {
                    SubscriptionId = subscription.ExternalId,
                    PlanName = subscription.Plan.Name,
                    Status = mpSubscription.Status,
                    Amount = subscription.Plan.TransactionAmount,
                    NextBillingDate = mpSubscription.NextPaymentDate, // [cite: 22]
                    LastFourCardDigits = subscription.LastFourCardDigits,
                };
            }
        );
    }

    public async Task ChangeSubscriptionStatusAsync(string newStatus)
    {
        var userId = await _userContext.GetCurrentUserId();

        // Validação de status permitidos [cite: 9]
        var allowed = new[] { "paused", "authorized", "cancelled" };
        if (!allowed.Contains(newStatus))
            throw new AppServiceException("Status de assinatura inválido.");

        // Busca assinatura ativa no banco [cite: 10]
        var subscription = await _repository.GetActiveSubscriptionByUserIdAsync(userId)
            ?? throw new ResourceNotFoundException("Nenhuma assinatura ativa encontrada para atualização.");

        // Chama o serviço do Mercado Pago 
        var dto = new UpdateSubscriptionStatusDto(newStatus);
        var result = await _mpSubscriptionService.UpdateSubscriptionStatusAsync(
            subscription.ExternalId,
            dto
        );

        if (result.Status == newStatus)
        {
            // Mapeia "authorized" de volta para "active" se necessário, conforme sua regra de negócio [cite: 13-14]
            subscription.Status = (newStatus == "authorized") ? "active" : newStatus;
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