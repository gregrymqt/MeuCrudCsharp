using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Repositories;

public class UserAccountRepository(ApiDbContext context) : IUserAccountRepository
{
    public async Task<Users?> GetUserByIdAsync(string userId)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    // SaveChangesAsync removido - UnitOfWork é responsável por persistir
}
