using System.Threading.Tasks;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Chargebacks.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static MeuCrudCsharp.Features.MercadoPago.Chargebacks.ViewModels.ChargeBackViewModels;

namespace MeuCrudCsharp.Features.MercadoPago.Chargebacks.Controllers;

[Route("api/admin/chargebacks")]
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

    [HttpGet("{id}/details")]
    [ProducesResponseType(typeof(ChargebackDetailViewModel), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetChargebackDetails(string id)
    {
        try
        {
            // O Service já cuida do Cache e da chamada externa
            var viewModel = await _chargebackService.GetChargebackDetailAsync(id);
            return Ok(viewModel);
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        // ExternalApiException e AppServiceException podem ser tratados aqui
        // ou deixados para um Middleware Global de erro
    }
}
