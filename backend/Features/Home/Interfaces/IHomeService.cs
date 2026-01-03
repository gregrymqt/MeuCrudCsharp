using System.Threading.Tasks;
using MeuCrudCsharp.Features.Home.DTOs;

namespace MeuCrudCsharp.Features.Home.Interfaces;

public interface IHomeService
{
    // Leitura
    Task<HomeContentDto> GetHomeContentAsync();

    // CRUD Hero (Agora aceita CreateUpdateHeroDto)
    Task<HeroSlideDto> CreateHeroAsync(CreateUpdateHeroDto dto);
    Task UpdateHeroAsync(int id, CreateUpdateHeroDto dto);
    Task DeleteHeroAsync(int id);

    // CRUD Services (Agora aceita CreateUpdateServiceDto)
    Task<ServiceDto> CreateServiceAsync(CreateUpdateServiceDto dto);
    Task UpdateServiceAsync(int id, CreateUpdateServiceDto dto);
    Task DeleteServiceAsync(int id);
}
