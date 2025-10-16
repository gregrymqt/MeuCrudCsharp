using System.Security.Cryptography;
using System.Text;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Job;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Services;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Webhooks.Services
{
    public class WebhookService : IWebhookService
    {
        private readonly ILogger<WebhookService> _logger;
        private readonly IQueueService _queueService;
        private readonly MercadoPagoSettings _mercadoPagoSettings;

        public WebhookService(
            ILogger<WebhookService> logger,
            IQueueService queueService,
            IOptions<MercadoPagoSettings> mercadoPagoSettings)
        {
            _logger = logger;
            _queueService = queueService;
            _mercadoPagoSettings = mercadoPagoSettings.Value;
        }

        // --- MÉTODO DE VALIDAÇÃO MOVIDO DA CONTROLLER ---
        public bool IsSignatureValid(HttpRequest request, MercadoPagoNotification notification)
        {
            // A implementação exata do seu método IsSignatureValid entra aqui...
            // (Copiei e colei seu método original)
            if (string.IsNullOrEmpty(_mercadoPagoSettings.WebhookSecret))
            {
                _logger.LogWarning("A chave secreta do webhook (MercadoPago:WebhookSecret) não está configurada. Validação da assinatura ignorada.");
                return false;
            }

            try
            {
                if (!request.Headers.TryGetValue("x-request-id", out var xRequestId) || 
                    !request.Headers.TryGetValue("x-signature", out var xSignature))
                {
                    _logger.LogWarning("Headers de assinatura (x-request-id, x-signature) ausentes.");
                    return false;
                }

                var signatureParts = xSignature.ToString().Split(',');
                var ts = signatureParts.FirstOrDefault(p => p.Trim().StartsWith("ts="))?.Split('=')[1];
                var hash = signatureParts.FirstOrDefault(p => p.Trim().StartsWith("v1="))?.Split('=')[1];

                if (string.IsNullOrEmpty(ts) || string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(notification.Data?.Id))
                {
                    _logger.LogWarning("Partes da assinatura (ts, v1) ou data.id estão ausentes.");
                    return false;
                }

                var manifest = $"id:{notification.Data.Id};request-id:{xRequestId};ts:{ts};";
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_mercadoPagoSettings.WebhookSecret));
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

        // --- MÉTODO PARA PROCESSAR E ENFILEIRAR OS TÓPICOS ---
        public async Task ProcessWebhookNotificationAsync(MercadoPagoNotification notification)
        {
            // Usamos um switch para tratar cada tipo de notificação
            switch (notification.Type)
            {
                case "payment":
                    _logger.LogInformation("Enfileirando notificação para o pagamento ID: {PaymentId}", notification.Data.Id);
                    await _queueService.EnqueueJobAsync<ProcessPaymentNotificationJob>(notification.Data.Id);
                    break;
        
                case "subscription_authorized_payment":
                    _logger.LogInformation("Enfileirando pagamento autorizado de assinatura: {SubscriptionPaymentId}", notification.Data.Id);
                    await _queueService.EnqueueJobAsync<ProcessSubscriptionPaymentJob>(notification.Data.Id);
                    break;

                case "subscription_preapproval":
                    _logger.LogInformation("Enfileirando atualização de assinatura: {SubscriptionId}", notification.Data.Id);
                    await _queueService.EnqueueJobAsync<ProcessSubscriptionUpdateJob>(notification.Data.Id);
                    break;

                case "topic_claims_integration_wh":
                    _logger.LogInformation("Enfileirando topic claims integration wh");
                    await _queueService.EnqueueJobAsync<ProcessClaimJob>(notification.Data.Id);
                    break;
                case "topic_card_id_wh":
                    _logger.LogInformation("Enfileirando topic card id");
                    await _queueService.EnqueueJobAsync<ProcessCardUpdateJob>(notification.Data.Id);
                    break;

                default:
                    _logger.LogWarning("Notificação ignorada: tipo '{NotificationType}' não é tratado.", notification.Type);
                    break;
            }
        }
    }
}