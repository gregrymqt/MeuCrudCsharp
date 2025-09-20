using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.MercadoPago.Plans.DTOs;

/// <summary>
/// Represents the recurring payment details for a subscription plan.
/// </summary>
/// <param name="Frequency">The number of units for the frequency type (e.g., 1 for 1 month).</param>
/// <param name="FrequencyType">The type of frequency (e.g., "months").</param>
/// <param name="TransactionAmount">The cost of each recurrence.</param>
public record AutoRecurringDto(
    [property: JsonPropertyName("frequency")] int Frequency,
    [property: JsonPropertyName("frequency_type")] string FrequencyType,
    [property: JsonPropertyName("transaction_amount")] decimal TransactionAmount,
    [property: JsonPropertyName("CurrencyId")] string CurrencyId
);

/// <summary>
/// Data required to create a new subscription plan in the payment provider.
/// </summary>
public record CreatePlanDto(
    [property: JsonPropertyName("reason")]
    [Required(ErrorMessage = "The plan reason/name is required.")]
    [StringLength(256, ErrorMessage = "The reason must be up to 256 characters long.")]
    string? Reason,

    [property: JsonPropertyName("auto_recurring")]
    [Required(ErrorMessage = "Auto-recurring details are required.")]
    AutoRecurringDto? AutoRecurring,

    [property: JsonPropertyName("back_url")]
    [Required(ErrorMessage = "The back URL is required.")]
    [Url(ErrorMessage = "The back URL must be a valid URL.")]
    string? BackUrl,

    [property: JsonPropertyName("description")]
    [Required(ErrorMessage = "The description is required.")]
    string? Description
);

/// <summary>
/// A generic key-value pair for displaying plan features.
/// </summary>
public record PlanDetailDto(
    string Feature,
    object Value,
    string? DisplayValue
);

/// <summary>
/// Data for displaying a subscription plan on a public-facing UI.
/// </summary>
public record PlanDto(
    string PublicId,

    [Required]
    [StringLength(100)]
    string? Name,

    [Required]
    [StringLength(100)]
    string? Slug,

    [Required]
    [StringLength(50)]
    string? PriceDisplay,

    [Required]
    [StringLength(100)]
    string? BillingInfo,

    List<string> Features,
    bool IsRecommended
);

/// <summary>
/// Detailed response for a subscription plan from the payment provider.
/// </summary>
public record PlanResponseDto(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("reason")] string? Reason,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("date_created")] DateTime DateCreated,
    [property: JsonPropertyName("external_reference")] string? ExternalPlanId,
    [property: JsonPropertyName("auto_recurring")] AutoRecurringDto? AutoRecurring
);

/// <summary>
/// Paginated response from the payment provider's plan search endpoint.
/// </summary>
public record PlanSearchResponseDto(
    [property: JsonPropertyName("results")] List<PlanResponseDto> Results
);

/// <summary>
/// Data to update a subscription plan. Null properties will not be changed.
/// </summary>
public record UpdatePlanDto(
    [property: JsonPropertyName("reason")]
    [StringLength(256, ErrorMessage = "A razão deve ter no máximo 256 caracteres.")]
    string? Reason,
    
    [property: JsonPropertyName("transaction_amount")]
    [Range(0.01, 1000000.00, ErrorMessage = "O valor da transação deve ser positivo.")]
    decimal? TransactionAmount,

    [property: JsonPropertyName("frequency_type")]
    [StringLength(20, ErrorMessage = "O tipo de frequência deve ter no máximo 20 caracteres.")]
    string? FrequencyType
);