using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Subscriptions.DTOs
{
    public class CreateSubscriptionDto
    {
        [JsonPropertyName("preapproval_plan_id")]
        public string? PreapprovalPlanId { get; set; }

        [JsonPropertyName("payer_email")]
        public string? PayerEmail { get; set; }

        [JsonPropertyName("card_token_id")]
        public string? CardTokenId { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; } // Removido o aninhamento desnecessário

        [JsonPropertyName("back_url")]
        public string? BackUrl { get; set; } // Removido o aninhamento desnecessário
    }
}
