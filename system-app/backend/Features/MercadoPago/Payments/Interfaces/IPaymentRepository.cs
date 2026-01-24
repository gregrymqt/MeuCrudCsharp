namespace MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;

public interface IPaymentRepository
{
    // Verifica se existe QUALQUER pagamento para o usuário (retorna bool)
    Task<bool> HasAnyPaymentByUserIdAsync(string userId);

    // Busca pagamentos filtrando por UserID e opcionalmente pelo Tipo (Method)
    Task<List<Models.Payments>> GetPaymentsByUserIdAndTypeAsync(
        string userId,
        string? method = null
    );

    Task<Models.Payments?> GetByIdWithUserAsync(string paymentId);
    Task<Models.Payments?> GetByExternalIdWithUserAsync(string externalPaymentId);

    Task<Models.Payments?> GetByExternalIdWithSubscriptionAsync(string externalId);

    void Update(Models.Payments payment);

    Task AddAsync(Models.Payments payment);

    Task Remove(Models.Payments payment);
}
