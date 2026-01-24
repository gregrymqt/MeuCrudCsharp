using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;

public interface ISubscriptionRepository
{
    // Métodos de escrita (não chamam SaveChanges - Service usa UnitOfWork)
    Task AddAsync(Subscription subscription);
    void Update(Subscription subscription);
    void Remove(Subscription subscription);

    // Métodos de leitura
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

    Task<Subscription?> GetActiveSubscriptionByUserIdAsync(string userId);
    Task<Subscription?> GetActiveSubscriptionByCustomerIdAsync(string customerId);

    Task<bool> HasActiveSubscriptionByUserIdAsync(string userId);
}
