using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Plans.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using MeuCrudCsharp.Features.Shared.Work;
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
        private readonly IUnitOfWork _unitOfWork;

        public SubscriptionService(
            ILogger<SubscriptionService> logger,
            ICacheService cacheService,
            IMercadoPagoSubscriptionService mpSubscriptionService,
            ISubscriptionRepository subscriptionRepository,
            IPlanRepository planRepository,
            IOptions<GeneralSettings> generalSettings,
            IUnitOfWork unitOfWork
        )
        {
            _logger = logger;
            _cacheService = cacheService;
            _mpSubscriptionService = mpSubscriptionService;
            _subscriptionRepository = subscriptionRepository;
            _planRepository = planRepository;
            _generalSettings = generalSettings.Value;
            _unitOfWork = unitOfWork;
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

            // 1. Prepara a Entidade (apenas em memória, NÃO persiste ainda)
            var newSubscription = new Subscription
            {
                UserId = userId,
                Status = SubscriptionStatus.Pending.ToMpString(),
                ExternalId = planExternalId, // Temporário até vir do MP
                CardTokenId = savedCardId,
                PayerEmail = payerEmail,
                PlanId = localPlan.Id,
                CreatedAt = DateTime.UtcNow,
                LastFourCardDigits = lastFourDigits,
                CurrentAmount = (int)localPlan.TransactionAmount,
                CurrentPeriodStartDate = DateTime.UtcNow,
                CurrentPeriodEndDate = DateTime.UtcNow.AddMonths(localPlan.FrequencyInterval),
                PayerMpId = "pending_mp_id", // Placeholder obrigatório até o MP responder
                PaymentMethodId = "credit_card",
            };

            // Marca para adição (NÃO persiste ainda)
            await _subscriptionRepository.AddAsync(newSubscription);

            _logger.LogInformation(
                "Assinatura preparada para o usuário {UserId}. Aguardando resposta do MercadoPago.",
                userId
            );

            // 2. Prepara payload para MercadoPago
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
                SubscriptionStatus.Authorized.ToMpString(),
                userId
            );

            try
            {
                // 3. Chamada ao MercadoPago
                var subscriptionResponse = await _mpSubscriptionService.CreateSubscriptionAsync(
                    payloadForMp
                );

                _logger.LogInformation(
                    "Assinatura criada no MP com ID {ExternalId}",
                    subscriptionResponse.Id
                );

                // 4. Atualiza a entidade com dados do MP (ainda em memória)
                newSubscription.ExternalId = subscriptionResponse.Id;
                newSubscription.PayerMpId = subscriptionResponse.PayerId.ToString();
                newSubscription.PaymentMethodId = subscriptionResponse.PaymentMethodId;
                newSubscription.CurrentPeriodStartDate = subscriptionResponse
                    .AutoRecurring
                    .StartDate;
                newSubscription.CurrentPeriodEndDate = subscriptionResponse.AutoRecurring.EndDate;
                newSubscription.Status = subscriptionResponse.Status;

                // 5. ✅ COMMIT ÚNICO - ATOMICIDADE GARANTIDA
                // Persiste a subscription com TODOS os dados preenchidos (incluindo dados do MP)
                await _unitOfWork.CommitAsync();

                _logger.LogInformation(
                    "Assinatura {SubscriptionId} para o usuário {UserId} salva com sucesso. Status: {Status}",
                    newSubscription.ExternalId,
                    userId,
                    newSubscription.Status
                );

                return newSubscription;
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(
                    ex,
                    "Falha no MP ao criar assinatura para o usuário {UserId}. Rollback automático.",
                    userId
                );

                // ✅ ROLLBACK AUTOMÁTICO
                // Como não fizemos commit, o UnitOfWork descarta todas as mudanças automaticamente
                // Não precisa remover manualmente - o Entity Framework faz isso

                _logger.LogInformation(
                    "Rollback automático concluído. Assinatura para o usuário {UserId} NÃO foi persistida.",
                    userId
                );

                throw;
            }
            catch (DbUpdateException dbEx)
            {
                // Se falhar ao salvar no banco local APÓS sucesso no MP, temos um problema
                // Neste caso, precisamos cancelar no MP (compensação)
                _logger.LogError(
                    dbEx,
                    "Erro ao salvar assinatura local após sucesso no MP. Cancelando no MP..."
                );

                // Aqui subscriptionResponse está disponível porque só entra neste catch após sucesso do MP
                // Mas para evitar warning, vamos buscar o ExternalId da entidade
                if (string.IsNullOrEmpty(newSubscription.ExternalId) ||
                    newSubscription.ExternalId == planExternalId)
                    throw new AppServiceException(
                        "Erro de consistência de dados. A operação foi revertida.",
                        dbEx
                    );
                try
                {
                    await _mpSubscriptionService.CancelSubscriptionAsync(newSubscription.ExternalId);
                    _logger.LogInformation(
                        "Assinatura {ExternalId} cancelada no MP devido a erro de persistência local.",
                        newSubscription.ExternalId
                    );
                }
                catch (Exception cancelEx)
                {
                    _logger.LogError(
                        cancelEx,
                        "Falha ao cancelar assinatura {ExternalId} no MP. Intervenção manual necessária!",
                        newSubscription.ExternalId
                    );
                }

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
                    includePlan: false,
                    asNoTracking: false
                )
                ?? throw new ResourceNotFoundException(
                    $"Assinatura {subscriptionId} não encontrada."
                );

            var originalAmount = localSubscription.CurrentAmount;

            // Verifica se realmente precisa atualizar
            if (originalAmount == dto.TransactionAmount)
            {
                return await _mpSubscriptionService.GetSubscriptionByIdAsync(subscriptionId) ??
                       throw new InvalidOperationException();
            }

            // 1. Atualiza localmente (apenas em memória)
            localSubscription.CurrentAmount = (int)dto.TransactionAmount;

            try
            {
                // 2. Atualiza no MercadoPago primeiro
                var mpResponse = await _mpSubscriptionService.UpdateSubscriptionValueAsync(
                    subscriptionId,
                    dto
                );

                // 3. ✅ COMMIT ÚNICO - Se MP OK, persiste localmente
                await _unitOfWork.CommitAsync();

                // 4. Limpa cache
                await _cacheService.RemoveAsync($"SubscriptionDetails_{localSubscription.UserId}");

                _logger.LogInformation(
                    "Valor da assinatura {SubscriptionId} atualizado com sucesso. Novo valor: {NewAmount}",
                    subscriptionId,
                    dto.TransactionAmount
                );

                return mpResponse;
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(
                    ex,
                    "Falha no MP ao atualizar valor da assinatura {SubscriptionId}. Rollback automático.",
                    subscriptionId
                );

                // ✅ ROLLBACK AUTOMÁTICO
                // Como a entidade está rastreada mas não foi comitada, precisamos reverter manualmente
                localSubscription.CurrentAmount = originalAmount;

                _logger.LogInformation(
                    "Rollback concluído. Valor da assinatura {SubscriptionId} permanece em {OriginalAmount}",
                    subscriptionId,
                    originalAmount
                );

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

            // 1. Atualiza localmente (apenas em memória)
            localSubscription.Status = dto.Status;
            localSubscription.UpdatedAt = DateTime.UtcNow;

            try
            {
                // 2. Atualiza no MercadoPago primeiro
                var mpResponse = await _mpSubscriptionService.UpdateSubscriptionStatusAsync(
                    subscriptionId,
                    dto
                );

                // 3. ✅ COMMIT ÚNICO - Se MP OK, persiste localmente
                await _unitOfWork.CommitAsync();

                // 4. Limpa cache
                await _cacheService.RemoveAsync($"SubscriptionDetails_{localSubscription.UserId}");

                _logger.LogInformation(
                    "Status da assinatura {SubscriptionId} atualizado com sucesso. Novo status: {NewStatus}",
                    subscriptionId,
                    dto.Status
                );

                return mpResponse;
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(
                    ex,
                    "Falha no MP ao atualizar status da assinatura {SubscriptionId}. Rollback automático.",
                    subscriptionId
                );

                // ✅ ROLLBACK AUTOMÁTICO
                // Como a entidade está rastreada mas não foi comitada, precisamos reverter manualmente
                localSubscription.Status = originalStatus;

                _logger.LogInformation(
                    "Rollback concluído. Status da assinatura {SubscriptionId} permanece em {OriginalStatus}",
                    subscriptionId,
                    originalStatus
                );

                throw;
            }
        }

        // --- Métodos de Leitura e Helper ---

        public async Task<SubscriptionResponseDto> GetSubscriptionByIdAsync(string subscriptionId)
        {
            return await _mpSubscriptionService.GetSubscriptionByIdAsync(subscriptionId) ??
                   throw new InvalidOperationException();
        }

        /// <summary>
        /// Método auxiliar para ativar assinatura a partir de um pagamento único (Pix/Boleto).
        /// Usado quando o cliente paga sem criar uma assinatura recorrente no MP.
        /// </summary>
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
            await _unitOfWork.CommitAsync();

            _logger.LogInformation(
                "Assinatura ativada a partir de pagamento único. UserId: {UserId}, PaymentId: {PaymentId}",
                userId,
                paymentId
            );

            return newSubscription;
        }
    }
}