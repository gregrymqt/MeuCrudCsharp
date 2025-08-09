using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.Plans.DTOs
{
    /// <summary>
    /// Represents the data required to update an existing subscription plan.
    /// All properties are optional, allowing for partial updates.
    /// </summary>
    public class UpdatePlanDto
    {
        /// <summary>
        /// The new name or reason for the subscription plan.
        /// </summary>
        [StringLength(256, ErrorMessage = "The reason must be up to 256 characters long.")]
        public string? Reason { get; set; }

        /// <summary>
        /// The new URL to which the user will be redirected after completing the payment flow.
        /// </summary>
        [Url(ErrorMessage = "The back URL must be a valid URL.")]
        public string? BackUrl { get; set; }

        /// <summary>
        /// The new transaction amount for the recurring payment.
        /// </summary>
        [Range(
            typeof(decimal),
            "0.01",
            "1000000.00",
            ErrorMessage = "The transaction amount must be a positive value."
        )]
        public decimal TransactionAmount { get; set; }
    }
}
