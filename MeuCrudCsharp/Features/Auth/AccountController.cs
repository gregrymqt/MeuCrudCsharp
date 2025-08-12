using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("[controller]/[action]")]
[AllowAnonymous]
public class AccountController : Controller
{
    /// <summary>
    /// Inicia o fluxo de autenticação via Google e redireciona o usuário para o provedor.
    /// </summary>
    /// <param name="returnUrl">URL para a qual o usuário será redirecionado após autenticação.</param>
    /// <returns>Desafio de autenticação para o provedor do Google.</returns>
    [HttpGet]
    public IActionResult Login(string returnUrl = "/Profile/Index")
    {
        var properties = new AuthenticationProperties { RedirectUri = returnUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Realiza o logout do usuário atual e redireciona para a página inicial.
    /// </summary>
    /// <returns>Redirecionamento para a ação Index do controlador Home.</returns>
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        Response.Cookies.Delete("jwt");
        return RedirectToAction("Index", "Home");
    }
}
