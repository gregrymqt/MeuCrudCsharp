using System.Security.Claims;
using MeuCrudCsharp.Features.Auth.Dtos;
using MeuCrudCsharp.Models; // Substitua pelo seu namespace

namespace MeuCrudCsharp.Features.Auth.Interfaces;

public interface IAppAuthService
{
    Task<Users> SignInWithGoogleAsync(ClaimsPrincipal googleUserPrincipal, HttpContext httpContext);

    Task<UserSessionDto> GetAuthenticatedUserDataAsync(string userId);
}
