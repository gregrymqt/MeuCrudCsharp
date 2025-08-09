using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Jobs;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.MercadoPago.Webhooks.Controllers
{
    /// <summary>
    /// Controladora responsável por receber, validar e enfileirar webhooks do Mercado Pago.
    /// </summary>
    [ApiController]
    [Route("webhook")]
    public class WebHookController : ControllerBase
    {
        private readonly ILogger<WebHookController> _logger;
        private readonly IQueueService _queueService;
        private readonly string? _webhookSecret;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="WebHookController"/>.
        /// </summary>
        /// <param name="logger">O serviço de logging.</param>
        /// <param name="queueService">O serviço para enfileirar tarefas em segundo plano.</param>
        /// <param name="configuration">A configuração da aplicação para obter a chave secreta do webhook.</param>
        public WebHookController(
            ILogger<WebHookController> logger,
            IQueueService queueService,
            IConfiguration configuration
        )
        {
            _logger = logger;
            _queueService = queueService;
            _webhookSecret = configuration["MercadoPago:WebhookSecret"];
        }

        /// <summary>
        /// Endpoint para receber notificações de webhook do Mercado Pago.
        /// </summary>
        /// <remarks>
        /// Este endpoint valida a assinatura da requisição para garantir sua autenticidade.
        /// Se a notificação for válida e do tipo 'payment', ela é enfileirada para processamento assíncrono.
        /// Retornar um status 500 fará com que o Mercado Pago tente reenviar o webhook.
        /// </remarks>
        /// <param name="notification">O payload da notificação enviada pelo Mercado Pago.</param>
        /// <returns>
        /// <see cref="AcceptedResult"/> (202) se a notificação for enfileirada com sucesso.
        /// <see cref="OkObjectResult"/> (200) se a notificação for ignorada intencionalmente.
        /// <see cref="BadRequestObjectResult"/> (400) se a assinatura for inválida.
        /// <see cref="ObjectResult"/> (500) se ocorrer um erro interno que impeça o enfileiramento.
        /// </returns>
        [HttpPost("mercadopago")]
        public async Task<IActionResult> MercadoPagoWebhook(
            [FromBody] MercadoPagoNotification notification
        )
        {
            _logger.LogInformation(
                "Webhook do Mercado Pago recebido: {Payload}",
                JsonSerializer.Serialize(notification)
            );

            try
            {
                if (!IsSignatureValid(Request, notification))
                {
                    return BadRequest(new { error = "Assinatura inválida." });
                }

                _logger.LogInformation("Assinatura de webhook validada com sucesso.");

                if (notification.Type != "payment" || string.IsNullOrEmpty(notification.Data?.Id))
                {
                    _logger.LogWarning(
                        "Notificação ignorada: tipo não é 'payment' ou data.id está vazio."
                    );
                    return Ok(new { status = "ignorado" });
                }

                _logger.LogInformation(
                    "Enfileirando notificação para o pagamento ID: {PaymentId}",
                    notification.Data.Id
                );

                await _queueService.EnqueuePaymentNotificationAsync(notification.Data.Id);

                return Accepted(new { status = "enfileirado" });
            }
            catch (AppServiceException ex)
            {
                _logger.LogError(
                    ex,
                    "Falha de serviço ao tentar enfileirar a notificação do webhook."
                );
                return StatusCode(
                    500,
                    new { error = "Erro interno ao agendar o processamento da notificação." }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao processar webhook do Mercado Pago.");
                return StatusCode(500, new { error = "Erro interno no processamento do webhook." });
            }
        }

        /// <summary>
        /// Valida a assinatura HMAC-SHA256 da requisição do webhook.
        /// </summary>
        /// <remarks>
        /// Se a chave secreta não estiver configurada, a validação será ignorada em ambiente de DEBUG,
        /// mas falhará em qualquer outro ambiente para evitar uma falha de segurança em produção.
        /// </remarks>
        /// <param name="request">A requisição HTTP recebida.</param>
        /// <param name="notification">O corpo da notificação deserializado.</param>
        /// <returns><c>true</c> se a assinatura for válida ou se a validação for ignorada em DEBUG; caso contrário, <c>false</c>.</returns>
        private bool IsSignatureValid(HttpRequest request, MercadoPagoNotification notification)
        {
            if (string.IsNullOrEmpty(_webhookSecret))
            {
                _logger.LogWarning(
                    "A chave secreta do webhook (MercadoPago:WebhookSecret) não está configurada. Validação da assinatura ignorada."
                );
#if DEBUG
                return true;
#else
                _logger.LogError(
                    "FALHA DE SEGURANÇA: WebhookSecret não configurado em ambiente de Produção."
                );
                return false;
#endif
            }

            try
            {
                if (
                    !request.Headers.TryGetValue("x-request-id", out var xRequestId)
                    || !request.Headers.TryGetValue("x-signature", out var xSignature)
                )
                {
                    _logger.LogWarning(
                        "Headers de assinatura (x-request-id, x-signature) ausentes."
                    );
                    return false;
                }

                var signatureParts = xSignature.ToString().Split(',');
                var ts = signatureParts
                    .FirstOrDefault(p => p.Trim().StartsWith("ts="))
                    ?.Split('=')[1];
                var hash = signatureParts
                    .FirstOrDefault(p => p.Trim().StartsWith("v1="))
                    ?.Split('=')[1];

                if (
                    string.IsNullOrEmpty(ts)
                    || string.IsNullOrEmpty(hash)
                    || string.IsNullOrEmpty(notification.Data?.Id)
                )
                {
                    _logger.LogWarning("Partes da assinatura (ts, v1) ou data.id estão ausentes.");
                    return false;
                }

                var manifest = $"id:{notification.Data.Id};request-id:{xRequestId};ts:{ts};";
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret));
                var calculatedHash = BitConverter
                    .ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest)))
                    .Replace("-", "")
                    .ToLower();

                if (!calculatedHash.Equals(hash))
                {
                    _logger.LogWarning(
                        "Assinatura HMAC inválida. Hash Recebido: {ReceivedHash}, Hash Calculado: {CalculatedHash}",
                        hash,
                        calculatedHash
                    );
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante a validação da assinatura do webhook.");
                return false;
            }
        }
    }
}
