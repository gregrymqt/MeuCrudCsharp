using System.Threading.Tasks;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.MercadoPago.Chargebacks.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.MercadoPago.Chargebacks.Controllers;

[Route("api/admin/chargebacks")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ChargebackController : MercadoPagoApiControllerBase
{
    private readonly IChargebackService _chargebackService;

    public ChargebackController(IChargebackService chargebackService)
    {
        _chargebackService = chargebackService;
    }

    [HttpGet]
    public async Task<IActionResult> GetChargebacks(
        [FromQuery] string? searchTerm,
        [FromQuery] string? statusFilter,
        [FromQuery] int page = 1
    )
    {
        var viewModel = await _chargebackService.GetChargebacksAsync(
            searchTerm,
            statusFilter,
            page
        );
        return Ok(viewModel);
    }
}
