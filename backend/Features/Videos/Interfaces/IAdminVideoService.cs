using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Videos.DTOs;

namespace MeuCrudCsharp.Features.Videos.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that handles administrative CRUD operations for video metadata.
    /// </summary>
    public interface IAdminVideoService
    {
        /// <summary>
        /// Retrieves a paginated list of all video metadata.
        /// </summary>
        /// <param name="page">The page number to retrieve.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a paginated list of videos.</returns>
        Task<PaginatedResultDto<VideoDto>> GetAllVideosAsync(int page, int pageSize);

        /// <summary>
        /// Creates the metadata for a new video.
        /// </summary>
        /// <param name="createVideoDto">A DTO containing the video metadata and thumbnail file.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created video's DTO.</returns>
        /// <exception cref="AppServiceException">Thrown for business logic errors, such as if the associated course is not found.</exception>
        Task<VideoDto> CreateVideoAsync(CreateVideoDto createVideoDto);

        /// <summary>
        /// Updates the metadata of an existing video.
        /// </summary>
        /// <param name="id">The unique identifier of the video to update.</param>
        /// <param name="updateDto">A DTO containing the updated video metadata and an optional new thumbnail.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated video's DTO.</returns>
        /// <exception cref="ResourceNotFoundException">Thrown if a video with the specified ID is not found.</exception>
        Task<VideoDto> UpdateVideoAsync(Guid id, UpdateVideoDto updateDto);

        /// <summary>
        /// Deletes a video's metadata and its associated files from storage.
        /// </summary>
        /// <param name="id">The unique identifier of the video to delete.</param>
        /// <returns>A task that represents the asynchronous deletion operation.</returns>
        /// <exception cref="ResourceNotFoundException">Thrown if a video with the specified ID is not found.</exception>
        Task DeleteVideoAsync(Guid id);
    }
}
