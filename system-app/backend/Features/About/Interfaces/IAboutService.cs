using System.Threading.Tasks;
using MeuCrudCsharp.Features.About.DTOs;

namespace MeuCrudCsharp.Features.About.Interfaces;

public interface IAboutService
{
    // Leitura (Mantém o DTO de leitura)
    Task<AboutPageContentDto> GetAboutPageContentAsync();

    // Seção (Usa DTO de escrita)
    Task<AboutSectionDto?> CreateSectionAsync(CreateUpdateAboutSectionDto dto);
    Task<bool> UpdateSectionAsync(int id, CreateUpdateAboutSectionDto dto);
    Task DeleteSectionAsync(int id);

    // Membros (Usa DTO de escrita)
    Task<TeamMemberDto> CreateTeamMemberAsync(CreateUpdateTeamMemberDto dto);
    Task<bool> UpdateTeamMemberAsync(int id, CreateUpdateTeamMemberDto dto);
    Task DeleteTeamMemberAsync(int id);
}