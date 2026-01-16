using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.MercadoPago.Claims.Services;
using MeuCrudCsharp.Features.MercadoPago.Claims.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static MeuCrudCsharp.Features.MercadoPago.Claims.DTOs.MercadoPagoClaimsDTOs;

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

    // Rota fica: POST api/admin/claims/15/reply
    [HttpPost("{id}/reply")]
    public async Task<IActionResult> ReplyToClaim(
        long id,
        [FromBody] MercadoPagoClaimsViewModels.ReplyClaimViewModel model // Reutilize o DTO simples que tem só a mensagem
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _adminClaimService.ReplyToClaimAsync(id, model.Message);
            return Ok(new { message = "Resposta enviada com sucesso." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Erro ao enviar resposta: " + ex.Message });
        }
    }
}
