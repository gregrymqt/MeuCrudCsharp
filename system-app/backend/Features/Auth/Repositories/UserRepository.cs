namespace MeuCrudCsharp.Features.Auth.Repositories;

using Data;
using Interfaces;
using Models;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository para gerenciar operações de persistência de Users.
/// Apenas marca as mudanças no DbContext - NÃO persiste diretamente.
/// O Service é responsável por chamar UnitOfWork.CommitAsync().
/// </summary>
public class UserRepository(ApiDbContext dbContext) : IUserRepository
{
    public async Task<Users?> FindByGoogleIdAsync(string googleId) =>
        await dbContext.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);

    public async Task<Users?> GetByIdAsync(string id) =>
        await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);

    /// <summary>
    /// Marca um usuário existente para atualização.
    /// O Service chamará UnitOfWork.CommitAsync().
    /// </summary>
    public void Update(Users user)
    {
        dbContext.Users.Update(user);
        // O SaveChanges será chamado pelo Service via UnitOfWork
    }
}
