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
                // Usar nameof(GetPlanById) se você tiver um método que busca por ID
                return CreatedAtAction("GetPlanById", new { id = newPlan.Id }, newPlan);
            }
            catch (AppServiceException ex) // Captura erros de negócio (ex: nome duplicado)
            {
                // 400 Bad Request é mais apropriado para erros de validação ou regras de negócio
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex) // Captura erros inesperados e graves
            {
                // Logue o erro completo aqui para depuração
                return StatusCode(500, new { message = "Ocorreu um erro inesperado no servidor." });
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
