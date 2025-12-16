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

    public async Task<Users?> GetUserWithDetailsAsync(string userId)
{
    return await _dbContext.Users
        .AsNoTracking() // Mais rápido para apenas leitura
        .Include(u => u.Subscription)      // JOIN com Subscription [cite: 45]
            .ThenInclude(s => s.Plan)      // JOIN com Plan dentro de Subscription [cite: 61]
        .Include(u => u.Payments.OrderByDescending(p => p.CreatedAt).Take(10)) // JOIN com Payments (Top 10 mais recentes) [cite: 46]
        .FirstOrDefaultAsync(u => u.Id == userId);
}
}
