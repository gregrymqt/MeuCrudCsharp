using System;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Files.Interfaces;
using MeuCrudCsharp.Features.Home.DTOs;
using MeuCrudCsharp.Features.Home.Interfaces;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Home.Services;

public class HomeService : IHomeService
{
    private readonly IHomeRepository _repository;
    private readonly ICacheService _cache;
    private readonly IFileService _fileService; // Injeção do serviço de arquivos

    private const string HOME_CACHE_KEY = "HOME_PAGE_CONTENT";
    private const string FEATURE_CATEGORY = "HomeHero"; // Categoria para organizar os uploads

    public HomeService(IHomeRepository repository, ICacheService cache, IFileService fileService)
    {
        _repository = repository;
        _cache = cache;
        _fileService = fileService;
    }

    // ==========================================
    // LEITURA (PÚBLICA)
    // ==========================================
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
                                ImageUrl = h.ImageUrl, //
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

    // ==========================================
    // HERO (COM UPLOAD DE ARQUIVO)
    // ==========================================

    public async Task<HeroSlideDto> CreateHeroAsync(CreateUpdateHeroDto dto)
    {
        // 1. Lógica de Upload usando IFileService
        string imageUrl = string.Empty;

        if (dto.File != null)
        {
            // Salva o arquivo físico e no banco de arquivos
            var arquivoSalvo = await _fileService.SalvarArquivoAsync(dto.File, FEATURE_CATEGORY);

            // Usamos o caminho relativo retornado pelo serviço de arquivos
            // Você pode precisar prefixar com "/" ou a URL base dependendo de como serve estáticos
            imageUrl = arquivoSalvo.CaminhoRelativo;
        }

        // 2. Criação da Entidade
        var entity = new HomeHero
        {
            Title = dto.Title,
            Subtitle = dto.Subtitle,
            ImageUrl = imageUrl, // Salva o caminho retornado pelo FileService
            ActionText = dto.ActionText,
            ActionUrl = dto.ActionUrl,
        };

        await _repository.AddHeroAsync(entity);

        // 3. Limpa cache
        await _cache.RemoveAsync(HOME_CACHE_KEY);

        // 4. Retorna o DTO de leitura
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

    public async Task UpdateHeroAsync(int id, CreateUpdateHeroDto dto)
    {
        var entity = await _repository.GetHeroByIdAsync(id);
        if (entity == null)
            throw new ResourceNotFoundException($"Hero com ID {id} não encontrado.");

        // Atualiza campos de texto
        entity.Title = dto.Title;
        entity.Subtitle = dto.Subtitle;
        entity.ActionText = dto.ActionText;
        entity.ActionUrl = dto.ActionUrl;

        // Atualiza imagem APENAS se um novo arquivo foi enviado
        if (dto.File != null)
        {
            // Como HomeHero guarda apenas a string e não o ID do EntityFile,
            // tratamos como um novo upload.
            // (Idealmente, se HomeHero tivesse 'FileId', usaríamos SubstituirArquivoAsync)

            var arquivoSalvo = await _fileService.SalvarArquivoAsync(dto.File, FEATURE_CATEGORY);
            entity.ImageUrl = arquivoSalvo.CaminhoRelativo;
        }

        await _repository.UpdateHeroAsync(entity);
        await _cache.RemoveAsync(HOME_CACHE_KEY);
    }

    public async Task DeleteHeroAsync(int id)
    {
        var entity = await _repository.GetHeroByIdAsync(id);
        if (entity == null)
            throw new ResourceNotFoundException($"Hero com ID {id} não encontrado.");

        // Nota: Se você quisesse apagar o arquivo físico, precisaria buscar o ID dele
        // através do caminho (ImageUrl) ou alterar a model HomeHero para ter FileId.
        // Por enquanto, removemos apenas o registro do Hero.

        await _repository.DeleteHeroAsync(entity);
        await _cache.RemoveAsync(HOME_CACHE_KEY);
    }

    // ==========================================
    // SERVICES (SEM ARQUIVO - JSON PURO)
    // ==========================================

    public async Task<ServiceDto> CreateServiceAsync(CreateUpdateServiceDto dto)
    {
        var entity = new MeuCrudCsharp.Models.HomeService // Namespace completo para evitar ambiguidade com a classe Service
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
