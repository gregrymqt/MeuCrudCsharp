using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoPago.Error;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MeuCrudCsharp.Caching;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CreditCardController : ControllerBase
    {
        private const string IDEMPOTENCY_PREFIX = "CreditCardPayment";

        // As dependências permanecem as mesmas, mas agora ICacheService é o nosso serviço universal
        private readonly ICacheService _cacheService;
        private readonly ICreditCardPayments _creditCardPaymentService;

        public CreditCardController(
            ICacheService cacheService,
            ICreditCardPayments creditCardPaymentService)
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

            if (!Request.Headers.TryGetValue("X-Idempotency-Key", out var idempotencyKey) || string.IsNullOrEmpty(idempotencyKey))
            {
                return BadRequest(new { message = "O header 'X-Idempotency-Key' é obrigatório." });
            }

            // MUDANÇA 2: Construindo a chave de cache de forma explícita e padronizada
            var cacheKey = $"{IDEMPOTENCY_PREFIX}_idempotency_{idempotencyKey}";

            // MUDANÇA 3: Usando o método universal GetAsync<T>
            var cachedResponse = await _cacheService.GetAsync<CachedResponse>(cacheKey);

            if (cachedResponse != null)
            {
                Console.WriteLine("--> Resposta retornada do cache de idempotência.");
                return StatusCode(cachedResponse.StatusCode, cachedResponse.Body);
            }

            try
            {
                var result = await _creditCardPaymentService.CreatePaymentOrSubscriptionAsync(request);

                // MUDANÇA 4: Usando o método universal SetAsync<T> para salvar a resposta de sucesso
                var responseToCache = new CachedResponse(result, 201); // 201 Created é mais apropriado aqui
                await _cacheService.SetAsync(cacheKey, responseToCache, TimeSpan.FromHours(24));

                return CreatedAtAction(nameof(ProcessPaymentAsync), result);
            }
            catch (MercadoPagoApiException e)
            {
                var errorBody = new { error = "MercadoPago Error", message = e.ApiError.Message };

                // MUDANÇA 5 (MELHORIA): Fazendo cache da resposta de erro para garantir a idempotência
                var errorResponseToCache = new CachedResponse(errorBody, 400); // 400 Bad Request
                await _cacheService.SetAsync(cacheKey, errorResponseToCache, TimeSpan.FromHours(24));

                return BadRequest(errorBody);
            }
            catch (Exception ex)
            {
                var errorBody = new { message = "Ocorreu um erro inesperado.", error = ex.Message };

                // MUDANÇA 5 (MELHORIA): Também fazemos cache de erros internos
                var errorResponseToCache = new CachedResponse(errorBody, 500); // 500 Internal Server Error
                await _cacheService.SetAsync(cacheKey, errorResponseToCache, TimeSpan.FromHours(24));

                // Em um projeto real, você logaria os detalhes do erro 'ex'
                return StatusCode(500, errorBody);
            }
        }
    }
}