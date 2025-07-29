using MeuCrudCsharp.Features.Videos.DTOs;

namespace MeuCrudCsharp.Features.Videos.Interfaces
{
    public interface IAdminVideoService
    {
        Task<List<VideoDto>> GetAllVideosAsync(int page, int pageSize);
        Task<VideoDto> CreateVideoAsync(CreateVideoDto createDto);
        Task<bool> UpdateVideoAsync(Guid id, UpdateVideoDto updateDto);
        Task<(bool Success, string ErrorMessage)> DeleteVideoAsync(Guid id);
    }
}
