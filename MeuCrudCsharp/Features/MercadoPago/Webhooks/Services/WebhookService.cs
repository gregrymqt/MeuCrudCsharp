using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Job;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
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
            IOptions<MercadoPagoSettings> mercadoPagoSettings
        )
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
                _logger.LogWarning(
                    "A chave secreta do webhook (MercadoPago:WebhookSecret) não está configurada. Validação da assinatura ignorada."
                );
                return false;
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

                if (string.IsNullOrEmpty(ts) || string.IsNullOrEmpty(hash))
                {
                    _logger.LogWarning("Partes da assinatura (ts, v1) ou data.id estão ausentes.");
                    return false;
                }

                var dataId = notification.Data?.GetProperty("id").GetString();

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

        // --- MÉTODO PARA PROCESSAR E ENFILEIRAR OS TÓPICOS ---
        public async Task ProcessWebhookNotificationAsync(MercadoPagoNotification notification)
        {
            if (!notification.Data.HasValue)
            {
                _logger.LogWarning("Notificação recebida sem o campo 'data'. Ignorando.");
                return;
            }

            var data = notification.Data.Value;

            // Usamos um switch para tratar cada tipo de notificação
            switch (notification.Type)
            {
                case "payment":
                    var paymentData = data.Deserialize<PaymentNotificationData>();
                    _logger.LogInformation(
                        "Enfileirando notificação para o pagamento ID: {PaymentId}",
                        paymentData.Id
                    );
                    await _queueService.EnqueueJobAsync<
                        ProcessPaymentNotificationJob,
                        PaymentNotificationData
                    >(paymentData);
                    break;

                case "subscription_authorized_payment":
                    var subPaymentData = data.Deserialize<PaymentNotificationData>();
                    _logger.LogInformation(
                        "Enfileirando pagamento autorizado de assinatura: {SubscriptionPaymentId}",
                        subPaymentData.Id
                    );
                    await _queueService.EnqueueJobAsync<
                        ProcessSubscriptionPaymentJob,
                        PaymentNotificationData
                    >(subPaymentData);
                    break;

                case "subscription_preapproval_plan":
                case "subscription_preapproval": // Adicionando o tipo de notificação de criação/atualização de assinatura
                    var subPreapprovalData = data.Deserialize<PaymentNotificationData>();
                    _logger.LogInformation(
                        "Enfileirando atualização de assinatura: {SubscriptionId}",
                        subPreapprovalData.Id
                    );
                    await _queueService.EnqueueJobAsync<
                        ProcessCreateSubscriptionJob,
                        PaymentNotificationData
                    >(subPreapprovalData);
                    break;

                case "topic_claims_integration_wh":
                    var claimData = data.Deserialize<ClaimNotificationPayload>();
                    _logger.LogInformation(
                        "Enfileirando notificação de Claim ID: {ClaimId}",
                        claimData.Id
                    );
                    await _queueService.EnqueueJobAsync<ProcessClaimJob, ClaimNotificationPayload>(
                        claimData
                    );
                    break;

                case "topic_card_id_wh":
                    var cardUpdateData = data.Deserialize<CardUpdateNotificationPayload>();
                    _logger.LogInformation(
                        "Enfileirando notificação de atualização de cartão para o cliente: {CustomerId}",
                        cardUpdateData.CustomerId
                    );
                    await _queueService.EnqueueJobAsync<
                        ProcessCardUpdateJob,
                        CardUpdateNotificationPayload
                    >(cardUpdateData);
                    break;

                case "chargebacks":
                    var chargebackData = data.Deserialize<ChargebackNotificationPayload>();
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
                    _logger.LogWarning(
                        "Notificação ignorada: tipo '{NotificationType}' não é tratado.",
                        notification.Type
                    );
                    break;
            }
        }
    }
}
