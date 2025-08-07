using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Courses.DTOs;
using MeuCrudCsharp.Features.Courses.Interfaces;
using MeuCrudCsharp.Features.Exceptions; // Nossas exceções customizadas
using MeuCrudCsharp.Features.Videos.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.Courses.Services
{
    public class CourseService : ICourseService
    {
        private readonly ApiDbContext _context;
        private readonly ILogger<CourseService> _logger;
        private readonly ICacheService _cacheService;

        public CourseService(
            ApiDbContext context,
            ILogger<CourseService> logger,
            ICacheService cacheService
        )
        {
            _context = context;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<CourseDto?> GetCourseByIdAsync(Guid id)
        {
            // MUDANÇA: Substituímos o .Include() por uma projeção direta com .Select()
            var courseDto = await _context
                .Courses.AsNoTracking()
                .Where(c => c.Id == id) // Filtra primeiro
                .Select(c => new CourseDto // Projeta o resultado para o DTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Videos = c
                        .Videos.Select(v => new VideoDto
                        {
                            Id = v.Id,
                            Title = v.Title,
                            Description = v.Description,
                            StorageIdentifier = v.StorageIdentifier,
                            UploadDate = v.UploadDate,
                            Duration = v.Duration,
                            Status = v.Status.ToString(),
                            CourseName = c.Name,
                            ThumbnailUrl = v.ThumbnailUrl, // Inclui a URL da miniatura
                        })
                        .ToList(),
                })
                .FirstOrDefaultAsync(); // Executa a consulta otimizada

            if (courseDto == null)
            {
                throw new ResourceNotFoundException($"Curso com ID {id} não encontrado.");
            }

            return courseDto;
        }

        public async Task<List<CourseDto>> GetAllCoursesWithVideosAsync()
        {
            const string cacheKey = "AllCoursesWithVideos";
            return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogInformation(
                        "Buscando todos os cursos do banco de dados (cache miss)."
                    );

                    // CORREÇÃO: Mapeamento dos vídeos preenchido
                    var courses = await _context
                        .Courses.AsNoTracking()
                        .Include(c => c.Videos) // Com .Select(), o .Include() é opcional/redundante, mas não prejudica.
                        .OrderBy(c => c.Name)
                        .Select(c => new CourseDto
                        {
                            Id = c.Id,
                            Name = c.Name,
                            Description = c.Description,
                            Videos = c
                                .Videos.Select(v => new VideoDto
                                {
                                    Id = v.Id,
                                    Title = v.Title,
                                    Description = v.Description,
                                    StorageIdentifier = v.StorageIdentifier,
                                    UploadDate = v.UploadDate,
                                    Duration = v.Duration,
                                    Status = v.Status.ToString(),
                                    CourseName = c.Name,
                                    ThumbnailUrl = v.ThumbnailUrl, // Inclui a URL da miniatura
                                })
                                .ToList(),
                        })
                        .ToListAsync();

                    return courses;
                },
                TimeSpan.FromMinutes(10)
            );
        }

        public async Task<CourseDto> CreateCourseAsync(CreateCourseDto createDto)
        {
            try
            {
                if (await _context.Courses.AnyAsync(c => c.Name == createDto.Name))
                {
                    throw new AppServiceException("Já existe um curso com este nome.");
                }

                var newCourse = new Models.Course
                {
                    Name = createDto.Name,
                    Description = createDto.Description,
                };

                _context.Courses.Add(newCourse);
                await _context.SaveChangesAsync();

                await _cacheService.RemoveAsync("AllCoursesWithVideos"); // Invalida o cache

                _logger.LogInformation(
                    "Novo curso '{CourseName}' criado com sucesso.",
                    newCourse.Name
                );
                return new CourseDto
                {
                    Id = newCourse.Id,
                    Name = newCourse.Name,
                    Description = newCourse.Description,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado ao criar o curso '{CourseName}'.",
                    createDto.Name
                );
                throw new AppServiceException("Ocorreu um erro ao criar o curso.", ex);
            }
        }

        public async Task<CourseDto> UpdateCourseAsync(Guid id, UpdateCourseDto updateDto)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);
            if (course == null)
            {
                throw new ResourceNotFoundException(
                    $"Curso com ID {id} não encontrado para atualização."
                );
            }

            course.Name = updateDto.Name;
            course.Description = updateDto.Description;
            await _context.SaveChangesAsync();

            await _cacheService.RemoveAsync("AllCoursesWithVideos"); // Invalida o cache da lista

            _logger.LogInformation("Curso {CourseId} atualizado.", id);
            return new CourseDto
            {
                Id = course.Id,
                Name = course.Name,
                Description = course.Description,
            };
        }

        public async Task DeleteCourseAsync(Guid id)
        {
            var course = await _context
                .Courses.Include(c => c.Videos) // Inclui os vídeos para a verificação
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                throw new ResourceNotFoundException(
                    $"Curso com ID {id} não encontrado para deleção."
                );
            }

            // REGRA DE NEGÓCIO: Não permitir deletar curso com vídeos
            if (course.Videos.Any())
            {
                _logger.LogWarning(
                    "Tentativa de deletar o curso {CourseId} que possui {VideoCount} vídeos.",
                    id,
                    course.Videos.Count
                );
                throw new AppServiceException(
                    "Não é possível deletar um curso que possui vídeos associados. Por favor, remova os vídeos primeiro."
                );
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            await _cacheService.RemoveAsync("AllCoursesWithVideos"); // Invalida o cache

            _logger.LogInformation("Curso {CourseId} deletado com sucesso.", id);
        }
    }
}
