using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Plans.DTOs;
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

    public async Task AddAsync(Plan plan) =>
        await _context.Plans.AddAsync(plan);


    public void Update(Plan plan) =>
        _context.Plans.Update(plan);


    public async Task<int> SaveChangesAsync() =>
        await _context.SaveChangesAsync();

    public void Remove(object payload) =>
         _context.Remove(payload);


    public async Task<Plan?> GetByPublicIdAsync(Guid publicId, bool asNoTracking = true) // 'true' é um bom padrão para leituras
    {
        // 1. Inicia a consulta base. Neste ponto, nada foi executado no banco.
        IQueryable<Plan> query = _context.Plans;

        // 2. Aplica o AsNoTracking() APENAS SE a variável for true.
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        // 3. Executa a consulta final no banco de dados com a condição.
        return await query.FirstOrDefaultAsync(p => p.PublicId == publicId);
    }


    public async Task<Plan?> GetActiveByExternalIdAsync(string externalId) =>
        await _context.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsActive && p.ExternalPlanId == externalId);


    // NOVO MÉTODO - Lógica movida do PlanService
    public async Task<PagedResultDto<Plan>> GetActivePlansAsync(int page, int pageSize)
    {
        var query = _context.Plans
            .AsNoTracking()
            .Where(p => p.IsActive == true);

        // 1. Obtenha a contagem TOTAL antes de paginar
        var totalCount = await query.CountAsync();

        // 2. Aplique a ordenação e a paginação
        var items = await query
            .OrderBy(p => p.TransactionAmount)
            .Skip((page - 1) * pageSize) // Pula os itens das páginas anteriores
            .Take(pageSize)              // Pega o número de itens para a página atual
            .ToListAsync();

        // 3. Retorne o objeto de resultado paginado
        return new PagedResultDto<Plan>(items, page, pageSize, totalCount);
    }


    public async Task<List<Plan>> GetByExternalIdsAsync(IEnumerable<string> externalIds) =>
        await _context.Plans
            .Where(p => externalIds.Contains(p.ExternalPlanId))
            .ToListAsync();
}