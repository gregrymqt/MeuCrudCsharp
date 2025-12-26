using System;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;

public interface IPaymentRepository
    {
        // Verifica se existe QUALQUER pagamento para o usuário (retorna bool)
        Task<bool> HasAnyPaymentByUserIdAsync(string userId);

        // Busca pagamentos filtrando por UserID e opcionalmente pelo Tipo (Method)
        Task<List<Models.Payments>> GetPaymentsByUserIdAndTypeAsync(string userId, string? method = null);
    }
