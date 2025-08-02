using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Profiles.Admin.Dtos
{
    public class UpdateSubscriptionStatusDto
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; } // "paused" ou "cancelled" ou "authorized"
    }
}
