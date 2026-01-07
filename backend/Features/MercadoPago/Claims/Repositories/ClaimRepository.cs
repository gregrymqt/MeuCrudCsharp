using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Claims.Interfaces;
using MeuCrudCsharp.Models;
using MeuCrudCsharp.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.Repositories;

public class ClaimRepository : IClaimRepository
{
    private readonly ApiDbContext _context;

    public ClaimRepository(ApiDbContext context)
    {
        _context = context;
    }

    public async Task<Models.Claims?> GetByIdAsync(long id)
    {
        return await _context.Claims.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<(List<Models.Claims> Claims, int TotalCount)> GetPaginatedClaimsAsync(
        string? searchTerm,
        string? statusFilter,
        int page,
        int pageSize
    )
    {
        var query = _context.Claims.Include(c => c.User).AsQueryable();

        // 1. Correção do Erro CS8602 (Dereference of possibly null reference)
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(c =>
                c.MpClaimId.ToString().Contains(searchTerm)
                || (c.User != null && c.User.Name.Contains(searchTerm)) // Verificação de nulo adicionada
            );
        }

        if (!string.IsNullOrEmpty(statusFilter))
        {
            // Filtra convertendo o Enum do banco para string para comparar
            query = query.Where(c => c.Status.ToString() == statusFilter);
        }

        var totalCount = await query.CountAsync();

        var claims = await query
            .OrderByDescending(c => c.DataCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (claims, totalCount);
    }

    public async Task UpdateClaimStatusAsync(Models.Claims claim, string newStatus)
    {
        // 2. Correção do Erro CS0266 (Conversão de Enum incorreta)
        // O seu model usa 'InternalClaimStatus', não 'MpClaimStatus'
        if (Enum.TryParse<InternalClaimStatus>(newStatus, true, out var parsedStatus))
        {
            claim.Status = parsedStatus;
            _context.Claims.Update(claim);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Models.Claims>> GetClaimsByUserIdAsync(string userId)
    {
        return await _context
            .Claims.Where(c => c.UserId == userId)
            .OrderByDescending(c => c.DataCreated)
            .ToListAsync();
    }
}
