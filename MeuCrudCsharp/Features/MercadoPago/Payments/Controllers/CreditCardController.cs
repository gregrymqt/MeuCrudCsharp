using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoPago.Error;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CreditCardController : ControllerBase
    {
        private const string IDEMPOTENCY_PREFIX = "Credit_Card";

        // --- CORREÇÃO 1: Injetamos APENAS a interface ICacheService ---
        // O Program.cs vai decidir se isso é uma instância de MemoryCacheService ou RedisCacheService.
        private readonly ICacheService _cacheService;
        private readonly ICreditCardPayments _creditCardPaymentService;

        public CreditCardController(
            ICacheService cacheService,
            ICreditCardPayments creditCardPaymentService
        )
        {
            _cacheService = cacheService;
            _creditCardPaymentService = creditCardPaymentService;
        }

        [HttpPost("process-payment")]
        public async Task<IActionResult> ProcessPaymentAsync([FromBody] PaymentRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!Request.Headers.TryGetValue("X-Idempotency-Key", out var idempotencyKey))
            {
                return BadRequest(new { message = "O header 'X-Idempotency-Key' é obrigatório." });
            }

            // A lógica de verificação do cache agora é mais robusta.
            var cachedResponse = await _cacheService.GetCachedResponseAsync(
                IDEMPOTENCY_PREFIX,
                idempotencyKey.ToString()
            );

            if (cachedResponse != null)
            {
                // --- CORREÇÃO 2: Retornamos a resposta original do cache ---
                // Isso garante a idempotência correta.
                Console.WriteLine("--> Resposta retornada do cache.");
                return StatusCode(cachedResponse.StatusCode, cachedResponse.Body);
            }

            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (
                    string.IsNullOrEmpty(userIdString)
                    || !Guid.TryParse(userIdString, out Guid userIdAsGuid)
                )
                {
                    return Unauthorized(new { message = "Token de usuário inválido ou ausente." });
                }

                // A lógica de negócio principal permanece a mesma.
                var result = await _creditCardPaymentService.CreatePaymentOrSubscriptionAsync(
                    request
                );

                // --- CORREÇÃO 3: Armazenamos o resultado PURO no cache ---
                // O objeto 'result' e o status code 200 são armazenados.
                await _cacheService.StoreResponseAsync(
                    IDEMPOTENCY_PREFIX,
                    idempotencyKey.ToString(),
                    result, // Armazena o objeto de dados, não o IActionResult
                    200 // Ou o status code que você considerar sucesso, ex: 201
                );

                // --- CORREÇÃO 4: A chamada duplicada ao Redis foi removida ---

                return Ok(result);
            }
            catch (MercadoPagoApiException e)
            {
                return BadRequest(
                    new { error = "MercadoPago Error", message = e.ApiError.Message }
                );
            }
            catch (Exception ex)
            {
                // Em um projeto real, você logaria o erro 'ex'
                return StatusCode(500, new { message = "Ocorreu um erro inesperado." });
            }
        }
    }
}
