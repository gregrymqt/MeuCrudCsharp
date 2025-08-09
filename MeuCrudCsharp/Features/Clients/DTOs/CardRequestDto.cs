using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Clients.DTOs
{
    public class CardRequestDto
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }
}
