using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Videos.DTOs;
using MeuCrudCsharp.Features.Videos.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MeuCrudCsharp.Features.Videos.Service
{
    public class AdminVideoService : IAdminVideoService
    {
        private readonly ApiDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;

        public AdminVideoService(ApiDbContext context, IWebHostEnvironment env, IMemoryCache cache)
        {
            _context = context;
            _env = env;
            _cache = cache;
        }

        public async Task<List<VideoDto>> GetAllVideosAsync(int page, int pageSize)
        {
            var cacheKey = $"AdminVideos_Page{page}_Size{pageSize}";
            if (_cache.TryGetValue(cacheKey, out List<VideoDto> cachedVideos))
            {
                return cachedVideos;
            }

            var videos = await _context
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
                })
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(
                TimeSpan.FromMinutes(5)
            );
            _cache.Set(cacheKey, videos, cacheOptions);

            return videos;
        }

        public async Task<VideoDto> CreateVideoAsync(CreateVideoDto createDto)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c =>
                c.Name == createDto.CourseName
            );
            if (course == null)
            {
                course = new Courses { Name = createDto.CourseName };
                _context.Courses.Add(course);
            }

            var video = new Video
            {
                Title = createDto.Title,
                Description = createDto.Description,
                StorageIdentifier = createDto.StorageIdentifier,
                Course = course,
            };

            _context.Videos.Add(video);
            await _context.SaveChangesAsync();

            _cache.Remove("AdminVideos_Page1_Size10");

            return new VideoDto
            {
                Id = video.Id,
                Title = video.Title,
                Description = video.Description,
                StorageIdentifier = video.StorageIdentifier,
                UploadDate = video.UploadDate,
                Status = video.Status.ToString(),
                CourseName = course.Name,
            };
        }

        public async Task<bool> UpdateVideoAsync(Guid id, UpdateVideoDto updateDto)
        {
            var video = await _context.Videos.FindAsync(id);
            if (video == null)
                return false;

            video.Title = updateDto.Title;
            video.Description = updateDto.Description;

            _context.Videos.Update(video);
            await _context.SaveChangesAsync();

            // Invalidar cache (lógica mais robusta seria necessária aqui)
            return true;
        }

        public async Task<(bool Success, string ErrorMessage)> DeleteVideoAsync(Guid id)
        {
            var video = await _context.Videos.FindAsync(id);
            if (video == null)
                return (false, "Vídeo não encontrado.");

            var videoFolderPath = Path.Combine(_env.WebRootPath, "Videos", video.StorageIdentifier);
            try
            {
                if (Directory.Exists(videoFolderPath))
                {
                    Directory.Delete(videoFolderPath, recursive: true);
                }
            }
            catch (Exception ex)
            {
                // LogError(ex);
                return (false, $"Erro ao deletar a pasta do vídeo: {ex.Message}.");
            }

            _context.Videos.Remove(video);
            await _context.SaveChangesAsync();

            // Invalidar cache
            return (true, string.Empty);
        }
    }
}
