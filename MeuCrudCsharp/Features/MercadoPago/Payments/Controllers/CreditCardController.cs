using System;
using System.Threading.Tasks;
using MercadoPago.Error;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Caching;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Controllers
{
    /// <summary>
    /// Controladora responsável por processar pagamentos com cartão de crédito.
    /// Implementa um mecanismo de idempotência para garantir que uma mesma requisição
    /// de pagamento não seja processada múltiplas vezes.
    /// </summary>
    /// 
    public class CreditCardController : ApiControllerBase
    {
        private const string IDEMPOTENCY_PREFIX = "CreditCardPayment";
        private readonly ICacheService _cacheService;
        private readonly ICreditCardPaymentService _creditCardPaymentService;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="CreditCardController"/>.
        /// </summary>
        /// <param name="cacheService">O serviço de cache para lidar com a idempotência.</param>
        /// <param name="creditCardPaymentService">O serviço que processa a lógica de pagamento com cartão de crédito.</param>
        public CreditCardController(
            ICacheService cacheService,
            ICreditCardPaymentService creditCardPaymentService
        )
        {
            _cacheService = cacheService;
            _creditCardPaymentService = creditCardPaymentService;
        }

        /// <summary>
        /// Processa um pagamento com cartão de crédito ou cria uma assinatura.
        /// </summary>
        /// <remarks>
        /// Este endpoint utiliza um mecanismo de idempotência. É obrigatório o envio do header 'X-Idempotency-Key'
        /// com um valor único para cada tentativa de pagamento.
        ///
        /// O sistema armazena em cache a primeira resposta (sucesso ou erro) para uma dada chave de idempotência.
        /// Requisições subsequentes com a mesma chave retornarão a resposta original em cache,
        /// prevenindo o processamento duplicado.
        /// </remarks>
        /// <param name="request">Os dados da requisição de pagamento.</param>
        /// <returns>O resultado do processamento do pagamento.</returns>
        /// <response code="201">Pagamento processado e criado com sucesso.</response>
        /// <response code="400">A requisição é inválida, o header 'X-Idempotency-Key' está ausente ou ocorreu um erro na API do Mercado Pago.</response>
        /// <response code="401">O usuário não está autenticado.</response>
        /// <response code="500">Ocorreu um erro interno inesperado no servidor.</response>
        [HttpPost("process-payment")]
        public async Task<IActionResult> ProcessPaymentAsync([FromBody] PaymentRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (
                !Request.Headers.TryGetValue("X-Idempotency-Key", out var idempotencyKey)
                || string.IsNullOrEmpty(idempotencyKey)
            )
            {
                return BadRequest(new { message = "O header 'X-Idempotency-Key' é obrigatório." });
            }

            var cacheKey = $"{IDEMPOTENCY_PREFIX}_idempotency_{idempotencyKey}";
            var cachedResponse = await _cacheService.GetAsync<CachedResponse>(cacheKey);

            if (cachedResponse != null)
            {
                Console.WriteLine("--> Resposta retornada do cache de idempotência.");
                return StatusCode(cachedResponse.StatusCode, cachedResponse.Body);
            }

            try
            {
                var result = await _creditCardPaymentService.CreatePaymentOrSubscriptionAsync(
                    request
                );

                var responseToCache = new CachedResponse(result, 201);
                await _cacheService.SetAsync(cacheKey, responseToCache, TimeSpan.FromHours(24));

                return CreatedAtAction(nameof(ProcessPaymentAsync), result);
            }
            catch (MercadoPagoApiException e)
            {
                var errorBody = new { error = "MercadoPago Error", message = e.ApiError.Message };

                var errorResponseToCache = new CachedResponse(errorBody, 400);
                await _cacheService.SetAsync(
                    cacheKey,
                    errorResponseToCache,
                    TimeSpan.FromHours(24)
                );

                return BadRequest(errorBody);
            }
            catch (Exception ex)
            {
                var errorBody = new { message = "Ocorreu um erro inesperado.", error = ex.Message };

                var errorResponseToCache = new CachedResponse(errorBody, 500);
                await _cacheService.SetAsync(
                    cacheKey,
                    errorResponseToCache,
                    TimeSpan.FromHours(24)
                );

                return StatusCode(500, errorBody);
            }
        }
    }
}
