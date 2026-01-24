// Infrastructure/Services/UserContext.cs

using System.Security.Claims;
using MeuCrudCsharp.Features.Auth.Interfaces;

namespace MeuCrudCsharp.Features.Auth.Utils;

public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Task<string> GetCurrentUserId()
    {
        return Task.FromResult(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                               ?? throw new ArgumentException("No user id claim found"));
    }

    public Task<string> GetCurrentEmail()
    {
        return Task.FromResult(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
                               ?? throw new ArgumentException("No email claim found"));
    }
}
