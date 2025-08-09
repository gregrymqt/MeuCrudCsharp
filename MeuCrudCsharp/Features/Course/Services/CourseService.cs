using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Courses.DTOs;
using MeuCrudCsharp.Features.Courses.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Videos.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.Courses.Services
{
    /// <summary>
    /// Serviço para gerenciar as operações de CRUD para cursos.
    /// </summary>
    public class CourseService : ICourseService
    {
        private readonly ApiDbContext _context;
        private readonly ILogger<CourseService> _logger;
        private readonly ICacheService _cacheService;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="CourseService"/>.
        /// </summary>
        /// <param name="context">O contexto do banco de dados.</param>
        /// <param name="logger">O serviço de logging.</param>
        /// <param name="cacheService">O serviço de cache para otimização de performance.</param>
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

        /// <summary>
        /// Obtém um curso específico pelo seu ID, incluindo a lista de vídeos associados.
        /// </summary>
        /// <param name="id">O ID do curso a ser buscado.</param>
        /// <returns>O DTO do curso encontrado.</returns>
        /// <exception cref="ResourceNotFoundException">Lançada se o curso com o ID especificado não for encontrado.</exception>
        public async Task<CourseDto?> GetCourseByIdAsync(Guid id)
        {
            try
            {
                var courseDto = await _context
                    .Courses.AsNoTracking()
                    .Where(c => c.Id == id)
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
                                ThumbnailUrl = v.ThumbnailUrl,
                            })
                            .ToList(),
                    })
                    .FirstOrDefaultAsync();

                if (courseDto == null)
                {
                    throw new ResourceNotFoundException($"Curso com ID {id} não encontrado.");
                }

                return courseDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar o curso com ID {CourseId}.", id);
                throw new AppServiceException("Ocorreu um erro ao buscar o curso.", ex);
            }
        }

        /// <summary>
        /// Obtém todos os cursos com seus respectivos vídeos, utilizando cache para melhorar a performance.
        /// </summary>
        /// <returns>Uma lista de DTOs de cursos.</returns>
        /// <remarks>Os dados são cacheados por 10 minutos para reduzir acessos ao banco de dados.</remarks>
        public async Task<List<CourseDto>> GetAllCoursesWithVideosAsync()
        {
            try
            {
                const string cacheKey = "AllCoursesWithVideos";
                return await _cacheService.GetOrCreateAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation(
                            "Buscando todos os cursos do banco de dados (cache miss)."
                        );

                        var courses = await _context
                            .Courses.AsNoTracking()
                            .Include(c => c.Videos)
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
                                        ThumbnailUrl = v.ThumbnailUrl,
                                    })
                                    .ToList(),
                            })
                            .ToListAsync();

                        return courses;
                    },
                    TimeSpan.FromMinutes(10)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os cursos.");
                throw new AppServiceException("Ocorreu um erro ao buscar a lista de cursos.", ex);
            }
        }

        /// <summary>
        /// Cria um novo curso com base nos dados fornecidos.
        /// </summary>
        /// <param name="createDto">DTO com os dados para a criação do curso.</param>
        /// <returns>O DTO do curso recém-criado.</returns>
        /// <exception cref="AppServiceException">Lançada se já existir um curso com o mesmo nome ou se ocorrer um erro inesperado.</exception>
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

                await _cacheService.RemoveAsync("AllCoursesWithVideos");

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

        /// <summary>
        /// Atualiza um curso existente.
        /// </summary>
        /// <param name="id">O ID do curso a ser atualizado.</param>
        /// <param name="updateDto">DTO com os novos dados do curso.</param>
        /// <returns>O DTO do curso atualizado.</returns>
        /// <exception cref="ResourceNotFoundException">Lançada se o curso com o ID especificado não for encontrado.</exception>
        public async Task<CourseDto> UpdateCourseAsync(Guid id, UpdateCourseDto updateDto)
        {
            try
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

                await _cacheService.RemoveAsync("AllCoursesWithVideos");

                _logger.LogInformation("Curso {CourseId} atualizado.", id);
                return new CourseDto
                {
                    Id = course.Id,
                    Name = course.Name,
                    Description = course.Description,
                };
            }
            catch (ResourceNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar o curso com ID {CourseId}.", id);
                throw new AppServiceException("Ocorreu um erro ao atualizar o curso.", ex);
            }
        }

        /// <summary>
        /// Deleta um curso pelo seu ID.
        /// </summary>
        /// <param name="id">O ID do curso a ser deletado.</param>
        /// <exception cref="ResourceNotFoundException">Lançada se o curso não for encontrado.</exception>
        /// <exception cref="AppServiceException">Lançada se o curso possuir vídeos associados, impedindo a exclusão.</exception>
        /// <remarks>A exclusão só é permitida se o curso não tiver nenhum vídeo vinculado.</remarks>
        public async Task DeleteCourseAsync(Guid id)
        {
            try
            {
                var course = await _context
                    .Courses.Include(c => c.Videos)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (course == null)
                {
                    throw new ResourceNotFoundException(
                        $"Curso com ID {id} não encontrado para deleção."
                    );
                }

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

                await _cacheService.RemoveAsync("AllCoursesWithVideos");

                _logger.LogInformation("Curso {CourseId} deletado com sucesso.", id);
            }
            catch (ResourceNotFoundException)
            {
                throw;
            }
            catch (AppServiceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar o curso com ID {CourseId}.", id);
                throw new AppServiceException("Ocorreu um erro ao deletar o curso.", ex);
            }
        }
    }
}
