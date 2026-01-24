using System;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Repositories;

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
            // Baseado na propriedade [cite: 40]
            query = query.Where(p => p.Method == method);
        }

        return await query
            .OrderByDescending(p => p.DateApproved) // [cite: 42]
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

    public void Update(Models.Payments payment)
    {
        context.Payments.Update(payment);
    }

    public async Task AddAsync(Models.Payments payment)
    {
        await context.Payments.AddAsync(payment);
    }

    public async Task Remove(Models.Payments payment)
    {
        context.Payments.Remove(payment);
    }
}
