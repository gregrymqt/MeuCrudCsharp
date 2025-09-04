using System.Security.Claims;
using MeuCrudCsharp.Features.Base;
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
        private readonly MercadoPagoSettings _mpSettings;

        // Injeção de Dependências: Logger, a futura Service e as Configurações
        public PixController(
            ILogger<PixController> logger,
            IPixPaymentService paymentService,
            IOptions<MercadoPagoSettings> mpSettings)
        {
            _logger = logger;
            _paymentService = paymentService;
            _mpSettings = mpSettings.Value;
        }

        /// <summary>
        /// Endpoint público para obter a Public Key do Mercado Pago.
        /// </summary>
        [HttpGet("getpublickey")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult GetPublicKey()
        {
            _logger.LogInformation("Endpoint GetPublicKey foi chamado.");
            return Ok(new { publicKey = _mpSettings.PublicKey });
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
            _logger.LogInformation("Endpoint CreatePixPayment foi chamado.");

            // 1. Obter o ID do usuário logado a partir do token JWT ou cookie de sessão
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Tentativa de criar pagamento sem um usuário autenticado.");
                return Unauthorized(new { message = "Usuário não autenticado." });
            }

            _logger.LogInformation("Usuário ID: {UserId} está criando um pagamento PIX.", userId);

            try
            {
                // 2. Chamar a service, passando o usuário e os dados do pagamento
                var response = await _paymentService.CreatePixPaymentAsync(userId, request);

                // 3. Retornar a resposta da service (QR Code, etc.) para o front-end
                return Ok(response);
            }
            catch (Exception ex)
            {
                // Em um caso real, você pode ter exceções customizadas para diferentes erros
                _logger.LogError(ex, "Erro ao criar pagamento PIX para o usuário {UserId}.", userId);
                return BadRequest(new { message = $"Ocorreu um erro ao processar o pagamento: {ex.Message}" });
            }
        }
    }
