using MeuCrudCsharp.Features.Videos.DTOs;

namespace MeuCrudCsharp.Features.Videos.Interfaces
{
    public interface IAdminVideoService
    {
        Task<List<VideoDto>> GetAllVideosAsync(int page, int pageSize);
        Task<VideoDto> CreateVideoAsync(CreateVideoDto createDto, IFormFile? thumbnailFile);

        // ALTERADO: A assinatura agora retorna Task<VideoDto>
        Task<VideoDto> UpdateVideoAsync(Guid id, UpdateVideoDto updateDto, IFormFile? thumbnailFile);

        Task DeleteVideoAsync(Guid id);
    }
}
