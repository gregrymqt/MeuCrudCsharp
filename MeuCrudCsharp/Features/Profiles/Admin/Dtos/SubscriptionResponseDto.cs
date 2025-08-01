using System;
using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Profiles.Admin.Dtos
{
    // DTO auxiliar para os dados do cartão aninhados
    public class SubscriptionCardDto
    {
        [JsonPropertyName("last_four_digits")]
        public string LastFourDigits { get; set; }
    }

    public class SubscriptionResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("preapproval_plan_id")]
        public string PreapprovalPlanId { get; set; }

        [JsonPropertyName("payer_id")]
        public long PayerId { get; set; }

        [JsonPropertyName("payer_email")]
        public string PayerEmail { get; set; }

        [JsonPropertyName("date_created")]
        public DateTime DateCreated { get; set; }

        // --- PROPRIEDADES ADICIONADAS ---

        [JsonPropertyName("next_invoice_date")]
        public DateTime? NextBillingDate { get; set; } // Data da próxima fatura

        [JsonPropertyName("card")]
        public SubscriptionCardDto? Card { get; set; } // Objeto aninhado para o cartão
    }
}