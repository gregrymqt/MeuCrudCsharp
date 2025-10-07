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
    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanDto createDto)
    {
        try
        {
            // CORREÇÃO: Chamando o _planService, que orquestra a criação no banco E na API.
            var newPlan = await _planService.CreatePlanAsync(createDto);

            // CORREÇÃO: Usando nameof para segurança de tipo e a propriedade correta (PublicId).
            return CreatedAtAction(nameof(GetPlanById), new { id = newPlan.PublicId }, newPlan);
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
    public async Task<IActionResult> GetPlans()
    {
        try
        {
            // CORREÇÃO: Chamando o método padrão para buscar planos do nosso sistema.
            var plans = await _planService.GetActiveApiPlansAsync();
            return Ok(plans);
        }
        catch (AppServiceException ex)
        {
            // Este erro é mais genérico agora, pois a falha pode ser no banco.
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