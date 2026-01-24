using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Repositories;

/// <summary>
/// Repository para gerenciar operações de persistência de Payments.
/// Apenas marca as mudanças no DbContext - NÃO persiste diretamente.
/// O Service é responsável por chamar UnitOfWork.CommitAsync().
/// </summary>
public class PaymentRepository(ApiDbContext context) : IPaymentRepository
{
    public async Task<bool> HasAnyPaymentByUserIdAsync(string userId)
    {
        // Gera um "SELECT 1 ... LIMIT 1", muito performático
        return await context.Payments.AsNoTracking().AnyAsync(p => p.UserId == userId);
    }

    public async Task<List<Models.Payments>> GetPaymentsByUserIdAndTypeAsync(
        string userId,
        string? method = null
    )
    {
        var query = context
            .Payments.AsNoTracking() // Leitura rápida sem trackear mudanças
            .Where(p => p.UserId == userId);

        if (!string.IsNullOrEmpty(method))
        {
            // Filtra pelo método se ele for informado (ex: "credit_card", "pix")
            query = query.Where(p => p.Method == method);
        }

        return await query
            .OrderByDescending(p => p.DateApproved)
            .ToListAsync();
    }

    public async Task<Models.Payments?> GetByIdWithUserAsync(string paymentId)
    {
        return await context
            .Payments.Include(p => p.User) // Inclui User para processamento de notificação
            .FirstOrDefaultAsync(p => p.Id == paymentId);
    }

    public async Task<Models.Payments?> GetByExternalIdWithUserAsync(string externalPaymentId)
    {
        return await context
            .Payments.Include(p => p.User) // Essencial para pegar o e-mail do cliente
            .FirstOrDefaultAsync(p => p.ExternalId == externalPaymentId);
    }

    public async Task<Models.Payments?> GetByExternalIdWithSubscriptionAsync(string externalId)
    {
        return await context
            .Payments.Include(p => p.Subscription) // Vital para a lógica de reembolso de assinatura
            .FirstOrDefaultAsync(p => p.ExternalId == externalId);
    }

    /// <summary>
    /// Marca um pagamento para atualização.
    /// NÃO persiste - O Service chamará UnitOfWork.CommitAsync().
    /// </summary>
    public void Update(Models.Payments payment)
    {
        context.Payments.Update(payment);
        // NÃO chama SaveChanges - deixa pro Service via UnitOfWork
    }

    /// <summary>
    /// Adiciona um novo pagamento ao contexto.
    /// NÃO persiste - O Service chamará UnitOfWork.CommitAsync().
    /// </summary>
    public async Task AddAsync(Models.Payments payment)
    {
        await context.Payments.AddAsync(payment);
        // NÃO chama SaveChanges - deixa pro Service via UnitOfWork
    }

    /// <summary>
    /// Marca um pagamento para remoção.
    /// NÃO persiste - O SaveChanges será chamado pelo Service via UnitOfWork.
    /// </summary>
    public Task Remove(Models.Payments payment)
    {
        context.Payments.Remove(payment);
        // NÃO chama SaveChanges - deixa pro Service via UnitOfWork
        return Task.CompletedTask;
    }
}
