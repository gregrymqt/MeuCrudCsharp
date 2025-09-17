using System;
using System.Security.Claims;
using System.Text.Json;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Clients.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.Subscriptions.DTOs;
using MeuCrudCsharp.Features.Subscriptions.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Subscriptions.Services
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
            ICacheService cacheService
        )
            : base(httpClient, logger)
        {
            _context = context;
            _clientService = clientService;
            _cacheService = cacheService;
        }

        /// <inheritdoc />
        public async Task<SubscriptionResponseDto> CreateSubscriptionAndCustomerIfNeededAsync(
            CreateSubscriptionDto createDto,
            ClaimsPrincipal users
        )
        {
            var userIdString = users.FindFirstValue(ClaimTypes.NameIdentifier);

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
                customerId,
                createDto.CardTokenId
            );

            var subscriptionResponse = await CreateSubscriptionAsync(
                createDto.PreapprovalPlanId,
                savedCard.Id,
                createDto.PayerEmail
            );

            var localPlan = await _context
                .Plans.AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.ExternalPlanId == subscriptionResponse.PreapprovalPlanId
                );

            if (localPlan == null)
            {
                throw new ResourceNotFoundException(
                    $"Plan with external ID '{subscriptionResponse.PreapprovalPlanId}' not found."
                );
            }

            var newSubscription = new Subscription
            {
                UserId = userIdString,
                PlanId = localPlan.Id,
                ExternalId = subscriptionResponse.Id,
                Status = subscriptionResponse.Status,
                PayerEmail = subscriptionResponse.PayerEmail,
                CreatedAt = DateTime.UtcNow,
                LastFourCardDigits = savedCard.LastFourDigits,
            };

            _context.Subscriptions.Add(newSubscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Subscription {SubscriptionId} created successfully for user {UserId}",
                newSubscription.ExternalId,
                userIdString
            );
            return subscriptionResponse;
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
            {
                TransactionAmount = dto.TransactionAmount,
            };

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
            var payload = new UpdateSubscriptionStatusDto { Status = dto.Status };
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
            var payload = new SubscriptionWithCardRequestDto
            {
                PreapprovalPlanId = preapprovalPlanId,
                CardId = cardId,
                Payer = new PayerRequestDto { Email = payerEmail },
            };
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
