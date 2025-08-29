using MeuCrudCsharp.Features.Plans.DTOs;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Plans.Interfaces;

public interface IMercadoPagoPlanService
{
    /// <summary>
        /// Creates a new subscription plan.
        /// </summary>
        /// <param name="createDto">The data transfer object containing the details for the new plan.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created <see cref="Plan"/> entity.</returns>
        /// <exception cref="AppServiceException">Thrown for business logic errors, such as a duplicate plan name.</exception>
        /// <exception cref="ExternalApiException">Thrown if there is a communication failure with the payment provider API.</exception>
        Task<Plan> CreatePlanAsync(CreatePlanDto createDto);

        /// <summary>
        /// Updates an existing subscription plan.
        /// </summary>
        /// <param name="externalPlanId">The unique identifier of the plan to update.</param>
        /// <param name="updateDto">The data transfer object with the updated plan details.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated <see cref="Plan"/> entity.</returns>
        /// <exception cref="ResourceNotFoundException">Thrown if a plan with the specified ID is not found.</exception>
        /// <exception cref="ExternalApiException">Thrown if there is a communication failure with the payment provider API.</exception>
        Task<Plan> UpdatePlanAsync(string externalPlanId, UpdatePlanDto updateDto);

        /// <summary>
        /// Deletes a subscription plan.
        /// </summary>
        /// <param name="externalPlanId">The unique identifier of the plan to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ResourceNotFoundException">Thrown if a plan with the specified ID is not found.</exception>
        /// <exception cref="ExternalApiException">Thrown if there is a communication failure with the payment provider API.</exception>
        Task DeletePlanAsync(string externalPlanId);
}