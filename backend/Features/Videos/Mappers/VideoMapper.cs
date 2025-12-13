using MeuCrudCsharp.Features.Videos.DTOs;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Videos.Mappers
{
    // Features/Videos/DTOs/VideoMapper.cs
    public static class VideoMapper
    {
        public static VideoDto ToDto(Video video)
        {
            return new VideoDto
            {
                Id = video.PublicId, // ⭐️ Expondo o PublicId para o front-end
                Title = video.Title,
                Description = video.Description,
                StorageIdentifier = video.StorageIdentifier,
                UploadDate = video.UploadDate,
                Duration = video.Duration,
                Status = video.Status.ToString(),
                CourseName = video.Course?.Name, // O '?' protege contra Course nulo
                ThumbnailUrl = video.ThumbnailUrl,
            };
        }
    }
}
