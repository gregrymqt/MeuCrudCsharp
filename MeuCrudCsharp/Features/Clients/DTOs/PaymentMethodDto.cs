using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Clients.DTOs
{
    public class PaymentMethodDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; } // Ex: "Visa"
    }
}
