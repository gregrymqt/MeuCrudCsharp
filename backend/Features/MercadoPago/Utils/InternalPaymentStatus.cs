namespace MeuCrudCsharp.Features.MercadoPago.Utils
{
    /// <summary>
    /// Centraliza os valores dos status de pagamento internos do sistema para evitar "magic strings".
    /// </summary>
    public static class InternalPaymentStatus
    {
        public const string Pendente = "pendente";
        public const string Iniciando = "iniciando"; // Status de transição usado pelo Job
        public const string Aprovado = "aprovado";
        public const string Recusado = "recusado";
        public const string Reembolsado = "reembolsado";
        public const string Cancelado = "cancelado";
        public const string Autorizado = "autorizado";
        public const string EmDisputa = "em disputa";
        public const string Chargeback = "chargeback";
    }
}