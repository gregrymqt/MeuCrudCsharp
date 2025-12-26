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

    Task<Subscription?> GetActiveSubscriptionByUserIdAsync(string userId);

    Task<Subscription?> GetActiveByUserIdAsync(string userId);

    Task<bool> HasActiveSubscriptionByUserIdAsync(string userId);
    Task<int> SaveChangesAsync();

    void Remove(Subscription subscription);
}
