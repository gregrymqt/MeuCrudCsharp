using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Caching;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Profiles.Admin.Dtos;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
                            {
                                Id = Guid.Parse(u.Id),
                                Name = u.Name,
                                Email = u.Email,
                                SubscriptionStatus =
                                    u.Subscription != null
                                        ? u.Subscription.Status
                                        : "Sem Assinatura",
                                PlanName =
                                    u.Subscription != null ? u.Subscription.Plan.Name : "N/A",
                                RegistrationDate = u.CreatedAt,
                            })
                            .ToListAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Falha ao buscar a lista de alunos do banco de dados."
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

        /// <summary>
        /// Invalidates and removes the cached list of all students.
        /// This should be called after any operation that creates, updates, or deletes a user or their subscription.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="AppServiceException">Thrown if an error occurs while clearing the cache.</exception>
        public async Task InvalidateStudentsCacheAsync()
        {
            try
            {
                await _cacheService.RemoveAsync("Admin_AllStudentsWithSubscription");
                _logger.LogInformation(
                    "Student cache ('Admin_AllStudentsWithSubscription') was successfully invalidated."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to invalidate student cache. The application will continue, but the cache may be out of sync."
                );
                throw new AppServiceException(
                    "An error occurred while clearing the student cache.",
                    ex
                );
            }
        }
    }
}
