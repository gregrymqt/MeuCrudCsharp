using System.Security.Claims;
using EllipticCurve;
using MercadoPago.Resource.User;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeuCrudCsharp.Pages.Payment.Type;

[Authorize]
public class Pix : PageModel
{
    public Users? user { get; set; }

    private readonly ILogger<Pix> _logger;

    public Pix(ILogger<Pix> logger)
    {
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        try
        {
            if (User.Identity is not ClaimsIdentity identity)
            {
                // O usuário não está logado ou a identidade não é a esperada
                return RedirectToPage("/Account/Login");
            }
            else
            {
                user = new Users
                {
                    UserName = identity.Name,
                    Email = identity.FindFirst(ClaimTypes.Email)?.Value, // <-- Pega o EMAIL
                    Id = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value // <-- Pega o ID
                };
            }

            return Page();
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Erro ao configurar a página de pagamento para o pix"
            );
            Console.WriteLine(e);
            throw;
        }
    }
}