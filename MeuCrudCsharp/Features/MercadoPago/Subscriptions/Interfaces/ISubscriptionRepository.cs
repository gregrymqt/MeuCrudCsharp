using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;

public interface ISubscriptionRepository
{
    Task AddAsync(Subscription subscription);
    
    Task<Subscription?> GetByExternalIdAsync(string externalId, bool includePlan = false, bool asNoTracking = true);

    Task<int> SaveChangesAsync();

    void Remove(Subscription subscription);
}