using Microsoft.EntityFrameworkCore;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Auth.Interfaces;

namespace MeuCrudCsharp.Features.Auth.Repositories
{
    public class UserRoleRepository : IUserRoleRepository
    {
        // Use o tipo concreto do seu contexto para acessar as tabelas do Identity
        private readonly ApiDbContext _context; // 

        public UserRoleRepository(ApiDbContext context) // 
        {
            _context = context;
        }

        public async Task<List<string>> GetRolesByUserIdAsync(string userId)
        {
            // Correção da lógica de busca
            // Fazemos um JOIN entre a tabela de ligação (UserRoles) e a tabela de Roles
            // para recuperar o Nome da Role (ex: "Admin") e não o ID.
            
            var query = from userRole in _context.UserRoles
                        join role in _context.Roles on userRole.RoleId equals role.Id
                        where userRole.UserId == userId
                        select role.Name; // Selecionamos o NOME (string)

            // Executa a query no banco de dados
            return await query.AsNoTracking().ToListAsync(); 
        }
    }
}