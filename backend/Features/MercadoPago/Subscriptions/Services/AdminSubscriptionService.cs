using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Plans.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using MeuCrudCsharp.Models;
using MeuCrudCsharp.Models.Enums;
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
            var localPlan =
                await _planRepository.GetActiveByExternalIdAsync(planExternalId)
                ?? throw new ResourceNotFoundException(
                    $"Plano com ID externo '{planExternalId}' não encontrado."
                );

            var newSubscription = new Subscription
            {
                UserId = userId,
                Status = SubscriptionStatus.Pending.ToMpString(), // Usa o Enum convertido
                ExternalId = planExternalId, // Temporário até vir do MP
                CardTokenId = savedCardId,
                PayerEmail = payerEmail,
                PlanId = localPlan.Id,
                CreatedAt = DateTime.UtcNow,
                LastFourCardDigits = lastFourDigits,
                CurrentAmount = (int)localPlan.TransactionAmount, // Grava o valor inicial na assinatura
                CurrentPeriodStartDate = DateTime.UtcNow,
                CurrentPeriodEndDate = DateTime.UtcNow.AddMonths(localPlan.FrequencyInterval),
                PayerMpId = "pending_mp_id", // Placeholder obrigatório até o MP responder
                PaymentMethodId = "credit_card", // Padrão
            };

            await _subscriptionRepository.AddAsync(newSubscription);
            await _subscriptionRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Assinatura local {SubscriptionInternalId} criada com status 'pending'.",
                newSubscription.Id
            );

            var periodStartDate = DateTime.UtcNow;
            var periodEndDate = periodStartDate.AddMonths(localPlan.FrequencyInterval);
            var frequencyType =
                localPlan.FrequencyType == PlanFrequencyType.Months ? "months" : "days";

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
                SubscriptionStatus.Authorized.ToMpString(), // Envia como 'authorized' para o MP
                userId // External Reference
            );

            SubscriptionResponseDto? subscriptionResponse = null;

            try
            {
                // 3. Chamada Externa
                subscriptionResponse = await _mpSubscriptionService.CreateSubscriptionAsync(
                    payloadForMp
                );
                _logger.LogInformation(
                    "Assinatura criada no MP com ID {ExternalId}",
                    subscriptionResponse.Id
                );
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(ex, "Falha no MP. Rollback local...");
                _subscriptionRepository.Remove(newSubscription);
                await _subscriptionRepository.SaveChangesAsync();
                throw;
            }

            try
            {
                newSubscription.ExternalId = subscriptionResponse.Id;
                newSubscription.PayerMpId = subscriptionResponse.PayerId.ToString(); // CORRIGIDO: Salva no campo certo
                newSubscription.PaymentMethodId = subscriptionResponse.PaymentMethodId;
                newSubscription.CurrentPeriodStartDate = subscriptionResponse
                    .AutoRecurring
                    .StartDate;
                newSubscription.CurrentPeriodEndDate = subscriptionResponse.AutoRecurring.EndDate;
                newSubscription.Status = subscriptionResponse.Status; // Status real que veio do MP

                await _subscriptionRepository.SaveChangesAsync();
                return newSubscription;
            }
            catch (DbUpdateException dbEx)
            {
                // 5. Compensação (Se falhar ao salvar no banco local, cancela no MP)
                _logger.LogError(dbEx, "Erro ao salvar atualização local. Cancelando no MP...");
                await _mpSubscriptionService.CancelSubscriptionAsync(subscriptionResponse.Id);
                throw new AppServiceException(
                    "Erro de consistência de dados. A operação foi revertida.",
                    dbEx
                );
            }
        }

        public async Task<SubscriptionResponseDto> UpdateSubscriptionValueAsync(
            string subscriptionId,
            UpdateSubscriptionValueDto dto
        )
        {
            _logger.LogInformation(
                "Atualizando valor da assinatura: {SubscriptionId}",
                subscriptionId
            );

            var localSubscription =
                await _subscriptionRepository.GetByExternalIdAsync(
                    subscriptionId,
                    includePlan: false, // Não precisamos carregar o Plan para editar o valor da Sub
                    asNoTracking: false
                )
                ?? throw new ResourceNotFoundException(
                    $"Assinatura {subscriptionId} não encontrada."
                );

            var originalAmount = localSubscription.CurrentAmount;

            // CORREÇÃO: Compara e atualiza o valor NA ASSINATURA, não no Plano global
            if (originalAmount == dto.TransactionAmount)
            {
                return await _mpSubscriptionService.GetSubscriptionByIdAsync(subscriptionId);
            }

            // Atualiza localmente (Optimistic update)
            localSubscription.CurrentAmount = (int)dto.TransactionAmount;
            await _subscriptionRepository.SaveChangesAsync();

            try
            {
                // Atualiza MP
                var mpResponse = await _mpSubscriptionService.UpdateSubscriptionValueAsync(
                    subscriptionId,
                    dto
                );

                await _cacheService.RemoveAsync($"SubscriptionDetails_{localSubscription.UserId}");
                return mpResponse;
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(ex, "Falha no MP. Revertendo valor local...");

                // Rollback Local
                localSubscription.CurrentAmount = originalAmount;
                await _subscriptionRepository.SaveChangesAsync();
                throw;
            }
        }

        public async Task<SubscriptionResponseDto> UpdateSubscriptionStatusAsync(
            string subscriptionId,
            UpdateSubscriptionStatusDto dto
        )
        {
            var localSubscription =
                await _subscriptionRepository.GetByExternalIdAsync(
                    subscriptionId,
                    asNoTracking: false
                ) ?? throw new ResourceNotFoundException("Assinatura não encontrada.");

            var originalStatus = localSubscription.Status;

            // Atualiza localmente
            localSubscription.Status = dto.Status; // Já deve vir validado ou use o Enum para validar antes
            localSubscription.UpdatedAt = DateTime.UtcNow;
            await _subscriptionRepository.SaveChangesAsync();

            try
            {
                var mpResponse = await _mpSubscriptionService.UpdateSubscriptionStatusAsync(
                    subscriptionId,
                    dto
                );
                await _cacheService.RemoveAsync($"SubscriptionDetails_{localSubscription.UserId}");
                return mpResponse;
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(ex, "Falha no MP. Revertendo status local...");

                // Rollback Local
                localSubscription.Status = originalStatus;
                await _subscriptionRepository.SaveChangesAsync();
                throw;
            }
        }

        // --- Métodos de Leitura e Helper ---

        public async Task<SubscriptionResponseDto> GetSubscriptionByIdAsync(string subscriptionId)
        {
            return await _mpSubscriptionService.GetSubscriptionByIdAsync(subscriptionId);
        }

        // Mantive este método para suportar sua lógica de ativação manual, se ainda usar
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
                throw new ResourceNotFoundException("Plano não encontrado.");

            var now = DateTime.UtcNow;
            var expirationDate = now.AddMonths(localPlan.FrequencyInterval);

            var newSubscription = new Subscription
            {
                UserId = userId,
                PlanId = localPlan.Id,
                ExternalId = paymentId, // Em caso de pagamento avulso, o ID externo é o PaymentId
                Status = SubscriptionStatus.Authorized.ToMpString(),
                PayerEmail = payerEmail,
                PaymentId = paymentId,
                CreatedAt = now,
                CurrentPeriodStartDate = now,
                CurrentPeriodEndDate = expirationDate,
                LastFourCardDigits = lastFourCardDigits,
                CurrentAmount = (int)localPlan.TransactionAmount,
                PayerMpId = "single_payment",
                PaymentMethodId = "pix_or_ticket",
            };

            await _subscriptionRepository.AddAsync(newSubscription);
            await _subscriptionRepository.SaveChangesAsync();

            return newSubscription;
        }
    }
}
