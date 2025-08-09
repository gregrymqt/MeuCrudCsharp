using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Clients.DTOs
{
    /// <summary>
    /// Requisição para vincular um novo cartão a um cliente.
    /// </summary>
    public class CardRequestDto
    {
        /// <summary>
        /// Token do cartão gerado pelo provedor de pagamentos.
        /// </summary>
        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }
}
