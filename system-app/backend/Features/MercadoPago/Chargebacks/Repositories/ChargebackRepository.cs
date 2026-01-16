using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Chargebacks.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Chargebacks.Repositories;

public class ChargebackRepository : IChargebackRepository
{
    private readonly ApiDbContext _context;

    public ChargebackRepository(ApiDbContext context)
    {
        _context = context;
    }

    public async Task<(List<Chargeback> Chargebacks, int TotalCount)> GetPaginatedChargebacksAsync(
        string? searchTerm,
        string? statusFilter,
        int page,
        int pageSize
    )
    {
        var query = _context.Chargebacks.Include(c => c.User).AsQueryable();

        // 1. Filtro Inteligente
        if (!string.IsNullOrEmpty(searchTerm))
        {
            if (long.TryParse(searchTerm, out var idSearch))
            {
                // Busca exata pelo ID ou parcial pelo nome
                query = query.Where(c =>
                    c.ChargebackId == idSearch || c.User.Name.Contains(searchTerm)
                );
            }
            else
            {
                // Busca apenas textual
                query = query.Where(c => c.User.Name.Contains(searchTerm));
            }
        }

        // 2. Filtro de Enum Corrigido
        if (
            !string.IsNullOrEmpty(statusFilter)
            && Enum.TryParse<ChargebackStatus>(statusFilter, true, out var statusEnum)
        )
        {
            query = query.Where(c => c.Status == statusEnum);
        }

        // Contagem antes da paginação
        var totalCount = await query.CountAsync();

        // Paginação
        var chargebacks = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (chargebacks, totalCount);
    }

    public async Task<bool> ExistsByExternalIdAsync(long chargebackId)
    {
        // Verifica se existe sem travar o registro (AsNoTracking)
        return await _context
            .Chargebacks.AsNoTracking()
            .AnyAsync(c => c.ChargebackId == chargebackId);
    }

    public async Task<Chargeback?> GetByExternalIdAsync(long chargebackId)
    {
        // Busca para edição, então NÃO usamos AsNoTracking aqui (o EF precisa rastrear mudanças)
        return await _context.Chargebacks.FirstOrDefaultAsync(c => c.ChargebackId == chargebackId);
    }

    public async Task AddAsync(Chargeback chargeback)
    {
        await _context.Chargebacks.AddAsync(chargeback);
        // O SaveChanges será chamado pelo UnitOfWork
    }

    public void Update(Chargeback chargeback)
    {
        _context.Chargebacks.Update(chargeback);
        // O SaveChanges será chamado pelo UnitOfWork
    }
}
