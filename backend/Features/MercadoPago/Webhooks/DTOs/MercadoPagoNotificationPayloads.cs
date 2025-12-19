using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs
{
    public class MercadoPagoWebhookNotification
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("date_created")]
        public DateTime DateCreated { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("api_version")]
        public string ApiVersion { get; set; }

        [JsonPropertyName("application_id")]
        public string ApplicationId { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        // ALTERAÇÃO IMPORTANTE: Usamos JsonElement para manipular o JSON cru
        [JsonPropertyName("data")]
        public JsonElement Data { get; set; }
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
        public string Id { get; set; }

        [JsonPropertyName("customer_id")]
        public string CustomerId { get; set; }
    }
}
