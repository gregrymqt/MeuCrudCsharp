using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Token;

[ApiController]
[Route("api/antiforgery")]
public class AntiforgeryController : ControllerBase
{
    private readonly IAntiforgery _antiforgery;

    public AntiforgeryController(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    // Este endpoint NÃO deve ter [Authorize] para que o front-end possa
    // pegar o token antes mesmo de o usuário estar logado, se necessário.
    [HttpGet("token")]
    [IgnoreAntiforgeryToken] // Ignora a validação para si mesmo
    public IActionResult GetToken()
    {
        // Gera o par de tokens (um para o cookie, outro para o header)
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

        // Envia o token que o JavaScript precisa colocar no cabeçalho
        return Ok(new
        {
            token = tokens.RequestToken,
            headerName = tokens.HeaderName // O nome do header que o ASP.NET Core espera
        });
    }
}

