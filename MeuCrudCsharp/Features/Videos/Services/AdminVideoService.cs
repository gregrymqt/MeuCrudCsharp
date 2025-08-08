using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Videos.DTOs;
using MeuCrudCsharp.Features.Videos.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace MeuCrudCsharp.Features.Videos.Service
{
    public class AdminVideoService : IAdminVideoService
    {
        private readonly ApiDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ICacheService _cacheService;
        private readonly ILogger<AdminVideoService> _logger;
        private const string VideosCacheVersionKey = "videos_cache_version";

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

        public async Task<List<VideoDto>> GetAllVideosAsync(int page, int pageSize)
        {
            // MUDANÇA 2: Obter a versão atual do cache.
            var cacheVersion = await _cacheService.GetOrCreateAsync(
                VideosCacheVersionKey,
                () => Task.FromResult(Guid.NewGuid().ToString()), // Se não existir, cria uma nova versão.
                absoluteExpireTime: TimeSpan.FromDays(30)
            ); // A versão pode durar bastante.

            var cacheKey = $"AdminVideos_v{cacheVersion}_Page{page}_Size{pageSize}";

            // MUDANÇA 3: A chamada ao GetOrCreateAsync agora não usa mais o IChangeToken.
            return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogInformation(
                        "Buscando vídeos do banco (cache miss) para a chave: {CacheKey}",
                        cacheKey
                    );
                    try
                    {
                        return await _context
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
                                Status = v.Status.ToString(),
                                CourseName = v.Course.Name,
                                ThumbnailUrl = v.ThumbnailUrl, // Inclui a URL da miniatura
                            })
                            .ToListAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao buscar a lista de vídeos do banco de dados.");
                        throw new AppServiceException(
                            "Ocorreu um erro ao consultar os vídeos.",
                            ex
                        );
                    }
                },
                absoluteExpireTime: TimeSpan.FromMinutes(10)
            ); // O cache da página em si pode ser mais curto.
        }

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
                    ThumbnailUrl = video.ThumbnailUrl, // Inclui a URL da miniatura
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado ao criar o vídeo '{VideoTitle}'.",
                    createDto.Title
                );
                throw new AppServiceException("Ocorreu um erro ao salvar o novo vídeo.", ex);
            }
        }

        public async Task<VideoDto> UpdateVideoAsync(Guid id, UpdateVideoDto updateDto)
        {
            try
            {
                var video = await _context
                    .Videos.Include(v => v.Course)
                    .FirstOrDefaultAsync(v => v.Id == id);
                if (video == null)
                    throw new ResourceNotFoundException(
                        $"Vídeo com ID {id} não encontrado para atualização."
                    );

                // Se um novo arquivo de thumbnail foi enviado, processa a troca.
                if (updateDto.ThumbnailFile != null && updateDto.ThumbnailFile.Length > 0)
                {
                    // --- INÍCIO: Lógica para deletar a thumbnail antiga ---

                    // 1. Verifica se já existe uma thumbnail antiga registrada no banco.
                    if (!string.IsNullOrEmpty(video.ThumbnailUrl))
                    {
                        // 2. Converte a URL relativa para um caminho físico completo.
                        // Remove a barra inicial e converte para o separador do sistema operacional.
                        var relativePath = video
                            .ThumbnailUrl.TrimStart('/')
                            .Replace('/', Path.DirectorySeparatorChar);
                        var oldThumbnailPath = Path.Combine(_env.WebRootPath, relativePath);

                        try
                        {
                            // 3. Verifica se o arquivo físico existe e o deleta.
                            if (File.Exists(oldThumbnailPath))
                            {
                                File.Delete(oldThumbnailPath);
                                _logger.LogInformation(
                                    "Thumbnail antiga '{Path}' deletada com sucesso.",
                                    oldThumbnailPath
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            // 4. Se a deleção falhar, apenas registra um aviso e continua.
                            // Não queremos que a operação inteira falhe por isso.
                            _logger.LogWarning(
                                ex,
                                "Falha ao deletar a thumbnail antiga no caminho: {Path}",
                                oldThumbnailPath
                            );
                        }
                    }
                    // --- FIM: Lógica para deletar a thumbnail antiga ---

                    // 5. Salva a nova thumbnail e atualiza a URL no banco.
                    video.ThumbnailUrl = await SaveThumbnailAsync(updateDto.ThumbnailFile);
                }

                video.Title = updateDto.Title;
                video.Description = updateDto.Description;

                await _context.SaveChangesAsync();
                await InvalidateVideosCache();
                _logger.LogInformation("Vídeo {VideoId} atualizado com sucesso.", id);

                // Retorna o DTO com os dados atualizados
                return new VideoDto
                {
                    Id = video.Id,
                    Title = video.Title,
                    Description = video.Description,
                    StorageIdentifier = video.StorageIdentifier,
                    UploadDate = video.UploadDate,
                    Status = video.Status.ToString(),
                    CourseName = video.Course.Name,
                    ThumbnailUrl = video.ThumbnailUrl,
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco de dados ao atualizar o vídeo {VideoId}.", id);
                throw new AppServiceException(
                    "Ocorreu um erro ao salvar as alterações do vídeo.",
                    ex
                );
            }
        }

        public async Task DeleteVideoAsync(Guid id)
        {
            var video = await _context.Videos.FindAsync(id);
            if (video == null)
                throw new ResourceNotFoundException(
                    $"Vídeo com ID {id} não encontrado para deleção."
                );

            // Tratamento de exceção para a deleção de arquivos
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
                    "Falha ao deletar a pasta de arquivos para o vídeo {VideoId}. A remoção do banco de dados será abortada.",
                    id
                );
                throw new AppServiceException(
                    "Ocorreu um erro ao remover os arquivos físicos do vídeo.",
                    ex
                );
            }

            // Tratamento de exceção para a deleção no banco
            try
            {
                _context.Videos.Remove(video);
                await _context.SaveChangesAsync();

                InvalidateVideosCache();

                _logger.LogInformation(
                    "Vídeo {VideoId} deletado do banco de dados com sucesso.",
                    id
                );
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro de banco de dados ao deletar o vídeo {VideoId}.", id);
                throw new AppServiceException(
                    "Ocorreu um erro ao remover o vídeo do nosso sistema.",
                    ex
                );
            }
        }

        private async Task<string?> SaveThumbnailAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return null; // Nenhum arquivo enviado
            }

            // Cria um diretório para as thumbnails se ele não existir
            var thumbnailsDirectory = Path.Combine(_env.WebRootPath, "thumbnails");
            Directory.CreateDirectory(thumbnailsDirectory);

            // Gera um nome de arquivo único para evitar conflitos
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(thumbnailsDirectory, fileName);

            // Salva o arquivo no disco
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Retorna o caminho relativo para ser salvo no banco de dados
            return $"/thumbnails/{fileName}";
        }

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
                    "Cache de vídeos invalidado. Nova versão: {CacheVersion}",
                    newVersion
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao invalidar o cache de vídeos (atualizar a chave de versão)."
                );
                throw new AppServiceException("Ocorreu um erro ao limpar o cache de vídeos.", ex);
            }
        }
    }
}
