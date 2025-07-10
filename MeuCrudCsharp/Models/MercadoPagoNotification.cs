// Crie um novo arquivo, ex: Models/MercadoPagoNotification.cs

using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Models
{
    // Modelo para deserializar o corpo (body) da notificação do webhook
    public class MercadoPagoNotification
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("api_version")]
        public string ApiVersion { get; set; }

        [JsonPropertyName("data")]
        public NotificationData Data { get; set; }

        [JsonPropertyName("date_created")]
        public DateTime DateCreated { get; set; }

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("live_mode")]
        public bool LiveMode { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }
    }

    public class NotificationData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }
}