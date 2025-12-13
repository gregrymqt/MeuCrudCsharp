using System.Security.Claims;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Controllers;

    [Route("api/[controller]")]
    public class PixController : ControllerBase
    {
        private readonly ILogger<PixController> _logger;
        private readonly IPixPaymentService _paymentService;
        
        // Injeção de Dependências: Logger, a futura Service e as Configurações
        public PixController(
            ILogger<PixController> logger,
            IPixPaymentService paymentService)
        {
            _logger = logger;
            _paymentService = paymentService;
        }
        
        /// <summary>
        /// Cria uma intenção de pagamento PIX. Requer autenticação.
        /// </summary>
        /// <param name="request">Dados do pagamento e do pagador.</param>
        [HttpPost("createpix")]
        [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreatePixPayment([FromBody] CreatePixPaymentRequest request)
        {
            // 1. Adicionar a validação do header de idempotência
            if (!Request.Headers.TryGetValue("X-Idempotency-Key", out var idempotencyKey) ||
                string.IsNullOrEmpty(idempotencyKey))
            {
                return BadRequest(new { message = "O header 'X-Idempotency-Key' é obrigatório." });
            }

            try
            {
                // 2. Chamar o novo método no serviço, passando a chave
                var response = await _paymentService.CreateIdempotentPixPaymentAsync(request, idempotencyKey.ToString());
        
                // 3. Montar a resposta HTTP com base no resultado do serviço
                return StatusCode(response.StatusCode, response.Body);
            }
            catch (AppServiceException ex) // Exceção de serviço pode ser tratada aqui
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao criar o pix");
                return StatusCode(500, new { message = "Ocorreu um erro inesperado." });
            }
        }
    }
