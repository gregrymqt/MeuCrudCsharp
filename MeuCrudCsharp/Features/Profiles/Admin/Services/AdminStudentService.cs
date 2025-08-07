using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Profiles.Admin.Dtos;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // MUDANÇA 1: Adicionando a dependência do Logger

namespace MeuCrudCsharp.Features.Profiles.Admin.Services
{
    public class AdminStudentService : IAdminStudentService
    {
        private readonly ApiDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly ILogger<AdminStudentService> _logger; // MUDANÇA 1

        public AdminStudentService(
            ApiDbContext context,
            ICacheService cacheService,
            ILogger<AdminStudentService> logger
        ) // MUDANÇA 1
        {
            _context = context;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<List<StudentDto>> GetAllStudentsAsync()
        {
            const string cacheKey = "Admin_AllStudentsWithSubscription";

            return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    // MUDANÇA 2: Tratamento de exceção dentro da 'factory' do cache
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
                        // Lança nossa exceção customizada para ser tratada pelo Controller
                        throw new AppServiceException(
                            "Ocorreu um erro ao consultar os dados dos alunos.",
                            ex
                        );
                    }
                },
                absoluteExpireTime: TimeSpan.FromMinutes(5)
            );
        }

        public async Task InvalidateStudentsCacheAsync()
        {
            // MUDANÇA 3: Tratamento de exceção no método de invalidação
            try
            {
                await _cacheService.RemoveAsync("Admin_AllStudentsWithSubscription");
                _logger.LogInformation(
                    "Cache de alunos ('Admin_AllStudentsWithSubscription') invalidado com sucesso."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao invalidar o cache de alunos. A aplicação continuará, mas o cache pode ficar dessincronizado."
                );
                // Lança a exceção para que o chamador saiba que a invalidação falhou
                throw new AppServiceException("Ocorreu um erro ao limpar o cache de alunos.", ex);
            }
        }
    }
}
