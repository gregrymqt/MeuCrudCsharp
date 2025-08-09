using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Plans.DTOs;
using MeuCrudCsharp.Features.Plans.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Plans.Controllers
{
    /// <summary>
    /// Manages administrative CRUD operations for subscription plans.
    /// Requires 'Admin' role for access.
    /// </summary>
    [ApiController]
    [Route("api/admin/plans")]
    [Authorize(Roles = "Admin")]
    public class AdminPlansController : ControllerBase
    {
        private readonly IPlanService _planService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminPlansController"/> class.
        /// </summary>
        /// <param name="planService">The service responsible for plan business logic.</param>
        public AdminPlansController(IPlanService planService)
        {
            _planService = planService;
        }

        /// <summary>
        /// Retrieves all active subscription plans.
        /// </summary>
        /// <returns>A list of active plans.</returns>
        /// <response code="200">Returns the list of active plans.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not in the 'Admin' role.</response>
        /// <response code="500">If an unexpected server error occurs.</response>
        [HttpGet]
        public async Task<IActionResult> GetPlans()
        {
            try
            {
                var plans = await _planService.GetActivePlansAsync();
                return Ok(plans);
            }
            catch (Exception)
            {
                // In a real application, you would log the exception details.
                return StatusCode(
                    500,
                    new { message = "An unexpected error occurred while fetching plans." }
                );
            }
        }

        /// <summary>
        /// Creates a new subscription plan.
        /// </summary>
        /// <param name="createDto">The data transfer object containing the details for the new plan.</param>
        /// <returns>The newly created plan.</returns>
        /// <response code="201">Returns the newly created plan.</response>
        /// <response code="400">If the plan data is invalid (e.g., duplicate name).</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not in the 'Admin' role.</response>
        /// <response code="500">If an unexpected server error occurs.</response>
        [HttpPost]
        public async Task<IActionResult> CreatePlan([FromBody] CreatePlanDto createDto)
        {
            try
            {
                var newPlan = await _planService.CreatePlanAsync(createDto);
                // Assuming a 'GetPlanById' method exists for the CreatedAtAction response.
                return CreatedAtAction("GetPlanById", new { id = newPlan.Id }, newPlan);
            }
            catch (AppServiceException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected server error occurred." });
            }
        }

        /// <summary>
        /// Updates an existing subscription plan.
        /// </summary>
        /// <param name="id">The unique identifier of the plan to update.</param>
        /// <param name="updateDto">The data transfer object with the updated plan details.</param>
        /// <returns>The updated plan.</returns>
        /// <response code="200">Returns the updated plan.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not in the 'Admin' role.</response>
        /// <response code="404">If a plan with the specified ID is not found.</response>
        /// <response code="500">If an unexpected server error occurs.</response>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePlan(string id, [FromBody] UpdatePlanDto updateDto)
        {
            try
            {
                var updatedPlan = await _planService.UpdatePlanAsync(id, updateDto);
                return Ok(updatedPlan);
            }
            catch (ResourceNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { message = "Error updating the plan.", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Deletes a subscription plan.
        /// </summary>
        /// <param name="id">The unique identifier of the plan to delete.</param>
        /// <response code="204">Indicates the plan was successfully deleted.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not in the 'Admin' role.</response>
        /// <response code="404">If a plan with the specified ID is not found.</response>
        /// <response code="500">If an unexpected server error occurs.</response>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlan(string id)
        {
            try
            {
                await _planService.DeletePlanAsync(id);
                return NoContent();
            }
            catch (ResourceNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { message = "Error deleting the plan.", error = ex.Message }
                );
            }
        }
    }
}
