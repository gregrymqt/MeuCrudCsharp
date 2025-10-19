using MercadoPago.Resource.User;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Services;

public class UserAccountRepository : IUserAccountRepository
{
    private readonly ApiDbContext _context;

    public UserAccountRepository(ApiDbContext context)
    {
        _context = context;
    }

    public async Task<Users?> GetUserByIdAsync(string userId) =>
        await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

    public async Task<Subscription?> GetActiveSubscriptionByUserIdAsync(string userId) =>
        await _context
            .Subscriptions.Include(s => s.Plan) // Incluir o plano é importante para os detalhes
            .FirstOrDefaultAsync(s =>
                s.UserId == userId && (s.Status == "active" || s.Status == "paused")
            );

    public async Task<IEnumerable<Payments>> GetPaymentHistoryByUserIdAsync(
        string userId,
        int pageNumber,
        int pageSize
    ) =>
        await _context
            .Payments.AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<Payments?> GetPaymentByIdAndUserIdAsync(string userId, string paymentId) =>
        await _context
            .Payments.AsNoTracking()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId);

    public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;
}
