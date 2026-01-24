using MeuCrudCsharp.Documents.Models;
using MeuCrudCsharp.Features.Exceptions;
// Confirme se sua Model está aqui ou apenas em .Documents
using MeuCrudCsharp.Features.Support.DTOs;
using MeuCrudCsharp.Features.Support.Interfaces;
using MeuCrudCsharp.Features.Support.Utils;
using MeuCrudCsharp.Features.Videos.DTOs;

namespace MeuCrudCsharp.Features.Support.Services
{
    public class SupportService : ISupportService
    {
        private readonly ISupportRepository _repository;

        public SupportService(ISupportRepository repository)
        {
            _repository = repository;
        }

        public async Task CreateTicketAsync(string userId, CreateSupportTicketDto dto)
        {
            var document = new SupportTicketDocument
            {
                UserId = userId,
                Context = dto.Context,
                Explanation = dto.Explanation,
                Status = "Open",
                CreatedAt = DateTime.UtcNow,
            };

            await _repository.CreateAsync(document);
        }

        // --- NOVO MÉTODO GET BY ID ---
        public async Task<SupportTicketResponseDto> GetTicketByIdAsync(string id)
        {
            // 1. Busca no banco
            var ticket = await _repository.GetByIdAsync(id);

            // 2. Valida se existe
            if (ticket == null)
            {
                throw new ResourceNotFoundException("Ticket de suporte não encontrado.");
            }

            // 3. Mapeia para DTO
            return new SupportTicketResponseDto
            {
                Id = ticket.Id,
                UserId = ticket.UserId,
                Context = ticket.Context,
                Explanation = ticket.Explanation,
                Status = ticket.Status,
                CreatedAt = ticket.CreatedAt,
            };
        }

        // -----------------------------

        public async Task<PaginatedResultDto<SupportTicketResponseDto>> GetAllTicketsPaginatedAsync(
            int page,
            int pageSize
        )
        {
            // 1. Busca os dados paginados do repositório
            var (documents, total) = await _repository.GetAllPaginatedAsync(page, pageSize);

            // 2. Usa o Mapper para converter (IEnumerable -> List)
            var itemsDto = SupportMapper.ToDtoList(documents);

            // O PaginatedResultDto calcula TotalPages internamente com base no count e pageSize.
            return new PaginatedResultDto<SupportTicketResponseDto>(
                itemsDto,
                (int)total,
                page,
                pageSize
            );
        }

        public async Task UpdateTicketStatusAsync(string id, UpdateSupportTicketDto dto)
        {
            var ticket = await _repository.GetByIdAsync(id);
            if (ticket == null)
            {
                throw new ResourceNotFoundException("Ticket de suporte não encontrado.");
            }

            await _repository.UpdateStatusAsync(id, dto.Status);
        }
    }
}
