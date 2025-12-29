using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs
{
    public class MercadoPagoWebhookNotification
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } // ID da notificação ou do recurso

        [JsonPropertyName("type")]
        public string Type { get; set; } // ex: "chargeback"

        [JsonPropertyName("date_created")]
        public DateTime DateCreated { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; } // ex: "chargeback.created" ou "chargeback.updated"

        [JsonPropertyName("data")]
        public MercadoPagoWebhookData Data { get; set; }
    }

    public class MercadoPagoWebhookData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } // AQUI ESTÁ O ID DO CHARGEBACK
    }

    // DTO para o Payload do Chargeback
    public class ChargebackNotificationPayload
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }

    // DTO genérico para Pagamentos/Assinaturas (se forem iguais)
    public class PaymentNotificationData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }

    // DTO para Claims, caso use
    public class ClaimNotificationPayload
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }

    // DTO para Updates de Cartão
    public class CardUpdateNotificationPayload
    {
        [JsonPropertyName("id")]
        public string NewCardId { get; set; }

        [JsonPropertyName("customer_id")]
        public string CustomerId { get; set; }
    }
}
