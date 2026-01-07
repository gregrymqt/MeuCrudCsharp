using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Caching;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Courses.DTOs;
using MeuCrudCsharp.Features.Courses.Interfaces;
using MeuCrudCsharp.Features.Courses.Mappers;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Videos.DTOs;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Courses.Services
{
    public class CourseService : ICourseService
    {
        // CORREÇÃO 1: Injetamos o Repository, não o DbContext
        private readonly ICourseRepository _repository;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CourseService> _logger;
        private const string CoursesCacheVersionKey = "courses_cache_version";

        public CourseService(
            ICourseRepository repository, // <--- Mudança Aqui
            ILogger<CourseService> logger,
            ICacheService cacheService
        )
        {
            _repository = repository;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<CourseDto>> SearchCoursesByNameAsync(string name)
        {
            // Usa o método otimizado do Repository
            var courses = await _repository.SearchByNameAsync(name);
            return courses.Select(c => CourseMapper.ToDto(c));
        }

        public async Task<PaginatedResultDto<CourseDto>> GetCoursesWithVideosPaginatedAsync(
            int pageNumber,
            int pageSize
        )
        {
            var cacheVersion = await GetCacheVersionAsync();
            var cacheKey = $"Courses_v{cacheVersion}_Page{pageNumber}_Size{pageSize}";

            return await _cacheService.GetOrCreateAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogInformation("Buscando cursos do banco (cache miss)...");

                        // CORREÇÃO 2: Usa a lógica de paginação do Repository
                        var (items, totalCount) = await _repository.GetPaginatedWithVideosAsync(
                            pageNumber,
                            pageSize
                        );

                        var dtos = items.Select(c => CourseMapper.ToDtoWithVideos(c)).ToList();

                        return new PaginatedResultDto<CourseDto>(
                            dtos,
                            totalCount,
                            pageNumber,
                            pageSize
                        );
                    },
                    TimeSpan.FromMinutes(10)
                ) ?? throw new AppServiceException("Erro ao obter cursos paginados.");
        }

        public async Task<CourseDto> CreateCourseAsync(CreateUpdateCourseDto createDto)
        {
            // Validação usando Repository
            if (await _repository.ExistsByNameAsync(createDto.Name!))
            {
                throw new AppServiceException("Já existe um curso com este nome.");
            }

            var newCourse = new Course
            {
                Name = createDto.Name!,
                Description = createDto.Description ?? string.Empty,
            };

            // Persistência via Repository
            await _repository.AddAsync(newCourse);
            await _repository.SaveChangesAsync(); // Commit da transação

            await _cacheService.InvalidateCacheByKeyAsync(CoursesCacheVersionKey);

            _logger.LogInformation("Novo curso '{CourseName}' criado.", newCourse.Name);
            return CourseMapper.ToDto(newCourse);
        }

        public async Task<CourseDto> UpdateCourseAsync(
            Guid publicId,
            CreateUpdateCourseDto updateDto
        )
        {
            // Busca usando método interno que já usa repository
            var course = await FindCourseByPublicIdOrFailAsync(publicId);

            course.Name = updateDto.Name!;
            course.Description = updateDto.Description ?? string.Empty;

            // O EF Core rastreia mudanças, mas chamamos Update para garantir e SaveChanges
            _repository.Update(course);
            await _repository.SaveChangesAsync();

            await _cacheService.InvalidateCacheByKeyAsync(CoursesCacheVersionKey);

            _logger.LogInformation("Curso {CourseId} atualizado.", publicId);
            return CourseMapper.ToDto(course);
        }

        public async Task DeleteCourseAsync(Guid publicId)
        {
            // CORREÇÃO 3: Usa o método específico do repo que já traz os vídeos (Include)
            var course = await _repository.GetByPublicIdWithVideosAsync(publicId);

            if (course == null)
            {
                throw new ResourceNotFoundException($"Curso com ID {publicId} não encontrado.");
            }

            if (course.Videos.Any())
            {
                throw new AppServiceException(
                    "Não é possível deletar um curso que possui vídeos associados."
                );
            }

            _repository.Delete(course);
            await _repository.SaveChangesAsync();

            await _cacheService.InvalidateCacheByKeyAsync(CoursesCacheVersionKey);

            _logger.LogInformation("Curso {CourseId} deletado.", publicId);
        }

        public async Task<Models.Course> FindCourseByPublicIdOrFailAsync(Guid publicId)
        {
            var course = await _repository.GetByPublicIdAsync(publicId);
            if (course == null)
            {
                throw new ResourceNotFoundException(
                    $"Curso com o PublicId {publicId} não encontrado."
                );
            }

            return course;
        }

        public async Task<Course> GetOrCreateCourseByNameAsync(string courseName)
        {
            if (string.IsNullOrWhiteSpace(courseName))
                throw new ArgumentException("Nome vazio.", nameof(courseName));

            var course = await _repository.GetByNameAsync(courseName);

            if (course == null)
            {
                _logger.LogInformation("Criando curso '{CourseName}'...", courseName);
                course = new Course { Name = courseName };

                // Adiciona mas NÃO salva ainda (Unit of Work implícito na chamada pai)
                // Se isso for chamado isoladamente, quem chamar deve garantir o Save.
                await _repository.AddAsync(course);
            }

            return course;
        }

        private Task<string?> GetCacheVersionAsync()
        {
            return _cacheService.GetOrCreateAsync(
                CoursesCacheVersionKey,
                () => Task.FromResult(Guid.NewGuid().ToString()),
                TimeSpan.FromDays(30)
            );
        }
    }
}
