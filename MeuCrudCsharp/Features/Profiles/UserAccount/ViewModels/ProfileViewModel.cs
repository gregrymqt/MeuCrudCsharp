using System.Collections.Generic;
using System.Linq;
using MeuCrudCsharp.Features.Profiles.UserAccount.DTOs;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.ViewModels
{
    /// <summary>
    /// Represents the aggregated data model for a user's main profile page.
    /// This ViewModel combines user profile information, subscription details, and payment history.
    /// </summary>
    public class ProfileViewModel
    {
        /// <summary>
        /// The user's core profile information.
        /// </summary>
        public UserProfileDto? UserProfile { get; set; }

        /// <summary>
        /// The user's current subscription details. Can be null if the user is not subscribed.
        /// </summary>
        public SubscriptionDetailsDto? Subscription { get; set; }

        /// <summary>
        /// A collection of the user's past payments. Initialized to an empty list to prevent null reference issues.
        /// </summary>
        public IEnumerable<Models.Payments> PaymentHistory { get; set; } =
            Enumerable.Empty<Models.Payments>();
    }
}
