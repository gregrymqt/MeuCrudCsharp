using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Chargebacks.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Chargebacks.Repositories;

/// <summary>
/// Repository para gerenciar operações de persistência de Chargebacks.
/// Segue o padrão Repository + UnitOfWork (não chama SaveChanges diretamente).
/// </summary>
public class ChargebackRepository(ApiDbContext context) : IChargebackRepository
{
    /// <summary>
    /// Busca paginada de chargebacks com filtros opcionais.
    /// </summary>
    public async Task<(List<Chargeback> Chargebacks, int TotalCount)> GetPaginatedChargebacksAsync(
        string? searchTerm,
        string? statusFilter,
        int page,
        int pageSize
    )
    {
        var query = context.Chargebacks.Include(c => c.User).AsQueryable();

        // 1. Filtro Inteligente
        if (!string.IsNullOrEmpty(searchTerm))
        {
            if (long.TryParse(searchTerm, out var idSearch))
            {
                // Busca exata pelo ID ou parcial pelo nome
                query = query.Where(c =>
                    c.ChargebackId == idSearch || 
                    (c.User != null && c.User.Name.Contains(searchTerm))
                );
            }
            else
            {
                // Busca apenas textual
                query = query.Where(c => c.User != null && c.User.Name.Contains(searchTerm));
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

    /// <summary>
    /// Verifica se um chargeback já existe pelo ID externo (Mercado Pago).
    /// </summary>
    public async Task<bool> ExistsByExternalIdAsync(long chargebackId)
    {
        // Verifica se existe sem travar o registro (AsNoTracking)
        return await context
            .Chargebacks.AsNoTracking()
            .AnyAsync(c => c.ChargebackId == chargebackId);
    }

    /// <summary>
    /// Busca chargeback pelo ID externo (Mercado Pago) para edição.
    /// Não usa AsNoTracking para permitir rastreamento de mudanças pelo EF.
    /// </summary>
    public async Task<Chargeback?> GetByExternalIdAsync(long chargebackId)
    {
        // Busca para edição, então NÃO usamos AsNoTracking aqui (o EF precisa rastrear mudanças)
        return await context.Chargebacks.FirstOrDefaultAsync(c => c.ChargebackId == chargebackId);
    }

    /// <summary>
    /// Adiciona um novo chargeback ao contexto.
    /// O SaveChanges será chamado pelo UnitOfWork.
    /// </summary>
    public async Task AddAsync(Chargeback chargeback)
    {
        await context.Chargebacks.AddAsync(chargeback);
        // O SaveChanges será chamado pelo UnitOfWork
    }

    /// <summary>
    /// Marca um chargeback existente para atualização.
    /// O SaveChanges será chamado pelo UnitOfWork.
    /// </summary>
    public void Update(Chargeback chargeback)
    {
        context.Chargebacks.Update(chargeback);
        // O SaveChanges será chamado pelo UnitOfWork
    }
}
