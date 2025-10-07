using MeuCrudCsharp.Features.Auth.Interfaces;

namespace MeuCrudCsharp.Features.Auth.Services;

using Data; // Substitua pelo seu namespace do DbContext
using Models; // Substitua pelo seu namespace de Models
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

public class UserRepository : IUserRepository
{
    private readonly ApiDbContext _dbContext;

    public UserRepository(ApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Users?> FindByGoogleIdAsync(string googleId) =>
        await _dbContext.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);


    public async Task<Users?> GetByIdAsync(string id) => await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
    async public Task<int> SaveChangesAsync() => await _dbContext.SaveChangesAsync();
}