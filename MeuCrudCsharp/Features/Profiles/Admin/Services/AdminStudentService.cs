using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Profiles.Admin.Dtos;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Profiles.Admin.Services
{
    /// <summary>
    /// Implements <see cref="IAdminStudentService"/> to provide administrative functionalities for student profiles.
    /// </summary>
    public class AdminStudentService : IAdminStudentService
    {
        private readonly ApiDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly ILogger<AdminStudentService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminStudentService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="cacheService">The caching service for performance optimization.</param>
        /// <param name="logger">The logger for recording events and errors.</param>
        public AdminStudentService(
            ApiDbContext context,
            ICacheService cacheService,
            ILogger<AdminStudentService> logger
        )
        {
            _context = context;
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <inheritdoc />
        /// <remarks>
        /// This method caches the list of students for 5 minutes to improve performance
        /// on repeated requests.
        /// </remarks>
        public async Task<List<StudentDto>> GetAllStudentsAsync()
        {
            const string cacheKey = "Admin_AllStudentsWithSubscription";

            return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    try
                    {
                        _logger.LogInformation(
                            "Buscando a lista de alunos do banco de dados (cache miss)."
                        );
                        return await _context
                            .Users.AsNoTracking()
                            .Include(u => u.Subscription)
                            .ThenInclude(s => s.Plan)
                            .OrderBy(u => u.Name)
                            .Select(u => new StudentDto
                            (
                                u.PublicId.ToString(),
                                u.Name,
                                u.Email,
                                u.Subscription != null
                                    ? u.Subscription.Status
                                    : "Sem Assinatura",
                                u.Subscription != null ? u.Subscription.Plan.Name : "N/A",
                                u.CreatedAt
                            ))
                            .ToListAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Falha ao buscar o aluno no banco de dados."
                        );
                        throw new AppServiceException(
                            "An error occurred while querying student data.",
                            ex
                        );
                    }
                },
                absoluteExpireTime: TimeSpan.FromMinutes(5)
            );
        }

        public async Task<StudentDto> GetStudentByIdAsync(Guid id)
        {
                    try
                    {
                        _logger.LogInformation(
                            "Buscando o alunos pelo id no banco de dados (cache miss)."
                        );

                        var user = await _context.Users.AsNoTracking()
                            .Include(users => users.Subscription)
                            .ThenInclude(s => s.Plan)
                            .SingleOrDefaultAsync(u => u.PublicId == id);

                        if (user == null)
                        {
                            _logger.LogWarning($"Tentativa de buscar aluno com ID {id} não encontrado.");
                            throw new KeyNotFoundException($"Aluno com ID {id} não encontrado.");
                        }

                        var studentDto = new StudentDto(
                            user.PublicId.ToString(),
                            user.Name,
                            user.Email,
                            user.Subscription?.Status ??
                            "Sem Assinatura", 
                            user.Subscription?.Plan?.Name ?? "N/A", 
                            user.CreatedAt);

                        return studentDto;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Falha ao buscar o aluno no banco de dados."
                        );
                        throw new AppServiceException(
                            "An error occurred while querying student data.",
                            ex
                        );
                    }
        }
    }
}