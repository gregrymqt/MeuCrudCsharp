// Em Controllers/WebHookController.cs

using MeuCrudCsharp.Features.MercadoPago.Jobs;
using MeuCrudCsharp.Features.MercadoPago.Tokens;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json; // Adicione este using

namespace MeuCrudCsharp.Features.MercadoPago.Webhooks.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // É uma boa prática usar um prefixo como 'api'
    public class WebHookController : ControllerBase
    {
        private readonly ILogger<WebHookController> _logger;
        private readonly TokenMercadoPago _tokenMercadoPago;
        private readonly IQueueService _queueService; // Serviço para enfileirar
        private readonly string _webhookSecret;
        private readonly IHostEnvironment _environment;

        public WebHookController(
            ILogger<WebHookController> logger,
            TokenMercadoPago tokenMercadoPago,
            IQueueService queueService,
            IHostEnvironment environment)
        {
            _logger = logger;
            _tokenMercadoPago = tokenMercadoPago;
            _queueService = queueService;
            _environment = environment;

            // Pega o secret do appsettings.json, muito mais seguro
            _webhookSecret = _tokenMercadoPago._webhook_Secret;
        }

        [HttpPost("mercadopago")] // Rota específica para o webhook do MP
        public async Task<IActionResult> MercadoPagoWebhook([FromBody] MercadoPagoNotification notification)
        {
            // Log do corpo da requisição para depuração
            _logger.LogInformation("Webhook do Mercado Pago recebido: {Payload}", JsonSerializer.Serialize(notification));

            try
            {
                // 1. Validação da Assinatura (essencial para segurança)
                // Apenas em ambiente de produção, para facilitar testes locais.
                if (_environment.IsProduction())
                {
                    if (!IsSignatureValid(Request, notification))
                    {
                        _logger.LogWarning("Assinatura de webhook inválida recebida.");
                        // Retorna 401 Unauthorized, pois a requisição não é confiável
                        return Unauthorized(new { error = "Assinatura inválida" });
                    }
                    _logger.LogInformation("Assinatura de webhook validada com sucesso.");
                }

                // 2. Processa a notificação baseada no tipo
                if (notification.Type != "payment" || string.IsNullOrEmpty(notification.Data?.Id))
                {
                    _logger.LogWarning("Notificação ignorada: tipo não é 'payment' ou data.id está vazio.");
                    // Retornamos 200 OK para que o MP não tente reenviar uma notificação malformada.
                    return Ok(new { status = "ignorado" });
                }

                _logger.LogInformation("Enfileirando notificação para o pagamento ID: {PaymentId}", notification.Data.Id);

                // 3. Envia o ID do pagamento para a fila processar em segundo plano
                await _queueService.EnqueuePaymentNotificationAsync(notification.Data.Id);

                // 4. Retorna resposta imediata para o Mercado Pago
                return Ok(new { status = "enfileirado" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao processar webhook do Mercado Pago.");
                return StatusCode(500, new { error = "Erro interno ao processar notificação." });
            }
        }

        private bool IsSignatureValid(HttpRequest request, MercadoPagoNotification notification)
        {
            try
            {
                // Obtém os headers necessários
                if (!request.Headers.TryGetValue("x-request-id", out var xRequestId) ||
                    !request.Headers.TryGetValue("x-signature", out var xSignature))
                {
                    _logger.LogWarning("Headers de assinatura ausentes.");
                    return false;
                }

                // Obtém o data.id do corpo da requisição
                var dataId = notification.Data?.Id;
                if (string.IsNullOrEmpty(dataId))
                {
                    _logger.LogWarning("data.id não encontrado no corpo da notificação para validação.");
                    return false;
                }

                // Separa a assinatura em partes (ts e v1)
                var signatureParts = xSignature.ToString().Split(',');
                string ts = null;
                string hash = null;

                foreach (var part in signatureParts)
                {
                    var keyValue = part.Trim().Split('=', 2);
                    if (keyValue.Length == 2)
                    {
                        if (keyValue[0] == "ts") ts = keyValue[1];
                        else if (keyValue[0] == "v1") hash = keyValue[1];
                    }
                }

                if (string.IsNullOrEmpty(ts) || string.IsNullOrEmpty(hash))
                {
                    _logger.LogWarning("Timestamp (ts) ou hash (v1) não encontrados no header x-signature.");
                    return false;
                }

                // Gera a string de manifesto no formato oficial
                var manifest = $"id:{dataId};request-id:{xRequestId};ts:{ts};";

                // Calcula o hash HMAC usando SHA-256
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret));
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
                var calculatedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                // Compara os hashes de forma segura
                var isValid = calculatedHash.Equals(hash);

                if (!isValid)
                {
                    _logger.LogWarning("Assinatura HMAC inválida. Hash Recebido: {ReceivedHash}, Hash Calculado: {CalculatedHash}, Manifesto: {Manifest}", hash, calculatedHash, manifest);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante a validação da assinatura do webhook.");
                return false;
            }
        }
    }
}