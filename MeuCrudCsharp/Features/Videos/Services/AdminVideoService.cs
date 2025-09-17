using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Caching;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Videos.DTOs;
using MeuCrudCsharp.Features.Videos.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.Videos.Services
{
    /// <summary>
    /// Implements <see cref="IAdminVideoService"/> to provide administrative functionalities
    /// for video management. This service handles CRUD operations for video metadata,
    /// file storage for thumbnails and video assets, and cache invalidation.
    /// </summary>
    public class AdminVideoService : IAdminVideoService
    {
        private readonly ApiDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<AdminVideoService> _logger;
        private const string VideosCacheVersionKey = "videos_cache_version";

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminVideoService"/> class.
        /// </summary>
        /// <param name="context">The database context for accessing videos and courses.</param>
        /// <param name="cacheService">The caching service for versioned cache entries.</param>
        /// <param name="fileStorageService">The file storage service for saving and deleting thumbnails and video assets.</param>
        /// <param name="logger">The logger for recording informational messages and errors.</param>
        public AdminVideoService(
            ApiDbContext context,
            ICacheService cacheService,
            IFileStorageService fileStorageService,
            ILogger<AdminVideoService> logger
        )
        {
            _context = context;
            _cacheService = cacheService;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Uses a version-based caching strategy. Whenever a video is created, updated, or deleted,
        /// <see cref="InvalidateVideosCache"/> is called to bump the version. All cache keys
        /// include that version, ensuring stale entries are ignored until the next rebuild.
        /// </remarks>
        public async Task<PaginatedResultDto<VideoDto>> GetAllVideosAsync(int page, int pageSize)
        {
            var cacheVersion = await _cacheService.GetOrCreateAsync(
                VideosCacheVersionKey,
                () => Task.FromResult(Guid.NewGuid().ToString()),
                TimeSpan.FromDays(30)
            );

            var cacheKey = $"AdminVideos_v{cacheVersion}_Page{page}_Size{pageSize}";

            return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogInformation(
                        "Cache miss for videos. Fetching page {Page} (size {Size}) from database.",
                        page,
                        pageSize
                    );

                    var totalCount = await _context.Videos.CountAsync();
                    var items = await _context
                        .Videos.AsNoTracking()
                        .Include(v => v.Course)
                        .OrderByDescending(v => v.UploadDate)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(v => VideoMapper.ToDto(v))
                        .ToListAsync();

                    return new PaginatedResultDto<VideoDto>(items, totalCount, page, pageSize);
                },
                TimeSpan.FromMinutes(10)
            );
        }

        /// <inheritdoc />
        public async Task<VideoDto> CreateVideoAsync(CreateVideoDto createDto)
        {
            var course = await GetOrCreateCourseAsync(createDto.CourseName);
            var thumbnailUrl = await _fileStorageService.SaveThumbnailAsync(
                createDto.ThumbnailFile
            );

            var video = new Video
            {
                Title = createDto.Title,
                Description = createDto.Description,
                StorageIdentifier = createDto.StorageIdentifier,
                CourseId = course.Id,
                ThumbnailUrl = thumbnailUrl,
                // PublicId é gerado automaticamente no model
            };

            _context.Videos.Add(video);
            await _context.SaveChangesAsync();
            await InvalidateVideosCache();

            // Atribui Course para que o mapper use o nome corretamente
            video.Course = course;
            return VideoMapper.ToDto(video);
        }

        /// <inheritdoc />
        public async Task<VideoDto> UpdateVideoAsync(Guid publicId, UpdateVideoDto updateDto)
        {
            var video = await FindVideoByPublicIdOrFailAsync(publicId);

            if (updateDto.ThumbnailFile != null && updateDto.ThumbnailFile.Length > 0)
            {
                // Remove thumbnail antigo e armazena o novo
                await _fileStorageService.DeleteThumbnailAsync(video.ThumbnailUrl);
                video.ThumbnailUrl = await _fileStorageService.SaveThumbnailAsync(
                    updateDto.ThumbnailFile
                );
            }

            video.Title = updateDto.Title;
            video.Description = updateDto.Description;

            await _context.SaveChangesAsync();
            await InvalidateVideosCache();

            _logger.LogInformation("Vídeo {VideoId} atualizado com sucesso.", video.Id);
            return VideoMapper.ToDto(video);
        }

        /// <summary>
        /// Deletes a video and its associated assets from storage and the database.
        /// Also invalidates the video listing cache.
        /// </summary>
        /// <param name="publicId">The public identifier of the video to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ResourceNotFoundException">
        /// Thrown if no video with the given <paramref name="publicId"/> exists.
        /// </exception>
        public async Task DeleteVideoAsync(Guid publicId)
        {
            var video = await FindVideoByPublicIdOrFailAsync(publicId, includeCourse: false);

            await _fileStorageService.DeleteVideoAssetsAsync(video.StorageIdentifier);
            await _fileStorageService.DeleteThumbnailAsync(video.ThumbnailUrl);

            _context.Videos.Remove(video);
            await _context.SaveChangesAsync();
            await InvalidateVideosCache();

            _logger.LogInformation(
                "Vídeo {VideoId} deletado com sucesso do banco de dados.",
                video.Id
            );
        }

        /// <summary>
        /// Bumps the cache version for videos, effectively invalidating all existing
        /// <c>GetAllVideosAsync</c> cache entries.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task InvalidateVideosCache()
        {
            var newVersion = Guid.NewGuid().ToString();
            await _cacheService.SetAsync(VideosCacheVersionKey, newVersion, TimeSpan.FromDays(30));
            _logger.LogInformation(
                "Cache de vídeos invalidado. Nova versão: {CacheVersion}",
                newVersion
            );
        }

        /// <summary>
        /// Retrieves a <see cref="Video"/> entity by <paramref name="publicId"/>.
        /// </summary>
        /// <param name="publicId">The public identifier of the video.</param>
        /// <param name="includeCourse">
        /// If true, includes the related <see cref="Course"/> entity in the query.
        /// </param>
        /// <returns>The matching <see cref="Video"/> entity.</returns>
        /// <exception cref="ResourceNotFoundException">
        /// Thrown if no video with the given <paramref name="publicId"/> is found.
        /// </exception>
        private async Task<Video> FindVideoByPublicIdOrFailAsync(
            Guid publicId,
            bool includeCourse = true
        )
        {
            var query = _context.Videos.AsQueryable();
            if (includeCourse)
            {
                query = query.Include(v => v.Course);
            }

            var video = await query.FirstOrDefaultAsync(v => v.PublicId == publicId);
            if (video == null)
            {
                throw new ResourceNotFoundException(
                    $"Vídeo com o PublicId {publicId} não foi encontrado."
                );
            }

            return video;
        }

        /// <summary>
        /// Finds an existing <see cref="Course"/> by name or creates a new one if none exists.
        /// </summary>
        /// <param name="courseName">The name of the course to retrieve or create.</param>
        /// <returns>
        /// The existing or newly created <see cref="Course"/> entity.
        /// </returns>
        private async Task<Models.Course> GetOrCreateCourseAsync(string courseName)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Name == courseName);
            if (course == null)
            {
                course = new Models.Course { Name = courseName };
                _context.Courses.Add(course);
                // SaveChangesAsync será chamado pela chamada de criação/atualização principal
            }

            return course;
        }
    }
}
