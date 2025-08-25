using System.Text.Json;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Caching;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.Profiles.UserAccount.DTOs;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using MeuCrudCsharp.Features.Subscriptions.DTOs;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Services
{
    /// <summary>
    /// Implements <see cref="IUserAccountService"/> to manage user account operations.
    /// This service orchestrates data retrieval and actions related to user profiles, subscriptions,
    /// and payments, interacting with the database, cache, and the Mercado Pago API.
    /// </summary>
    public class UserAccountService : MercadoPagoServiceBase, IUserAccountService
    {
        private readonly ApiDbContext _context;
        private readonly ICacheService _cacheService;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserAccountService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="cacheService">The caching service for performance optimization.</param>
        /// <param name="httpClient">The HTTP client for making API requests, passed to the base class.</param>
        /// <param name="logger">The logger for recording events and errors, passed to the base class.</param>
        public UserAccountService(
            ApiDbContext context,
            ICacheService cacheService,
            HttpClient httpClient,
            ILogger<UserAccountService> logger
        )
            : base(httpClient, logger)
        {
            _context = context;
            _cacheService = cacheService;
        }

        /// <inheritdoc />
        public async Task<UserProfileDto> GetUserProfileAsync(string userId)
        {
            string cacheKey = $"UserProfile_{userId}";
            return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    try
                    {
                        var user = await _context
                            .Users.AsNoTracking()
                            .FirstOrDefaultAsync(u => u.Id == userId.ToString());
                        if (user == null)
                            throw new ResourceNotFoundException(
                                $"User with ID {userId} not found."
                            );

                        return new UserProfileDto
                        {
                            Name = user.Name,
                            Email = user.Email,
                            AvatarUrl = user.AvatarUrl,
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error fetching user profile {UserId} from the database.",
                            userId
                        );
                        throw new AppServiceException(
                            "An error occurred while fetching the profile data.",
                            ex
                        );
                    }
                },
                TimeSpan.FromMinutes(15)
            );
        }

        /// <inheritdoc />
        public async Task<SubscriptionDetailsDto?> GetUserSubscriptionDetailsAsync(string userId)
        {
            string cacheKey = $"SubscriptionDetails_{userId}";
            return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    try
                    {
                        var subscription = await _context
                            .Subscriptions.AsNoTracking()
                            .Include(s => s.Plan)
                            .FirstOrDefaultAsync(s =>
                                s.UserId == userId && s.Status != "cancelled"
                            );

                        if (subscription?.Plan == null)
                            return null;

                        var endpoint = $"/preapproval/{subscription.ExternalId}";
                        var responseBody = await SendMercadoPagoRequestAsync(
                            HttpMethod.Get,
                            endpoint,
                            (object?)null
                        );
                        var mpSubscription = JsonSerializer.Deserialize<SubscriptionResponseDto>(
                            responseBody
                        );

                        if (mpSubscription == null)
                            return null;

                        return new SubscriptionDetailsDto
                        {
                            SubscriptionId = subscription.ExternalId,
                            PlanName = subscription.Plan.Name,
                            Status = mpSubscription.Status,
                            Amount = subscription.Plan.TransactionAmount,
                            NextBillingDate = mpSubscription.NextBillingDate,
                            LastFourCardDigits = mpSubscription.Card?.LastFourDigits,
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error fetching subscription details for user {UserId}.",
                            userId
                        );
                        throw new AppServiceException(
                            "An error occurred while fetching your subscription details.",
                            ex
                        );
                    }
                }
            );
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Payments>> GetUserPaymentHistoryAsync(string userId)
        {
            try
            {
                string? cacheKey = $"PaymentHistory_{userId}";
                return await _cacheService.GetOrCreateAsync(
                    cacheKey,
                    async () =>
                    {
                        return await _context
                            .Payments.AsNoTracking()
                            .Where(p => p.UserId == userId)
                            .OrderByDescending(p => p.CreatedAt)
                            .ToListAsync();
                    },
                    TimeSpan.FromMinutes(10)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payment history for user {UserId}.", userId);
                throw new ResourceNotFoundException(
                    "An error occurred while fetching your payment history.",
                    ex
                );
            }
        }

        /// <inheritdoc />
        public async Task<bool> UpdateSubscriptionCardAsync(string userId, string newCardToken)
        {
            try
            {
                var subscription = await FindActiveSubscriptionAsync(userId, "for card update");

                var endpoint = $"/preapproval/{subscription.ExternalId}";
                var payload = new UpdateCardTokenDto { NewCardToken = newCardToken };
                await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);

                await _cacheService.RemoveAsync($"SubscriptionDetails_{userId}");

                _logger.LogInformation(
                    "Card for subscription {SubscriptionId} of user {UserId} was updated.",
                    subscription.ExternalId,
                    userId
                );
                return true;
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(
                    ex,
                    "External API error while trying to update the card for user {UserId}.",
                    userId
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while updating the card for user {UserId}.",
                    userId
                );
                throw new AppServiceException("An error occurred while updating your card.", ex);
            }
        }

        /// <inheritdoc />
        public async Task<bool> CancelSubscriptionAsync(string userId)
        {
            try
            {
                var subscription = await FindActiveSubscriptionAsync(userId, "for cancellation");

                var endpoint = $"/preapproval/{subscription.ExternalId}";
                var payload = new { status = "cancelled" };
                var responseBody = await SendMercadoPagoRequestAsync(
                    HttpMethod.Put,
                    endpoint,
                    payload
                );
                var result = JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody);

                if (result?.Status == "cancelled")
                {
                    subscription.Status = "cancelled";
                    subscription.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await _cacheService.RemoveAsync($"SubscriptionDetails_{userId}");
                    _logger.LogInformation(
                        "Subscription {SubscriptionId} for user {UserId} was cancelled.",
                        subscription.ExternalId,
                        userId
                    );
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while cancelling the subscription for user {UserId}.",
                    userId
                );
                throw new AppServiceException(
                    "An error occurred while cancelling your subscription.",
                    ex
                );
            }
        }

        /// <inheritdoc />
        public async Task<bool> ReactivateSubscriptionAsync(string userId)
        {
            try
            {
                var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
                    s.UserId == userId && s.Status == "paused"
                );

                if (subscription == null)
                    return false; // Not an error, just nothing to reactivate.

                var endpoint = $"/preapproval/{subscription.ExternalId}";
                var payload = new { status = "authorized" };
                var responseBody = await SendMercadoPagoRequestAsync(
                    HttpMethod.Put,
                    endpoint,
                    payload
                );
                var result = JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody);

                if (result?.Status == "authorized")
                {
                    subscription.Status = "active";
                    subscription.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await _cacheService.RemoveAsync($"SubscriptionDetails_{userId}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while reactivating the subscription for user {UserId}.",
                    userId
                );
                throw new AppServiceException(
                    "An error occurred while reactivating your subscription.",
                    ex
                );
            }
        }

        /// <inheritdoc />
        public async Task<Payments> GetPaymentForReceiptAsync(string userId, string paymentId)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching payment {PaymentId} for user {UserId}.",
                    paymentId,
                    userId
                );

                var payment = await _context
                    .Payments.AsNoTracking()
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId);

                if (payment == null)
                {
                    throw new ResourceNotFoundException(
                        $"Payment with ID {paymentId} not found or does not belong to user {userId}."
                    );
                }

                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching payment {PaymentId} from the database.",
                    paymentId
                );
                throw new AppServiceException(
                    "An error occurred while fetching your payment data.",
                    ex
                );
            }
        }

        /// <summary>
        /// Finds a user's active or paused subscription.
        /// </summary>
        /// <param name="userId">The user's unique identifier.</param>
        /// <param name="action">A description of the action being performed, for logging and error messages.</param>
        /// <returns>The user's active or paused <see cref="Subscription"/> entity.</returns>
        /// <exception cref="ResourceNotFoundException">Thrown if no active or paused subscription is found.</exception>
        private async Task<Subscription> FindActiveSubscriptionAsync(string userId, string action)
        {
            var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
                s.UserId == userId && (s.Status == "active" || s.Status == "paused")
            );

            if (subscription == null)
                throw new ResourceNotFoundException($"Active subscription not found {action}.");

            return subscription;
        }
    }
}
