using System;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Home.Interfaces;

public interface IHomeRepository
{
    // Métodos para Hero
    Task<List<HomeHero>> GetAllHeroesAsync();
    Task<HomeHero?> GetHeroByIdAsync(int id);
    Task AddHeroAsync(HomeHero hero);
    Task UpdateHeroAsync(HomeHero hero);
    Task DeleteHeroAsync(HomeHero hero);

    // Métodos para Services
    Task<List<HomeService>> GetAllServicesAsync();
    Task<HomeService?> GetServiceByIdAsync(int id);
    Task AddServiceAsync(HomeService service);
    Task UpdateServiceAsync(HomeService service);
    Task DeleteServiceAsync(HomeService service);
}
