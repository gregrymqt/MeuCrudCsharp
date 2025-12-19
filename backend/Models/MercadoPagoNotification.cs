// Crie um novo arquivo, ex: Models/MercadoPagoNotification.cs

using System.Text.Json;
using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Models
{
    // Modelo para deserializar o corpo (body) da notificação do webhook
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
}
