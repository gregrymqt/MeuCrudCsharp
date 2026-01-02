using System;
using MeuCrudCsharp.Features.About.DTOs;

namespace MeuCrudCsharp.Features.About.Interfaces;

// Objeto container para retornar tudo de uma vez para o Front
    public class AboutPageContentDto
    {
        public List<AboutSectionDto> Sections { get; set; } = new();
        public AboutTeamSectionDto TeamSection { get; set; } = new();
    }

    public interface IAboutService
    {
        // Leitura Pública
        Task<AboutPageContentDto> GetAboutPageContentAsync();

        // CRUD Sections
        Task<AboutSectionDto> CreateSectionAsync(AboutSectionDto dto);
        Task UpdateSectionAsync(int id, AboutSectionDto dto);
        Task DeleteSectionAsync(int id);

        // CRUD Team Members
        Task<TeamMemberDto> CreateTeamMemberAsync(TeamMemberDto dto);
        Task UpdateTeamMemberAsync(int id, TeamMemberDto dto);
        Task DeleteTeamMemberAsync(int id);
    }
