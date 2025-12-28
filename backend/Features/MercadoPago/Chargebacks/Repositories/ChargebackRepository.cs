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
        // Assume que você tem uma entidade Chargeback no seu ApiDbContext
        var query = _context.Chargebacks.Include(c => c.User).AsQueryable();

        // Aplicar filtros
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(c =>
                c.ChargebackId.ToString().Contains(searchTerm) || c.User.Name.Contains(searchTerm)
            );
        }

        if (!string.IsNullOrEmpty(statusFilter))
        {
            query = query.Where(c => c.Status.ToString() == statusFilter);
        }

        var totalCount = await query.CountAsync();

        var chargebacks = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (chargebacks, totalCount);
    }
}
