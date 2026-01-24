using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.Repositories;

public class SubscriptionRepository(ApiDbContext context) : ISubscriptionRepository
{
    public async Task AddAsync(Subscription subscription)
    {
        await context.Subscriptions.AddAsync(subscription);
    }

    public async Task<Subscription?> GetByExternalIdAsync(
        string externalId,
        bool includePlan = false,
        bool asNoTracking = true
    )
    {
        IQueryable<Subscription> query = context.Subscriptions;

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (includePlan)
        {
            query = query.Include(s => s.Plan);
        }

        return await query.FirstOrDefaultAsync(s => s.ExternalId == externalId);
    }

    public async Task<Subscription?> GetActiveSubscriptionByUserIdAsync(string userId)
    {
        // Status que consideramos "Vigentes"
        var activeStatuses = new[] { "authorized", "pending", "paused" };

        return await context
            .Subscriptions.AsNoTracking() // Performance
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId && activeStatuses.Contains(s.Status))
            .OrderByDescending(s => s.CurrentPeriodEndDate) // Pega a mais recente
            .FirstOrDefaultAsync();
    }

    public async Task<Subscription?> GetActiveSubscriptionByCustomerIdAsync(string customerId)
    {
        // Status que consideramos "Vigentes"
        var activeStatuses = new[] { "authorized", "pending", "paused" };

        return await context
            .Subscriptions
            .Include(s => s.User) // ✅ Inclui User para envio de email
            .Where(s => 
                s.User != null && 
                s.User.CustomerId == customerId && 
                activeStatuses.Contains(s.Status))
            .OrderByDescending(s => s.CurrentPeriodEndDate) // Pega a mais recente
            .FirstOrDefaultAsync();
    }

    public void Remove(Subscription subscription)
    {
        context.Subscriptions.Remove(subscription);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync();
    }

    public Task<bool> HasActiveSubscriptionByUserIdAsync(string userId)
    {
        return context
            .Subscriptions.AsNoTracking()
            .AnyAsync(s =>
                s.UserId == userId
                && s.CurrentPeriodEndDate > DateTime.UtcNow
                && // [cite: 8]
                (s.Status == "paid" || s.Status == "authorized") // [cite: 8, 38]
            );
    }

    public async Task<Subscription?> GetByIdAsync(string subscriptionId)
    {
        // O Service passa o ID que está salvo na tabela de Pagamentos.
        // Geralmente é o ExternalId (ex: "2c938084...").
        return await context.Subscriptions.FirstOrDefaultAsync(s =>
            s.ExternalId == subscriptionId
        );
    }

    public void Update(Subscription subscription)
    {
        context.Subscriptions.Update(subscription);
    }
}
