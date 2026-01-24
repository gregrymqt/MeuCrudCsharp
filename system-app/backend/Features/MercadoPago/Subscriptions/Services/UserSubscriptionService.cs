namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.Services;

using Auth.Interfaces;
using Caching.Interfaces;
using Exceptions;
using MercadoPago.Subscriptions.DTOs;
using MercadoPago.Subscriptions.Interfaces;
using Models.Enums;
using Shared.Work;

public class UserSubscriptionService : IUserSubscriptionService
{
    private readonly IUserContext _userContext;
    private readonly ISubscriptionRepository _repository;
    private readonly IMercadoPagoSubscriptionService _mpSubscriptionService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<UserSubscriptionService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public UserSubscriptionService(
        IUserContext userContext,
        ISubscriptionRepository repository,
        IMercadoPagoSubscriptionService mpSubscriptionService,
        ICacheService cacheService,
        ILogger<UserSubscriptionService> logger,
        IUnitOfWork unitOfWork
    )
    {
        _userContext = userContext;
        _repository = repository;
        _mpSubscriptionService = mpSubscriptionService;
        _cacheService = cacheService;
        _logger = logger;
        _unitOfWork = unitOfWork;
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

        // 2. Busca assinatura ativa (precisa ser rastreada para atualização)
        var subscription = await _repository.GetByExternalIdAsync(
            await GetActiveSubscriptionExternalIdAsync(userId),
            includePlan: false,
            asNoTracking: false // ✅ Rastreada para poder atualizar
        ) ?? throw new ResourceNotFoundException(
            "Nenhuma assinatura ativa encontrada para atualização."
        );

        var originalStatus = subscription.Status;

        // 3. Atualiza localmente (apenas em memória)
        subscription.Status = statusEnum.ToMpString();
        subscription.UpdatedAt = DateTime.UtcNow;

        try
        {
            // 4. Chama o MercadoPago primeiro
            var dto = new UpdateSubscriptionStatusDto(statusEnum.ToMpString());
            var result = await _mpSubscriptionService.UpdateSubscriptionStatusAsync(
                subscription.ExternalId,
                dto
            );

            // 5. Verifica se MP retornou o status esperado
            if (result.Status != statusEnum.ToMpString())
            {
                _logger.LogWarning(
                    "MP retornou status {MpStatus} diferente do solicitado {RequestedStatus}",
                    result.Status,
                    statusEnum.ToMpString()
                );
                subscription.Status = result.Status; // Usa o status que MP retornou
            }

            // 6. ✅ COMMIT ÚNICO - Se MP OK, persiste localmente
            await _unitOfWork.CommitAsync();

            // 7. Limpa cache
            await _cacheService.RemoveAsync($"SubscriptionDetails_{userId}");

            _logger.LogInformation(
                "Assinatura {Id} do usuário {UserId} atualizada para {Status}.",
                subscription.ExternalId,
                userId,
                subscription.Status
            );
        }
        catch (ExternalApiException ex)
        {
            _logger.LogError(
                ex,
                "Falha no MP ao atualizar status da assinatura para {NewStatus}. Rollback automático.",
                newStatus
            );

            // ✅ ROLLBACK AUTOMÁTICO
            // Como a entidade está rastreada mas não foi comitada, precisamos reverter manualmente
            subscription.Status = originalStatus;

            _logger.LogInformation(
                "Rollback concluído. Status da assinatura permanece em {OriginalStatus}",
                originalStatus
            );

            throw;
        }
    }

    /// <summary>
    /// Helper method para buscar ExternalId da assinatura ativa.
    /// </summary>
    private async Task<string> GetActiveSubscriptionExternalIdAsync(string userId)
    {
        var subscription = await _repository.GetActiveSubscriptionByUserIdAsync(userId);
        return subscription?.ExternalId 
            ?? throw new ResourceNotFoundException("Nenhuma assinatura ativa encontrada.");
    }
}
