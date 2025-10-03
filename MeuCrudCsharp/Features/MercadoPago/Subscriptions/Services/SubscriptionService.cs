using System;
using System.Security.Claims;
using System.Text.Json;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
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
    public class SubscriptionService : MercadoPagoServiceBase, ISubscriptionService
    {
        private readonly ApiDbContext _context;
        private readonly IClientService _clientService;
        private readonly ICacheService _cacheService;
        private readonly IUserContext _userContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for making API requests, passed to the base class.</param>
        /// <param name="logger">The logger for recording events and errors.</param>
        /// <param name="context">The database context.</param>
        /// <param name="clientService">The service for managing payment provider customers and cards.</param>
        /// <param name="cacheService">The caching service for performance optimization.</param>
        public SubscriptionService(
            IHttpClientFactory httpClient,
            ILogger<SubscriptionService> logger,
            ApiDbContext context,
            IClientService clientService,
            ICacheService cacheService,
            IUserContext userContext
        )
            : base(httpClient, logger)
        {
            _context = context;
            _clientService = clientService;
            _cacheService = cacheService;
            _userContext = userContext;
        }

        /// <inheritdoc />
        public async Task<Subscription> CreateSubscriptionAndCustomerIfNeededAsync(
            CreateSubscriptionDto createDto,
            ClaimsPrincipal users
        )
        {
            var userIdString = _userContext.GetCurrentUserId();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userIdString);
            if (user == null)
            {
                throw new AppServiceException("User not found.");
            }

            string customerId = user.MercadoPagoCustomerId;

            if (string.IsNullOrEmpty(customerId))
            {
                _logger.LogInformation(
                    "User {UserId} does not have a customer in the payment provider. Creating one now...",
                    userIdString
                );

                var newCustomer = await _clientService.CreateCustomerAsync(user.Email, user.Name);
                customerId = newCustomer.Id;
                user.MercadoPagoCustomerId = customerId;
            }

            var savedCard = await _clientService.AddCardToCustomerAsync(
                createDto.CardTokenId
            );

            var subscriptionResponse = await CreateSubscriptionAsync(
                createDto.PreapprovalPlanId,
                savedCard.Id,
                createDto.PayerEmail
            );

            var localPlan = await _context
                .Plans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ExternalPlanId == subscriptionResponse.PreapprovalPlanId);

            if (localPlan == null)
            {
                throw new ResourceNotFoundException($"Plan with external ID '{subscriptionResponse.PreapprovalPlanId}' not found.");
            }

            // LÓGICA MOVIDA DO OUTRO MÉTODO PARA CÁ:
            var now = DateTime.UtcNow;
            var periodStartDate = subscriptionResponse.AutoRecurring?.StartDate ?? now;
            DateTime periodEndDate;

            if (subscriptionResponse.NextPaymentDate.HasValue)
            {
                periodEndDate = subscriptionResponse.NextPaymentDate.Value;
            }
            else
            {
                periodEndDate = periodStartDate.AddMonths(localPlan.FrequencyInterval);
            }
    
            // Agora criamos a entidade Subscription completa, com todos os dados.
            var newSubscription = new Subscription
            {
                UserId = userIdString,
                PlanId = localPlan.Id,
                ExternalId = subscriptionResponse.Id,
                Status = subscriptionResponse.Status,
                PayerEmail = subscriptionResponse.PayerEmail,
                CreatedAt = now,
                LastFourCardDigits = savedCard.LastFourDigits,
                CurrentPeriodStartDate = periodStartDate,
                CurrentPeriodEndDate = periodEndDate,
                // O PaymentId do primeiro pagamento pode vir do webhook ou de uma consulta posterior.
                // Por enquanto, podemos deixá-lo nulo ou usar o ID da assinatura.
                PaymentId = subscriptionResponse.Id 
            };

            _context.Subscriptions.Add(newSubscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Subscription {SubscriptionId} created successfully for user {UserId}",
                newSubscription.ExternalId,
                userIdString
            );

            // Retornamos a entidade que acabamos de criar no nosso banco.
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
            // 1. Encontrar o plano no nosso banco de dados
            var localPlan = await _context.Plans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PublicId == planPublicId);

            if (localPlan == null)
            {
                _logger.LogError("Tentativa de ativar assinatura para um plano inexistente. PublicId: {PlanPublicId}",
                    planPublicId);
                throw new ResourceNotFoundException($"Plano com ID '{planPublicId}' não encontrado.");
            }

            var now = DateTime.UtcNow;

            // 2. Calcular a data de expiração com base na duração do plano
            var expirationDate = now.AddMonths(localPlan.FrequencyInterval); // Supondo que a frequência seja em meses

            // 3. Criar a nova entidade de assinatura local
            var newSubscription = new Subscription
            {
                UserId = userId,
                PlanId = localPlan.Id,
                ExternalId = paymentId, // Para pagamentos únicos, o ExternalId pode ser o próprio PaymentId
                Status = "active",
                PayerEmail = payerEmail,
                PaymentId = paymentId,
                CreatedAt = now,
                CurrentPeriodStartDate = now,
                CurrentPeriodEndDate = expirationDate,
                LastFourCardDigits = lastFourCardDigits 
            };

            _context.Subscriptions.Add(newSubscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Assinatura {SubscriptionId} ativada para o usuário {UserId} via pagamento único {PaymentId}. Válida até {ExpirationDate}",
                newSubscription.Id,
                userId,
                paymentId,
                expirationDate
            );

            return newSubscription;
        }

        /// <inheritdoc />
        public async Task<SubscriptionResponseDto> GetSubscriptionByIdAsync(string subscriptionId)
        {
            var endpoint = $"/preapproval/{subscriptionId}";
            var responseBody = await SendMercadoPagoRequestAsync(
                HttpMethod.Get,
                endpoint,
                (object?)null
            );
            return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
                   ?? throw new AppServiceException("Failed to deserialize subscription data.");
        }

        /// <inheritdoc />
        public async Task<SubscriptionResponseDto> UpdateSubscriptionValueAsync(
            string subscriptionId,
            UpdateSubscriptionValueDto dto
        )
        {
            _logger.LogInformation(
                "Initiating value update for MP subscription: {SubscriptionId}",
                subscriptionId
            );

            var endpoint = $"/v1/preapproval/{subscriptionId}";

            var payload = new UpdateSubscriptionValueDto
            (
                dto.TransactionAmount
            );

            var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);
            var mpSubscriptionResponse =
                JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
                ?? throw new AppServiceException(
                    "Failed to deserialize the subscription update response."
                );

            // Sync the change with the local database (optional but recommended).
            var localSubscription = await _context
                .Subscriptions.Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.ExternalId == subscriptionId);

            if (localSubscription?.Plan != null)
            {
                localSubscription.Plan.TransactionAmount = dto.TransactionAmount;
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Local plan value associated with subscription {SubscriptionId} has been updated.",
                    subscriptionId
                );
            }
            else
            {
                _logger.LogWarning(
                    "Subscription {SubscriptionId} updated in MP, but the local plan was not found for synchronization.",
                    subscriptionId
                );
            }

            await _cacheService.RemoveAsync($"SubscriptionDetails_{localSubscription.UserId}");

            return mpSubscriptionResponse;
        }

        /// <inheritdoc />
        public async Task<SubscriptionResponseDto> UpdateSubscriptionStatusAsync(
            string subscriptionId,
            UpdateSubscriptionStatusDto dto
        )
        {
            var endpoint = $"/v1/preapproval/{subscriptionId}";
            var payload = new UpdateSubscriptionStatusDto(dto.Status);
            var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);
            var mpSubscriptionResponse =
                JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
                ?? throw new AppServiceException(
                    "Failed to deserialize the status update response."
                );

            var localSubscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
                s.ExternalId == subscriptionId
            );

            if (localSubscription != null)
            {
                localSubscription.Status = dto.Status;
                localSubscription.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await _cacheService.RemoveAsync($"SubscriptionDetails_{localSubscription.UserId}");
                _logger.LogInformation(
                    "Status for subscription {SubscriptionId} updated to {Status} in the local database.",
                    subscriptionId,
                    dto.Status
                );
            }
            else
            {
                _logger.LogWarning(
                    "Subscription {SubscriptionId} was updated in Mercado Pago, but was not found in the local database for synchronization.",
                    subscriptionId
                );
            }

            return mpSubscriptionResponse;
        }

        /// <summary>
        /// A private helper method to encapsulate the API call for creating a subscription.
        /// </summary>
        /// <param name="preapprovalPlanId">The ID of the pre-approval plan.</param>
        /// <param name="cardId">The ID of the pre-saved card.</param>
        /// <param name="payerEmail">The email of the payer.</param>
        /// <returns>The response DTO from the subscription creation API call.</returns>
        /// <exception cref="AppServiceException">Thrown if the API response cannot be deserialized.</exception>
        private async Task<SubscriptionResponseDto> CreateSubscriptionAsync(
            string preapprovalPlanId,
            string cardId,
            string payerEmail
        )
        {
            const string endpoint = "/preapproval";
            var payload = new SubscriptionWithCardRequestDto(
                preapprovalPlanId,
                cardId,
                new PayerRequestDto(payerEmail, null, null, null) // <-- Criação direta
            );
            var responseBody = await SendMercadoPagoRequestAsync(
                HttpMethod.Post,
                endpoint,
                payload
            );
            return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
                   ?? throw new AppServiceException(
                       "Failed to deserialize the subscription creation response."
                   );
        }
    }
}