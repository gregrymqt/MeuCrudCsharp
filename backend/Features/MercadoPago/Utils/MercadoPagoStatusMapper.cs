namespace MeuCrudCsharp.Features.MercadoPago.Utils
{
    public static class PaymentStatusMapper
    {
        private static readonly Dictionary<string, string> _statusMap = new()
        {
            // O mapper agora usa as constantes da nossa nova classe!
            { "approved", InternalPaymentStatus.Aprovado },
            { "pending", InternalPaymentStatus.Pendente },
            { "in_process", InternalPaymentStatus.Pendente },
            { "rejected", InternalPaymentStatus.Recusado },
            { "refunded", InternalPaymentStatus.Reembolsado },
            { "cancelled", InternalPaymentStatus.Cancelado },
            { "authorized", InternalPaymentStatus.Autorizado },
            { "in_mediation", InternalPaymentStatus.EmDisputa },
            { "charged_back", InternalPaymentStatus.Chargeback },
        };

        public static string MapFromMercadoPago(string mercadoPagoStatus)
        {
            var key = mercadoPagoStatus?.ToLowerInvariant() ?? string.Empty;

            // O valor padrão também usa a constante
            return _statusMap.TryGetValue(key, out var status)
                ? status
                : InternalPaymentStatus.Pendente;
        }
    }
}
