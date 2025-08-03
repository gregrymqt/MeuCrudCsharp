using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Controllers
{
    [ApiController]
    [Route("api/preferences")]
    [Authorize] // Apenas usuários logados podem criar uma preferência
    public class PreferenceController : ControllerBase
    {
        private readonly IPreferencePayment _preferencePaymentService;

        public PreferenceController(IPreferencePayment preferencePaymentService)
        {
            _preferencePaymentService = preferencePaymentService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PaymentRequestDto request)
        {
            try
            {
                // O ClaimsPrincipal (User) é automaticamente populado pelo ASP.NET Core
                var preference = await _preferencePaymentService.CreatePreferenceAsync(request.Amount, User);

                // Retorna apenas o ID da preferência, que é o que o frontend precisa
                return Ok(new { preferenceId = preference.Id });
            }
            catch (ArgumentException ex) // Captura erros de validação (ex: valor <= 0)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ExternalApiException ex) // Captura erros da API do Mercado Pago
            {
                // 502 Bad Gateway indica que um serviço externo do qual dependemos falhou.
                return StatusCode(502, new { message = "O serviço de pagamento está indisponível no momento.", error = ex.Message });
            }
            catch (AppServiceException ex) // Captura erros internos do nosso sistema
            {
                return StatusCode(500, new { message = "Ocorreu um erro interno ao processar sua solicitação.", error = ex.Message });
            }
        }
    }
}