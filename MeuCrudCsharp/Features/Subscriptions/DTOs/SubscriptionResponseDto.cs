using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Subscriptions.DTOs
{
    /// <summary>
    /// Represents the detailed response from the payment provider's API for a subscription.
    /// </summary>
    public class SubscriptionResponseDto
    {
        /// <summary>
        /// The unique identifier of the subscription from the payment provider.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The current status of the subscription (e.g., "authorized", "paused", "cancelled").
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// The ID of the pre-approval plan to which the user is subscribed.
        /// </summary>
        [JsonPropertyName("preapproval_plan_id")]
        public string? PreapprovalPlanId { get; set; }

        /// <summary>
        /// The unique identifier of the payer in the payment provider's system.
        /// </summary>
        [JsonPropertyName("payer_id")]
        public long PayerId { get; set; }

        /// <summary>
        /// The email address of the payer.
        /// </summary>
        [JsonPropertyName("payer_email")]
        public string? PayerEmail { get; set; }

        /// <summary>
        /// The date and time when the subscription was created.
        /// </summary>
        [JsonPropertyName("date_created")]
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// The date of the next scheduled billing attempt.
        /// </summary>
        [JsonPropertyName("next_invoice_date")]
        public DateTime? NextBillingDate { get; set; }

        /// <summary>
        /// The details of the payment card associated with the subscription.
        /// </summary>
        [JsonPropertyName("card")]
        public SubscriptionCardDto? Card { get; set; }
    }

    /// <summary>
    /// A helper DTO representing the card details nested within the subscription response.
    /// </summary>
    public class SubscriptionCardDto
    {
        /// <summary>
        /// The last four digits of the associated credit card.
        /// </summary>
        [JsonPropertyName("last_four_digits")]
        public string? LastFourDigits { get; set; }

        /// <summary>
        /// The brand of the associated credit card (e.g., "Visa", "Mastercard").
        /// </summary>
        [JsonPropertyName("brand")]
        public string? Brand { get; set; }
    }
}
