using System;

namespace MeuCrudCsharp.Features.Auth.Interfaces;

public interface IUserRoleRepository
{
    // Retorna apenas a lista de nomes (ex: ["Admin", "User"])
    Task<List<string>> GetRolesByUserIdAsync(string userId);
}
