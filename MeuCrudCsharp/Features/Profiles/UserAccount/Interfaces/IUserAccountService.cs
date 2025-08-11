using System.Collections.Generic;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Profiles.UserAccount.DTOs;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that manages a user's account.
    /// This includes retrieving profile and subscription information, as well as performing subscription actions.
    /// </summary>
    public interface IUserAccountService
    {
        /// <summary>
        /// Retrieves the public profile information for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user's profile data.</returns>
        Task<UserProfileDto> GetUserProfileAsync(string userId);

        /// <summary>
        /// Retrieves the subscription details for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user's subscription details, or null if they have no subscription.</returns>
        Task<SubscriptionDetailsDto?> GetUserSubscriptionDetailsAsync(string userId);

        /// <summary>
        /// Retrieves the payment history for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of the user's payments.</returns>
        Task<IEnumerable<Models.Payments>> GetUserPaymentHistoryAsync(string userId);

        /// <summary>
        /// Retrieves a specific payment record for a user, intended for generating a receipt.
        /// </summary>
        /// <param name="userId">The unique identifier of the user who owns the payment.</param>
        /// <param name="paymentId">The unique identifier of the payment.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the specific payment record.</returns>
        Task<Models.Payments> GetPaymentForReceiptAsync(string userId, string paymentId);

        /// <summary>
        /// Updates the payment card associated with the user's active subscription.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="newCardToken">The new card token from the payment provider.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is true if the update was successful; otherwise, false.</returns>
        Task<bool> UpdateSubscriptionCardAsync(string userId, string newCardToken);

        /// <summary>
        /// Cancels the user's active subscription.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is true if the cancellation was successful; otherwise, false.</returns>
        Task<bool> CancelSubscriptionAsync(string userId);

        /// <summary>
        /// Reactivates a user's previously cancelled subscription.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is true if the reactivation was successful; otherwise, false.</returns>
        Task<bool> ReactivateSubscriptionAsync(string userId);
    }
}
