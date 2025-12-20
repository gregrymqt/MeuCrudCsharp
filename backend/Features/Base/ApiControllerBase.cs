using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Base;

[ApiController] // Atributo padrão para APIs
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public abstract class ApiControllerBase : ControllerBase { }

[RateLimit(5, 60)]
public abstract class MercadoPagoApiControllerBase : ApiControllerBase
{
    // Esse é o método "Tradutor" que as suas Controllers vão chamar no catch
    protected IActionResult HandleException(Exception ex, string friendlyMessage)
    {
        // Dica: Aqui seria o lugar ideal para colocar um _logger.LogError(ex, ...)

        // Cenario 1: Erro de Regra de Negócio ou Validação (Retorna 400)
        // Note que ExternalApiException entra aqui pois herda de AppServiceException
        if (ex is AppServiceException || ex is InvalidOperationException)
        {
            return BadRequest(
                new
                {
                    success = false,
                    message = friendlyMessage,
                    error = ex.Message,
                }
            );
        }

        // Cenario 2: Não Encontrado (Retorna 404)
        if (ex is ResourceNotFoundException)
        {
            return NotFound(
                new
                {
                    success = false,
                    message = ex.Message, // Aqui geralmente a msg da exception já é amigável
                }
            );
        }

        // Cenario 3: Erro Crítico/Inesperado (Retorna 500)
        return StatusCode(
            500,
            new
            {
                success = false,
                message = friendlyMessage,
                details = "Ocorreu um erro interno no servidor. Tente novamente mais tarde.",
            }
        );
    }
}
