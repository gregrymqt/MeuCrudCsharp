
using MeuCrudCsharp.Features.MercadoPago.Plans.DTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Plans.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that manages subscription plans.
    /// This includes creating, retrieving, updating, and deleting plans both locally and with the payment provider.
    /// </summary>
    public interface IPlanService
    {
        /// <summary>
        /// Retrieves all active subscription plans, formatted for public display.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="PlanDtos"/>.</returns>
        Task<List<PlanDto>> GetActiveDbPlansAsync();
        Task<List<PlanDto>> GetActiveApiPlansAsync();

    }
}
