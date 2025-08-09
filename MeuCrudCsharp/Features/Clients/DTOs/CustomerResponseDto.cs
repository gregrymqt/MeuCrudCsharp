using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Clients.DTOs
{
    /// <summary>
    /// Resposta com os dados do cliente no provedor de pagamentos.
    /// </summary>
    public class CustomerResponseDto
    {
        /// <summary>
        /// Identificador do cliente no provedor.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

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

        /// <summary>
        /// Data de criação do registro no provedor.
        /// </summary>
        [JsonPropertyName("date_created")]
        public DateTime DateCreated { get; set; }
    }
}
