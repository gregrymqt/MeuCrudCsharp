using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Plans.DTOs;
using MeuCrudCsharp.Features.Plans.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Plans.Controllers
{
    [ApiController]
    [Route("api/admin/plans")]
    [Authorize(Roles = "Admin")]
    public class AdminPlansController : ControllerBase
    {
        private readonly IPlanService _planService;

        public AdminPlansController(IPlanService planService)
        {
            _planService = planService;
        }

        // READ (GET all) - Usa o serviço que tem a lógica de API-first com fallback
        [HttpGet]
        public async Task<IActionResult> GetPlans()
        {
            var plans = await _planService.GetActivePlansAsync();
            return Ok(plans);
        }

        // CREATE (POST)
        [HttpPost]
        public async Task<IActionResult> CreatePlan([FromBody] CreatePlanDto createDto)
        {
            try
            {
                var newPlan = await _planService.CreatePlanAsync(createDto);
                return CreatedAtAction(nameof(GetPlans), new { id = newPlan.Id }, newPlan);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { message = "Erro ao criar o plano.", error = ex.Message }
                );
            }
        }

        // UPDATE (PUT)
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
                    new { message = "Erro ao atualizar o plano.", error = ex.Message }
                );
            }
        }

        // DELETE (DELETE)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlan(string id)
        {
            try
            {
                await _planService.DeletePlanAsync(id);
                return NoContent(); // 204 No Content
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { message = "Erro ao deletar o plano.", error = ex.Message }
                );
            }
        }
    }
}
