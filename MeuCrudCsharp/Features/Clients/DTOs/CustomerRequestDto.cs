
using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Clients.DTOs
{
    /// <summary>
    /// Requisição para criação/atualização de um cliente no provedor de pagamentos.
    /// </summary>
    public class CustomerRequestDto
    {
        /// <summary>
        /// E-mail do cliente.
        /// </summary>
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        /// <summary>
        /// Primeiro nome do cliente.
        /// </summary>
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }
    }
}
