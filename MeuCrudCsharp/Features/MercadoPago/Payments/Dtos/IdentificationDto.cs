using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Dtos
{
    /// <summary>
    /// Representa os dados de identificação de um pagador (Payer).
    /// </summary>
    public class IdentificationDto
    {
        /// <summary>
        /// O tipo do documento de identificação (ex: "CPF", "CNPJ").
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// O número do documento de identificação.
        /// </summary>
        public string? Number { get; set; }
    }
}
