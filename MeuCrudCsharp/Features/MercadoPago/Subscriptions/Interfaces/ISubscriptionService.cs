using System.Security.Claims;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that manages user subscriptions with a payment provider.
    /// </summary>
    public interface ISubscriptionService
    {
        /// <summary>
        /// Creates a new subscription for the authenticated user. If the user is not yet a customer
        /// in the payment provider's system, a new customer record is created first.
        /// </summary>
        /// <param name="createDto">The DTO containing the plan and payment details for the new subscription.</param>
        /// <param name="user">The claims principal of the authenticated user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the details of the created subscription.</returns>
        /// <exception cref="AppServiceException">Thrown for business logic errors, such as an invalid user or plan.</exception>
        /// <exception cref="ExternalApiException">Thrown if there is a communication failure with the payment provider API.</exception>
        Task<SubscriptionResponseDto> CreateSubscriptionAndCustomerIfNeededAsync(
            CreateSubscriptionDto createDto,
            ClaimsPrincipal user
        );

        /// <summary>
        /// Retrieves the details of a specific subscription by its ID from the payment provider.
        /// </summary>
        /// <param name="subscriptionId">The unique identifier of the subscription.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the subscription details.</returns>
        /// <exception cref="ExternalApiException">Thrown if there is a communication failure with the payment provider API.</exception>
        Task<SubscriptionResponseDto> GetSubscriptionByIdAsync(string subscriptionId);

        /// <summary>
        /// Updates the status of an existing subscription (e.g., to pause or reactivate it).
        /// </summary>
        /// <param name="subscriptionId">The unique identifier of the subscription to update.</param>
        /// <param name="dto">The DTO containing the new status.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated subscription details.</returns>
        /// <exception cref="ExternalApiException">Thrown if there is a communication failure with the payment provider API.</exception>
        Task<SubscriptionResponseDto> UpdateSubscriptionStatusAsync(
            string subscriptionId,
            UpdateSubscriptionStatusDto dto
        );

        /// <summary>
        /// Updates the recurring transaction amount of an existing subscription.
        /// </summary>
        /// <param name="subscriptionId">The unique identifier of the subscription to update.</param>
        /// <param name="dto">The DTO containing the new transaction amount and currency.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated subscription details.</returns>
        /// <exception cref="ExternalApiException">Thrown if there is a communication failure with the payment provider API.</exception>
        Task<SubscriptionResponseDto> UpdateSubscriptionValueAsync(
            string subscriptionId,
            UpdateSubscriptionValueDto dto
        );
    }
}
