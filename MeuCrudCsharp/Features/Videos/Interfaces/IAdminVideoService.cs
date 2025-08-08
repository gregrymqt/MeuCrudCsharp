using MeuCrudCsharp.Features.Videos.DTOs;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MeuCrudCsharp.Features.Videos.Interfaces
{
    public interface IAdminVideoService
    {
        Task<List<VideoDto>> GetAllVideosAsync(int page, int pageSize);
        Task<VideoDto> CreateVideoAsync(CreateVideoDto createVideoDto);

        // ALTERADO: A assinatura agora retorna Task<VideoDto>
        Task<VideoDto> UpdateVideoAsync(Guid id, UpdateVideoDto updateDto);

        Task DeleteVideoAsync(Guid id);
    }
}
