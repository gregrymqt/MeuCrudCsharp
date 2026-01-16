using MeuCrudCsharp.Documents.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MeuCrudCsharp.Features.Support.Documents.Models
{
    public class SupportTicketDocument : IMongoDocument
    {
        public static string CollectionName => "support_tickets";

        // O Id do Mongo é gerado automaticamente, aqui convertemos para string para facilitar o uso no C#
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Referência ao ID do usuário no seu SQL (Identity)
        [BsonElement("userId")]
        public string UserId { get; set; }

        // O "Assunto" ou Categoria (ex: Financeiro, Bug, Dúvida)
        [BsonElement("context")]
        public string Context { get; set; }

        // A mensagem detalhada do usuário
        [BsonElement("explanation")]
        public string Explanation { get; set; }

        // Campos de controle (Boas práticas)
        [BsonElement("status")]
        public string Status { get; set; } = "Open"; // Open, InProgress, Closed

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
