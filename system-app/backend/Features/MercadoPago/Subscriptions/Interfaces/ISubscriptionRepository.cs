using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;

public interface ISubscriptionRepository
{
    Task AddAsync(Subscription subscription);

    Task<Subscription?> GetByExternalIdAsync(
        string externalId,
        bool includePlan = false,
        bool asNoTracking = true
    );

    Task<Subscription?> GetByIdAsync(string subscriptionId);
    Task<Subscription?> GetByPaymentIdAsync(
        string paymentId,
        bool includePlan = false,
        bool includeUser = false
    );
    void Update(Subscription subscription);

    Task<Subscription?> GetActiveSubscriptionByUserIdAsync(string userId);
    Task<Subscription?> GetActiveSubscriptionByCustomerIdAsync(string customerId);

    Task<bool> HasActiveSubscriptionByUserIdAsync(string userId);
    Task<int> SaveChangesAsync();

    void Remove(Subscription subscription);
}
