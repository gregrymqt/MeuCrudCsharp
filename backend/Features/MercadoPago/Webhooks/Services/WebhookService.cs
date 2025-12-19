using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Job;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
using Microsoft.Extensions.Options;

// Adicione os usings dos seus Jobs e QueueService aqui

namespace MeuCrudCsharp.Features.MercadoPago.Webhooks.Services
{
    public interface IWebhookService
    {
        bool IsSignatureValid(HttpRequest request, MercadoPagoWebhookNotification notification);
        Task ProcessWebhookNotificationAsync(MercadoPagoWebhookNotification notification);
    }

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
                return false; // Ou true, dependendo da sua política de dev
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

                // CORREÇÃO: Com JsonElement, o GetProperty funciona corretamente agora
                if (!notification.Data.TryGetProperty("id", out var idElement))
                {
                    _logger.LogWarning("Payload sem Data.Id para validação.");
                    return false;
                }

                var dataId = idElement.ToString();
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
            // CORREÇÃO: Verificação correta de JsonElement nulo/indefinido
            if (
                notification.Data.ValueKind == JsonValueKind.Null
                || notification.Data.ValueKind == JsonValueKind.Undefined
            )
            {
                _logger.LogWarning("Notificação recebida sem dados válidos (Data null/undefined).");
                return;
            }

            // CORREÇÃO: Removemos o ponto extra que causava erro CS1001
            var dataElement = notification.Data;

            // Opções para garantir case-insensitive (snake_case -> PascalCase se precisar)
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            try
            {
                switch (notification.Type)
                {
                    case "payment":
                        // Deserialize direto do JsonElement
                        var paymentData = JsonSerializer.Deserialize<PaymentNotificationData>(
                            dataElement.GetRawText(),
                            jsonOptions
                        );
                        if (paymentData != null)
                        {
                            _logger.LogInformation("Job Pagamento ID: {Id}", paymentData.Id);
                            await _queueService.EnqueueJobAsync<
                                ProcessPaymentNotificationJob,
                                PaymentNotificationData
                            >(paymentData);
                        }
                        break;

                    case "subscription_authorized_payment":
                        var subPaymentData = JsonSerializer.Deserialize<PaymentNotificationData>(
                            dataElement.GetRawText(),
                            jsonOptions
                        );
                        if (subPaymentData != null)
                        {
                            _logger.LogInformation(
                                "Job Assinatura Pagamento ID: {Id}",
                                subPaymentData.Id
                            );
                            await _queueService.EnqueueJobAsync<
                                ProcessRenewalSubscriptionJob,
                                PaymentNotificationData
                            >(subPaymentData);
                        }
                        break;

                    case "subscription_preapproval_plan":
                        var planData = JsonSerializer.Deserialize<PaymentNotificationData>(
                            dataElement.GetRawText(),
                            jsonOptions
                        );
                        if (planData != null)
                        {
                            _logger.LogInformation("Job Plano ID: {Id}", planData.Id);
                            await _queueService.EnqueueJobAsync<
                                ProcessPlanSubscriptionJob,
                                PaymentNotificationData
                            >(planData);
                        }
                        break;

                    case "subscription_preapproval":
                        var subData = JsonSerializer.Deserialize<PaymentNotificationData>(
                            dataElement.GetRawText(),
                            jsonOptions
                        );
                        if (subData != null)
                        {
                            _logger.LogInformation("Job Assinatura ID: {Id}", subData.Id);
                            await _queueService.EnqueueJobAsync<
                                ProcessCreateSubscriptionJob,
                                PaymentNotificationData
                            >(subData);
                        }
                        break;

                    case "claim":
                        var claimData = JsonSerializer.Deserialize<ClaimNotificationPayload>(
                            dataElement.GetRawText(),
                            jsonOptions
                        );
                        if (claimData != null)
                        {
                            _logger.LogInformation("Job Claim ID: {Id}", claimData.Id);
                            await _queueService.EnqueueJobAsync<
                                ProcessClaimJob,
                                ClaimNotificationPayload
                            >(claimData);
                        }
                        break;

                    case "automatic-payments":
                        var cardData = JsonSerializer.Deserialize<CardUpdateNotificationPayload>(
                            dataElement.GetRawText(),
                            jsonOptions
                        );
                        if (cardData != null)
                        {
                            _logger.LogInformation("Job Cartão Cliente: {Id}", cardData.CustomerId);
                            await _queueService.EnqueueJobAsync<
                                ProcessCardUpdateJob,
                                CardUpdateNotificationPayload
                            >(cardData);
                        }
                        break;

                    // O SEU FOCO: CHARGEBACKS
                    case "chargeback": // As vezes o MP manda type "chargeback"
                    case "topic_chargebacks_wh": // Documentação antiga as vezes cita esse topic
                        var chargebackData =
                            JsonSerializer.Deserialize<ChargebackNotificationPayload>(
                                dataElement.GetRawText(),
                                jsonOptions
                            );
                        if (chargebackData != null)
                        {
                            _logger.LogInformation(
                                "Enfileirando notificação de Chargeback ID: {ChargebackId}",
                                chargebackData.Id
                            );
                            await _queueService.EnqueueJobAsync<
                                ProcessChargebackJob,
                                ChargebackNotificationPayload
                            >(chargebackData);
                        }
                        break;

                    default:
                        _logger.LogWarning("Tipo '{Type}' não tratado.", notification.Type);
                        break;
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(
                    jsonEx,
                    "Erro ao deserializar payload do webhook tipo {Type}",
                    notification.Type
                );
            }
        }
    }
}
