using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Courses.DTOs;
using MeuCrudCsharp.Features.Courses.Interfaces;
using MeuCrudCsharp.Features.Courses.Mappers;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Shared.Work;
using MeuCrudCsharp.Features.Videos.DTOs;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Courses.Services
{
    public class CourseService(
        ICourseRepository repository,
        ILogger<CourseService> logger,
        ICacheService cacheService,
        IUnitOfWork unitOfWork)
        : ICourseService
    {
        private const string CoursesCacheVersionKey = "courses_cache_version";

        public async Task<IEnumerable<CourseDto>> SearchCoursesByNameAsync(string name)
        {
            // Usa o método otimizado do Repository
            var courses = await repository.SearchByNameAsync(name);
            return courses.Select(CourseMapper.ToDto);
        }

        public async Task<PaginatedResultDto<CourseDto>> GetCoursesWithVideosPaginatedAsync(
            int pageNumber,
            int pageSize
        )
        {
            var cacheVersion = await GetCacheVersionAsync();
            var cacheKey = $"Courses_v{cacheVersion}_Page{pageNumber}_Size{pageSize}";

            return await cacheService.GetOrCreateAsync(
                    cacheKey,
                    async () =>
                    {
                        logger.LogInformation("Buscando cursos do banco (cache miss)...");

                        // CORREÇÃO 2: Usa a lógica de paginação do Repository
                        var (items, totalCount) = await repository.GetPaginatedWithVideosAsync(
                            pageNumber,
                            pageSize
                        );

                        var dtos = items.Select(CourseMapper.ToDtoWithVideos).ToList();

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
            if (await repository.ExistsByNameAsync(createDto.Name!))
            {
                throw new AppServiceException("Já existe um curso com este nome.");
            }

            var newCourse = new Course
            {
                Name = createDto.Name!,
                Description = createDto.Description ?? string.Empty,
            };

            // Persistência via Repository
            await repository.AddAsync(newCourse);
            await unitOfWork.CommitAsync(); // Persiste no banco

            await cacheService.InvalidateCacheByKeyAsync(CoursesCacheVersionKey);

            logger.LogInformation("Novo curso '{CourseName}' criado.", newCourse.Name);
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

            // O EF Core rastreia mudanças
            repository.Update(course);
            await unitOfWork.CommitAsync(); // Persiste no banco

            await cacheService.InvalidateCacheByKeyAsync(CoursesCacheVersionKey);

            logger.LogInformation("Curso {CourseId} atualizado.", publicId);
            return CourseMapper.ToDto(course);
        }

        public async Task DeleteCourseAsync(Guid publicId)
        {
            // CORREÇÃO 3: Usa o método específico do repo que já traz os vídeos (Include)
            var course = await repository.GetByPublicIdWithVideosAsync(publicId);

            if (course == null)
            {
                throw new ResourceNotFoundException($"Curso com ID {publicId} não encontrado.");
            }

            if (course.Videos.Count != 0)
            {
                throw new AppServiceException(
                    "Não é possível deletar um curso que possui vídeos associados."
                );
            }

            repository.Delete(course);
            await unitOfWork.CommitAsync(); // Persiste no banco

            await cacheService.InvalidateCacheByKeyAsync(CoursesCacheVersionKey);

            logger.LogInformation("Curso {CourseId} deletado.", publicId);
        }

        public async Task<Models.Course> FindCourseByPublicIdOrFailAsync(Guid publicId)
        {
            var course = await repository.GetByPublicIdAsync(publicId);
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

            var course = await repository.GetByNameAsync(courseName);

            if (course != null) return course;
            logger.LogInformation("Criando curso '{CourseName}'...", courseName);
            course = new Course { Name = courseName };

            // Adiciona mas NÃO salva ainda (Unit of Work implícito na chamada pai)
            // Se isso for chamado isoladamente, quem chamar deve garantir o Save.
            await repository.AddAsync(course);

            return course;
        }

        private Task<string?> GetCacheVersionAsync()
        {
            return cacheService.GetOrCreateAsync(
                CoursesCacheVersionKey,
                () => Task.FromResult(Guid.NewGuid().ToString()),
                TimeSpan.FromDays(30)
            );
        }
    }
}
