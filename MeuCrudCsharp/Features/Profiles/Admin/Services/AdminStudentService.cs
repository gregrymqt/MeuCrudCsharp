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
        private readonly UserManager<Users> _userManager;

        public AdminStudentService(ApiDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<List<StudentDto>> GetAllStudentsAsync()
        {
            // Pega todos os usuários que têm a role "User" (ou seja, não são admins)
            var studentUsers = await _userManager.GetUsersInRoleAsync("User");

            if (studentUsers == null || !studentUsers.Any())
            {
                return new List<StudentDto>();
            }

            // Pega os IDs dos usuários para a consulta
            var studentIds = studentUsers.Select(s => s.Id).ToList();

            // Busca os dados completos, incluindo o status do pagamento
            var students = await _context
                .Users.Where(u => studentIds.Contains(u.Id))
                .Include(u => u.Payments) // Inclui os dados de pagamento
                .OrderByDescending(u => u.Email) // Ordena por um campo do IdentityUser
                .Select(u => new StudentDto
                {
                    Id = Guid.Parse(u.Id),
                    Name = u.Name, // Sua propriedade customizada
                    Email = u.Email,
                    // Se não houver pagamento, o status é "N/A"
                    SubscriptionStatus = u.Payments != null ? u.Payments.Status : "N/A",
                    RegistrationDate =
                        u.LockoutEnd == null ? DateTime.UtcNow : u.LockoutEnd.Value.DateTime, // Exemplo, use a data de criação real se tiver
                })
                .ToListAsync();

            return students;
        }
    }
}
