using MeuCrudCsharp.Features.Support.Documents;
using MeuCrudCsharp.Features.Support.Documents.Models;
using MeuCrudCsharp.Features.Support.Interfaces;
// using MeuCrudCsharp.Features.Support.Documents.Models; // Provavelmente desnecessário se a Model está no namespace acima
using MeuCrudCsharp.Features.Support.Repositories; // Ajuste para o namespace correto da Interface
using MongoDB.Driver;

namespace MeuCrudCsharp.Features.Support.Repositories
{
    public class SupportRepository : ISupportRepository
    {
        private readonly IMongoCollection<SupportTicketDocument> _tickets;

        public SupportRepository(IMongoDatabase database)
        {
            // --- MUDANÇA AQUI ---
            // Em vez de "support_tickets" fixo, usamos a propriedade estática da classe.
            // Isso evita erros de digitação e mantém tudo centralizado na Model.
            _tickets = database.GetCollection<SupportTicketDocument>(
                SupportTicketDocument.CollectionName
            );
        }

        public async Task CreateAsync(SupportTicketDocument ticket)
        {
            await _tickets.InsertOneAsync(ticket);
        }

        public async Task<(
            IEnumerable<SupportTicketDocument> Data,
            long Total
        )> GetAllPaginatedAsync(int page, int pageSize)
        {
            var filter = Builders<SupportTicketDocument>.Filter.Empty;

            // Contagem total para saber se tem mais páginas
            var total = await _tickets.CountDocumentsAsync(filter);

            var data = await _tickets
                .Find(filter)
                .SortByDescending(t => t.CreatedAt) // Mais recentes primeiro
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (data, total);
        }

        public async Task<SupportTicketDocument?> GetByIdAsync(string id)
        {
            return await _tickets.Find(t => t.Id == id).FirstOrDefaultAsync();
        }

        public async Task UpdateStatusAsync(string id, string newStatus)
        {
            var filter = Builders<SupportTicketDocument>.Filter.Eq(t => t.Id, id);
            var update = Builders<SupportTicketDocument>.Update.Set(t => t.Status, newStatus);

            await _tickets.UpdateOneAsync(filter, update);
        }
    }
}
