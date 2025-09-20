// Infrastructure/Services/UserContext.cs
using System.Security.Claims;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Exceptions;

namespace MeuCrudCsharp.Features.Auth.Services;

public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCurrentUserId()
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (string.IsNullOrEmpty(userIdString))
        {
            // É uma boa prática lançar uma exceção específica e clara.
            throw new AppServiceException(
                "A identificação do usuário não pôde ser encontrada na sessão atual."
            );
        }

        return userIdString;
    }
}