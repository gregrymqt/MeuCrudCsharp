using System.Security.Cryptography;
using System.Text;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Job;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.Interfaces;
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
            IOptions<MercadoPagoSettings> mercadoPagoSettings
        )
        {
            _logger = logger;
            _queueService = queueService;
            _mercadoPagoSettings = mercadoPagoSettings.Value;
        }

        public bool IsSignatureValid(
            HttpRequest request,
            MercadoPagoWebhookNotification notification
        )
        {
            if (string.IsNullOrEmpty(_mercadoPagoSettings.WebhookSecret))
            {
                _logger.LogWarning("WebhookSecret não configurado. Validação ignorada.");
                return false;
            }

            try
            {
                if (
                    !request.Headers.TryGetValue("x-request-id", out var xRequestId)
                    || !request.Headers.TryGetValue("x-signature", out var xSignature)
                )
                {
                    _logger.LogWarning("Headers de assinatura ausentes.");
                    return false;
                }

                var signatureParts = xSignature.ToString().Split(',');
                var ts = signatureParts
                    .FirstOrDefault(p => p.Trim().StartsWith("ts="))
                    ?.Split('=')[1];
                var hash = signatureParts
                    .FirstOrDefault(p => p.Trim().StartsWith("v1="))
                    ?.Split('=')[1];

                if (string.IsNullOrEmpty(ts) || string.IsNullOrEmpty(hash))
                {
                    _logger.LogWarning("Falha ao extrair ts ou v1 da assinatura.");
                    return false;
                }

                // --- CORREÇÃO 1: Acesso direto ao objeto, sem TryGetProperty ---
                if (notification.Data == null || string.IsNullOrEmpty(notification.Data.Id))
                {
                    _logger.LogWarning("Payload sem Data.Id para validação.");
                    return false;
                }

                var dataId = notification.Data.Id;
                // ----------------------------------------------------------------

                var manifest = $"id:{dataId};request-id:{xRequestId};ts:{ts};";

                using var hmac = new HMACSHA256(
                    Encoding.UTF8.GetBytes(_mercadoPagoSettings.WebhookSecret)
                );
                var calculatedHash = BitConverter
                    .ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest)))
                    .Replace("-", "")
                    .ToLower();

                if (!calculatedHash.Equals(hash))
                {
                    _logger.LogWarning(
                        "Assinatura inválida. Recebido: {Hash}, Calculado: {Calc}",
                        hash,
                        calculatedHash
                    );
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro na validação da assinatura.");
                return false;
            }
        }

        public async Task ProcessWebhookNotificationAsync(
            MercadoPagoWebhookNotification notification
        )
        {
            // --- CORREÇÃO 2: Verificação de nulo padrão do C# ---
            if (notification.Data == null || string.IsNullOrEmpty(notification.Data.Id))
            {
                _logger.LogWarning(
                    "Notificação recebida sem dados válidos (Data null ou Id vazio)."
                );
                return;
            }
            // ----------------------------------------------------

            try
            {
                // Como o objeto já veio deserializado, nós apenas repassamos o ID para os Jobs.
                // O Webhook do MP geralmente só manda o ID dentro do Data mesmo.

                string entityId = notification.Data.Id;

                switch (notification.Type)
                {
                    case "payment":
                        // --- CORREÇÃO 3: Criamos o DTO manualmente em vez de deserializar de novo ---
                        var paymentData = new PaymentNotificationData { Id = entityId };

                        _logger.LogInformation("Job Pagamento ID: {Id}", paymentData.Id);
                        await _queueService.EnqueueJobAsync<
                            ProcessPaymentNotificationJob,
                            PaymentNotificationData
                        >(paymentData);
                        break;

                    case "subscription_authorized_payment":
                        var subPaymentData = new PaymentNotificationData { Id = entityId };

                        _logger.LogInformation(
                            "Job Assinatura Pagamento ID: {Id}",
                            subPaymentData.Id
                        );
                        await _queueService.EnqueueJobAsync<
                            ProcessRenewalSubscriptionJob,
                            PaymentNotificationData
                        >(subPaymentData);
                        break;

                    case "subscription_preapproval_plan":
                        var planData = new PaymentNotificationData { Id = entityId };

                        _logger.LogInformation("Job Plano ID: {Id}", planData.Id);
                        await _queueService.EnqueueJobAsync<
                            ProcessPlanSubscriptionJob,
                            PaymentNotificationData
                        >(planData);
                        break;

                    case "subscription_preapproval":
                        var subData = new PaymentNotificationData { Id = entityId };

                        _logger.LogInformation("Job Assinatura ID: {Id}", subData.Id);
                        await _queueService.EnqueueJobAsync<
                            ProcessCreateSubscriptionJob,
                            PaymentNotificationData
                        >(subData);
                        break;

                    case "claim":
                        // Atenção: Certifique-se que ClaimNotificationPayload tem a propriedade Id compatível
                        var claimData = new ClaimNotificationPayload { Id = entityId };

                        _logger.LogInformation("Job Claim ID: {Id}", claimData.Id);
                        await _queueService.EnqueueJobAsync<
                            ProcessClaimJob,
                            ClaimNotificationPayload
                        >(claimData);
                        break;

                    case "automatic-payments":
                        // Atenção: Aqui o ID costuma ser do cliente ou do cartão. Ajuste conforme sua DTO.
                        var cardData = new CardUpdateNotificationPayload { CustomerId = entityId }; // Suposição baseada no log anterior

                        _logger.LogInformation("Job Cartão Cliente: {Id}", cardData.CustomerId);
                        await _queueService.EnqueueJobAsync<
                            ProcessCardUpdateJob,
                            CardUpdateNotificationPayload
                        >(cardData);
                        break;

                    case "chargeback":
                    case "topic_chargebacks_wh":
                        var chargebackData = new ChargebackNotificationPayload { Id = entityId };

                        _logger.LogInformation(
                            "Enfileirando notificação de Chargeback ID: {ChargebackId}",
                            chargebackData.Id
                        );
                        await _queueService.EnqueueJobAsync<
                            ProcessChargebackJob,
                            ChargebackNotificationPayload
                        >(chargebackData);
                        break;

                    default:
                        _logger.LogWarning("Tipo '{Type}' não tratado.", notification.Type);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao processar payload do webhook tipo {Type}",
                    notification.Type
                );
            }
        }
    }
}
