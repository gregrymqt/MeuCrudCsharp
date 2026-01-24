using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Claims.Interfaces;
using MeuCrudCsharp.Models;
using MeuCrudCsharp.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.Repositories;

/// <summary>
/// Repository para gerenciar operações de persistência de Claims.
/// Segue o padrão Repository + UnitOfWork (não chama SaveChanges diretamente).
/// </summary>
public class ClaimRepository(ApiDbContext context) : IClaimRepository
{
    /// <summary>
    /// Busca uma claim pelo ID interno com dados do usuário incluídos.
    /// Usado para edição, então não usa AsNoTracking.
    /// </summary>
    public async Task<Models.Claims?> GetByIdAsync(long id)
    {
        return await context.Claims.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <summary>
    /// Busca paginada de claims com filtros opcionais.
    /// </summary>
    public async Task<(List<Models.Claims> Claims, int TotalCount)> GetPaginatedClaimsAsync(
        string? searchTerm,
        string? statusFilter,
        int page,
        int pageSize
    )
    {
        var query = context.Claims.Include(c => c.User).AsQueryable();

        // Filtro de busca textual
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(c =>
                c.MpClaimId.ToString().Contains(searchTerm) ||
                (c.User != null && !string.IsNullOrEmpty(c.User.Name) && c.User.Name.Contains(searchTerm))
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

    /// <summary>
    /// Atualiza o status de uma claim.
    /// O SaveChanges será chamado pelo UnitOfWork.
    /// </summary>
    public void UpdateClaimStatus(Models.Claims claim, InternalClaimStatus newStatus)
    {
        claim.Status = newStatus;
        context.Claims.Update(claim);
        // O SaveChanges será chamado pelo UnitOfWork
    }

    /// <summary>
    /// Marca uma claim existente para atualização.
    /// O SaveChanges será chamado pelo UnitOfWork.
    /// </summary>
    public void Update(Models.Claims claim)
    {
        context.Claims.Update(claim);
        // O SaveChanges será chamado pelo UnitOfWork
    }

    /// <summary>
    /// Adiciona uma nova claim ao contexto.
    /// O SaveChanges será chamado pelo UnitOfWork.
    /// </summary>
    public async Task AddAsync(Models.Claims claim)
    {
        await context.Claims.AddAsync(claim);
        // O SaveChanges será chamado pelo UnitOfWork
    }

    /// <summary>
    /// Verifica se uma claim já existe pelo ID do Mercado Pago.
    /// Usa AsNoTracking pois é apenas verificação.
    /// </summary>
    public async Task<bool> ExistsByMpClaimIdAsync(long mpClaimId)
    {
        return await context.Claims.AsNoTracking().AnyAsync(c => c.MpClaimId == mpClaimId);
    }

    /// <summary>
    /// Busca uma claim pelo ID do Mercado Pago.
    /// Não usa AsNoTracking para permitir rastreamento de mudanças.
    /// </summary>
    public async Task<Models.Claims?> GetByMpClaimIdAsync(long mpClaimId)
    {
        return await context.Claims.FirstOrDefaultAsync(c => c.MpClaimId == mpClaimId);
    }

    /// <summary>
    /// Busca todas as claims de um usuário específico.
    /// Usa AsNoTracking pois é apenas leitura.
    /// </summary>
    public async Task<List<Models.Claims>> GetClaimsByUserIdAsync(string userId)
    {
        return await context
            .Claims.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.DataCreated)
            .ToListAsync();
    }
}
