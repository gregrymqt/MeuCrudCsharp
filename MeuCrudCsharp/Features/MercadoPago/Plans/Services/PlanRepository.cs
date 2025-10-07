using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Plans.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Plans.Services;

public class PlanRepository : IPlanRepository
{
    private readonly ApiDbContext _context; // Seu DbContext

    public PlanRepository(ApiDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Plan plan)
    {
        await _context.Plans.AddAsync(plan);
    }

    public void Update(Plan plan)
    {
        _context.Plans.Update(plan);
    }
    
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
    
    public async Task<Plan?> GetByPublicIdAsync(Guid publicId)
    {
        return await _context.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PublicId == publicId);
    }

    public async Task<Plan?> GetActiveByExternalIdAsync(string externalId)
    {
        return await _context.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsActive && p.ExternalPlanId == externalId);
    }

    // NOVO MÉTODO - Lógica movida do PlanService
    public async Task<List<Plan>> GetActivePlansAsync()
    {
        return await _context.Plans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.TransactionAmount)
            .ToListAsync();
    }

    public async Task<List<Plan>> GetByExternalIdsAsync(IEnumerable<string> externalIds)
    {
        return await _context.Plans
            .Where(p => externalIds.Contains(p.ExternalPlanId))
            .ToListAsync();
    }
    
}