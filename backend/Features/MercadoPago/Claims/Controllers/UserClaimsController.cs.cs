using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.MercadoPago.Claims.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Claims.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static MeuCrudCsharp.Features.MercadoPago.Claims.DTOs.MercadoPagoClaimsDTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.Controllers;

    [Route("api/admin/claims")]
    [Authorize(Roles = "Admin")]
    public class UserClaimsController : MercadoPagoApiControllerBase
    {private readonly IUserClaimService _userClaimService;

    public UserClaimsController(IUserClaimService userClaimService)
    {
        _userClaimService = userClaimService;
    }

    // GET: api/user/claims
    // Lista apenas as reclamações DO usuário logado [cite: 4]
    [HttpGet]
    public async Task<ActionResult<List<MercadoPagoClaimsViewModels.ClaimSummaryViewModel>>> GetMyClaims()
    {
        var result = await _userClaimService.GetMyClaimsAsync();
        return Ok(result);
    }

    // GET: api/user/claims/{id}
    // Detalhes (Chat) - Valida se a claim pertence mesmo ao usuário [cite: 4]
    [HttpGet("{id}")]
    public async Task<ActionResult<MercadoPagoClaimsViewModels.ClaimDetailViewModel>> GetMyClaimDetail(int id)
    {
        try
        {
            var result = await _userClaimService.GetMyClaimDetailAsync(id);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(); // Retorna 403 se tentar ver claim de outro
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // POST: api/user/claims/{id}/reply
    // Aluno responde a loja [cite: 4]
    [HttpPost("{id}/reply")]
    public async Task<IActionResult> Reply(int id, [FromBody] ReplyRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("A mensagem não pode ser vazia.");

        try
        {
            await _userClaimService.ReplyAsync(id, request.Message);
            return Ok(new { message = "Mensagem enviada." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // POST: api/user/claims/{id}/mediation
    // Aluno pede ajuda ao Mercado Pago (Escalar disputa) [cite: 4]
    [HttpPost("{id}/mediation")]
    public async Task<IActionResult> RequestMediation(int id)
    {
        try
        {
            await _userClaimService.RequestMediationAsync(id);
            return Ok(new { message = "Mediação solicitada ao Mercado Pago." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
