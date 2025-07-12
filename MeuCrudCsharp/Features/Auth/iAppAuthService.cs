using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Auth
{
    public interface IAppAuthService
    {
        Task SignInUser(Users user, HttpContext httpContext);
    }
}
