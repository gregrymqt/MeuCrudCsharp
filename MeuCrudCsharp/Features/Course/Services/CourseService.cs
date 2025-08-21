using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Course.DTOs;
using MeuCrudCsharp.Features.Courses.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Videos.DTOs;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        ///
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        /// <exception cref="AppServiceException"></exception>
        public async Task<PaginatedResultDto<CourseDto>> GetCoursesWithVideosPaginatedAsync(
            int pageNumber,
            int pageSize
        )
        {
            try
            {
                // A chave de cache agora deve incluir a página e o tamanho
                var cacheKey = $"CoursesWithVideos_Page{pageNumber}_Size{pageSize}";
                return await _cacheService.GetOrCreateAsync(
                    cacheKey,
                    async () =>
                    {
                        var totalCount = await _context.Courses.CountAsync(); // Pega o total de cursos

                        var courses = await _context
                            .Courses.AsNoTracking()
                            .Include(c => c.Videos)
                            .OrderBy(c => c.Name)
                            .Skip((pageNumber - 1) * pageSize) // Pula os itens das páginas anteriores
                            .Take(pageSize)
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

                        return new PaginatedResultDto<CourseDto>(
                            courses,
                            totalCount,
                            pageNumber,
                            pageSize
                        );
                    },
                    TimeSpan.FromMinutes(10)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar cursos paginados.");
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
                    // Correção: Garantir que valores não nulos sejam passados para o modelo.
                    Name = createDto.Name!, // O atributo [Required] no DTO garante que Name não será nulo.
                    Description = createDto.Description ?? string.Empty, // Se a descrição for nula, usa uma string vazia.
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
            var coursePrivate = await GetVideoByPublicIdAsync(id);
            try
            {
                var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == coursePrivate.Id);
                if (course == null)
                {
                    throw new ResourceNotFoundException(
                        $"Curso com ID {id} não encontrado para atualização."
                    );
                }

                // Correção: Garantir que valores não nulos sejam passados para o modelo.
                course.Name = updateDto.Name!; // O [Required] garante que não será nulo.
                course.Description = updateDto.Description ?? string.Empty; // Se a descrição for nula, usa uma string vazia.
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
            var coursePrivate = await GetVideoByPublicIdAsync(id);
            try
            {
                var course = await _context
                    .Courses.Include(c => c.Videos)
                    .FirstOrDefaultAsync(c => c.Id == coursePrivate.Id);

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

        private async Task<Models.Course> GetVideoByPublicIdAsync(Guid publicId)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(v => v.PublicId == publicId);

            if (course == null)
            {
                // Lançar uma exceção específica é uma ótima prática.
                throw new AppServiceException($"Vídeo com o PublicId {publicId} não foi encontrado.");
            }

            return course;
        }
    }


}

