using System.Text.Json;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;
using MeuCrudCsharp.Features.Profiles.UserAccount.DTOs;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

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
        private readonly IClientService _clientService;

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
            IHttpClientFactory httpClient,
            ILogger<UserAccountService> logger,
            IClientService clientService
        )
            : base(httpClient, logger)
        {
            _context = context;
            _cacheService = cacheService;
            _clientService = clientService;
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
                try
                {
                        await _clientService.AddCardToCustomerAsync(newCardToken);
                        _logger.LogInformation(
                            "Sucesso na operação secundária: Novo cartão também foi adicionado ao perfil do cliente no MP."
                        );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "A atualização do cartão na assinatura do usuário {UserId} funcionou, mas falhou ao adicionar o mesmo cartão ao seu perfil de cliente. Isso não impacta a assinatura atual.",
                        userId
                    );
                }

                await _cacheService.RemoveAsync($"SubscriptionDetails_{userId}");
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

        /// <summary>
        /// Método unificado para atualizar o status de uma assinatura (pausar, reativar, cancelar).
        /// </summary>
        /// <param name="userId">O ID do usuário logado.</param>
        /// <param name="newStatus">O novo status desejado ('paused', 'authorized', 'cancelled').</param>
        /// <returns>True se a operação for bem-sucedida.</returns>
        public async Task<bool> UpdateSubscriptionStatusAsync(string userId, string newStatus)
        {
            // 1. Validação do status recebido para segurança
            var allowedStatuses = new[] { "paused", "authorized", "cancelled" };
            if (!allowedStatuses.Contains(newStatus))
            {
                _logger.LogWarning(
                    "Tentativa de atualização de assinatura com status inválido '{Status}' para o usuário {UserId}.",
                    newStatus, userId);
                throw new AppServiceException("Status de assinatura inválido.");
            }

            try
            {
                // 2. Encontra a assinatura do usuário no seu banco de dados
                var subscription = await FindActiveSubscriptionAsync(userId, $"for status update to '{newStatus}'");

                // 3. Prepara e envia a requisição para o Mercado Pago
                var endpoint = $"/preapproval/{subscription.ExternalId}";
                var payload = new { status = newStatus }; // Usa o status recebido como parâmetro
                var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);
                var result = JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody);

                // 4. Verifica a resposta do Mercado Pago e atualiza seu banco de dados
                if (result?.Status == newStatus)
                {
                    // Mapeia o status do MP para o status do seu sistema (ex: "authorized" -> "active")
                    subscription.Status = (newStatus == "authorized") ? "active" : newStatus;
                    subscription.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await _cacheService.RemoveAsync($"SubscriptionDetails_{userId}");

                    _logger.LogInformation(
                        "Assinatura {SubscriptionId} do usuário {UserId} atualizada para o status '{Status}'.",
                        subscription.ExternalId,
                        userId,
                        subscription.Status
                    );
                    return true;
                }

                _logger.LogWarning(
                    "Mercado Pago não confirmou a atualização de status para '{Status}' para a assinatura {SubscriptionId}.",
                    newStatus,
                    subscription.ExternalId
                );
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado ao tentar atualizar o status da assinatura para '{Status}' para o usuário {UserId}.",
                    newStatus,
                    userId
                );
                throw new AppServiceException($"Ocorreu um erro ao tentar atualizar sua assinatura para '{newStatus}'.",
                    ex);
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