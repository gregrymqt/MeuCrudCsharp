// Infrastructure/Services/UserContext.cs
using System.Security.Claims;
using MeuCrudCsharp.Features.User.Interfaces;

namespace MeuCrudCsharp.Features.User.Services;

public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new ArgumentException("No user id claim found");
    }

    public async Task<string> GetCurrentEmail()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
            ?? throw new ArgumentException("No email claim found");
    }
}
