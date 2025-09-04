namespace MeuCrudCsharp.Features.MercadoPago.Payments.Dtos
{
    /// <summary>
    /// Representa a resposta simplificada de uma operação de pagamento.
    /// Contém as informações essenciais para o frontend e para o armazenamento local.
    /// </summary>
    public class PaymentResponseDto
    {
        /// <summary>
        /// O status final do pagamento (ex: "approved", "rejected", "in_process").
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// O ID numérico único do pagamento gerado pelo provedor (Mercado Pago).
        /// </summary>
        public long? Id { get; set; }

        /// <summary>
        /// O tipo do método de pagamento utilizado (ex: "credit_card", "ticket").
        /// </summary>
        public string? PaymentTypeId { get; set; }

        /// <summary>
        /// Uma mensagem adicional, geralmente usada para fornecer detalhes em caso de erro.
        /// O setter é interno para controlar sua atribuição dentro da camada de serviço.
        /// </summary>
        public string? Message { get; internal set; }
        
        public string QrCodeBase64  { get; internal set; }
        
        public string QrCode { get; internal set; }
        
    }
}
