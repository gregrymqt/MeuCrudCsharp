using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Clients.DTOs
{
    public class CardResponseDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("customer_id")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("last_four_digits")]
        public string? LastFourDigits { get; set; }

        [JsonPropertyName("payment_method")]
        public PaymentMethodDto? PaymentMethod { get; set; }
    }
}
