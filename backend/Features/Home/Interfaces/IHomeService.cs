using MeuCrudCsharp.Features.Home.DTOs;

namespace MeuCrudCsharp.Features.Home.Interfaces;

public interface IHomeService
{
    Task<HomeContentDto> GetHomeContentAsync();

    // Mudei para HeroSlideDto? (pode ser nulo se for chunk)
    Task<HeroSlideDto?> CreateHeroAsync(CreateUpdateHeroDto dto);

    // Mudei para bool (false se for chunk, true se acabou)
    Task<bool> UpdateHeroAsync(int id, CreateUpdateHeroDto dto);

    Task DeleteHeroAsync(int id);

    // Services continuam iguais (sem chunks)
    Task<ServiceDto> CreateServiceAsync(CreateUpdateServiceDto dto);
    Task UpdateServiceAsync(int id, CreateUpdateServiceDto dto);
    Task DeleteServiceAsync(int id);
}
