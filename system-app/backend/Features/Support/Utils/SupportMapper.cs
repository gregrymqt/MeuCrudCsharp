using MeuCrudCsharp.Documents.Models;
using MeuCrudCsharp.Features.Support.DTOs;

namespace MeuCrudCsharp.Features.Support.Utils
{
    public static class SupportMapper
    {
        public static SupportTicketResponseDto ToDto(SupportTicketDocument document)
        {
            if (document == null)
                return null;

            return new SupportTicketResponseDto
            {
                Id = document.Id,
                UserId = document.UserId,
                Context = document.Context,
                Explanation = document.Explanation,
                Status = document.Status,
                CreatedAt = document.CreatedAt,
            };
        }

        public static List<SupportTicketResponseDto> ToDtoList(
            IEnumerable<SupportTicketDocument> documents
        )
        {
            if (documents == null)
                return new List<SupportTicketResponseDto>();

            return documents.Select(ToDto).ToList();
        }
    }
}
