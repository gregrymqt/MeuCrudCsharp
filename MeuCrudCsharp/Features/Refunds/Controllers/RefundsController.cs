using MeuCrudCsharp.Features.Refunds.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Refunds.Controllers
{
    [ApiController]
    [Route("api/profile")] // Rota base
    [Authorize] // Garante que apenas usuários logados podem acessar
    public class RefundsController : ControllerBase
    {
        private readonly RefundService _refundService;
        private readonly ILogger<RefundsController> _logger;

        public RefundsController(RefundService refundService, ILogger<RefundsController> logger)
        {
            _refundService = refundService;
            _logger = logger;
        }

        [HttpPost("request-refund")]
        public async Task<IActionResult> RequestRefund()
        {
            try
            {
                await _refundService.RequestUserRefundAsync();
                return Ok(new { message = "Solicitação de reembolso processada com sucesso." });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Falha na regra de negócio ao solicitar reembolso.");
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao comunicar com a API do Mercado Pago durante o reembolso."
                );
                return StatusCode(
                    502,
                    new
                    {
                        message = "Houve um erro ao processar seu reembolso com nosso provedor de pagamentos. Tente novamente mais tarde.",
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao processar a solicitação de reembolso.");
                return StatusCode(
                    500,
                    new { message = "Ocorreu um erro interno. Nossa equipe já foi notificada." }
                );
            }
        }
    }
}
