using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Caching;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Videos.DTOs;
using MeuCrudCsharp.Features.Videos.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.Videos.Service
{
    /// <summary>
    /// Implements <see cref="IAdminVideoService"/> to provide administrative functionalities for video management.
    /// This service handles CRUD operations for video metadata, file storage for thumbnails, and cache invalidation.
    /// </summary>
    public class AdminVideoService : IAdminVideoService
    {
        private readonly ApiDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ICacheService _cacheService;
        private readonly ILogger<AdminVideoService> _logger;
        private const string VideosCacheVersionKey = "videos_cache_version";

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminVideoService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="env">The web hosting environment for file path information.</param>
        /// <param name="cacheService">The caching service for performance optimization.</param>
        /// <param name="logger">The logger for recording events and errors.</param>
        public AdminVideoService(
            ApiDbContext context,
            IWebHostEnvironment env,
            ICacheService cacheService,
            ILogger<AdminVideoService> logger
        )
        {
            _context = context;
            _env = env;
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <inheritdoc />
        /// <remarks>
        /// This method uses a version-based caching strategy. When video data changes, the cache version is updated,
        /// effectively invalidating all cached pages of videos at once.
        /// </remarks>
        public async Task<PaginatedResult<VideoDto>> GetAllVideosAsync(int page, int pageSize)
        {
            var cacheVersion = await _cacheService.GetOrCreateAsync(
                VideosCacheVersionKey,
                () => Task.FromResult(Guid.NewGuid().ToString()),
                absoluteExpireTime: TimeSpan.FromDays(30)
            );

            var cacheKey = $"AdminVideos_v{cacheVersion}_Page{page}_Size{pageSize}";

            return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogInformation(
                        "Fetching videos from database (cache miss) for key: {CacheKey}",
                        cacheKey
                    );
                    try
                    {
                        var totalCount = await _context.Videos.CountAsync();

                        var items = await _context
                            .Videos.Include(v => v.Course)
                            .OrderByDescending(v => v.UploadDate)
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .Select(v => new VideoDto
                            {
                                Id = v.Id,
                                Title = v.Title,
                                Description = v.Description,
                                StorageIdentifier = v.StorageIdentifier,
                                UploadDate = v.UploadDate,
                                Duration = v.Duration,
                                Status = v.Status.ToString(),
                                CourseName = v.Course.Name,
                                ThumbnailUrl = v.ThumbnailUrl,
                            })
                            .ToListAsync();

                        return new PaginatedResult<VideoDto>(items, totalCount, page, pageSize);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error fetching the list of videos from the database."
                        );
                        throw new AppServiceException(
                            "An error occurred while querying for videos.",
                            ex
                        );
                    }
                },
                absoluteExpireTime: TimeSpan.FromMinutes(10)
            );
        }

        /// <inheritdoc />
        public async Task<VideoDto> CreateVideoAsync(CreateVideoDto createDto)
        {
            try
            {
                var course = await _context.Courses.FirstOrDefaultAsync(c =>
                    c.Name == createDto.CourseName
                );
                if (course == null)
                {
                    course = new Models.Course { Name = createDto.CourseName };
                    _context.Courses.Add(course);
                }
                string? thumbnailUrl = await SaveThumbnailAsync(createDto.ThumbnailFile);

                var video = new Video
                {
                    Title = createDto.Title,
                    Description = createDto.Description,
                    StorageIdentifier = createDto.StorageIdentifier,
                    Course = course,
                    ThumbnailUrl = thumbnailUrl,
                };

                _context.Videos.Add(video);
                await _context.SaveChangesAsync();

                await InvalidateVideosCache();

                return new VideoDto
                {
                    Id = video.Id,
                    Title = video.Title,
                    Description = video.Description,
                    StorageIdentifier = video.StorageIdentifier,
                    UploadDate = video.UploadDate,
                    Duration = video.Duration,
                    Status = video.Status.ToString(),
                    CourseName = course.Name,
                    ThumbnailUrl = video.ThumbnailUrl,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while creating video '{VideoTitle}'.",
                    createDto.Title
                );
                throw new AppServiceException("An error occurred while saving the new video.", ex);
            }
        }

        /// <inheritdoc />
        public async Task<VideoDto> UpdateVideoAsync(Guid id, UpdateVideoDto updateDto)
        {
            try
            {
                var video = await _context
                    .Videos.Include(v => v.Course)
                    .FirstOrDefaultAsync(v => v.Id == id);
                if (video == null)
                    throw new ResourceNotFoundException(
                        $"Video with ID {id} not found for update."
                    );

                if (updateDto.ThumbnailFile != null && updateDto.ThumbnailFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(video.ThumbnailUrl))
                    {
                        var relativePath = video
                            .ThumbnailUrl.TrimStart('/')
                            .Replace('/', Path.DirectorySeparatorChar);
                        var oldThumbnailPath = Path.Combine(_env.WebRootPath, relativePath);

                        try
                        {
                            if (File.Exists(oldThumbnailPath))
                            {
                                File.Delete(oldThumbnailPath);
                                _logger.LogInformation(
                                    "Old thumbnail '{Path}' deleted successfully.",
                                    oldThumbnailPath
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(
                                ex,
                                "Failed to delete old thumbnail at path: {Path}",
                                oldThumbnailPath
                            );
                        }
                    }

                    video.ThumbnailUrl = await SaveThumbnailAsync(updateDto.ThumbnailFile);
                }

                video.Title = updateDto.Title;
                video.Description = updateDto.Description;

                await _context.SaveChangesAsync();
                await InvalidateVideosCache();
                _logger.LogInformation("Video {VideoId} updated successfully.", id);

                return new VideoDto
                {
                    Id = video.Id,
                    Title = video.Title,
                    Description = video.Description,
                    StorageIdentifier = video.StorageIdentifier,
                    UploadDate = video.UploadDate,
                    Duration = video.Duration,
                    Status = video.Status.ToString(),
                    CourseName = video.Course.Name,
                    ThumbnailUrl = video.ThumbnailUrl,
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating video {VideoId}.", id);
                throw new AppServiceException("An error occurred while saving video changes.", ex);
            }
        }

        /// <inheritdoc />
        public async Task DeleteVideoAsync(Guid id)
        {
            var video = await _context.Videos.FindAsync(id);
            if (video == null)
                throw new ResourceNotFoundException($"Video with ID {id} not found for deletion.");

            try
            {
                var videoFolderPath = Path.Combine(
                    _env.WebRootPath,
                    "Videos",
                    video.StorageIdentifier
                );
                if (Directory.Exists(videoFolderPath))
                {
                    Directory.Delete(videoFolderPath, recursive: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete the file folder for video {VideoId}. Database removal will be aborted.",
                    id
                );
                throw new AppServiceException(
                    "An error occurred while removing the physical video files.",
                    ex
                );
            }

            try
            {
                _context.Videos.Remove(video);
                await _context.SaveChangesAsync();

                await InvalidateVideosCache();

                _logger.LogInformation(
                    "Video {VideoId} deleted from the database successfully.",
                    id
                );
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting video {VideoId}.", id);
                throw new AppServiceException(
                    "An error occurred while removing the video from our system.",
                    ex
                );
            }
        }

        /// <summary>
        /// Saves a thumbnail file to the server and returns its relative URL path.
        /// </summary>
        /// <param name="file">The thumbnail file to save.</param>
        /// <returns>The relative URL path to the saved thumbnail, or null if no file was provided.</returns>
        private async Task<string?> SaveThumbnailAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            var thumbnailsDirectory = Path.Combine(_env.WebRootPath, "thumbnails");
            Directory.CreateDirectory(thumbnailsDirectory);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(thumbnailsDirectory, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/thumbnails/{fileName}";
        }

        /// <summary>
        /// Invalidates the video cache by updating the master cache version key.
        /// </summary>
        /// <exception cref="AppServiceException">Thrown if updating the cache version fails.</exception>
        private async Task InvalidateVideosCache()
        {
            try
            {
                var newVersion = Guid.NewGuid().ToString();
                await _cacheService.SetAsync(
                    VideosCacheVersionKey,
                    newVersion,
                    TimeSpan.FromDays(30)
                );
                _logger.LogInformation(
                    "Videos cache invalidated. New version: {CacheVersion}",
                    newVersion
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to invalidate videos cache (update version key).");
                throw new AppServiceException(
                    "An error occurred while clearing the videos cache.",
                    ex
                );
            }
        }
    }
}
