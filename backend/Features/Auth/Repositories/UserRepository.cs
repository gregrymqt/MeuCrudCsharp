namespace MeuCrudCsharp.Features.Auth.Repositories;

using System.Threading.Tasks;
using Data; // Substitua pelo seu namespace do DbContext
using MeuCrudCsharp.Features.Auth.Interfaces;
using Microsoft.EntityFrameworkCore;
using Models; // Substitua pelo seu namespace de Models

public class UserRepository : IUserRepository
{
    private readonly ApiDbContext _dbContext;

    public UserRepository(ApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Users?> FindByGoogleIdAsync(string googleId) =>
        await _dbContext.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);

    public async Task<Users?> GetByIdAsync(string id) =>
        await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<int> SaveChangesAsync() => await _dbContext.SaveChangesAsync();
}
