using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Clients.DTOs
{
    /// <summary>
    /// Resposta com os dados de um cartão armazenado no provedor de pagamentos.
    /// </summary>
    public class CardResponseDto
    {
        /// <summary>
        /// Identificador do cartão no provedor.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Identificador do cliente associado ao cartão.
        /// </summary>
        [JsonPropertyName("customer_id")]
        public string? CustomerId { get; set; }

        /// <summary>
        /// Últimos quatro dígitos do cartão.
        /// </summary>
        [JsonPropertyName("last_four_digits")]
        public string? LastFourDigits { get; set; }

        /// <summary>
        /// Informações do método de pagamento do cartão.
        /// </summary>
        [JsonPropertyName("payment_method")]
        public PaymentMethodDto? PaymentMethod { get; set; }
    }
}
