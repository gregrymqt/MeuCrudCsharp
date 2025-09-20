namespace MeuCrudCsharp.Features.MercadoPago.Payments.Utils;

public static class PaymentStatusMapper
{
    // O dicionário agora é privado, estático e somente leitura (readonly)
    private static readonly Dictionary<string, string> _statusMap = new()
    {
        { "approved", "aprovado" },
        { "pending", "pendente" },
        { "in_process", "pendente" }, // Mapeia para o mesmo status de "pending"
        { "rejected", "recusado" },
        { "refunded", "reembolsado" },
        { "cancelled", "cancelado" },
    };

    /// <summary>
    /// Mapeia o status de pagamento do Mercado Pago para o status interno do sistema.
    /// </summary>
    /// <param name="mercadoPagoStatus">O status retornado pela API do Mercado Pago.</param>
    /// <returns>O status mapeado. O padrão é "pendente" se não for encontrado.</returns>
    public static string MapFromMercadoPago(string mercadoPagoStatus)
    {
        // Garante que a chave de busca não seja nula e esteja em minúsculas
        var key = mercadoPagoStatus?.ToLowerInvariant() ?? string.Empty;

        return _statusMap.TryGetValue(key, out var status) ? status : "pendente";
    }
}