using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Refunds.DTOs
{
    public class RefundRequestDto
    {
        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; } // O valor a ser reembolsado. Se nulo, reembolsa o total.
    }
}
