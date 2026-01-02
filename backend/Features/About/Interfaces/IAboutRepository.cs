using System;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.About.Interfaces;

public interface IAboutRepository
    {
        // --- Generic Sections (Texto + Imagem) ---
        Task<List<AboutSection>> GetAllSectionsAsync();
        Task<AboutSection?> GetSectionByIdAsync(int id);
        Task AddSectionAsync(AboutSection section);
        Task UpdateSectionAsync(AboutSection section);
        Task DeleteSectionAsync(AboutSection section);

        // --- Team Members (Membros da Equipe) ---
        Task<List<TeamMember>> GetAllTeamMembersAsync();
        Task<TeamMember?> GetTeamMemberByIdAsync(int id);
        Task AddTeamMemberAsync(TeamMember member);
        Task UpdateTeamMemberAsync(TeamMember member);
        Task DeleteTeamMemberAsync(TeamMember member);
    }
