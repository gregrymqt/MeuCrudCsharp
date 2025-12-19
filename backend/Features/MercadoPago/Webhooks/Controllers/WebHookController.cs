using System.Text.Json;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.Services;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.MercadoPago.Webhooks.Controllers
{
    [ApiController]
    [Route("webhook")]
    public class WebHookController : ControllerBase // Usei ControllerBase padrão, mude se tiver uma Base específica
    {
        private readonly ILogger<WebHookController> _logger;
        private readonly IWebhookService _webhookService;

        public WebHookController(ILogger<WebHookController> logger, IWebhookService webhookService)
        {
            _logger = logger;
            _webhookService = webhookService;
        }

        [HttpPost("mercadopago")]
        public async Task<IActionResult> MercadoPagoWebhook(
            [FromBody] MercadoPagoWebhookNotification notification
        )
        {
            // Logar o corpo pode ser útil para debug, mas cuidado com dados sensíveis em produção
            _logger.LogInformation(
                "Webhook recebido: Tipo={Type}, Action={Action}, ID={Id}",
                notification.Type,
                notification.Action,
                notification.Data.ToString()
            );

            try
            {
                // 1. Validação
                if (!_webhookService.IsSignatureValid(Request, notification))
                {
                    // Retornar BadRequest faz o MP tentar de novo?
                    // Geralmente sim. Se a assinatura falhar, é segurança, então recusamos.
                    return BadRequest(new { error = "Assinatura inválida." });
                }

                // 2. Processamento
                await _webhookService.ProcessWebhookNotificationAsync(notification);

                // Retorna 200/202 para o Mercado Pago parar de enviar
                return Accepted(new { status = "received" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro fatal no endpoint de Webhook.");
                // Retornar 500 faz o MP tentar novamente mais tarde (com backoff exponencial)
                return StatusCode(500, new { error = "Erro interno." });
            }
        }
    }
}
