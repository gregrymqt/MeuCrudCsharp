using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Subscriptions.DTOs
{
    public class UpdateSubscriptionStatusDto
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; } // "paused" ou "cancelled" ou "authorized"
    }
}
