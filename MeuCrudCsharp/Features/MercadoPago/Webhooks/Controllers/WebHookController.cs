using System.Text.Json;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.MercadoPago.Webhooks.Controllers
{
    [ApiController]
    [Route("webhook")]
    public class WebHookController : ApiControllerBase
    {
        private readonly ILogger<WebHookController> _logger;
        private readonly IWebhookService _webhookService; // Injeta o novo serviço

        public WebHookController(
            ILogger<WebHookController> logger,
            IWebhookService webhookService // Remove as outras dependências
        )
        {
            _logger = logger;
            _webhookService = webhookService;
        }

        [HttpPost("mercadopago")]
        public async Task<IActionResult> MercadoPagoWebhook([FromBody] MercadoPagoNotification notification)
        {
            _logger.LogInformation("Webhook do Mercado Pago recebido: {Payload}", JsonSerializer.Serialize(notification));

            try
            {
                // 1. Delega a validação para o serviço
                if (!_webhookService.IsSignatureValid(Request, notification))
                {
                    _logger.LogWarning("Assinatura de webhook inválida.");
                    return BadRequest(new { error = "Assinatura inválida." });
                }

                _logger.LogInformation("Assinatura de webhook validada com sucesso.");

                // 2. Delega o processamento e enfileiramento para o serviço
                await _webhookService.ProcessWebhookNotificationAsync(notification);

                return Accepted(new { status = "enfileirado_para_processamento" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao processar webhook do Mercado Pago.");
                return StatusCode(500, new { error = "Erro interno no processamento do webhook." });
            }
        }

    }
}