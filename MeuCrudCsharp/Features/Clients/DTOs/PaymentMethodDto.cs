using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Clients.DTOs
{
    /// <summary>
    /// Informações sobre o método de pagamento do cartão.
    /// </summary>
    public class PaymentMethodDto
    {
        /// <summary>
        /// Nome/label do método de pagamento (por exemplo, "Visa").
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
