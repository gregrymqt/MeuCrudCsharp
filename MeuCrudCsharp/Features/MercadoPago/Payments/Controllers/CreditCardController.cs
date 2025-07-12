using MercadoPago.Error;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos; // <-- Adicionar using para o DTO
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using Microsoft.AspNetCore.Authorization; // <-- Adicionar para proteger a rota
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.Design;
using System.Security.Claims;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // <-- Garante que apenas usuários autenticados podem usar este controller
    public class CreditCardController : ControllerBase
    {
        private const string IDEMPOTENCY_PREFIX = "Credit_Card";
        // As injeções de dependência estão corretas!
        private readonly IPreferenceService _preferenceService;
        private readonly ICacheService _cacheService;
        private readonly ICreditCardPayment _creditCardPaymentService;

        public CreditCardController(IPreferenceService preferenceService,
                                      ICacheService cacheService,
                                      ICreditCardPayment creditCardPaymentService)
        {
            _preferenceService = preferenceService;
            _cacheService = cacheService;
            _creditCardPaymentService = creditCardPaymentService;
        }

        // Renomeado para seguir a convenção de nomenclatura async do C#
        [HttpPost("process-payment")]
        public async Task<IActionResult> ProcessPaymentAsync([FromBody] PaymentRequestDto request)
        {
            // O [FromBody] diz ao ASP.NET para pegar o JSON do corpo da requisição
            // e tentar encaixá-lo no objeto CreateCardPaymentRequestDto.

            // A validação do ModelState continua funcionando, mas agora ela valida o DTO.
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Pegando o Header de Idempotência da forma correta
            if (!Request.Headers.TryGetValue("X-Idempotency-Key", out var idempotencyKey))
            {
                // Retorna um erro claro se o header estiver faltando
                return BadRequest(new { message = "O header 'X-Idempotency-Key' é obrigatório." });
            }

            // 2. Lógica de Cache (como você pediu)
            // Supondo que o seu serviço de cache pode armazenar e retornar um IActionResult
            var cachedResponse = await _cacheService.GetCachedResponse(IDEMPOTENCY_PREFIX + idempotencyKey);
            if (cachedResponse != null)
            {
                return cachedResponse; // Retorna a resposta do cache se o pagamento já foi processado
            }

            try
            {
                // 3. Pegando o ID do usuário autenticado (que vem como uma string)
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized();
                }

                if (!Guid.TryParse(userIdString, out Guid userIdAsGuid))
                {
                    // Se a conversão falhar, significa que o ID no token não é um Guid válido.
                    // Retornamos um erro claro em vez de deixar a aplicação quebrar.
                    return BadRequest(new { message = "O formato do ID de usuário no token é inválido." });
                }

                var result = await _creditCardPaymentService.CreatePaymentAsync(request, userIdAsGuid, 100);

                // 5. Armazenando a resposta no cache em caso de sucesso
                await _cacheService.StoreResponse(IDEMPOTENCY_PREFIX + idempotencyKey, Ok(result), TimeSpan.FromDays(1));

                return Ok(result);
            }
            catch (MercadoPagoApiException e)
            {
                // Retorna um erro específico da API do Mercado Pago
                return BadRequest(new { error = "MercadoPago Error", message = e.ApiError.Message });
            }
            catch (Exception ex)
            {
                // Para qualquer outro erro inesperado, retorna um erro 500
                // Em um projeto real, você também logaria esse erro (ex.Log(ex))
                return StatusCode(500, new { message = "Ocorreu um erro inesperado." });
            }
        }
    }
}