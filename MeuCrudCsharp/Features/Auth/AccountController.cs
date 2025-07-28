// Em Controllers/AccountController.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;

[Route("[controller]/[action]")] // Rota mais limpa
public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login(string returnUrl = "/Profile/Index")
    {
        // Apenas redireciona para o Google. O resto é automático.
        var properties = new AuthenticationProperties { RedirectUri = returnUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}