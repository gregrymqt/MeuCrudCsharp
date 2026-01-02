using System;
using MeuCrudCsharp.Features.Home.DTOs;

namespace MeuCrudCsharp.Features.Home.Interfaces;

public interface IHomeService
    {
        // Método principal que o Front-end chama ao carregar a página
        Task<HomeContentDto> GetHomeContentAsync();

        // CRUD Hero
        Task<HeroSlideDto> CreateHeroAsync(HeroSlideDto dto);
        Task UpdateHeroAsync(int id, HeroSlideDto dto);
        Task DeleteHeroAsync(int id);

        // CRUD Services
        Task<ServiceDto> CreateServiceAsync(ServiceDto dto);
        Task UpdateServiceAsync(int id, ServiceDto dto);
        Task DeleteServiceAsync(int id);
    }
