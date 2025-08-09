using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Refunds.DTOs
{
    /// <summary>
    /// Represents the data required to request a refund for a payment.
    /// </summary>
    public class RefundRequestDto
    {
        /// <summary>
        /// The amount to be refunded. If this value is null or not provided,
        /// a full refund of the original payment will be attempted.
        /// </summary>
        [JsonPropertyName("amount")]
        [Range(
            typeof(decimal),
            "0.01",
            "1000000.00",
            ErrorMessage = "If provided, the refund amount must be a positive value."
        )]
        public decimal? Amount { get; set; }
    }
}
