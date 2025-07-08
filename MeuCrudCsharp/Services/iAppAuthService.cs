using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Services
{
    public interface IAppAuthService
    {
        Task SignInUser(Users user, HttpContext httpContext);
    }
}
