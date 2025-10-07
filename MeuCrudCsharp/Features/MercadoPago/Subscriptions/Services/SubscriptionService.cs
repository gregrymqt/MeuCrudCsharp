using System;
using System.Security.Claims;
using System.Text.Json;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Plans.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.Services
{
    /// <summary>
    /// Implements <see cref="ISubscriptionService"/> to manage the lifecycle of user subscriptions.
    /// This service orchestrates the creation and management of subscriptions by coordinating
    /// with the local database, the payment provider (Mercado Pago), and a caching layer.
    /// </summary>
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ILogger<SubscriptionService> _logger;
        private readonly ICacheService _cacheService;
        private readonly IUserContext _userContext;

        // NOVAS DEPENDÊNCIAS (Abstrações)
        private readonly IMercadoPagoSubscriptionService _mpSubscriptionService;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IPlanRepository _planRepository; // Assumindo que você tenha um repositório de Planos
        private readonly IUserRepository _userRepository; // Assumindo que você tenha um repositório de Usuários
        private readonly IClientService _clientService; // Este já estava bem separado!

        // O DbContext foi REMOVIDO daqui.
        public SubscriptionService(
            ILogger<SubscriptionService> logger,
            ICacheService cacheService,
            IUserContext userContext,
            IMercadoPagoSubscriptionService mpSubscriptionService,
            ISubscriptionRepository subscriptionRepository,
            IPlanRepository planRepository,
            IUserRepository userRepository,
            IClientService clientService)
        {
            _logger = logger;
            _cacheService = cacheService;
            _userContext = userContext;
            _mpSubscriptionService = mpSubscriptionService;
            _subscriptionRepository = subscriptionRepository;
            _planRepository = planRepository;
            _userRepository = userRepository;
            _clientService = clientService;
        }

        /// <inheritdoc />
        public async Task<Subscription> CreateSubscriptionAndCustomerIfNeededAsync(CreateSubscriptionDto createDto)
        {
            var userId = _userContext.GetCurrentUserId();
            var user = await _userRepository.GetByIdAsync(userId); // 1. Usa o repositório de usuários
            if (user == null) throw new AppServiceException("Usuário não encontrado.");

            // Lógica de negócio para criar o cliente se necessário
            if (string.IsNullOrEmpty(user.MercadoPagoCustomerId))
            {
                var newCustomer = await _clientService.CreateCustomerAsync(user.Email, user.Name);
                user.MercadoPagoCustomerId = newCustomer.Id;
                await _userRepository.SaveChangesAsync(); // Salva a atualização no usuário
            }

            var savedCard = await _clientService.AddCardToCustomerAsync(createDto.CardTokenId);

            // 2. Chama o serviço de API para criar a assinatura externa
            var subscriptionResponse = await _mpSubscriptionService.CreateSubscriptionAsync(
                new SubscriptionWithCardRequestDto(
                    createDto.PreapprovalPlanId, savedCard.Id,
                    new PayerRequestDto(createDto.PayerEmail, null, null, null)
                ));

            // 3. Usa o repositório de planos para buscar o plano local
            var localPlan = await _planRepository.GetActiveByExternalIdAsync(subscriptionResponse.PreapprovalPlanId);
            if (localPlan == null)
                throw new ResourceNotFoundException(
                    $"Plano com ID externo '{subscriptionResponse.PreapprovalPlanId}' não encontrado.");

            // Lógica de negócio para calcular datas e criar a entidade local
            var newSubscription = CreateSubscriptionEntity(userId, localPlan, savedCard, subscriptionResponse);

            // 4. Usa o repositório de assinaturas para persistir a nova entidade
            await _subscriptionRepository.AddAsync(newSubscription);
            await _subscriptionRepository.SaveChangesAsync();

            _logger.LogInformation("Assinatura {SubscriptionId} criada para o usuário {UserId}",
                newSubscription.ExternalId, userId);
            return newSubscription;
        }

        public async Task<Subscription> ActivateSubscriptionFromSinglePaymentAsync(
            string userId,
            Guid planPublicId,
            string paymentId,
            string payerEmail,
            string? lastFourCardDigits
        )
        {
            // CORREÇÃO 1: Usando o _planRepository para buscar o plano.
            var localPlan = await _planRepository.GetByPublicIdAsync(planPublicId);

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

            // CORREÇÃO 2: Usando o _subscriptionRepository para salvar.
            await _subscriptionRepository.AddAsync(newSubscription);
            await _subscriptionRepository.SaveChangesAsync();

            // CORREÇÃO 3 (Erro 'AppendFormatted'): Usando placeholders do log estruturado.
            _logger.LogInformation(
                "Assinatura {SubscriptionId} ativada para o usuário {UserId} via pagamento único {PaymentId}. Válida até {ExpirationDate}",
                newSubscription.Id, userId, paymentId, expirationDate
            );

            return newSubscription;
        }

        /// <summary>
        /// Busca dados de uma assinatura diretamente da API do Mercado Pago.
        /// </summary>
        public async Task<SubscriptionResponseDto> GetSubscriptionByIdAsync(string subscriptionId)
        {
            // CORREÇÃO: A lógica foi movida para o serviço de API. O serviço principal apenas delega a chamada.
            return await _mpSubscriptionService.GetSubscriptionByIdAsync(subscriptionId);
        }

        /// <summary>
        /// Atualiza o valor de uma assinatura na API externa e sincroniza o plano local.
        /// </summary>
        public async Task<SubscriptionResponseDto> UpdateSubscriptionValueAsync(
            string subscriptionId,
            UpdateSubscriptionValueDto dto
        )
        {
            _logger.LogInformation("Iniciando atualização de valor para assinatura MP: {SubscriptionId}",
                subscriptionId);

            // 1. Chama o serviço da API para fazer a atualização externa
            var mpSubscriptionResponse = await _mpSubscriptionService.UpdateSubscriptionValueAsync(subscriptionId, dto);

            // 2. CORREÇÃO: Usa o repositório para buscar a assinatura local e seu plano associado
            var localSubscription =
                await _subscriptionRepository.GetByExternalIdAsync(subscriptionId, includePlan: true);

            if (localSubscription?.Plan != null)
            {
                localSubscription.Plan.TransactionAmount = dto.TransactionAmount;
                // CORREÇÃO: Salva as mudanças através do repositório
                await _subscriptionRepository.SaveChangesAsync();

                _logger.LogInformation("Valor do plano local associado à assinatura {SubscriptionId} foi atualizado.",
                    subscriptionId);

                // CORREÇÃO: Invalida o cache APENAS se a assinatura local foi encontrada
                await _cacheService.RemoveAsync($"SubscriptionDetails_{localSubscription.UserId}");
            }
            else
            {
                _logger.LogWarning(
                    "Assinatura {SubscriptionId} atualizada no MP, mas o plano local não foi encontrado para sincronização.",
                    subscriptionId);
            }

            return mpSubscriptionResponse;
        }

        /// <inheritdoc />
        public async Task<SubscriptionResponseDto> UpdateSubscriptionStatusAsync(string subscriptionId,
            UpdateSubscriptionStatusDto dto)
        {
            // 1. Atualiza primeiro na fonte externa (API)
            var mpSubscriptionResponse =
                await _mpSubscriptionService.UpdateSubscriptionStatusAsync(subscriptionId, dto);

            // 2. Sincroniza o status no banco de dados local via repositório
            var localSubscription = await _subscriptionRepository.GetByExternalIdAsync(subscriptionId);
            if (localSubscription != null)
            {
                localSubscription.Status = dto.Status;
                localSubscription.UpdatedAt = DateTime.UtcNow;
                await _subscriptionRepository.SaveChangesAsync();
                await _cacheService.RemoveAsync($"SubscriptionDetails_{localSubscription.UserId}");
                _logger.LogInformation("Status da assinatura {SubscriptionId} atualizado para {Status} localmente.",
                    subscriptionId, dto.Status);
            }
            else
            {
                _logger.LogWarning(
                    "Assinatura {SubscriptionId} atualizada no MP, mas não encontrada localmente para sincronização.",
                    subscriptionId);
            }

            return mpSubscriptionResponse;
        }

        private Subscription CreateSubscriptionEntity(string userId, Plan localPlan, CardResponseDto savedCard,
            SubscriptionResponseDto subResponse)
        {
            var now = DateTime.UtcNow;
            var periodStartDate = subResponse.AutoRecurring?.StartDate ?? now;
            var periodEndDate = subResponse.NextPaymentDate ?? periodStartDate.AddMonths(localPlan.FrequencyInterval);

            return new Subscription
            {
                UserId = userId,
                PlanId = localPlan.Id,
                ExternalId = subResponse.Id,
                Status = subResponse.Status,
                PayerEmail = subResponse.PayerEmail,
                CreatedAt = now,
                LastFourCardDigits = savedCard.LastFourDigits,
                CurrentPeriodStartDate = periodStartDate,
                CurrentPeriodEndDate = periodEndDate,
                PaymentId = subResponse.Id
            };
        }
    }
}