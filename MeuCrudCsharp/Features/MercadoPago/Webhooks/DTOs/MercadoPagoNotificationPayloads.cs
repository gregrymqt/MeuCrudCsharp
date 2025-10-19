using System;
using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs
{
    /// <summary>
    /// Contém as classes que representam os diferentes objetos 'data'
    /// que podem vir nos webhooks do Mercado Pago.
    /// </summary>
    /// <summary>
    /// Payload para notificações de atualização de cartão.
    /// Corresponde ao tópico 'topic_card_id_wh'.
    /// </summary>
    public class CardUpdateNotificationPayload
    {
        [JsonPropertyName("customer_id")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("new_card_id")]
        public long NewCardId { get; set; }

        [JsonPropertyName("old_card_id")]
        public long OldCardId { get; set; }
    }

    /// <summary>
    /// Payload para notificações de 'claims' (disputas, reclamações).
    /// Corresponde ao tópico 'topic_claims_integration_wh'.
    /// </summary>
    public class ClaimNotificationPayload
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("resource")]
        public string? Resource { get; set; }
    }

    /// <summary>
    /// Payload para notificações de 'chargebacks'.
    /// </summary>
    public class ChargebackNotificationPayload
    {
        [JsonPropertyName("checkout")]
        public string? Checkout { get; set; }

        [JsonPropertyName("date_updated")]
        public DateTime DateUpdated { get; set; }

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("payment_id")]
        public long PaymentId { get; set; }

        [JsonPropertyName("product_id")]
        public string? ProductId { get; set; }

        [JsonPropertyName("site_id")]
        public string? SiteId { get; set; }

        [JsonPropertyName("transaction_intent_id")]
        public string? TransactionIntentId { get; set; }
    }

    public class PaymentNotificationData
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
