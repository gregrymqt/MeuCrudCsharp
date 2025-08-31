using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Plans.DTOs;
using MeuCrudCsharp.Features.Plans.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Plans.Controllers
{
    /// <summary>
    /// Manages administrative CRUD operations for subscription plans.
    /// Requires 'Admin' role for access.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("api/admin/plans")]
    public class AdminPlansController : ApiControllerBase
    {
        private readonly IPlanService _planService;
        private readonly IMercadoPagoPlanService _mercadoPagoPlanService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminController"/> class.
        /// </summary>
        /// <param name="planService">The service responsible for plan business logic.</param>
        public AdminPlansController(IPlanService planService, IMercadoPagoPlanService mercadoPagoPlanService)
        {
            _planService = planService;
            _mercadoPagoPlanService = mercadoPagoPlanService;
        }


        [HttpGet] // Rota diferente para não haver conflito
        public async Task<IActionResult> GetPlansFromApi()
        {
            try
            {
                var plans = await _planService.GetActiveApiPlansAsync();
                return Ok(plans);
            }
            catch (AppServiceException ex)
            {
                return StatusCode(502,
                    new { message = "Erro ao se comunicar com a API de pagamentos.", error = ex.Message });
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
            var newPlan = await _mercadoPagoPlanService.CreatePlanAsync(createDto);
            return CreatedAtAction("getPlanById", new { id = newPlan.Id }, newPlan);
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
            var updatedPlan = await _mercadoPagoPlanService.UpdatePlanAsync(id, updateDto);
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
            await _mercadoPagoPlanService.DeletePlanAsync(id);
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