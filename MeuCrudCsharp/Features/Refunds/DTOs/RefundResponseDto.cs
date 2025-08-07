using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Refunds.DTOs
{
    public class RefundResponseDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("payment_id")]
        public long PaymentId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("date_created")]
        public DateTime DateCreated { get; set; }
    }
}
