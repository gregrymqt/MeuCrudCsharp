using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.MercadoPago.Chargebacks.DTOs;

public class MercadoPagoChargebackResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    [JsonPropertyName("coverage_applied")]
    public bool CoverageApplied { get; set; }

    [JsonPropertyName("coverage_elegible")]
    public bool CoverageEligible { get; set; }

    [JsonPropertyName("documentation_required")]
    public bool DocumentationRequired { get; set; }

    [JsonPropertyName("documentation_status")]
    public string DocumentationStatus { get; set; }

    [JsonPropertyName("documentation")]
    public List<MercadoPagoDocumentation>? Documentation { get; set; }

    [JsonPropertyName("date_documentation_deadline")]
    public DateTime? DateDocumentationDeadline { get; set; }

    [JsonPropertyName("date_created")]
    public DateTime DateCreated { get; set; }

    [JsonPropertyName("date_last_updated")]
    public DateTime DateLastUpdated { get; set; }

    [JsonPropertyName("payments")]
    public List<MercadoPagoChargebackPayment>? Payments { get; set; }
}

public class MercadoPagoChargebackPayment
{
    [JsonPropertyName("id")]
    public string Id { get; set; } // ID do pagamento no MP

    [JsonPropertyName("status")]
    public string Status { get; set; }
}

public class MercadoPagoDocumentation
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}
