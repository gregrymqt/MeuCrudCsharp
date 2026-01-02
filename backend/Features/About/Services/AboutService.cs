using System;
using MeuCrudCsharp.Features.About.DTOs;
using MeuCrudCsharp.Features.About.Interfaces;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.About.Services;

public class AboutService : IAboutService
    {
        private readonly IAboutRepository _repository;
        private readonly ICacheService _cache;
        
        // Chave de cache única para a página About
        private const string ABOUT_CACHE_KEY = "ABOUT_PAGE_CONTENT";

        public AboutService(IAboutRepository repository, ICacheService cache)
        {
            _repository = repository;
            _cache = cache;
        }

        // ==========================================
        // LEITURA (Full Page)
        // ==========================================
        public async Task<AboutPageContentDto> GetAboutPageContentAsync()
        {
            // Usa o GetOrCreateAsync do seu CacheService [cite: 9]
            return await _cache.GetOrCreateAsync(ABOUT_CACHE_KEY, async () =>
            {
                // Busca dados brutos do banco
                var sections = await _repository.GetAllSectionsAsync();
                var members = await _repository.GetAllTeamMembersAsync();

                // Monta o objeto final
                return new AboutPageContentDto
                {
                    // Mapeia Sections
                    Sections = sections.Select(s => new AboutSectionDto
                    {
                        Id = s.Id,
                        Title = s.Title,            // [cite: 15]
                        Description = s.Description,// [cite: 16]
                        ImageUrl = s.ImageUrl,      // [cite: 17]
                        ImageAlt = s.ImageAlt,      // [cite: 18]
                        ContentType = "section1"    // Definido no DTO
                    }).ToList(),

                    // Mapeia Time (Agrupa todos os membros em uma seção única de time)
                    TeamSection = new AboutTeamSectionDto
                    {
                        Title = "Nosso Time",
                        Description = "Conheça os especialistas por trás do projeto",
                        ContentType = "section2",
                        Members = members.Select(m => new TeamMemberDto
                        {
                            Id = m.Id,
                            Name = m.Name,          // [cite: 20]
                            Role = m.Role,          // [cite: 21]
                            PhotoUrl = m.PhotoUrl,  // [cite: 22]
                            LinkedinUrl = m.LinkedinUrl, // [cite: 23]
                            GithubUrl = m.GithubUrl      // [cite: 24]
                        }).ToList()
                    }
                };
            }) ?? new AboutPageContentDto();
        }

        // ==========================================
        // ESCRITA - SECTIONS
        // ==========================================
        public async Task<AboutSectionDto> CreateSectionAsync(AboutSectionDto dto)
        {
            var entity = new AboutSection
            {
                Title = dto.Title,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                ImageAlt = dto.ImageAlt,
                OrderIndex = 0 // Default, pode implementar lógica de ordem depois
            };

            await _repository.AddSectionAsync(entity);
            await _cache.RemoveAsync(ABOUT_CACHE_KEY); // Invalida o cache [cite: 9]

            dto.Id = entity.Id;
            return dto;
        }

        public async Task UpdateSectionAsync(int id, AboutSectionDto dto)
        {
            var entity = await _repository.GetSectionByIdAsync(id);
            if (entity == null)
                throw new ResourceNotFoundException($"Seção {id} não encontrada."); // 

            entity.Title = dto.Title;
            entity.Description = dto.Description;
            entity.ImageUrl = dto.ImageUrl;
            entity.ImageAlt = dto.ImageAlt;

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
        // ESCRITA - TEAM MEMBERS
        // ==========================================
        public async Task<TeamMemberDto> CreateTeamMemberAsync(TeamMemberDto dto)
        {
            var entity = new TeamMember
            {
                Name = dto.Name,
                Role = dto.Role,
                PhotoUrl = dto.PhotoUrl,
                LinkedinUrl = dto.LinkedinUrl,
                GithubUrl = dto.GithubUrl
            };

            await _repository.AddTeamMemberAsync(entity);
            await _cache.RemoveAsync(ABOUT_CACHE_KEY);

            dto.Id = entity.Id;
            return dto;
        }

        public async Task UpdateTeamMemberAsync(int id, TeamMemberDto dto)
        {
            var entity = await _repository.GetTeamMemberByIdAsync(id);
            if (entity == null)
                throw new ResourceNotFoundException($"Membro {id} não encontrado.");

            entity.Name = dto.Name;
            entity.Role = dto.Role;
            entity.PhotoUrl = dto.PhotoUrl;
            entity.LinkedinUrl = dto.LinkedinUrl;
            entity.GithubUrl = dto.GithubUrl;

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
