using MeuCrudCsharp.Documents.Models;

namespace MeuCrudCsharp.Features.Support.Interfaces
{
    public interface ISupportRepository
    {
        Task CreateAsync(SupportTicketDocument ticket);
        Task<(IEnumerable<SupportTicketDocument> Data, long Total)> GetAllPaginatedAsync(
            int page,
            int pageSize
        );
        Task<SupportTicketDocument?> GetByIdAsync(string id);
        Task UpdateStatusAsync(string id, string newStatus);
    }
}
