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
    [ApiController]
    [Route("webhook")] // Rota simplificada para webhook.mercadopago
    public class WebHookController : ControllerBase
    {
        private readonly ILogger<WebHookController> _logger;
        private readonly IQueueService _queueService;
        private readonly string? _webhookSecret;

        // MUDANÇA 1: Removida a dependência direta do IHostEnvironment no controller.
        // A lógica de validação da assinatura agora é autocontida.
        public WebHookController(
            ILogger<WebHookController> logger,
            IQueueService queueService,
            IConfiguration configuration) // Injetando IConfiguration para pegar a chave
        {
            _logger = logger;
            _queueService = queueService;

            // Pega a chave secreta da configuração. É uma boa prática centralizar o acesso.
            _webhookSecret = configuration["MercadoPago:WebhookSecret"];
        }

        [HttpPost("mercadopago")]
        public async Task<IActionResult> MercadoPagoWebhook([FromBody] MercadoPagoNotification notification)
        {
            _logger.LogInformation("Webhook do Mercado Pago recebido: {Payload}", JsonSerializer.Serialize(notification));

            try
            {
                // MUDANÇA 2: Validação da assinatura movida para o início e simplificada.
                // A validação agora acontece em todos os ambientes se a chave secreta estiver configurada.
                if (!IsSignatureValid(Request, notification))
                {
                    // Retorna 400 Bad Request, que é mais apropriado para uma assinatura inválida.
                    return BadRequest(new { error = "Assinatura inválida." });
                }

                _logger.LogInformation("Assinatura de webhook validada com sucesso.");

                if (notification.Type != "payment" || string.IsNullOrEmpty(notification.Data?.Id))
                {
                    _logger.LogWarning("Notificação ignorada: tipo não é 'payment' ou data.id está vazio.");
                    // Retornamos 200 OK para que o MP não tente reenviar.
                    return Ok(new { status = "ignorado" });
                }

                _logger.LogInformation("Enfileirando notificação para o pagamento ID: {PaymentId}", notification.Data.Id);

                // A chamada ao serviço de fila agora está dentro de um try-catch.
                await _queueService.EnqueuePaymentNotificationAsync(notification.Data.Id);

                // 202 Accepted é um status HTTP semanticamente correto para "Recebi sua requisição e vou processá-la em segundo plano".
                return Accepted(new { status = "enfileirado" });
            }
            // MUDANÇA 3: Capturando nossas exceções customizadas do serviço de fila
            catch (AppServiceException ex)
            {
                // Este erro significa que não conseguimos ENFILEIRAR o job (ex: Redis fora do ar).
                // É um erro crítico do nosso lado.
                _logger.LogError(ex, "Falha de serviço ao tentar enfileirar a notificação do webhook.");
                // Retornar 500 faz com que o Mercado Pago tente enviar o webhook novamente mais tarde, o que é o desejado.
                return StatusCode(500, new { error = "Erro interno ao agendar o processamento da notificação." });
            }
            catch (Exception ex)
            {
                // Captura qualquer outro erro inesperado no próprio controller.
                _logger.LogError(ex, "Erro inesperado ao processar webhook do Mercado Pago.");
                return StatusCode(500, new { error = "Erro interno no processamento do webhook." });
            }
        }

        private bool IsSignatureValid(HttpRequest request, MercadoPagoNotification notification)
        {
            // MUDANÇA 4: Validação "Fail-Fast" da chave secreta.
            if (string.IsNullOrEmpty(_webhookSecret))
            {
                _logger.LogWarning("A chave secreta do webhook (MercadoPago:WebhookSecret) não está configurada. Validação da assinatura ignorada.");
                // Em desenvolvimento, permite que o webhook passe sem validação.
                // Em produção, isso deve ser considerado um erro de configuração.
#if DEBUG
                return true;
#else
                    _logger.LogError("FALHA DE SEGURANÇA: WebhookSecret não configurado em ambiente de Produção.");
                    return false;
#endif
            }

            try
            {
                if (!request.Headers.TryGetValue("x-request-id", out var xRequestId) ||
                    !request.Headers.TryGetValue("x-signature", out var xSignature))
                {
                    _logger.LogWarning("Headers de assinatura (x-request-id, x-signature) ausentes.");
                    return false;
                }

                // ... (o restante da sua lógica de validação HMAC está perfeita e permanece a mesma) ...

                var signatureParts = xSignature.ToString().Split(',');
                var ts = signatureParts.FirstOrDefault(p => p.Trim().StartsWith("ts="))?.Split('=')[1];
                var hash = signatureParts.FirstOrDefault(p => p.Trim().StartsWith("v1="))?.Split('=')[1];

                if (string.IsNullOrEmpty(ts) || string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(notification.Data?.Id))
                {
                    _logger.LogWarning("Partes da assinatura (ts, v1) ou data.id estão ausentes.");
                    return false;
                }

                var manifest = $"id:{notification.Data.Id};request-id:{xRequestId};ts:{ts};";
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret));
                var calculatedHash = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest))).Replace("-", "").ToLower();

                if (!calculatedHash.Equals(hash))
                {
                    _logger.LogWarning("Assinatura HMAC inválida. Hash Recebido: {ReceivedHash}, Hash Calculado: {CalculatedHash}", hash, calculatedHash);
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