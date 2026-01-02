using System;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Home.DTOs;
using MeuCrudCsharp.Features.Home.Interfaces;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Home.Services;

public class HomeService : IHomeService
{
    private readonly IHomeRepository _repository;
    private readonly ICacheService _cache;

    // Chave única para o Cache da Home
    private const string HOME_CACHE_KEY = "HOME_PAGE_CONTENT";

    public HomeService(IHomeRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache; // Injeção do serviço de cache 
    }

    // ==========================================
    // LEITURA (PÚBLICA)
    // ==========================================
    public async Task<HomeContentDto> GetHomeContentAsync()
    {
        // Tenta pegar do cache, se não existir, executa a função interna e salva 
        return await _cache.GetOrCreateAsync(HOME_CACHE_KEY, async () =>
        {
            var heroes = await _repository.GetAllHeroesAsync();
            var services = await _repository.GetAllServicesAsync();

            return new HomeContentDto
            {
                Hero = heroes.Select(h => new HeroSlideDto
                {
                    Id = h.Id,
                    Title = h.Title,
                    Subtitle = h.Subtitle,
                    ImageUrl = h.ImageUrl, // String direta [cite: 8]
                    ActionText = h.ActionText,
                    ActionUrl = h.ActionUrl
                }).ToList(),

                Services = services.Select(s => new ServiceDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    IconClass = s.IconClass,
                    ActionText = s.ActionText,
                    ActionUrl = s.ActionUrl
                }).ToList()
            };
        }) ?? new HomeContentDto();
    }

    // ==========================================
    // MÉTODOS DE ESCRITA (ADMIN)
    // Sempre que alterar algo, limpamos o Cache
    // ==========================================
    public async Task<HeroSlideDto> CreateHeroAsync(HeroSlideDto dto)
    {
        var entity = new HomeHero
        {
            Title = dto.Title,
            Subtitle = dto.Subtitle,
            ImageUrl = dto.ImageUrl, // URL já vem pronta do Controller de Upload
            ActionText = dto.ActionText,
            ActionUrl = dto.ActionUrl
        };

        await _repository.AddHeroAsync(entity);

        // Invalida o cache para que a próxima visita veja o novo banner [cite: 30]
        await _cache.RemoveAsync(HOME_CACHE_KEY);

        dto.Id = entity.Id;
        return dto;
    }

    public async Task UpdateHeroAsync(int id, HeroSlideDto dto)
    {
        var entity = await _repository.GetHeroByIdAsync(id);
        if (entity == null)
            throw new ResourceNotFoundException($"Hero com ID {id} não encontrado.");

        entity.Title = dto.Title;
        entity.Subtitle = dto.Subtitle;
        entity.ImageUrl = dto.ImageUrl;
        entity.ActionText = dto.ActionText;
        entity.ActionUrl = dto.ActionUrl;

        await _repository.UpdateHeroAsync(entity);
        await _cache.RemoveAsync(HOME_CACHE_KEY);
    }

    public async Task DeleteHeroAsync(int id)
    {
        var entity = await _repository.GetHeroByIdAsync(id);
        if (entity == null)
            throw new ResourceNotFoundException($"Hero com ID {id} não encontrado.");

        await _repository.DeleteHeroAsync(entity);
        await _cache.RemoveAsync(HOME_CACHE_KEY);
    }

    public async Task<ServiceDto> CreateServiceAsync(ServiceDto dto)
    {
        var entity = new Models.HomeService
        {
            Title = dto.Title,
            Description = dto.Description,
            IconClass = dto.IconClass,
            ActionText = dto.ActionText,
            ActionUrl = dto.ActionUrl
        };

        await _repository.AddServiceAsync(entity);
        await _cache.RemoveAsync(HOME_CACHE_KEY);

        dto.Id = entity.Id;
        return dto;
    }

    public async Task UpdateServiceAsync(int id, ServiceDto dto)
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