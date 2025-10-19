using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Claims.Interfaces;
using MeuCrudCsharp.Models;
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
        return await _context.Claims.FindAsync(id);
    }

    public async Task<(List<Models.Claims> Claims, int TotalCount)> GetPaginatedClaimsAsync(
        string? searchTerm,
        string? statusFilter,
        int page,
        int pageSize
    )
    {
        var query = _context
            .Claims.Include(c => c.User) // Inclui o usuário para poder filtrar pelo nome
            .AsQueryable();

        // Aplicar filtros
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(c =>
                c.ClaimId.Contains(searchTerm) || c.User.Name.Contains(searchTerm)
            );
        }

        if (!string.IsNullOrEmpty(statusFilter))
        {
            query = query.Where(c => c.Status.ToString().ToLower() == statusFilter);
        }

        // Obter a contagem total de itens antes da paginação
        var totalCount = await query.CountAsync();

        // Aplicar ordenação e paginação
        var claims = await query
            .OrderByDescending(c => c.DataCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (claims, totalCount);
    }

    public async Task UpdateClaimStatusAsync(Models.Claims claim, string newStatus)
    {
        // Tenta converter a string do novo status para o enum correspondente.
        // O 'true' ignora a diferença entre maiúsculas e minúsculas.
        if (Enum.TryParse<ClaimStatus>(newStatus, true, out var parsedStatus))
        {
            claim.Status = parsedStatus;
            _context.Claims.Update(claim);
            await _context.SaveChangesAsync();
        }
    }
}
