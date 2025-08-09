using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;

namespace MeuCrudCsharp.Features.Subscriptions.DTOs
{
    /// <summary>
    /// Represents the data required to create a new subscription using a pre-saved payment card.
    /// </summary>
    public class SubscriptionWithCardRequestDto
    {
        /// <summary>
        /// The unique identifier of the pre-approval plan the user is subscribing to.
        /// </summary>
        [JsonPropertyName("preapproval_plan_id")]
        [Required(ErrorMessage = "The plan ID is required.")]
        public string? PreapprovalPlanId { get; set; }

        /// <summary>
        /// The unique identifier of the pre-saved card to be used for the subscription.
        /// </summary>
        [JsonPropertyName("card_id")]
        [Required(ErrorMessage = "The card ID is required.")]
        public string? CardId { get; set; }

        /// <summary>
        /// The details of the person paying for the subscription.
        /// </summary>
        [JsonPropertyName("payer")]
        [Required(ErrorMessage = "Payer information is required.")]
        public PayerRequestDto? Payer { get; set; }
    }
}
