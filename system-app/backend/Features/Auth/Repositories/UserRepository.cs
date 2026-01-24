namespace MeuCrudCsharp.Features.Auth.Repositories;

using System.Threading.Tasks;
using Data;
using MeuCrudCsharp.Features.Auth.Interfaces;
using Models;
using Microsoft.EntityFrameworkCore;

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

    // SaveChangesAsync removido - UnitOfWork é responsável por persistir
}
