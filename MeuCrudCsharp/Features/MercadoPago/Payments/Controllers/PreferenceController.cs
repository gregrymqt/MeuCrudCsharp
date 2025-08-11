using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Controllers
{
    /// <summary>
    /// Controladora responsável por criar preferências de pagamento no Mercado Pago.
    /// </summary>
    [ApiController]
    [Route("api/preferences")]
    public class PreferenceController : ControllerBase
    {
        private readonly IPreferencePayment _preferencePaymentService;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="PreferenceController"/>.
        /// </summary>
        /// <param name="preferencePaymentService">O serviço que contém a lógica para criar preferências de pagamento.</param>
        public PreferenceController(IPreferencePayment preferencePaymentService)
        {
            _preferencePaymentService = preferencePaymentService;
        }

        /// <summary>
        /// Cria uma nova preferência de pagamento para o usuário autenticado.
        /// </summary>
        /// <remarks>
        /// A preferência de pagamento é um conjunto de informações sobre um produto ou serviço
        /// que é usado pelo frontend (Payment Brick) para iniciar o processo de pagamento.
        /// </remarks>
        /// <param name="request">DTO contendo o valor do pagamento.</param>
        /// <returns>Um objeto contendo o ID da preferência criada.</returns>
        /// <response code="200">Retorna o ID da preferência criada com sucesso.</response>
        /// <response code="400">Se os dados da requisição forem inválidos (ex: valor não positivo).</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="500">Se ocorrer um erro interno na aplicação.</response>
        /// <response code="502">Se houver uma falha de comunicação com a API do Mercado Pago.</response>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PaymentRequestDto request)
        {
            try
            {
                // O ClaimsPrincipal (User) é automaticamente populado pelo ASP.NET Core
                var preference = await _preferencePaymentService.CreatePreferenceAsync(
                    request.Amount,
                    User
                );

                return Ok(new { preferenceId = preference.Id });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ExternalApiException ex)
            {
                return StatusCode(
                    502,
                    new
                    {
                        message = "O serviço de pagamento está indisponível no momento.",
                        error = ex.Message,
                    }
                );
            }
            catch (AppServiceException ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = "Ocorreu um erro interno ao processar sua solicitação.",
                        error = ex.Message,
                    }
                );
            }
        }
    }
}
