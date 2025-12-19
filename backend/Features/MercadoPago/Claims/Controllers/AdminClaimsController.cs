using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.MercadoPago.Claims.Services;
using MeuCrudCsharp.Features.MercadoPago.Claims.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.Controllers;

[Route("api/admin/claims")]
[Authorize(Roles = "Admin")]
public class AdminClaimsController : MercadoPagoApiControllerBase
{
    private readonly IAdminClaimService _adminClaimService;

    public AdminClaimsController(IAdminClaimService adminClaimService)
    {
        _adminClaimService = adminClaimService;
    }

    // GET: api/admin/claims?searchTerm=pnr&statusFilter=opened&page=1
    // Retorna a lista paginada (Inbox) [cite: 1]
    [HttpGet]
    public async Task<ActionResult<MercadoPagoClaimsViewModels.ClaimsIndexViewModel>> GetClaims(
        [FromQuery] string? searchTerm,
        [FromQuery] string? statusFilter,
        [FromQuery] int page = 1
    )
    {
        var result = await _adminClaimService.GetClaimsAsync(searchTerm, statusFilter, page);
        return Ok(result);
    }

    // GET: api/admin/claims/{id}
    // Entra na "Sala de Guerra" (Detalhes + Chat) [cite: 2]
    [HttpGet("{id}")]
    public async Task<
        ActionResult<MercadoPagoClaimsViewModels.ClaimDetailViewModel>
    > GetClaimDetails(long id)
    {
        try
        {
            var result = await _adminClaimService.GetClaimDetailsAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Se não achar no banco ou der erro no MP
            return NotFound(new { message = ex.Message });
        }
    }

    // POST: api/admin/claims/reply
    // Admin responde o aluno [cite: 3]
    [HttpPost("reply")]
    public async Task<IActionResult> ReplyToClaim(
        [FromBody] MercadoPagoClaimsViewModels.ReplyClaimViewModel model
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // O Service busca pelo ID do banco (15), descobre o ID do MP (50...)
            // e manda a mensagem.
            await _adminClaimService.ReplyToClaimAsync(model.InternalId, model.Message);

            return Ok(new { message = "Resposta enviada com sucesso." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Erro ao enviar resposta: " + ex.Message });
        }
    }
}
