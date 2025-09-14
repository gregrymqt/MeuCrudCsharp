using MeuCrudCsharp.Features.Base;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Controllers;

[Route("api/[controller]")]
public class ConfiguracaoController : ApiControllerBase
{
    private readonly MercadoPagoSettings _mercadoPagoSettings;

    public ConfiguracaoController(IOptions<MercadoPagoSettings> mercadoPagoSettings)
    {
        _mercadoPagoSettings = mercadoPagoSettings.Value;
    }
    [HttpGet("public-key")] // Rota mais descritiva
    public async Task<IActionResult> GetPublicKey()
    {
        return Ok(new { publicKey = _mercadoPagoSettings.PublicKey });
    }
}