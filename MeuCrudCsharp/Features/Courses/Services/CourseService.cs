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
    /// <summary>
    /// Serviço para gerenciar as operações de CRUD para cursos.
    /// </summary>
    public class CourseService : ICourseService
    {
        private readonly ApiDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CourseService> _logger;
        private const string CoursesCacheVersionKey = "courses_cache_version";

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
        /// <param name="name">O name do curso a ser buscado.</param>
        /// <returns>O DTO do curso encontrado.</returns>
        /// <exception cref="ResourceNotFoundException">Lançada se o curso com o ID especificado não for encontrado.</exception>
        // Na sua classe de serviço (ex: CourseService.cs)
        // MÉTODO SearchCoursesByNameAsync COM MELHORIA DE PERFORMANCE
        public async Task<IEnumerable<CourseDto>> SearchCoursesByNameAsync(string name)
        {
            // ✅ Sugestão: Use a sobrecarga de Contains que ignora o case.
            // O EF Core pode traduzir isso para uma forma mais otimizada (como ILIKE no PostgreSQL).
            var courses = await _context
                .Courses.AsNoTracking()
                .Where(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                .Select(c => CourseMapper.ToDto(c))
                .ToListAsync();

            return courses;
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
            var cacheVersion = await GetCacheVersionAsync();
            var cacheKey = $"Courses_v{cacheVersion}_Page{pageNumber}_Size{pageSize}";

            return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogInformation(
                        "Buscando cursos do banco (cache miss) para a chave: {CacheKey}",
                        cacheKey
                    );

                    var totalCount = await _context.Courses.CountAsync();
                    var courses = await _context
                        .Courses.AsNoTracking()
                        .Include(c => c.Videos)
                        .OrderBy(c => c.Name)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .Select(c => CourseMapper.ToDtoWithVideos(c)) // Usa o Mapper
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

        /// <summary>
        /// Cria um novo curso com base nos dados fornecidos.
        /// </summary>
        /// <param name="createDto">DTO com os dados para a criação do curso.</param>
        /// <returns>O DTO do curso recém-criado.</returns>
        /// <exception cref="AppServiceException">Lançada se já existir um curso com o mesmo nome ou se ocorrer um erro inesperado.</exception>
        public async Task<CourseDto> CreateCourseAsync(CreateCourseDto createDto)
        {
            if (await _context.Courses.AnyAsync(c => c.Name == createDto.Name))
            {
                throw new AppServiceException("Já existe um curso com este nome.");
            }

            var newCourse = new Models.Course
            {
                Name = createDto.Name,
                Description = createDto.Description ?? string.Empty,
            };

            _context.Courses.Add(newCourse);
            await _context.SaveChangesAsync();
            await _cacheService.InvalidateCacheByKeyAsync(CoursesCacheVersionKey);

            _logger.LogInformation("Novo curso '{CourseName}' criado com sucesso.", newCourse.Name);
            return CourseMapper.ToDto(newCourse); // Usa o Mapper
        }

        /// <summary>
        /// Atualiza um curso existente.
        /// </summary>
        /// <param name="publicId">O ID do curso a ser atualizado.</param>
        /// <param name="updateDto">DTO com os novos dados do curso.</param>
        /// <returns>O DTO do curso atualizado.</returns>
        /// <exception cref="ResourceNotFoundException">Lançada se o curso com o ID especificado não for encontrado.</exception>
        public async Task<CourseDto> UpdateCourseAsync(Guid publicId, UpdateCourseDto updateDto)
        {
            var course = await FindCourseByPublicIdOrFailAsync(publicId);

            course.Name = updateDto.Name;
            course.Description = updateDto.Description ?? string.Empty;

            await _context.SaveChangesAsync();
            await _cacheService.InvalidateCacheByKeyAsync(CoursesCacheVersionKey);

            _logger.LogInformation("Curso {CourseId} atualizado.", publicId);
            return CourseMapper.ToDto(course); // Usa o Mapper
        }

        /// <summary>
        /// Deleta um curso pelo seu ID.
        /// </summary>
        /// <param name="publicId">O ID do curso a ser deletado.</param>
        /// <exception cref="ResourceNotFoundException">Lançada se o curso não for encontrado.</exception>
        /// <exception cref="AppServiceException">Lançada se o curso possuir vídeos associados, impedindo a exclusão.</exception>
        /// <remarks>A exclusão só é permitida se o curso não tiver nenhum vídeo vinculado.</remarks>
        // MÉTODO DeleteCourseAsync CORRIGIDO
        public async Task DeleteCourseAsync(Guid publicId)
        {
            // ✅ CORREÇÃO: Busque o curso E inclua os vídeos para a validação.
            var course = await _context.Courses
                .Include(c => c.Videos) // Carrega a lista de vídeos associados
                .FirstOrDefaultAsync(c => c.PublicId == publicId);

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

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            await _cacheService.InvalidateCacheByKeyAsync(CoursesCacheVersionKey);

            _logger.LogInformation("Curso {CourseId} deletado com sucesso.", publicId);
        }

        public async Task<Models.Course> FindCourseByPublicIdOrFailAsync(Guid publicId)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.PublicId == publicId);
            if (course == null)
            {
                throw new ResourceNotFoundException(
                    $"Curso com o PublicId {publicId} não foi encontrado."
                );
            }

            return course;
        }
        
        public async Task<Course> GetOrCreateCourseByNameAsync(string courseName)
        {
            if (string.IsNullOrWhiteSpace(courseName))
            {
                throw new ArgumentException("O nome do curso não pode ser vazio.", nameof(courseName));
            }

            // Busca o curso pelo nome, ignorando case
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Name.Equals(courseName, StringComparison.OrdinalIgnoreCase));

            if (course == null)
            {
                _logger.LogInformation("Curso '{CourseName}' não encontrado. Criando um novo.", courseName);
                course = new Course { Name = courseName };
                _context.Courses.Add(course);
                // IMPORTANTE: O SaveChangesAsync será chamado pelo método que chamou este,
                // garantindo que tudo seja salvo em uma única transação.
            }

            return course;
        }


        private Task<string> GetCacheVersionAsync()
        {
            return _cacheService.GetOrCreateAsync(
                CoursesCacheVersionKey,
                () => Task.FromResult(Guid.NewGuid().ToString()),
                TimeSpan.FromDays(30)
            );
        }
    }
}