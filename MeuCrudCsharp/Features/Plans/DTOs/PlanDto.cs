using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.Plans.DTOs
{
    /// <summary>
    /// Represents the data for displaying a subscription plan on a public-facing UI, such as a pricing page.
    /// </summary>
    public class PlanDto
    {
        /// <summary>
        /// The display name of the plan (e.g., "Annual Plan").
        /// </summary>
        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        /// <summary>
        /// A URL-friendly identifier for the plan (e.g., "annual-plan").
        /// </summary>
        [Required]
        [StringLength(100)]
        public string? Slug { get; set; }

        /// <summary>
        /// A formatted string representing the price for display (e.g., "$41.58/month").
        /// </summary>
        [Required]
        [StringLength(50)]
        public string? PriceDisplay { get; set; }

        /// <summary>
        /// A descriptive string about the billing cycle (e.g., "Billed $499.00 annually").
        /// </summary>
        [Required]
        [StringLength(100)]
        public string? BillingInfo { get; set; }

        /// <summary>
        /// A list of features or benefits included in the plan.
        /// </summary>
        public List<string> Features { get; set; } = new List<string>();

        /// <summary>
        /// A flag to highlight a specific plan as the recommended option on the UI.
        /// </summary>
        public bool IsRecommended { get; set; }
    }
}
