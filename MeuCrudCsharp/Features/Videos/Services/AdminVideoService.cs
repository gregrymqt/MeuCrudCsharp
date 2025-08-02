using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Videos.DTOs;
using MeuCrudCsharp.Features.Videos.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace MeuCrudCsharp.Features.Videos.Service
{
    public class AdminVideoService : IAdminVideoService
    {
        private readonly ApiDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ICacheService _cacheService; // MUDANÇA 1: Usando nosso serviço universal

        // MUDANÇA 2: Criando um CancellationTokenSource para controlar a invalidação
        private static CancellationTokenSource _videosCacheTokenSource = new CancellationTokenSource();

        public AdminVideoService(ApiDbContext context, IWebHostEnvironment env, ICacheService cacheService)
        {
            _context = context;
            _env = env;
            _cacheService = cacheService; // MUDANÇA 1
        }

        public async Task<List<VideoDto>> GetAllVideosAsync(int page, int pageSize)
        {
            var cacheKey = $"AdminVideos_Page{page}_Size{pageSize}";

            // MUDANÇA 3: Usando o GetOrCreateAsync e atrelando ao nosso "sinalizador"
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                // Esta lógica só executa se o cache não existir
                return await _context.Videos
                    .Include(v => v.Course)
                    .OrderByDescending(v => v.UploadDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(v => new VideoDto { /* ... seu mapeamento ... */ })
                    .ToListAsync();
            },
            // Aqui está a mágica: o tempo de vida deste cache está agora
            // vinculado ao nosso CancellationTokenSource.
            expirationToken: new CancellationChangeToken(_videosCacheTokenSource.Token));
        }

        public async Task<VideoDto> CreateVideoAsync(CreateVideoDto createDto)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c =>
                c.Name == createDto.CourseName
            );
            if (course == null)
            {
                course = new Models.Course { Name = createDto.CourseName };
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

            InvalidateVideosCache();

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

            InvalidateVideosCache();
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

            InvalidateVideosCache();

            return (true, string.Empty);
        }
        private void InvalidateVideosCache()
        {
            // Cancela o token antigo, o que invalida todos os caches que dependem dele
            _videosCacheTokenSource.Cancel();

            // Cria um novo token para os próximos caches
            _videosCacheTokenSource = new CancellationTokenSource();
        }
    }
}
