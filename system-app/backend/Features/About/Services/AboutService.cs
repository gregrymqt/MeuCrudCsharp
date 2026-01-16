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

    public async Task<AboutSectionDto?> CreateSectionAsync(CreateUpdateAboutSectionDto dto)
    {
        string imageUrl = string.Empty;
        int? fileId = null;

        // 1. Lógica de Chunking (Arquivo Grande)
        if (dto.IsChunk && dto.File != null)
        {
            // Processa o pedaço e vê se completou
            var tempPath = await _fileService.ProcessChunkAsync(
                dto.File,
                dto.FileName,
                dto.ChunkIndex,
                dto.TotalChunks
            );

            // Se for null, ainda faltam pedaços. Retorna null para o Controller dar OK.
            if (tempPath == null)
                return null;

            // Se retornou path, o arquivo está completo na pasta temp! Vamos salvar.
            var arquivoSalvo = await _fileService.SalvarArquivoDoTempAsync(
                tempPath,
                dto.FileName,
                CAT_SECTION
            );
            imageUrl = arquivoSalvo.CaminhoRelativo;
            fileId = arquivoSalvo.Id;
        }
        // 2. Lógica de Upload Normal (Arquivo Pequeno)
        else if (dto.File != null)
        {
            var arquivoSalvo = await _fileService.SalvarArquivoAsync(dto.File, CAT_SECTION);
            imageUrl = arquivoSalvo.CaminhoRelativo;
            fileId = arquivoSalvo.Id;
        }

        // 3. Criação da Entidade (Só roda se não for chunk ou se for o ÚLTIMO chunk)
        var entity = new AboutSection
        {
            Title = dto.Title,
            Description = dto.Description,
            ImageAlt = dto.ImageAlt,
            ImageUrl = imageUrl,
            FileId = fileId,
            OrderIndex = dto.OrderIndex, // Use lógica de Max + 1 se quiser
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

    // Retorna TRUE se finalizou o update, FALSE se está esperando mais chunks
    public async Task<bool> UpdateSectionAsync(int id, CreateUpdateAboutSectionDto dto)
    {
        // Só busca a entidade se NÃO for um chunk intermediário (pra economizar banco)
        // OU se for chunk, só busca no último passo.
        // Mas para simplificar validação, buscamos logo.
        var entity = await _repository.GetSectionByIdAsync(id);
        if (entity == null)
            throw new ResourceNotFoundException($"Seção {id} não encontrada.");

        // === Lógica de Arquivo ===
        if (dto.IsChunk && dto.File != null)
        {
            var tempPath = await _fileService.ProcessChunkAsync(
                dto.File,
                dto.FileName,
                dto.ChunkIndex,
                dto.TotalChunks
            );

            // Se ainda não acabou os chunks, retorna false
            if (tempPath == null)
                return false;

            // Acabou! Substitui o arquivo usando o temp
            if (entity.FileId.HasValue)
            {
                var arquivoAtualizado = await _fileService.SubstituirArquivoDoTempAsync(
                    entity.FileId.Value,
                    tempPath,
                    dto.FileName
                );
                entity.ImageUrl = arquivoAtualizado.CaminhoRelativo;
                entity.FileId = arquivoAtualizado.Id;
            }
            else
            {
                var arquivoSalvo = await _fileService.SalvarArquivoDoTempAsync(
                    tempPath,
                    dto.FileName,
                    CAT_SECTION
                );
                entity.ImageUrl = arquivoSalvo.CaminhoRelativo;
                entity.FileId = arquivoSalvo.Id;
            }
        }
        else if (dto.File != null) // Upload normal
        {
            if (entity.FileId.HasValue)
            {
                var arquivoAtualizado = await _fileService.SubstituirArquivoAsync(
                    entity.FileId.Value,
                    dto.File
                );
                entity.ImageUrl = arquivoAtualizado.CaminhoRelativo;
                entity.FileId = arquivoAtualizado.Id;
            }
            else
            {
                var arquivoSalvo = await _fileService.SalvarArquivoAsync(dto.File, CAT_SECTION);
                entity.ImageUrl = arquivoSalvo.CaminhoRelativo;
                entity.FileId = arquivoSalvo.Id;
            }
        }

        // === Atualiza Dados ===
        // Só atualiza os dados de texto se for upload normal OU se for o último chunk
        entity.Title = dto.Title;
        entity.Description = dto.Description;
        entity.ImageAlt = dto.ImageAlt;

        await _repository.UpdateSectionAsync(entity);
        await _cache.RemoveAsync(ABOUT_CACHE_KEY);

        return true; // Update concluído
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

    public async Task<TeamMemberDto?> CreateTeamMemberAsync(CreateUpdateTeamMemberDto dto)
    {
        string photoUrl = string.Empty;
        int? fileId = null;

        if (dto.IsChunk && dto.File != null)
        {
            var tempPath = await _fileService.ProcessChunkAsync(
                dto.File,
                dto.FileName,
                dto.ChunkIndex,
                dto.TotalChunks
            );
            if (tempPath == null)
                return null; // Esperando mais chunks

            var arquivoSalvo = await _fileService.SalvarArquivoDoTempAsync(
                tempPath,
                dto.FileName,
                CAT_TEAM
            );
            photoUrl = arquivoSalvo.CaminhoRelativo;
            fileId = arquivoSalvo.Id;
        }
        else if (dto.File != null)
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

    public async Task<bool> UpdateTeamMemberAsync(int id, CreateUpdateTeamMemberDto dto)
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

        return true;
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
