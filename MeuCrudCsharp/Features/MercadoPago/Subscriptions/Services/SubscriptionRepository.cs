using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.Services;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly ApiDbContext _context;

    public SubscriptionRepository(ApiDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Subscription subscription)
    {
        await _context.Subscriptions.AddAsync(subscription);
    }

    public async Task<Subscription?> GetByExternalIdAsync(string externalId, bool includePlan = false, bool asNoTracking = true)
    {
        IQueryable<Subscription> query = _context.Subscriptions;

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

    public void Remove(Subscription subscription)
    {
        _context.Subscriptions.Remove(subscription);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}