using MeuCrudCsharp.Features.Support.DTOs;
using MeuCrudCsharp.Features.Videos.DTOs;

namespace MeuCrudCsharp.Features.Support.Interfaces
{
    public interface ISupportService
    {
        Task CreateTicketAsync(string userId, CreateSupportTicketDto dto);
        Task<PaginatedResultDto<SupportTicketResponseDto>> GetAllTicketsPaginatedAsync(
            int page,
            int pageSize
        );

        // Adicione esta linha:
        Task<SupportTicketResponseDto> GetTicketByIdAsync(string id);

        Task UpdateTicketStatusAsync(string id, UpdateSupportTicketDto dto);
    }
}
