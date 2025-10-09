using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using MeuCrudCsharp.Features.Profiles.UserAccount.DTOs;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Services
{
  
    public class UserAccountService : IUserAccountService 
    {
        private readonly IUserAccountRepository _repository;
        private readonly ICacheService _cacheService;
        private readonly IMercadoPagoSubscriptionService _mpSubscriptionService;
        private readonly IClientService _clientService;
        private readonly ILogger<UserAccountService> _logger;
        private readonly IUserContext _userContext;

        public UserAccountService(
            IUserAccountRepository repository,
            ICacheService cacheService,
            IMercadoPagoSubscriptionService mpSubscriptionService,
            IClientService clientService,
            ILogger<UserAccountService> logger,
            IUserContext userContext)
        {
            _repository = repository;
            _cacheService = cacheService;
            _mpSubscriptionService = mpSubscriptionService;
            _clientService = clientService;
            _logger = logger;
            _userContext = userContext;
        }

        public async Task<UserProfileDto> GetUserProfileAsync(string userId)
        {
            return await _cacheService.GetOrCreateAsync($"UserProfile_{userId}", async () =>
            {
                var user = await _repository.GetUserByIdAsync(userId)
                           ?? throw new ResourceNotFoundException($"User with ID {userId} not found.");

                // Lógica de mapeamento pode ir para um Mapper ou ficar aqui se for simples
                return new UserProfileDto { Name = user.Name, Email = user.Email, AvatarUrl = user.AvatarUrl };
            }, TimeSpan.FromMinutes(15));
        }

        public async Task<SubscriptionDetailsDto?> GetUserSubscriptionDetailsAsync(string userId)
        {
            return await _cacheService.GetOrCreateAsync($"SubscriptionDetails_{userId}", async () =>
            {
                // Orquestração: 1. Busca dados locais
                var subscription = await _repository.GetActiveSubscriptionByUserIdAsync(userId);
                if (subscription?.Plan == null) return null;

                // Orquestração: 2. Busca dados externos
                var mpSubscription = await _mpSubscriptionService.GetSubscriptionByIdAsync(subscription.ExternalId);
                if (mpSubscription == null) return null;

                // Orquestração: 3. Combina os dados
                return new SubscriptionDetailsDto
                {
                    SubscriptionId = subscription.ExternalId,
                    PlanName = subscription.Plan.Name,
                    Status = mpSubscription.Status,
                    Amount = subscription.Plan.TransactionAmount,
                    NextBillingDate = mpSubscription.NextPaymentDate,
                    LastFourCardDigits = subscription.LastFourCardDigits,
                };
            });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Payments>> GetUserPaymentHistoryAsync(string userId, int pageNumber = 1,
            int pageSize = 10)
        {
            try
            {
                // A chave de cache agora inclui a página e o tamanho
                return await _cacheService.GetOrCreateAsync(
                    $"PaymentHistory_{userId}_Page{pageNumber}_Size{pageSize}",
                    async () =>
                    {
                        return await _repository.GetPaymentHistoryByUserIdAsync(userId, pageNumber, pageSize);
                    },
                    TimeSpan.FromMinutes(10)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payment history for user {UserId}.", userId);
                // ✅ Sugestão: Usar AppServiceException para consistência
                throw new AppServiceException(
                    "An error occurred while fetching your payment history.",
                    ex
                );
            }
        }

        public async Task<bool> UpdateSubscriptionCardAsync(string newCardToken)
        {
            var userId = await _userContext.GetCurrentUserId();
            var user = await _repository.GetUserByIdAsync(userId);

            var subscription = await _repository.GetActiveSubscriptionByUserIdAsync(userId)
                               ?? throw new ResourceNotFoundException("Active subscription not found for card update.");

            await _mpSubscriptionService.UpdateSubscriptionCardAsync(subscription.ExternalId, newCardToken);

            await _cacheService.RemoveAsync($"SubscriptionDetails_{userId}");
            _logger.LogInformation("Card for subscription {SubscriptionId} updated.", subscription.ExternalId);

            // A lógica secundária permanece como responsabilidade do serviço orquestrador
            try
            {
                await _clientService.AddCardToCustomerAsync(user.MercadoPagoCustomerId,newCardToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Card updated on subscription but failed to add to customer profile.");
            }

            return true;
        }

        public async Task<bool> UpdateSubscriptionStatusAsync(string newStatus)
        {
            var userId = await _userContext.GetCurrentUserId();
            var allowed = new[] { "paused", "authorized", "cancelled" };
            if (!allowed.Contains(newStatus))
                throw new AppServiceException("Invalid subscription status.");

            var subscription = await _repository.GetActiveSubscriptionByUserIdAsync(userId)
                               ?? throw new ResourceNotFoundException(
                                   "Active subscription not found for status update.");

            var dto = new UpdateSubscriptionStatusDto(newStatus);
            var result = await _mpSubscriptionService.UpdateSubscriptionStatusAsync(subscription.ExternalId, dto);

            if (result.Status == newStatus)
            {
                subscription.Status = (newStatus == "authorized") ? "active" : newStatus;
                subscription.UpdatedAt = DateTime.UtcNow;
                await _repository.SaveChangesAsync();
                await _cacheService.RemoveAsync($"SubscriptionDetails_{userId}");
                _logger.LogInformation("Subscription {Id} updated to {Status}.", subscription.ExternalId,
                    subscription.Status);
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public async Task<PaymentReceiptDto> GetPaymentForReceiptAsync(string userId, string paymentId)
        {
            var payment = await _repository.GetPaymentByIdAndUserIdAsync(userId, paymentId)
                          ?? throw new ResourceNotFoundException(
                              $"Pagamento {paymentId} não encontrado para o usuário.");

            // Mapeamento da entidade para o DTO
            return new PaymentReceiptDto(
                payment.Id,
                payment.CreatedAt,
                payment.Amount,
                payment.Status,
                payment.User.Name,
                payment.CustomerCpf,
                payment.LastFourDigits // Supondo que a relação User foi incluída no repositório
            );
        }
    }
}