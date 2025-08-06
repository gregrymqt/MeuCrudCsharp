using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Clients.DTOs
{
    public class CustomerResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("date_created")]
        public DateTime DateCreated { get; set; }
    }
}
