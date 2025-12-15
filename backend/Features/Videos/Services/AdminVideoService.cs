using Hangfire;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Courses.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Files.Services;
using MeuCrudCsharp.Features.Videos.DTOs;
using MeuCrudCsharp.Features.Videos.Interfaces;
using MeuCrudCsharp.Features.Videos.Utils;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Videos.Services;

    public class AdminVideoService : IAdminVideoService
    {
        private readonly IVideoRepository _videoRepository;
        private readonly UploadService _uploadService; 
        private readonly IBackgroundJobClient _jobs;
        private readonly ICacheService _cacheService;
        private readonly IWebHostEnvironment _env; // ⭐️ Adicionado para o Helper
        private readonly ILogger<AdminVideoService> _logger;

        private const string VideosCacheVersionKey = "videos_cache_version";

        public AdminVideoService(
            IVideoRepository videoRepository,
            UploadService uploadService,
            IBackgroundJobClient jobs,
            ICacheService cacheService,
            IWebHostEnvironment env, // ⭐️ Injetando
            ILogger<AdminVideoService> logger)
        {
            _videoRepository = videoRepository;
            _uploadService = uploadService;
            _jobs = jobs;
            _cacheService = cacheService;
            _env = env;
            _logger = logger;
        }

        public async Task<Video> HandleVideoUploadAsync(
            IFormFile videoFile, 
            string title, 
            string description, 
            IFormFile? thumbnailFile)
        {
            // 1. Upload do Vídeo (Fisico + Metadados na tabela Files) [cite: 7]
            // Salva em: wwwroot/uploads/Videos
            var videoEntityFile = await _uploadService.SalvarArquivoAsync(videoFile, "Videos");

            // 2. Upload da Thumbnail (Opcional) [cite: 8]
            string? thumbnailUrl = null;
            if (thumbnailFile != null)
            {
                var thumbEntityFile = await _uploadService.SalvarArquivoAsync(thumbnailFile, "Thumbnails");
                thumbnailUrl = thumbEntityFile.CaminhoRelativo; // [cite: 10]
            }

            // 3. Criação da Entidade Vídeo
            var video = new Video
            {
                Title = title,
                Description = description,
                StorageIdentifier = Guid.NewGuid().ToString(), // ID usado na pasta HLS [cite: 11]
                FileId = videoEntityFile.Id, // Vínculo com o arquivo bruto
                ThumbnailUrl = thumbnailUrl,
                Status = VideoStatus.Processing,
                UploadDate = DateTime.UtcNow
            };

            await _videoRepository.AddAsync(video); // [cite: 12]

            // 4. Invalidação de Cache
            await _cacheService.InvalidateCacheByKeyAsync(VideosCacheVersionKey);

            // 5. Job de Processamento
            _logger.LogInformation("Enfileirando job para o vídeo {Identifier}", video.StorageIdentifier);
            
            // Dispara o Job passando o ID do Vídeo e do Arquivo Original [cite: 14]
            // Nota: Mantive 'VideoProcessingService' conforme seu arquivo, verifique se o nome da classe do Job é esse mesmo.
            _jobs.Enqueue<VideoProcessingService>(job => job.ProcessVideoToHlsAsync(video.Id, videoEntityFile.Id));

            return video;
        }

        public async Task<PaginatedResultDto<VideoDto>> GetAllVideosAsync(int page, int pageSize)
        {
            var cacheVersion = await _cacheService.GetOrCreateAsync(
                VideosCacheVersionKey,
                () => Task.FromResult(Guid.NewGuid().ToString()),
                TimeSpan.FromDays(30)
            );

            var cacheKey = $"AdminVideos_v{cacheVersion}_Page{page}_Size{pageSize}";

            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                _logger.LogInformation("Cache miss. Buscando vídeos página {Page} no banco.", page);

                // Busca paginada no repositório [cite: 18]
                var result = await _videoRepository.GetAllPaginatedAsync(page, pageSize);

                // Mapeamento Entity -> DTO usando a lógica do seu arquivo [cite: 19]
                var dtos = result.Items.Select(v => new VideoDto
                {
                    Id = v.PublicId, // Usando PublicId (Guid) para o DTO conforme boas práticas
                    Title = v.Title,
                    Description = v.Description,
                    StorageIdentifier = v.StorageIdentifier,
                    ThumbnailUrl = v.ThumbnailUrl,
                    Status = v.Status.ToString(),
                    UploadDate = v.UploadDate
                }).ToList();

                return new PaginatedResultDto<VideoDto>(dtos, result.TotalCount, page, pageSize);
            }, TimeSpan.FromMinutes(10));
        }

        public async Task<Video> UpdateVideoAsync(Guid id, UpdateVideoDto dto)
        {
            // Busca pelo Guid (PublicId) [cite: 22]
            var video = await _videoRepository.GetByPublicIdAsync(id); 

            if (video == null)
                throw new ResourceNotFoundException("Vídeo não encontrado.");

            // Atualiza Thumbnail se enviada
            if (dto.ThumbnailFile != null && dto.ThumbnailFile.Length > 0)
            {
                // Salva a nova thumbnail [cite: 25]
                // Nota: O método SalvarArquivoAsync cria um novo registro e arquivo.
                // Como ThumbnailUrl é apenas uma string na model Video (e não uma FK para Files), 
                // apenas atualizamos o caminho.
                var novaThumb = await _uploadService.SalvarArquivoAsync(dto.ThumbnailFile, "Thumbnails");
                video.ThumbnailUrl = novaThumb.CaminhoRelativo; // [cite: 26]
            }

            video.Title = dto.Title;
            video.Description = dto.Description;

            await _videoRepository.UpdateAsync(video);
            await _cacheService.InvalidateCacheByKeyAsync(VideosCacheVersionKey);

            return video;
        }

        public async Task DeleteVideoAsync(Guid id)
        {
            var video = await _videoRepository.GetByPublicIdAsync(id);
            if (video == null) throw new ResourceNotFoundException("Vídeo não encontrado.");

            // 1. Deletar Arquivo Original MP4 (Usando o UploadService Genérico)
            if (video.FileId > 0)
            {
                // Isso remove o arquivo físico da pasta uploads/Videos e o registro na tabela Files [cite: 28, 29]
                await _uploadService.DeletarArquivoAsync(video.FileId);
            }

            // 2. Deletar Pasta HLS (Usando o Helper que acabamos de criar)
            // Isso remove a pasta uploads/Videos/{GUID}/... gerada pelo Job [cite: 31]
            VideoDirectoryHelper.DeleteHlsFolder(_env.WebRootPath, video.StorageIdentifier);

            // 3. Deletar o Registro do Vídeo
            await _videoRepository.DeleteAsync(video);
            
            // 4. Limpar Cache
            await _cacheService.InvalidateCacheByKeyAsync(VideosCacheVersionKey);
        }
    }


