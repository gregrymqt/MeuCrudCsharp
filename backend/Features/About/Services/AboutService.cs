using System;
using MeuCrudCsharp.Features.About.DTOs;
using MeuCrudCsharp.Features.About.Interfaces;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Files.Interfaces;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.About.Services;

public class AboutService : IAboutService
{
    private readonly IAboutRepository _repository;
    private readonly ICacheService _cache;
    private readonly IFileService _fileService; // Injeção

    private const string ABOUT_CACHE_KEY = "ABOUT_PAGE_CONTENT";
    private const string CAT_SECTION = "AboutSection"; // Categorias para organizar arquivos
    private const string CAT_TEAM = "AboutTeam";

    public AboutService(IAboutRepository repository, ICacheService cache, IFileService fileService)
    {
        _repository = repository;
        _cache = cache;
        _fileService = fileService;
    }

    public async Task<AboutPageContentDto> GetAboutPageContentAsync()
    {
        // Lógica de leitura mantida igual
        return await _cache.GetOrCreateAsync(
                ABOUT_CACHE_KEY,
                async () =>
                {
                    var sections = await _repository.GetAllSectionsAsync();
                    var members = await _repository.GetAllTeamMembersAsync();

                    return new AboutPageContentDto
                    {
                        Sections = sections
                            .Select(s => new AboutSectionDto
                            {
                                Id = s.Id,
                                Title = s.Title,
                                Description = s.Description,
                                ImageUrl = s.ImageUrl,
                                ImageAlt = s.ImageAlt,
                                ContentType = "section1",
                            })
                            .ToList(),

                        TeamSection = new AboutTeamSectionDto
                        {
                            Title = "Nosso Time",
                            Description = "Conheça os especialistas",
                            ContentType = "section2",
                            Members = members
                                .Select(m => new TeamMemberDto
                                {
                                    Id = m.Id,
                                    Name = m.Name,
                                    Role = m.Role,
                                    PhotoUrl = m.PhotoUrl,
                                    LinkedinUrl = m.LinkedinUrl,
                                    GithubUrl = m.GithubUrl,
                                })
                                .ToList(),
                        },
                    };
                }
            ) ?? new AboutPageContentDto();
    }

    // ==========================================
    // SEÇÕES
    // ==========================================

    public async Task<AboutSectionDto> CreateSectionAsync(CreateUpdateAboutSectionDto dto)
    {
        string imageUrl = string.Empty;

        // Upload se houver arquivo
        if (dto.File != null)
        {
            var arquivoSalvo = await _fileService.SalvarArquivoAsync(dto.File, CAT_SECTION);
            imageUrl = arquivoSalvo.CaminhoRelativo;
        }

        var entity = new AboutSection
        {
            Title = dto.Title,
            Description = dto.Description,
            ImageAlt = dto.ImageAlt,
            ImageUrl = imageUrl,
            OrderIndex = 0,
        };

        await _repository.AddSectionAsync(entity);
        await _cache.RemoveAsync(ABOUT_CACHE_KEY);

        return new AboutSectionDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            ImageUrl = entity.ImageUrl,
            ImageAlt = entity.ImageAlt,
            ContentType = "section1",
        };
    }

    public async Task UpdateSectionAsync(int id, CreateUpdateAboutSectionDto dto)
    {
        var entity = await _repository.GetSectionByIdAsync(id);
        if (entity == null)
            throw new ResourceNotFoundException($"Seção {id} não encontrada.");

        entity.Title = dto.Title;
        entity.Description = dto.Description;
        entity.ImageAlt = dto.ImageAlt;

        // Se enviou nova imagem, substitui
        if (dto.File != null)
        {
            var arquivoSalvo = await _fileService.SalvarArquivoAsync(dto.File, CAT_SECTION);
            entity.ImageUrl = arquivoSalvo.CaminhoRelativo;
        }

        await _repository.UpdateSectionAsync(entity);
        await _cache.RemoveAsync(ABOUT_CACHE_KEY);
    }

    public async Task DeleteSectionAsync(int id)
    {
        var entity = await _repository.GetSectionByIdAsync(id);
        if (entity == null)
            throw new ResourceNotFoundException($"Seção {id} não encontrada.");

        await _repository.DeleteSectionAsync(entity);
        await _cache.RemoveAsync(ABOUT_CACHE_KEY);
    }

    // ==========================================
    // EQUIPE
    // ==========================================

    public async Task<TeamMemberDto> CreateTeamMemberAsync(CreateUpdateTeamMemberDto dto)
    {
        string photoUrl = string.Empty;

        if (dto.File != null)
        {
            var arquivoSalvo = await _fileService.SalvarArquivoAsync(dto.File, CAT_TEAM);
            photoUrl = arquivoSalvo.CaminhoRelativo;
        }

        var entity = new TeamMember
        {
            Name = dto.Name,
            Role = dto.Role,
            LinkedinUrl = dto.LinkedinUrl,
            GithubUrl = dto.GithubUrl,
            PhotoUrl = photoUrl,
        };

        await _repository.AddTeamMemberAsync(entity);
        await _cache.RemoveAsync(ABOUT_CACHE_KEY);

        return new TeamMemberDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Role = entity.Role,
            PhotoUrl = entity.PhotoUrl,
            LinkedinUrl = entity.LinkedinUrl,
            GithubUrl = entity.GithubUrl,
        };
    }

    public async Task UpdateTeamMemberAsync(int id, CreateUpdateTeamMemberDto dto)
    {
        var entity = await _repository.GetTeamMemberByIdAsync(id);
        if (entity == null)
            throw new ResourceNotFoundException($"Membro {id} não encontrado.");

        entity.Name = dto.Name;
        entity.Role = dto.Role;
        entity.LinkedinUrl = dto.LinkedinUrl;
        entity.GithubUrl = dto.GithubUrl;

        // Se enviou nova foto, substitui
        if (dto.File != null)
        {
            var arquivoSalvo = await _fileService.SalvarArquivoAsync(dto.File, CAT_TEAM);
            entity.PhotoUrl = arquivoSalvo.CaminhoRelativo;
        }

        await _repository.UpdateTeamMemberAsync(entity);
        await _cache.RemoveAsync(ABOUT_CACHE_KEY);
    }

    public async Task DeleteTeamMemberAsync(int id)
    {
        var entity = await _repository.GetTeamMemberByIdAsync(id);
        if (entity == null)
            throw new ResourceNotFoundException($"Membro {id} não encontrado.");

        await _repository.DeleteTeamMemberAsync(entity);
        await _cache.RemoveAsync(ABOUT_CACHE_KEY);
    }
}
