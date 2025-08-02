using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Profiles.Admin.Dtos;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Profiles.Admin.Services
{
    public class AdminStudentService : IAdminStudentService
    {
        private readonly ApiDbContext _context;
        private readonly ICacheService _cacheService; // 1. Injetamos o serviço de cache

        // 2. Atualizamos o construtor
        public AdminStudentService(ApiDbContext context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        public async Task<List<StudentDto>> GetAllStudentsAsync()
        {
            // 3. Usamos nosso serviço de cache universal
            const string? cacheKey = "Admin_AllStudentsWithSubscription";

            return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    // Esta lógica só executa se os dados não estiverem no cache
                    return await _context
                        .Users.AsNoTracking()
                        // 4. MUDANÇA NA CONSULTA: Incluindo a Assinatura e o Plano associado
                        .Include(u => u.Subscription)
                        .ThenInclude(s => s.Plan)
                        .OrderBy(u => u.Name)
                        .Select(u => new StudentDto
                        {
                            Id = Guid.Parse(u.Id),
                            Name = u.Name,
                            Email = u.Email,

                            // 5. MUDANÇA NO MAPEAMENTO: Usando os dados da nova estrutura
                            SubscriptionStatus =
                                u.Subscription != null ? u.Subscription.Status : "Sem Assinatura",
                            PlanName = u.Subscription != null ? u.Subscription.Plan.Name : "N/A",

                            // 6. MELHORIA: Usando uma data de criação real do usuário
                            RegistrationDate = u.CreatedAt,
                        })
                        .ToListAsync();
                },
                absoluteExpireTime: TimeSpan.FromMinutes(5)
            ); // Cache de 5 minutos
        }

        // ... Outros métodos do serviço ...
        // Lembre-se de invalidar o cache em métodos que alteram a lista de alunos!
        public async Task InvalidateStudentsCacheAsync()
        {
            await _cacheService.RemoveAsync("Admin_AllStudentsWithSubscription");
        }
    }
}
