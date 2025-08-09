using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Subscriptions.DTOs
{
    /// <summary>
    /// Represents the data required to update the status of an existing subscription.
    /// </summary>
    public class UpdateSubscriptionStatusDto
    {
        /// <summary>
        /// The new status for the subscription.
        /// </summary>
        /// <remarks>Allowed values are typically "paused", "cancelled", or "authorized".</remarks>
        [JsonPropertyName("status")]
        [Required(ErrorMessage = "The new status is required.")]
        public string? Status { get; set; }
    }
}
