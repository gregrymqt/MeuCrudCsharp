using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Files.Interfaces;
using MeuCrudCsharp.Features.Home.DTOs;
using MeuCrudCsharp.Features.Home.Interfaces;

namespace MeuCrudCsharp.Features.Home.Services;

public class HomeService : IHomeService
{
    private readonly IHomeRepository _repository;
    private readonly ICacheService _cache;
    private readonly IFileService _fileService;

    private const string HOME_CACHE_KEY = "HOME_PAGE_CONTENT";
    private const string FEATURE_CATEGORY = "HomeHero";

    public HomeService(IHomeRepository repository, ICacheService cache, IFileService fileService)
    {
        _repository = repository;
        _cache = cache;
        _fileService = fileService;
    }

    public async Task<HomeContentDto> GetHomeContentAsync()
    {
        return await _cache.GetOrCreateAsync(
                HOME_CACHE_KEY,
                async () =>
                {
                    var heroes = await _repository.GetAllHeroesAsync();
                    var services = await _repository.GetAllServicesAsync();

                    return new HomeContentDto
                    {
                        Hero = heroes
                            .Select(h => new HeroSlideDto
                            {
                                Id = h.Id,
                                Title = h.Title,
                                Subtitle = h.Subtitle,
                                ImageUrl = h.ImageUrl,
                                ActionText = h.ActionText,
                                ActionUrl = h.ActionUrl,
                            })
                            .ToList(),

                        Services = services
                            .Select(s => new ServiceDto
                            {
                                Id = s.Id,
                                Title = s.Title,
                                Description = s.Description,
                                IconClass = s.IconClass,
                                ActionText = s.ActionText,
                                ActionUrl = s.ActionUrl,
                            })
                            .ToList(),
                    };
                }
            ) ?? new HomeContentDto();
    }

    // =========================================================================
    // HERO (COM LÓGICA DE CHUNKS)
    // =========================================================================

    public async Task<HeroSlideDto?> CreateHeroAsync(CreateUpdateHeroDto dto)
    {
        string imageUrl = string.Empty;
        int? fileId = null;

        // 1. Lógica de Chunking (Arquivos Grandes/Fatiados)
        if (dto.IsChunk && dto.File != null)
        {
            // Processa o pedaço atual
            var tempPath = await _fileService.ProcessChunkAsync(
                dto.File,
                dto.FileName,
                dto.ChunkIndex,
                dto.TotalChunks
            );

            // Se retornar null, significa que ainda faltam pedaços.
            // Retornamos null para a Controller avisar o Front para mandar o próximo.
            if (tempPath == null)
                return null;

            // Se chegou aqui, o arquivo foi remontado com sucesso no Temp!
            // Agora movemos para a pasta oficial e registramos no banco.
            var arquivoSalvo = await _fileService.SalvarArquivoDoTempAsync(
                tempPath,
                dto.FileName,
                FEATURE_CATEGORY
            );
            imageUrl = arquivoSalvo.CaminhoRelativo;
            fileId = arquivoSalvo.Id;
        }
        // 2. Lógica de Upload Normal (Direto)
        else if (dto.File != null)
        {
            var arquivoSalvo = await _fileService.SalvarArquivoAsync(dto.File, FEATURE_CATEGORY);
            imageUrl = arquivoSalvo.CaminhoRelativo;
            fileId = arquivoSalvo.Id;
        }

        // 3. Cria a Entidade no Banco (Só executa se não for chunk ou se for o ÚLTIMO chunk)
        var entity = new Models.HomeHero
        {
            Title = dto.Title,
            Subtitle = dto.Subtitle,
            ImageUrl = imageUrl,
            FileId = fileId,
            ActionText = dto.ActionText,
            ActionUrl = dto.ActionUrl,
        };

        await _repository.AddHeroAsync(entity);
        await _cache.RemoveAsync(HOME_CACHE_KEY);

        return new HeroSlideDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Subtitle = entity.Subtitle,
            ImageUrl = entity.ImageUrl,
            ActionText = entity.ActionText,
            ActionUrl = entity.ActionUrl,
        };
    }

    public async Task<bool> UpdateHeroAsync(int id, CreateUpdateHeroDto dto)
    {
        var entity = await _repository.GetHeroByIdAsync(id);
        if (entity == null)
            throw new ResourceNotFoundException($"Hero com ID {id} não encontrado.");

        // --- LÓGICA DE ARQUIVO ---
        if (dto.IsChunk && dto.File != null)
        {
            var tempPath = await _fileService.ProcessChunkAsync(
                dto.File,
                dto.FileName,
                dto.ChunkIndex,
                dto.TotalChunks
            );

            // Se ainda não acabou os chunks, retorna false
            if (tempPath == null)
                return false;

            // Acabou! Substitui o arquivo usando o temp
            if (entity.FileId.HasValue)
            {
                var arquivoAtualizado = await _fileService.SubstituirArquivoDoTempAsync(
                    entity.FileId.Value,
                    tempPath,
                    dto.FileName
                );
                entity.ImageUrl = arquivoAtualizado.CaminhoRelativo;
                entity.FileId = arquivoAtualizado.Id;
            }
            else
            {
                var arquivoSalvo = await _fileService.SalvarArquivoDoTempAsync(
                    tempPath,
                    dto.FileName,
                    FEATURE_CATEGORY
                );
                entity.ImageUrl = arquivoSalvo.CaminhoRelativo;
                entity.FileId = arquivoSalvo.Id;
            }
        }
        else if (dto.File != null) // Upload normal
        {
            if (entity.FileId.HasValue)
            {
                var arquivoAtualizado = await _fileService.SubstituirArquivoAsync(
                    entity.FileId.Value,
                    dto.File
                );
                entity.ImageUrl = arquivoAtualizado.CaminhoRelativo;
                entity.FileId = arquivoAtualizado.Id;
            }
            else
            {
                var arquivoSalvo = await _fileService.SalvarArquivoAsync(
                    dto.File,
                    FEATURE_CATEGORY
                );
                entity.ImageUrl = arquivoSalvo.CaminhoRelativo;
                entity.FileId = arquivoSalvo.Id;
            }
        }

        // --- ATUALIZA DADOS DE TEXTO ---
        // Só atualizamos os textos se for upload normal OU o último chunk
        entity.Title = dto.Title;
        entity.Subtitle = dto.Subtitle;
        entity.ActionText = dto.ActionText;
        entity.ActionUrl = dto.ActionUrl;

        await _repository.UpdateHeroAsync(entity);
        await _cache.RemoveAsync(HOME_CACHE_KEY);

        return true; // Update finalizado
    }

    public async Task DeleteHeroAsync(int id)
    {
        var entity = await _repository.GetHeroByIdAsync(id);
        if (entity == null)
            throw new ResourceNotFoundException($"Hero com ID {id} não encontrado.");

        if (entity.FileId.HasValue)
        {
            await _fileService.DeletarArquivoAsync(entity.FileId.Value);
        }

        await _repository.DeleteHeroAsync(entity);
        await _cache.RemoveAsync(HOME_CACHE_KEY);
    }

    // =========================================================================
    // SERVICES (SEM ARQUIVOS - JSON PURO)
    // =========================================================================

    public async Task<ServiceDto> CreateServiceAsync(CreateUpdateServiceDto dto)
    {
        // Aqui usamos o namespace completo ou alias se houver conflito com "Service" do sistema
        var entity = new Models.HomeService
        {
            Title = dto.Title,
            Description = dto.Description,
            IconClass = dto.IconClass,
            ActionText = dto.ActionText,
            ActionUrl = dto.ActionUrl,
        };

        await _repository.AddServiceAsync(entity);
        await _cache.RemoveAsync(HOME_CACHE_KEY);

        return new ServiceDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            IconClass = entity.IconClass,
            ActionText = entity.ActionText,
            ActionUrl = entity.ActionUrl,
        };
    }

    public async Task UpdateServiceAsync(int id, CreateUpdateServiceDto dto)
    {
        var entity = await _repository.GetServiceByIdAsync(id);
        if (entity == null)
            throw new ResourceNotFoundException($"Serviço com ID {id} não encontrado.");

        entity.Title = dto.Title;
        entity.Description = dto.Description;
        entity.IconClass = dto.IconClass;
        entity.ActionText = dto.ActionText;
        entity.ActionUrl = dto.ActionUrl;

        await _repository.UpdateServiceAsync(entity);
        await _cache.RemoveAsync(HOME_CACHE_KEY);
    }

    public async Task DeleteServiceAsync(int id)
    {
        var entity = await _repository.GetServiceByIdAsync(id);
        if (entity == null)
            throw new ResourceNotFoundException($"Serviço com ID {id} não encontrado.");

        await _repository.DeleteServiceAsync(entity);
        await _cache.RemoveAsync(HOME_CACHE_KEY);
    }
}
