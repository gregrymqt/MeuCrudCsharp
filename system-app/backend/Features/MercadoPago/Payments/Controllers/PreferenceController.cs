using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoPago.Resource.User;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Controllers;

[Route("api/preferences")]
public class PreferenceController : MercadoPagoApiControllerBase
{
    private readonly IPreferencePaymentService _preferencePaymentService;

    public PreferenceController(IPreferencePaymentService preferencePaymentService)
    {
        _preferencePaymentService = preferencePaymentService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePreferenceDto model)
    {
        try
        {
            // O Service agora resolve o User internamente via IUserContext
            var preferenceId = await _preferencePaymentService.CreatePreferenceAsync(model);

            return Ok(new { preferenceId });
        }
        catch (Exception ex)
        {
            // Seus tratamentos de erro padronizados
            return BadRequest(new { message = ex.Message });
        }
    }
}
