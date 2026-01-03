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
        int? fileId = null;

        // Upload se houver arquivo
        if (dto.File != null)
        {
            var arquivoSalvo = await _fileService.SalvarArquivoAsync(dto.File, CAT_SECTION);
            imageUrl = arquivoSalvo.CaminhoRelativo;
            fileId = arquivoSalvo.Id; // Captura o ID
        }

        var entity = new AboutSection
        {
            Title = dto.Title,
            Description = dto.Description,
            ImageAlt = dto.ImageAlt,
            ImageUrl = imageUrl,
            FileId = fileId, // Salva o ID
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

        // UPDATE: Lógica de Substituição
        if (dto.File != null)
        {
            if (entity.FileId.HasValue)
            {
                // Se já tinha arquivo, SUBSTITUI (apaga o antigo físico e atualiza metadados)
                var arquivoAtualizado = await _fileService.SubstituirArquivoAsync(
                    entity.FileId.Value,
                    dto.File
                );
                entity.ImageUrl = arquivoAtualizado.CaminhoRelativo;
                // O ID geralmente se mantém o mesmo no update, ou muda dependendo da sua impl.
                // Assumindo que o objeto retornado tem os dados corretos:
                entity.FileId = arquivoAtualizado.Id;
            }
            else
            {
                // Se não tinha arquivo antes, apenas SALVA
                var arquivoSalvo = await _fileService.SalvarArquivoAsync(dto.File, CAT_SECTION);
                entity.ImageUrl = arquivoSalvo.CaminhoRelativo;
                entity.FileId = arquivoSalvo.Id;
            }
        }

        await _repository.UpdateSectionAsync(entity);
        await _cache.RemoveAsync(ABOUT_CACHE_KEY);
    }

    public async Task DeleteSectionAsync(int id)
    {
        var entity = await _repository.GetSectionByIdAsync(id);
        if (entity == null)
            throw new ResourceNotFoundException($"Seção {id} não encontrada.");

        // DELETE: Apaga o arquivo físico e registro do arquivo
        if (entity.FileId.HasValue)
        {
            await _fileService.DeletarArquivoAsync(entity.FileId.Value);
        }

        await _repository.DeleteSectionAsync(entity);
        await _cache.RemoveAsync(ABOUT_CACHE_KEY);
    }

    // ==========================================
    // EQUIPE
    // ==========================================

    public async Task<TeamMemberDto> CreateTeamMemberAsync(CreateUpdateTeamMemberDto dto)
    {
        string photoUrl = string.Empty;
        int? fileId = null;

        if (dto.File != null)
        {
            var arquivoSalvo = await _fileService.SalvarArquivoAsync(dto.File, CAT_TEAM);
            photoUrl = arquivoSalvo.CaminhoRelativo;
            fileId = arquivoSalvo.Id;
        }

        var entity = new TeamMember
        {
            Name = dto.Name,
            Role = dto.Role,
            LinkedinUrl = dto.LinkedinUrl,
            GithubUrl = dto.GithubUrl,
            PhotoUrl = photoUrl,
            FileId = fileId,
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

        // UPDATE: Substituição inteligente
        if (dto.File != null)
        {
            if (entity.FileId.HasValue)
            {
                var arquivoAtualizado = await _fileService.SubstituirArquivoAsync(
                    entity.FileId.Value,
                    dto.File
                );
                entity.PhotoUrl = arquivoAtualizado.CaminhoRelativo;
                entity.FileId = arquivoAtualizado.Id;
            }
            else
            {
                var arquivoSalvo = await _fileService.SalvarArquivoAsync(dto.File, CAT_TEAM);
                entity.PhotoUrl = arquivoSalvo.CaminhoRelativo;
                entity.FileId = arquivoSalvo.Id;
            }
        }

        await _repository.UpdateTeamMemberAsync(entity);
        await _cache.RemoveAsync(ABOUT_CACHE_KEY);
    }

    public async Task DeleteTeamMemberAsync(int id)
    {
        var entity = await _repository.GetTeamMemberByIdAsync(id);
        if (entity == null)
            throw new ResourceNotFoundException($"Membro {id} não encontrado.");

        // DELETE: Limpeza do arquivo
        if (entity.FileId.HasValue)
        {
            await _fileService.DeletarArquivoAsync(entity.FileId.Value);
        }

        await _repository.DeleteTeamMemberAsync(entity);
        await _cache.RemoveAsync(ABOUT_CACHE_KEY);
    }
}
