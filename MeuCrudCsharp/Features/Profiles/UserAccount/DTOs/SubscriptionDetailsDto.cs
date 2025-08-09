using System;
using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.DTOs
{
    /// <summary>
    /// Represents the detailed information of a user's subscription, intended for display on their account page.
    /// </summary>
    public class SubscriptionDetailsDto
    {
        /// <summary>
        /// The unique identifier of the subscription.
        /// </summary>
        public string? SubscriptionId { get; set; }

        /// <summary>
        /// The unique identifier of the subscription from the payment provider (e.g., Mercado Pago).
        /// </summary>
        [Required]
        public string? PlanName { get; set; }

        /// <summary>
        /// The current status of the subscription (e.g., "active", "paused", "cancelled").
        /// </summary>
        [Required]
        public string? Status { get; set; }

        /// <summary>
        /// The recurring amount charged for the subscription.
        /// </summary>
        [Required]
        [Range(typeof(decimal), "0.01", "1000000.00")]
        public decimal Amount { get; set; }

        /// <summary>
        /// The last four digits of the credit card associated with the subscription.
        /// </summary>
        [StringLength(4)]
        public string? LastFourCardDigits { get; set; }

        /// <summary>
        /// The date of the next scheduled billing attempt. Can be null if the subscription is not active.
        /// </summary>
        public DateTime? NextBillingDate { get; set; }
    }
}
