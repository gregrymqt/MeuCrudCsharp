using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Subscriptions.DTOs
{
    /// <summary>
    /// DTO para REPRESENTAR a resposta da API do Mercado Pago sobre a assinatura.
    /// </summary>
    public class SubscriptionResponseDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("preapproval_plan_id")]
        public string? PreapprovalPlanId { get; set; }

        [JsonPropertyName("payer_id")]
        public long PayerId { get; set; }

        [JsonPropertyName("payer_email")]
        public string? PayerEmail { get; set; }

        [JsonPropertyName("date_created")]
        public DateTime DateCreated { get; set; }

        [JsonPropertyName("next_invoice_date")]
        public DateTime? NextBillingDate { get; set; }

        [JsonPropertyName("card")]
        public SubscriptionCardDto? Card { get; set; }
    }

    /// <summary>
    /// DTO auxiliar para os dados do cartão aninhados na resposta.
    /// </summary>
    public class SubscriptionCardDto
    {
        [JsonPropertyName("last_four_digits")]
        public string? LastFourDigits { get; set; }

        [JsonPropertyName("brand")]
        public string? Brand { get; set; }

        // A LINHA PROBLEMÁTICA FOI REMOVIDA DAQUI
    }
}