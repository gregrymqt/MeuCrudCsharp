using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Profiles.Admin.Dtos
{
    public class CreateSubscriptionDto
    {
        [JsonPropertyName("preapproval_plan_id")]
        public string PreapprovalPlanId { get; set; }

        [JsonPropertyName("payer_email")]
        public string PayerEmail { get; set; }

        [JsonPropertyName("card_token_id")]
        public string CardTokenId { get; set; } // O token do cartão gerado pelo SDK do MP no frontend

        // Opcional: uma referência para ligar esta assinatura a um pedido no seu sistema
        [JsonPropertyName("external_reference")]
        public string? ExternalReference { get; set; }
    }
}
