using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MeuCrudCsharp.Features.Profiles.Admin.Dtos;

namespace MeuCrudCsharp.Features.Plans.DTOs
{
    /// <summary>
    /// Represents the data required to create a new subscription plan in the payment provider (e.g., Mercado Pago).
    /// </summary>
    public class CreatePlanDto
    {
        /// <summary>
        /// The name or reason for the subscription plan. This is displayed to the customer.
        /// </summary>
        [JsonPropertyName("reason")]
        [Required(ErrorMessage = "The plan reason/name is required.")]
        [StringLength(256, ErrorMessage = "The reason must be up to 256 characters long.")]
        public string? Reason { get; set; }

        /// <summary>
        /// Defines the recurring payment details, such as frequency and transaction amount.
        /// </summary>
        [JsonPropertyName("auto_recurring")]
        [Required(ErrorMessage = "Auto-recurring details are required.")]
        public AutoRecurringDto? AutoRecurring { get; set; }

        /// <summary>
        /// The URL to which the user will be redirected after completing the payment flow.
        /// </summary>
        [JsonPropertyName("back_url")]
        [Required(ErrorMessage = "The back URL is required.")]
        [Url(ErrorMessage = "The back URL must be a valid URL.")]
        public string? BackUrl { get; set; }

        /// <summary>
        /// An external identifier for the plan, used for associating it with a local plan record.
        /// </summary>
        [Required(ErrorMessage = "The external plan ID is required.")]
        public string? ExternalPlanId { get; set; }
    }
}
