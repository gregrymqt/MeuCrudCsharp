
using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Clients.DTOs
{
    // DTO para enviar os dados na criação de um cliente
    public class CustomerRequestDto
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }
    }
}
