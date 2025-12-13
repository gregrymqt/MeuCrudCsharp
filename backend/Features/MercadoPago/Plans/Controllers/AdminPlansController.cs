using MeuCrudCsharp.Features.Base;
using Microsoft.AspNetCore.Mvc;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Plans.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Plans.Interfaces;

namespace MeuCrudCsharp.Features.MercadoPago.Plans.Controllers;

[Route("api/admin/plans")]
public class AdminPlansController : ApiControllerBase
{
    // A única dependência da Controller agora é a IPlanService
    private readonly IPlanService _planService;

    public AdminPlansController(IPlanService planService)
    {
        _planService = planService;
    }

    /// <summary>
    /// Cria um novo plano de assinatura.
    /// </summary>
    // No seu arquivo PlansController.cs

    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanDto createDto)
    {
        try
        {
            var planResponseDto = await _planService.CreatePlanAsync(createDto);

            return CreatedAtAction(nameof(GetPlanById), new { id = planResponseDto.PublicId }, planResponseDto);
        }
        catch (AppServiceException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Ocorreu um erro inesperado no servidor." });
        }
    }

    /// <summary>
    /// Busca todos os planos ativos do sistema.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPlans([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            // Agora passamos os parâmetros de paginação para o serviço
            var plans = await _planService.GetActiveApiPlansAsync(page, pageSize);
            return Ok(plans);
        }
        catch (AppServiceException ex)
        {
            return StatusCode(500, new { message = "Erro ao buscar os planos.", error = ex.Message });
        }
    }

    /// <summary>
    /// Busca um plano específico pelo seu ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ActionName(nameof(GetPlanById))]
    public async Task<IActionResult> GetPlanById(Guid id)
    {
        // Chama o novo método que retorna o DTO específico para edição
        var planEditDto = await _planService.GetPlanEditDtoByIdAsync(id);

        if (planEditDto == null)
        {
            return NotFound(new { message = $"Plano com ID {id} não encontrado." });
        }

        return Ok(planEditDto);
    }

    /// <summary>
    /// Atualiza um plano existente.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdatePlanDto updateDto)
    {
        try
        {
            // CORREÇÃO: Chamando o _planService.
            var updatedPlan = await _planService.UpdatePlanAsync(id, updateDto);
            return Ok(updatedPlan);
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao atualizar o plano.", error = ex.Message });
        }
    }

    /// <summary>
    /// Desativa um plano (soft delete).
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        try
        {
            // CORREÇÃO: Chamando o método correto do _planService.
            await _planService.DeletePlanAsync(id);
            return NoContent(); // Sucesso, sem conteúdo para retornar.
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao deletar o plano.", error = ex.Message });
        }
    }
}