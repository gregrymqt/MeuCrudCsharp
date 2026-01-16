using MercadoPago.Resource.User;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Repositories;

public class UserAccountRepository : IUserAccountRepository
{
    private readonly ApiDbContext _context;

    public UserAccountRepository(ApiDbContext context)
    {
        _context = context;
    }

    public async Task<Users?> GetUserByIdAsync(string userId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
