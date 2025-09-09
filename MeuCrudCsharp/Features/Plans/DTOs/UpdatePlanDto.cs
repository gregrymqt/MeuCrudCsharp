using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Plans.DTOs
{
    /// <summary>
    /// Representa os dados para atualizar um plano de assinatura.
    /// Propriedades nulas não serão alteradas.
    /// </summary>
    public class UpdatePlanDto
    {
        /// <summary>
        /// O novo nome (razão) do plano.
        /// </summary>
        [StringLength(256, ErrorMessage = "A razão deve ter no máximo 256 caracteres.")]
        [JsonPropertyName("reason")] // Mapeia para o campo esperado pela API do Mercado Pago
        public string? Reason { get; set; }

        /// <summary>
        /// O novo valor da transação recorrente.
        /// </summary>
        [Range(0.01, 1000000.00, ErrorMessage = "O valor da transação deve ser positivo.")]
        [JsonPropertyName("transaction_amount")]
        public decimal? TransactionAmount { get; set; } // ALTERADO para decimal? para ser opcional

        // CAMPO ADICIONADO para corresponder ao front-end
        /// <summary>
        /// A nova frequência da cobrança (ex: "months").
        /// </summary>
        [StringLength(20, ErrorMessage = "O tipo de frequência deve ter no máximo 20 caracteres.")]
        [JsonPropertyName("frequency_type")]
        public string? FrequencyType { get; set; }
    }
}
