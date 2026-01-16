using Hangfire;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Files.Interfaces;
using MeuCrudCsharp.Features.Videos.DTOs;
using MeuCrudCsharp.Features.Videos.Interfaces;
using MeuCrudCsharp.Features.Videos.Utils;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Videos.Services;

public class AdminVideoService : IAdminVideoService
{
    private readonly IVideoRepository _videoRepository;
    private readonly IFileService _fileService;
    private readonly IBackgroundJobClient _jobs;
    private readonly ICacheService _cacheService;
    private readonly IWebHostEnvironment _env; // ⭐️ Adicionado para o Helper
    private readonly ILogger<AdminVideoService> _logger;

    private const string CAT_VIDEO = "Videos";
    private const string CAT_THUMB = "VideoThumbnails";

    private const string VideosCacheVersionKey = "videos_cache_version";

    public AdminVideoService(
        IVideoRepository videoRepository,
        IFileService fileService,
        IBackgroundJobClient jobs,
        ICacheService cacheService,
        IWebHostEnvironment env, // ⭐️ Injetando
        ILogger<AdminVideoService> logger
    )
    {
        _videoRepository = videoRepository;
        _fileService = fileService;
        _jobs = jobs;
        _cacheService = cacheService;
        _env = env;
        _logger = logger;
    }

    public async Task<VideoDto?> HandleVideoUploadAsync(CreateVideoDto dto)
    {
        // Precisamos capturar o ID do arquivo salvo, não só a URL
        int fileId = 0;
        string thumbnailUrl = string.Empty;
        string storageIdentifier = Guid.NewGuid().ToString();

        // 1. Lógica de Chunking (Para o VÍDEO)
        if (dto.IsChunk && dto.File != null)
        {
            // CS8604 Resolvido: Garantindo que fileName não é nulo
            var fileName = dto.FileName ?? $"{Guid.NewGuid()}.tmp";

            var tempPath = await _fileService.ProcessChunkAsync(
                dto.File,
                fileName,
                dto.ChunkIndex,
                dto.TotalChunks
            );

            if (tempPath == null)
                return null; // Ainda recebendo pedaços

            var videoSalvo = await _fileService.SalvarArquivoDoTempAsync(
                tempPath,
                fileName,
                CAT_VIDEO
            );

            // Captura o ID do arquivo para salvar na FK
            fileId = videoSalvo.Id;
        }
        else if (dto.File != null)
        {
            var videoSalvo = await _fileService.SalvarArquivoAsync(dto.File, CAT_VIDEO);
            fileId = videoSalvo.Id;
        }

        // 2. Lógica da Thumbnail
        if (dto.ThumbnailFile != null)
        {
            var thumbSalva = await _fileService.SalvarArquivoAsync(dto.ThumbnailFile, CAT_THUMB);
            thumbnailUrl = thumbSalva.CaminhoRelativo;
        }

        // 3. Salvar no Banco
        var entity = new Video
        {
            Title = dto.Title,
            Description = dto.Description,
            CourseId = dto.CourseId, // Agora ambos são int (CS0029 resolvido)
            StorageIdentifier = storageIdentifier,

            // CORREÇÃO: A Model não tem "VideoUrl", ela usa FileId
            FileId = fileId,

            ThumbnailUrl = thumbnailUrl,

            // CORREÇÃO: Usando o Enum (CS0029 resolvido)
            Status = VideoStatus.Processing,

            UploadDate = DateTime.UtcNow,
            Duration = TimeSpan.Zero,
        };

        // CORREÇÃO: Usando _videoRepository em vez de _repository (CS0103 resolvido)
        await _videoRepository.AddAsync(entity);

        return new VideoDto
        {
            // CORREÇÃO: Mapeando PublicId (Guid) para o DTO (CS0029 resolvido)
            Id = entity.PublicId,

            Title = entity.Title,

            // CORREÇÃO: Convertendo Enum para String (CS0029 resolvido)
            Status = entity.Status.ToString(),

            ThumbnailUrl = entity.ThumbnailUrl ?? string.Empty,
        };
    }

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
                _logger.LogInformation("Cache miss. Buscando vídeos página {Page} no banco.", page);

                // Busca paginada no repositório [cite: 18]
                var result = await _videoRepository.GetAllPaginatedAsync(page, pageSize);

                // Mapeamento Entity -> DTO usando a lógica do seu arquivo [cite: 19]
                var dtos = result
                    .Items.Select(v => new VideoDto
                    {
                        Id = v.PublicId, // Usando PublicId (Guid) para o DTO conforme boas práticas
                        Title = v.Title,
                        Description = v.Description,
                        StorageIdentifier = v.StorageIdentifier,
                        ThumbnailUrl = v.ThumbnailUrl,
                        Status = v.Status.ToString(),
                        UploadDate = v.UploadDate,
                    })
                    .ToList();

                return new PaginatedResultDto<VideoDto>(dtos, result.TotalCount, page, pageSize);
            },
            TimeSpan.FromMinutes(10)
        );
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
            var novaThumb = await _fileService.SalvarArquivoAsync(dto.ThumbnailFile, "Thumbnails");
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
        if (video == null)
            throw new ResourceNotFoundException("Vídeo não encontrado.");

        // 1. Deletar Arquivo Original MP4 (Usando o UploadService Genérico)
        if (video.FileId > 0)
        {
            // Isso remove o arquivo físico da pasta uploads/Videos e o registro na tabela Files [cite: 28, 29]
            await _fileService.DeletarArquivoAsync(video.FileId);
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
