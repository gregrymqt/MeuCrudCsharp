using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Plans.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ILogger<SubscriptionService> _logger;
        private readonly ICacheService _cacheService;
        private readonly IMercadoPagoSubscriptionService _mpSubscriptionService;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IPlanRepository _planRepository;
        private readonly GeneralSettings _generalSettings;


        public SubscriptionService(
            ILogger<SubscriptionService> logger,
            ICacheService cacheService,
            IMercadoPagoSubscriptionService mpSubscriptionService,
            ISubscriptionRepository subscriptionRepository,
            IPlanRepository planRepository,
            IOptions<GeneralSettings> generalSettings
        )
        {
            _logger = logger;
            _cacheService = cacheService;
            _mpSubscriptionService = mpSubscriptionService;
            _subscriptionRepository = subscriptionRepository;
            _planRepository = planRepository;
            _generalSettings = generalSettings.Value;
        }

        public async Task<Subscription> CreateSubscriptionAsync(
            string userId,
            string planExternalId,
            string savedCardId,
            string payerEmail,
            string lastFourDigits
        )
        {
            var localPlan = await _planRepository.GetActiveByExternalIdAsync(planExternalId)
                            ?? throw new ResourceNotFoundException(
                                $"Plano com ID externo '{planExternalId}' não encontrado.");

            var newSubscription = new Subscription
            {
                UserId = userId,
                Status = "pending_external",
                ExternalId = planExternalId,
                CardTokenId = savedCardId,
                PayerEmail = payerEmail,
                PlanId = localPlan.Id,
                CreatedAt = DateTime.UtcNow,
                LastFourCardDigits = lastFourDigits
            };

            await _subscriptionRepository.AddAsync(newSubscription);
            await _subscriptionRepository.SaveChangesAsync();
            _logger.LogInformation("Assinatura local {SubscriptionInternalId} criada com status 'pending'.",
                newSubscription.Id);

            var periodStartDate = DateTime.UtcNow;
            var periodEndDate = periodStartDate.AddMonths(localPlan.FrequencyInterval);
            var frequencyType = "";

            if (localPlan.FrequencyType == PlanFrequencyType.Months)
            {
                frequencyType = "months";
            }
            else if (localPlan.FrequencyType == PlanFrequencyType.Days)
            {
                frequencyType = "days";
            }

            var payloadForMp = new CreateSubscriptionDto(
                planExternalId,
                localPlan.Name,
                payerEmail,
                savedCardId,
                $"{_generalSettings.BaseUrl}/Profile/User/Index",
                new AutoRecurringDto(
                    localPlan.FrequencyInterval,
                    frequencyType,
                    localPlan.TransactionAmount,
                    "BRL",
                    periodStartDate,
                    periodEndDate
                ),
                "authorized",
                userId);

            SubscriptionResponseDto? subscriptionResponse = null;
            try
            {
                subscriptionResponse = await _mpSubscriptionService.CreateSubscriptionAsync(payloadForMp);

                _logger.LogInformation("Assinatura criada com sucesso no Mercado Pago com ID {ExternalId}",
                    subscriptionResponse.Id);
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(ex, "Falha na API do MP ao criar assinatura. Iniciando rollback local...");
                _subscriptionRepository.Remove(newSubscription);
                await _subscriptionRepository.SaveChangesAsync();
                _logger.LogInformation("Rollback concluído. Assinatura local {SubscriptionInternalId} removida.",
                    newSubscription.Id);
                throw;
            }

            try
            {
                newSubscription.ExternalId = subscriptionResponse.Id;
                newSubscription.Status = subscriptionResponse.Status;
                newSubscription.UserId = subscriptionResponse.PayerId.ToString();
                newSubscription.PaymentMethodId = subscriptionResponse.PaymentMethodId;
                newSubscription.CurrentPeriodStartDate = subscriptionResponse.AutoRecurring.StartDate;
                newSubscription.CurrentPeriodEndDate = subscriptionResponse.AutoRecurring.EndDate;

                await _subscriptionRepository.SaveChangesAsync();
                _logger.LogInformation(
                    "Assinatura local {SubscriptionInternalId} atualizada com ID externo {ExternalId}.",
                    newSubscription.Id, newSubscription.ExternalId);

                return newSubscription;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx,
                    "Falha ao salvar o ID externo na assinatura local. Iniciando compensação externa...");

                await _mpSubscriptionService.CancelSubscriptionAsync(subscriptionResponse.Id);
                _logger.LogInformation("Compensação concluída. Assinatura {ExternalId} cancelada no Mercado Pago.",
                    subscriptionResponse.Id);

                throw new AppServiceException("Ocorreu um erro ao finalizar sua assinatura. A operação foi revertida.",
                    dbEx);
            }
        }

        public async Task<Subscription> ActivateSubscriptionFromSinglePaymentAsync(
            string userId,
            Guid planPublicId,
            string paymentId,
            string payerEmail,
            string? lastFourCardDigits
        )
        {
            var localPlan = await _planRepository.GetByPublicIdAsync(planPublicId, true);

            if (localPlan == null)
            {
                _logger.LogError("Tentativa de ativar assinatura para um plano inexistente. PublicId: {PlanPublicId}",
                    planPublicId);
                throw new ResourceNotFoundException($"Plano com ID '{planPublicId}' não encontrado.");
            }

            var now = DateTime.UtcNow;
            var expirationDate = now.AddMonths(localPlan.FrequencyInterval);

            var newSubscription = new Subscription
            {
                UserId = userId,
                PlanId = localPlan.Id,
                ExternalId = paymentId,
                Status = "active",
                PayerEmail = payerEmail,
                PaymentId = paymentId,
                CreatedAt = now,
                CurrentPeriodStartDate = now,
                CurrentPeriodEndDate = expirationDate,
                LastFourCardDigits = lastFourCardDigits
            };

            await _subscriptionRepository.AddAsync(newSubscription);
            await _subscriptionRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Assinatura {SubscriptionId} ativada para o usuário {UserId} via pagamento único {PaymentId}. Válida até {ExpirationDate}",
                newSubscription.Id, userId, paymentId, expirationDate
            );

            return newSubscription;
        }

        public async Task<SubscriptionResponseDto> GetSubscriptionByIdAsync(string subscriptionId)
        {
            return await _mpSubscriptionService.GetSubscriptionByIdAsync(subscriptionId);
        }

        public async Task<SubscriptionResponseDto> UpdateSubscriptionValueAsync(
            string subscriptionId,
            UpdateSubscriptionValueDto dto
        )
        {
            _logger.LogInformation("Iniciando atualização de valor para assinatura: {SubscriptionId}", subscriptionId);

            var localSubscription = await _subscriptionRepository.GetByExternalIdAsync(
                                        subscriptionId,
                                        includePlan: true,
                                        asNoTracking: false) // 'false' é crucial para permitir a atualização
                                    ?? throw new ResourceNotFoundException(
                                        $"Assinatura {subscriptionId} não encontrada.");

            if (localSubscription.Plan == null)
            {
                throw new AppServiceException($"Plano associado à assinatura {subscriptionId} não foi encontrado.");
            }

            var originalAmount = localSubscription.Plan.TransactionAmount;

            if (originalAmount == dto.TransactionAmount)
            {
                _logger.LogWarning(
                    "Tentativa de atualizar o valor da assinatura {SubscriptionId} para o mesmo valor atual.",
                    subscriptionId);
                return await _mpSubscriptionService.GetSubscriptionByIdAsync(subscriptionId);
            }

            localSubscription.Plan.TransactionAmount = dto.TransactionAmount;
            await _subscriptionRepository.SaveChangesAsync();
            _logger.LogInformation(
                "Valor do plano para a assinatura {SubscriptionId} atualizado localmente (temporário).",
                subscriptionId);

            try
            {
                var mpSubscriptionResponse =
                    await _mpSubscriptionService.UpdateSubscriptionValueAsync(subscriptionId, dto);

                await _cacheService.RemoveAsync($"SubscriptionDetails_{localSubscription.UserId}");
                _logger.LogInformation("Valor da assinatura {SubscriptionId} sincronizado com sucesso no Mercado Pago.",
                    subscriptionId);

                return mpSubscriptionResponse;
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(ex,
                    "Falha na API do MP ao atualizar valor. Iniciando rollback local para a assinatura {SubscriptionId}...",
                    subscriptionId);

                localSubscription.Plan.TransactionAmount = originalAmount;
                await _subscriptionRepository.SaveChangesAsync();
                _logger.LogInformation(
                    "Rollback concluído. Valor da assinatura {SubscriptionId} revertido para o valor original.",
                    subscriptionId);

                throw;
            }
        }

        public async Task<SubscriptionResponseDto> UpdateSubscriptionStatusAsync(string subscriptionId,
            UpdateSubscriptionStatusDto dto)
        {
            var localSubscription =
                await _subscriptionRepository.GetByExternalIdAsync(subscriptionId, asNoTracking: false)
                ?? throw new ResourceNotFoundException("Assinatura não encontrada para atualização de status.");

            var originalStatus = localSubscription.Status;

            localSubscription.Status = dto.Status;
            localSubscription.UpdatedAt = DateTime.UtcNow;
            await _subscriptionRepository.SaveChangesAsync();
            _logger.LogInformation(
                "Status da assinatura {SubscriptionId} atualizado para {Status} localmente (temporário).",
                subscriptionId, dto.Status);

            try
            {
                var mpSubscriptionResponse =
                    await _mpSubscriptionService.UpdateSubscriptionStatusAsync(subscriptionId, dto);

                await _cacheService.RemoveAsync($"SubscriptionDetails_{localSubscription.UserId}");
                return mpSubscriptionResponse;
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(ex, "Falha na API do MP ao atualizar status. Iniciando rollback local...");

                localSubscription.Status = originalStatus;
                localSubscription.UpdatedAt = DateTime.UtcNow;
                await _subscriptionRepository.SaveChangesAsync();
                _logger.LogInformation(
                    "Rollback concluído. Status da assinatura {SubscriptionId} revertido para {OriginalStatus}.",
                    subscriptionId, originalStatus);

                throw;
            }
        }
    }
}