namespace MeuCrudCsharp.Features.MercadoPago.Payments.Dtos
{
    /// <summary>
    /// Representa os dados do pagador (payer) em uma requisição de pagamento.
    /// </summary>
    public class PayerRequestDto
    {
        /// <summary>
        /// O e-mail do pagador.
        /// </summary>
        public string? Email { get; set; }
        
        public string? FirstName { get; set; }
        
        public string? LastName { get; set; }

        /// <summary>
        /// Os dados de identificação (documento) do pagador.
        /// </summary>
        public IdentificationDto? Identification { get; set; }
    }
}
