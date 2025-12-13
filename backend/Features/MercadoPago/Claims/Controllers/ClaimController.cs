using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.MercadoPago.Claims.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Claims.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.Controllers
{
    [Route("api/admin/claims")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ClaimController : ApiControllerBase
    {
        private readonly IClaimService _claimService;

        public ClaimController(IClaimService claimService)
        {
            _claimService = claimService;
        }

        [HttpGet]
        public async Task<IActionResult> GetClaims(
            [FromQuery] string? searchTerm,
            [FromQuery] string? statusFilter,
            [FromQuery] int page = 1
        )
        {
            var viewModel = await _claimService.GetClaimsAsync(searchTerm, statusFilter, page);
            return Ok(viewModel);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateClaimStatus(
            long id,
            [FromBody] UpdateClaimStatusViewModel model
        )
        {
            await _claimService.UpdateClaimStatusAsync(id, model.Status);
            return NoContent();
        }
    }
}
